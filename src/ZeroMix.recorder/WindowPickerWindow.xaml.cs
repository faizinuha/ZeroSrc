using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace ZeroMix.Recorder
{
    public class WindowInfo
    {
        public string Title { get; set; } = "";
        public string ProcessName { get; set; } = "";
        public IntPtr Handle { get; set; }
        public ImageSource? Icon { get; set; }
    }

    public partial class WindowPickerWindow : Window
    {
        public IntPtr SelectedHandle { get; private set; } = IntPtr.Zero;
        public bool IsConfirmed { get; private set; } = false;

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        public WindowPickerWindow()
        {
            InitializeComponent();
            LoadWindows();
        }

        private void LoadWindows()
        {
            var windows = new List<WindowInfo>();

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                var sb = new System.Text.StringBuilder(256);
                GetWindowText(hWnd, sb, 256);
                string title = sb.ToString().Trim();

                if (string.IsNullOrEmpty(title)) return true;
                if (title.Length < 2) return true;

                // Skip taskbar dan shell windows
                if (title == "Program Manager" || title == "Windows Input Experience") return true;

                GetWindowThreadProcessId(hWnd, out uint pid);
                string processName = "";
                ImageSource? icon = null;

                try
                {
                    var proc = Process.GetProcessById((int)pid);
                    processName = proc.ProcessName;

                    // Skip proses sistem
                    if (processName is "explorer" or "SearchHost" or "ShellExperienceHost") return true;

                    // Ambil icon dari proses
                    var sysIcon = System.Drawing.Icon.ExtractAssociatedIcon(proc.MainModule?.FileName ?? "");
                    if (sysIcon != null)
                    {
                        icon = Imaging.CreateBitmapSourceFromHIcon(
                            sysIcon.Handle, Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        icon.Freeze();
                    }
                }
                catch { }

                // Cek ukuran window — skip yang terlalu kecil
                if (GetWindowRect(hWnd, out RECT rect))
                {
                    int w = rect.Right - rect.Left;
                    int h = rect.Bottom - rect.Top;
                    if (w < 100 || h < 100) return true;
                }

                windows.Add(new WindowInfo
                {
                    Title = title,
                    ProcessName = processName,
                    Handle = hWnd,
                    Icon = icon
                });

                return true;
            }, IntPtr.Zero);

            WindowList.ItemsSource = windows;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowList.SelectedItem is WindowInfo info)
            {
                SelectedHandle = info.Handle;
                IsConfirmed = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Pilih window dulu.", "ZeroMix",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            Close();
        }
    }
}
