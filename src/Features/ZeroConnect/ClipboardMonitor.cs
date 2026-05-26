using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ZeroMix.Features.ZeroConnect;

public class ClipboardMonitor : IDisposable
{
    private HwndSource? _source;
    private IntPtr _hwnd;
    private const int WM_CLIPBOARDUPDATE = 0x031D;

    public event EventHandler<string>? OnTextCopied;

    [DllImport("user32.dll")]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

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
            if (System.Windows.Clipboard.ContainsText())
            {
                var text = System.Windows.Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                    OnTextCopied?.Invoke(this, text);
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
