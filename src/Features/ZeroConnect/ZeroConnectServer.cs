// src/Features/ZeroConnect/ZeroConnectServer.cs
using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace ZeroMix.Features.ZeroConnect;

public class ZeroConnectServer : IDisposable
{
    private readonly HttpListener _httpListener;
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private readonly ClipboardBridgeService _clipboardService;
    private readonly FileTransferService _fileService;
    private readonly FileHistoryService? _history;
    private readonly ChunkAssembler _assembler = new();
    private readonly PairingService _pairingService;
    private CancellationTokenSource _cts = new();

    // Device tracking
    public class DeviceInfo
    {
        public string Id { get; set; } = "";
        public string Ip { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public string Name => string.IsNullOrEmpty(UserAgent) ? Id : UserAgent.Split('/').FirstOrDefault() ?? Id;
        public DateTime LastActivity { get; set; } = DateTime.MinValue;
    }

    private readonly ConcurrentDictionary<string, DeviceInfo> _clientInfos = new();

    // Rate limiting / auth tracking
    private readonly ConcurrentDictionary<string, (int Count, DateTime FirstFailedAt)> _failedAttempts = new();
    private readonly ConcurrentDictionary<string, bool> _authenticatedClients = new();
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan FailedWindow = TimeSpan.FromMinutes(5);
    private const int MaxDevices = 3;

    public event EventHandler<DeviceInfo[]?>? OnDevicesChanged;

    public event EventHandler<string>? OnLog;
    public int Port { get; } = 9876;
    public string PairingToken { get; private set; }

    public ZeroConnectServer(
        ClipboardBridgeService clipboardService,
        FileTransferService fileService,
        PairingService? pairingService = null)
    {
        _clipboardService = clipboardService;
        _fileService = fileService;
        _pairingService = pairingService ?? new PairingService();
        PairingToken = _pairingService.GenerateToken();

        _httpListener = new HttpListener();
        // Bind to localhost only untuk security (prevent remote access)
        _httpListener.Prefixes.Add($"http://127.0.0.1:{Port}/");
    }

        // Overload constructor accepting FileHistoryService
        public ZeroConnectServer(
            ClipboardBridgeService clipboardService,
            FileTransferService fileService,
            PairingService? pairingService,
            FileHistoryService? history)
        {
            _clipboardService = clipboardService;
            _fileService = fileService;
            _history = history;
            _pairingService = pairingService ?? new PairingService();
            PairingToken = _pairingService.GenerateToken();

            _httpListener = new HttpListener();
            // Bind to localhost only untuk security (prevent remote access)
            _httpListener.Prefixes.Add($"http://127.0.0.1:{Port}/");
        }

