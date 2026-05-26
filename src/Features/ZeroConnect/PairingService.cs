using QRCoder;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;

namespace ZeroMix.Features.ZeroConnect;

public class PairingService
{
    private string _currentToken = "";
    private DateTime _tokenExpiry = DateTime.MinValue;
    private const int TokenExpiryMinutes = 60; // Token valid for 1 hour

    public string GenerateToken()
    {
        // Generate new token every time, expires after 1 hour
        _currentToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
                              .Replace("+", "").Replace("/", "")[..16];
        _tokenExpiry = DateTime.Now.AddMinutes(TokenExpiryMinutes);
        return _currentToken;
    }

    public bool IsTokenValid(string token)
    {
        // Check if token matches AND hasn't expired
        return token == _currentToken && DateTime.Now < _tokenExpiry;
    }

    public string GetPairingUrl(int port, string token)
        => $"http://127.0.0.1:{port}/?token={token}";
    
    public string GetLocalIp()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
        socket.Connect("8.8.8.8", 65530);
        var endPoint = socket.LocalEndPoint as IPEndPoint;
        return endPoint?.Address.ToString() ?? "127.0.0.1";
    }

    public string GetPairingUrl(int port)
        => $"http://{GetLocalIp()}:{port}";

    public BitmapImage GenerateQrCode(string url)
    {
        var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        var pngBytes = qrCode.GetGraphic(10);

        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.StreamSource = new MemoryStream(pngBytes);
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }
}