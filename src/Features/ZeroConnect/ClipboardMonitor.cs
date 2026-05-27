using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ZeroMix.Features.ZeroConnect;

public class ClipboardMonitor : IDisposable
{
    private HwndSource? _source;
    private IntPtr _hwnd;
    private const int WM_CLIPBOARDUPDATE = 0x031D;

    // Optional reference to clipboard bridge service to detect self-origin changes
    public ClipboardBridgeService? ClipboardService { get; set; }

    public event EventHandler<string>? OnTextCopied;
    public event EventHandler<byte[]?>? OnImageCopied;

    [DllImport("user32.dll")]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private DateTime _lastEvent = DateTime.MinValue;

    public void Start(Window ownerWindow)
    {
        _hwnd = new WindowInteropHelper(ownerWindow).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source.AddHook(WndProc);
        AddClipboardFormatListener(_hwnd);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            handled = true;
            OnClipboardChanged();
        }
        return IntPtr.Zero;
    }

    private void OnClipboardChanged()
    {
        try
        {
            // Debounce rapid changes
            if (DateTime.UtcNow - _lastEvent < TimeSpan.FromMilliseconds(300)) return;
            _lastEvent = DateTime.UtcNow;

            // Skip if change originated from our own code
            if (ClipboardService != null && ClipboardService.IsSettingClipboard) return;

            if (System.Windows.Clipboard.ContainsText())
            {
                var text = System.Windows.Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                    OnTextCopied?.Invoke(this, text);
                return;
            }

            if (System.Windows.Clipboard.ContainsImage())
            {
                try
                {
                    var img = System.Windows.Clipboard.GetImage();
                    if (img != null)
                    {
                        // Encode to PNG bytes
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(img));
                        using var ms = new System.IO.MemoryStream();
                        encoder.Save(ms);
                        var bytes = ms.ToArray();
                        OnImageCopied?.Invoke(this, bytes);
                    }
                }
                catch { /* ignore image read errors */ }
            }
        }
        catch { /* Clipboard mungkin terkunci sementara */ }
    }

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero)
            RemoveClipboardFormatListener(_hwnd);
        _source?.RemoveHook(WndProc);
    }
}