        public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        _httpListener.Start();
        Log($"ZeroConnect listening on port {Port} (localhost only)");

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                _ = HandleContextAsync(context); // fire and forget
            }
            catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
            {
                Log($"[Error] {ex.Message}");
            }
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context)
    {
        // Serve PWA files without server-side URL token validation (PWA will send token over WS)
        if (!context.Request.IsWebSocketRequest)
        {
            await ServePwaAsync(context);
            return;
        }

        var clientIp = context.Request.RemoteEndPoint?.Address?.ToString() ?? "unknown";
        var userAgent = context.Request.UserAgent ?? "";

        if (IsBlocked(clientIp))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            Log($"[BLOCKED] Connection attempt from blocked IP {clientIp}");
            return;
        }

        // WebSocket upgrade
        var wsContext = await context.AcceptWebSocketAsync(null);
        var clientId = Guid.NewGuid().ToString("N")[..8];
        var ws = wsContext.WebSocket;

        try
        {
            // Perform receive/auth loop which will add to _clients when authenticated
            await ReceiveLoopAsync(clientId, ws, clientIp, userAgent);
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            _authenticatedClients.TryRemove(clientId, out _);
            _clientInfos.TryRemove(clientId, out _);
            RaiseDevicesChanged();
            Log($"[-] Device disconnected: {clientId}");
        }
    }

        private async Task ReceiveLoopAsync(string clientId, WebSocket ws, string clientIp, string userAgent)
    {
        var buffer = new byte[1024 * 1024 * 5]; // 5MB buffer
            bool authenticated = false;
            int receiveCount = 0;

            while (ws.State == WebSocketState.Open)
            {
                // For the first receive, enforce auth timeout of 5s
                WebSocketReceiveResult result;
                try
                {
                    var receiveTask = ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (receiveCount == 0)
                    {
                        var completed = await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(5)));
                        if (completed != receiveTask)
                        {
                            // Auth timeout
                            Log($"[Auth] Auth timeout for {clientId} ({clientIp})");
                            await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Auth timeout", CancellationToken.None);
                            break;
                        }
                    }

                    result = await receiveTask; // will propagate exceptions
                }
                catch (OperationCanceledException)
                {
                    if (!_cts.IsCancellationRequested)
                        Log($"[Auth] Receive cancelled for {clientId}");
                    break;
                }
                catch (Exception ex)
                {
                    Log($"[Error] Receive error: {ex.Message}");
                    break;
                }

                receiveCount++;

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var packet = ZeroConnectPacket.Deserialize(json);
                if (packet == null) continue;

                // Require Auth as first message
                if (!authenticated)
                {
                    if (packet.Type != PacketType.Auth || string.IsNullOrEmpty(packet.Payload))
                    {
                        RegisterFailedAttempt(clientIp);
                        Log($"[Auth] Invalid or missing auth from {clientId} ({clientIp})");
                        await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Auth required", CancellationToken.None);
                        break;
                    }

                    // Validate token
                    if (!_pairingService.IsTokenValid(packet.Payload))
                    {
                        RegisterFailedAttempt(clientIp);
                        Log($"[Auth] Bad token from {clientId} ({clientIp})");
                        await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid token", CancellationToken.None);
                        break;
                    }

                    // Passed auth
                    if (_authenticatedClients.Count >= MaxDevices)
                    {
                        Log($"[Auth] Max devices reached, rejecting {clientId} ({clientIp})");
                        await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Max devices reached", CancellationToken.None);
                        break;
                    }

                    _clients[clientId] = ws;
                    _authenticatedClients[clientId] = true;
                    authenticated = true;

                    // register device info
                    var info = new DeviceInfo { Id = clientId, Ip = clientIp, UserAgent = userAgent, LastActivity = DateTime.Now };
                    _clientInfos[clientId] = info;
                    RaiseDevicesChanged();

                    Log($"[+] Device authenticated: {clientId} ({clientIp})");

                    // proceed to next loop (do not treat auth packet as normal)
                    continue;
                }

                // update last activity
                if (_clientInfos.TryGetValue(clientId, out var di))
                {
                    di.LastActivity = DateTime.Now;
                    RaiseDevicesChanged();
                }

                await HandlePacketAsync(clientId, ws, packet);
            }
        }

    private async Task HandlePacketAsync(string clientId, WebSocket ws, ZeroConnectPacket packet)
    {
        switch (packet.Type)
        {
            case PacketType.ClipboardText:
                if (packet.Payload is not null)
                {
                    await App.Current.Dispatcher.InvokeAsync(() =>
                        _clipboardService.SetText(packet.Payload));
                    Log($"[Clipboard] Received text from {clientId}");
                }
                break;

            case PacketType.ClipboardImage:
                if (packet.Payload is not null)
                {
                    var bytes = Convert.FromBase64String(packet.Payload);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                        _clipboardService.SetImage(bytes));
                    Log($"[Image] Received image from {clientId}");
                }
                break;

            case PacketType.FileTransfer:
                if (packet.Payload is not null && packet.FileName is not null)
                {
                    var bytes = Convert.FromBase64String(packet.Payload);
                    var savedPath = await _fileService.SaveAsync(packet.FileName, bytes);
                    Log($"[File] Saved {packet.FileName} ({bytes.Length / 1024}KB)");
                }
                break;

            case PacketType.ChunkTransfer:
                if (packet.Payload is not null && packet.FileName is not null && packet.Meta is not null)
                {
                    var completed = _assembler.AddChunk(packet.FileName, packet.Meta, packet.Payload);
                    if (completed != null)
                    {
                        var meta = JsonSerializer.Deserialize<ChunkMeta>(packet.Meta);
                        var isImage = meta?.IsImage ?? false;
                        if (isImage)
                            await App.Current.Dispatcher.InvokeAsync(() =>
                                _clipboardService.SetImage(completed));
                        else
                            await _fileService.SaveAsync(packet.FileName, completed);

                        Log($"[File] ✓ {packet.FileName} ({completed.Length / 1024}KB) diterima");
                    }
                }
                break;

            case PacketType.Ping:
                var pong = new ZeroConnectPacket { Type = PacketType.Pong };
                await SendAsync(ws, pong);
                break;
        }
    }

    // Called when user copies something on PC → push to HP (text)
    public async Task BroadcastClipboardAsync(string text)
    {
        var packet = new ZeroConnectPacket
        {
            Type = PacketType.ClipboardText,
            Payload = text
        };
        await BroadcastAsync(packet);
    }

    // Send image bytes to all connected clients (base64 payload)
    public async Task BroadcastClipboardImageAsync(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0) return;
        var b64 = Convert.ToBase64String(imageBytes);
        var packet = new ZeroConnectPacket
        {
            Type = PacketType.ClipboardImage,
            Payload = b64
        };
        await BroadcastAsync(packet);
        Log($"[Clipboard] Image broadcasted ({imageBytes.Length / 1024}KB)");
    }

    private async Task BroadcastAsync(ZeroConnectPacket packet)
    {
        var tasks = _clients.Values
            .Where(ws => ws.State == WebSocketState.Open)
            .Select(ws => SendAsync(ws, packet));
        await Task.WhenAll(tasks);
    }

    private static async Task SendAsync(WebSocket ws, ZeroConnectPacket packet)
    {
        var bytes = Encoding.UTF8.GetBytes(packet.Serialize());
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    // Send a file (chunked) to all connected clients
    public async Task SendFileToAllAsync(string fileName, byte[] data)
    {
        if (data == null) return;
        const int CHUNK = 512 * 1024;
        var total = (int)Math.Ceiling(data.Length / (double)CHUNK);
        var fileId = Guid.NewGuid().ToString("N");
        var isImage = IsImageByExtension(fileName);

        for (int i = 0; i < total; i++)
        {
            var start = i * CHUNK;
            var len = Math.Min(CHUNK, data.Length - start);
            var slice = new byte[len];
            Array.Copy(data, start, slice, 0, len);
            var b64 = Convert.ToBase64String(slice);

            var meta = JsonSerializer.Serialize(new
            {
                fileId,
                chunkIndex = i,
                totalChunks = total,
                isImage
            });

            var packet = new ZeroConnectPacket
            {
                Type = PacketType.ChunkTransfer,
                Payload = b64,
                FileName = Path.GetFileName(fileName),
                Meta = meta
            };

            await BroadcastAsync(packet);
            // small delay to avoid flooding
            await Task.Delay(10);
        }

        Log($"[File] Sent {fileName} → { (_clients.Count)} clients ({data.Length/1024}KB)");
        try
        {
            if (_history != null)
            {
                var entry = new FileHistoryEntry
                {
                    FileName = Path.GetFileName(fileName),
                    Path = null,
                    Direction = "sent",
                    SizeBytes = data.LongLength,
                    Timestamp = DateTime.Now
                };
                await _history.AddEntryAsync(entry);
            }
        }
        catch { }
    }

    private static bool IsImageByExtension(string filename)
    {
        if (string.IsNullOrEmpty(filename)) return false;
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp" || ext == ".webp";
    }

    private static async Task ServePwaAsync(HttpListenerContext ctx)
    {
        // Serve embedded PWA HTML
        var html = PwaResources.GetHtml();
        var bytes = Encoding.UTF8.GetBytes(html);

        ctx.Response.ContentType = "text/html; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes);
        ctx.Response.Close();
    }

    private void RaiseDevicesChanged()
    {
        try
        {
            var arr = _clientInfos.Values.OrderBy(d => d.Name).ToArray();
            OnDevicesChanged?.Invoke(this, arr);
        }
        catch { }
    }

    public DeviceInfo[] GetConnectedDevices() => _clientInfos.Values.OrderBy(d => d.LastActivity).ToArray();

    public async Task DisconnectClientAsync(string clientId)
    {
        if (string.IsNullOrEmpty(clientId)) return;
        if (_clients.TryGetValue(clientId, out var ws))
        {
            try
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected by user", CancellationToken.None);
            }
            catch { }
            _clients.TryRemove(clientId, out _);
            _clientInfos.TryRemove(clientId, out _);
            RaiseDevicesChanged();
        }
    }

    private bool IsBlocked(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;
        if (_failedAttempts.TryGetValue(ip, out var entry))
        {
            if (DateTime.UtcNow - entry.FirstFailedAt > FailedWindow)
            {
                _failedAttempts.TryRemove(ip, out _);
                return false;
            }
            return entry.Count >= MaxFailedAttempts;
        }
        return false;
    }

    private void RegisterFailedAttempt(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return;
        _failedAttempts.AddOrUpdate(ip,
            (1, DateTime.UtcNow),
            (k, old) =>
            {
                if (DateTime.UtcNow - old.FirstFailedAt > FailedWindow)
                    return (1, DateTime.UtcNow);
                return (old.Count + 1, old.FirstFailedAt);
            });

        if (_failedAttempts.TryGetValue(ip, out var v) && v.Count >= MaxFailedAttempts)
            Log($"[BLOCKED] IP {ip} blocked after {v.Count} failed attempts");
    }

    public void Stop() => _cts.Cancel();
    public void Dispose() { Stop(); _httpListener.Close(); }
    private void Log(string msg) => OnLog?.Invoke(this, msg);
}