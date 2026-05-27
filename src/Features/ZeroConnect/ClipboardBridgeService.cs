// src/Features/ZeroConnect/ClipboardBridgeService.cs
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ZeroMix.Features.ZeroConnect;

public class ClipboardBridgeService
{
    // Flag to avoid loops when setting clipboard programmatically
    public bool IsSettingClipboard { get; private set; }

    // Harus dipanggil dari UI thread (dispatcher)
    public void SetText(string text)
    {
        try
        {
            IsSettingClipboard = true;
            System.Windows.Clipboard.SetText(text);
        }
        finally
        {
            IsSettingClipboard = false;
        }
    }

    public void SetImage(byte[] imageBytes)
    {
        try
        {
            IsSettingClipboard = true;
            using var ms = new MemoryStream(imageBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            System.Windows.Clipboard.SetImage(bitmap);
        }
        finally
        {
            IsSettingClipboard = false;
        }
    }

    public string? GetCurrentText()
        => System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : null;
}