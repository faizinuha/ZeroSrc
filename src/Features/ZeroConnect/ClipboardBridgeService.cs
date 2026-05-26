// src/Features/ZeroConnect/ClipboardBridgeService.cs
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ZeroMix.Features.ZeroConnect;

public class ClipboardBridgeService
{
    // Harus dipanggil dari UI thread (dispatcher)
    public void SetText(string text)
    {
        System.Windows.Clipboard.SetText(text);
    }

    public void SetImage(byte[] imageBytes)
    {
        using var ms = new MemoryStream(imageBytes);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = ms;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        System.Windows.Clipboard.SetImage(bitmap);
    }

    public string? GetCurrentText()
        => System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : null;
}