// src/Features/ZeroConnect/ZeroConnectServer.cs
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Collections.Concurrent;

namespace ZeroMix.Features.ZeroConnect;

public class ZeroConnectServer : IDisposable
{
    private readonly HttpListener _httpListener;
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private readonly ClipboardBridgeService _clipboardService;
    private readonly FileTransferService _fileService;
    private readonly ChunkAssembler _assembler = new();
    private readonly PairingService _pairingService;
    private CancellationTokenSource _cts = new();

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
        // Cek token di setiap request dengan validasi expiry
        var token = context.Request.QueryString["token"];
        if (string.IsNullOrEmpty(token) || !_pairingService.IsTokenValid(token))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            Log($"[BLOCKED] Invalid/expired token from {context.Request.RemoteEndPoint}");
            return;
        }

        // Serve PWA files (token sudah valid)
        if (!context.Request.IsWebSocketRequest)
        {
            await ServePwaAsync(context);
            return;
        }

        // WebSocket upgrade
        var wsContext = await context.AcceptWebSocketAsync(null);
        var clientId = Guid.NewGuid().ToString("N")[..8];
        var ws = wsContext.WebSocket;

        _clients[clientId] = ws;
        Log($"[+] Device connected: {clientId}");

        try
        {
            await ReceiveLoopAsync(clientId, ws);
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            Log($"[-] Device disconnected: {clientId}");
        }
    }

    private async Task ReceiveLoopAsync(string clientId, WebSocket ws)
    {
        var buffer = new byte[1024 * 1024 * 5]; // 5MB buffer

        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, _cts.Token);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", _cts.Token);
                break;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var packet = ZeroConnectPacket.Deserialize(json);

            if (packet == null) continue;

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
                    Log($"[File] Saved to {savedPath}");
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

    // Called when user copies something on PC → push to HP
    public async Task BroadcastClipboardAsync(string text)
    {
        var packet = new ZeroConnectPacket
        {
            Type = PacketType.ClipboardText,
            Payload = text
        };
        await BroadcastAsync(packet);
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

    public void Stop() => _cts.Cancel();
    public void Dispose() { Stop(); _httpListener.Close(); }
    private void Log(string msg) => OnLog?.Invoke(this, msg);
}