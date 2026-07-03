using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ZeroMix.Native;
using zeromix.SnapLayout.Models;

namespace zeromix.SnapLayout
{
    /// <summary>
    /// Core engine untuk Snap Layout — FancyZones-style window snapping.
    /// Mengelola: WinEvent drag detection, global hotkey, zone kalkulasi, eksekusi snap.
    /// </summary>
    public class SnapLayoutService : IDisposable
    {
        #region Win32 DllImport (tidak ada di ZeroMix.Native.Win32)

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public int ptMinPosition_X, ptMinPosition_Y;
            public int ptMaxPosition_X, ptMaxPosition_Y;
            public int rcNormalPosition_Left, rcNormalPosition_Top;
            public int rcNormalPosition_Right, rcNormalPosition_Bottom;
        }

        private const int SW_RESTORE = 9;
        private const int SW_SHOWMAXIMIZED = 3;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_ASYNCWINDOWPOS = 0x4000;

        // WinEvent constants
        private const uint EVENT_SYSTEM_MOVESIZESTART = 0x000A;
        private const uint EVENT_SYSTEM_MOVESIZEEND = 0x000B;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const int OBJID_WINDOW = 0x0000;

        // WM_HOTKEY
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9001;

        // Modifier keys
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        // VK codes
        public const uint VK_Z = 0x5A;

        #endregion

        #region Fields

        private Window? _owner;
        private HwndSource? _hwndSource;

        // WinEvent hooks
        private IntPtr _hookMoveStart = IntPtr.Zero;
        private IntPtr _hookMoveEnd = IntPtr.Zero;
        private Win32.Shell.WinEventDelegate? _winEventDelegate;
        private bool _hooksInstalled;

        // Hotkey state
        private uint _hotkeyModifiers = MOD_CONTROL | MOD_WIN;
        private uint _hotkeyVK = VK_Z;
        private bool _hotkeyRegistered;

        // Drag state
        private IntPtr _draggedHwnd = IntPtr.Zero;

        // Overlays
        private SnapOverlayWindow? _overlayWindow;
        private readonly List<SnapZoneWindow> _zoneWindows = new();
        private DispatcherTimer? _zoneHoverTimer;

        #endregion

        #region Properties

        public bool IsActive { get; private set; }
        public bool DragModeEnabled { get; set; } = true;
        public bool KeybindMode { get; set; } = true;
        public SnapLayoutPreset ActivePreset { get; set; } = SnapLayoutPreset.TwoColumns;
        public SnapLayoutPreset DragPreset { get; set; } = SnapLayoutPreset.TwoColumns;

        public uint HotkeyModifiers => _hotkeyModifiers;
        public uint HotkeyVK => _hotkeyVK;

        #endregion

        #region Events

        public event Action<IntPtr>? OnWindowDragStarted;
        public event Action<IntPtr, SnapZone?>? OnWindowDragEnded;
        public event Action<IntPtr, SnapZone>? OnSnapped;
        public event Action<string>? OnSnapFailed;
        public event Action? OnHotkeyTriggered;
        public event Action<string>? OnHotkeyConflict;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Start the Snap Layout service.
        /// </summary>
        /// <param name="owner">WPF Window untuk menerima WM_HOTKEY messages via HwndSource.</param>
        public void Start(Window owner)
        {
            if (IsActive) return;
            IsActive = true;
            _owner = owner;

            // Hook WndProc untuk WM_HOTKEY
            var helper = new WindowInteropHelper(owner);
            _hwndSource = HwndSource.FromHwnd(helper.Handle);
            if (_hwndSource != null)
                _hwndSource.AddHook(WndProc);

            // Install hooks
            InstallWinEventHooks();
            RegisterHotkeyInternal();

            Console.WriteLine("[SnapLayout] Service started");
        }

        /// <summary>
        /// Stop the service dan cleanup semua resource.
        /// </summary>
        public void Stop()
        {
            if (!IsActive) return;
            IsActive = false;

            HideZoneWindows();

            UnregisterHotkeyInternal();
            UninstallWinEventHooks();

            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }

            _zoneHoverTimer?.Stop();
            _zoneHoverTimer = null;
            _owner = null;
            _draggedHwnd = IntPtr.Zero;

            Console.WriteLine("[SnapLayout] Service stopped");
        }

        public void Dispose() => Stop();

        #endregion

        #region WinEvent Hooks (Drag Detection)

        private void InstallWinEventHooks()
        {
            if (_hooksInstalled) return;
            _hooksInstalled = true;

            _winEventDelegate = OnWinEvent;

            _hookMoveStart = Win32.Shell.SetWinEventHook(
                EVENT_SYSTEM_MOVESIZESTART, EVENT_SYSTEM_MOVESIZESTART,
                IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

            _hookMoveEnd = Win32.Shell.SetWinEventHook(
                EVENT_SYSTEM_MOVESIZEEND, EVENT_SYSTEM_MOVESIZEEND,
                IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

            if (_hookMoveStart == IntPtr.Zero || _hookMoveEnd == IntPtr.Zero)
                Console.WriteLine("[SnapLayout] WinEvent hook installation failed");
        }

        private void UninstallWinEventHooks()
        {
            _hooksInstalled = false;

            if (_hookMoveStart != IntPtr.Zero)
            {
                Win32.Shell.UnhookWinEvent(_hookMoveStart);
                _hookMoveStart = IntPtr.Zero;
            }
            if (_hookMoveEnd != IntPtr.Zero)
            {
                Win32.Shell.UnhookWinEvent(_hookMoveEnd);
                _hookMoveEnd = IntPtr.Zero;
            }

            _winEventDelegate = null;
        }

        private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (!IsActive) return;
            if (idObject != OBJID_WINDOW) return;
            if (hwnd == IntPtr.Zero) return;

            if (eventType == EVENT_SYSTEM_MOVESIZESTART)
            {
                if (!DragModeEnabled) return;

                _draggedHwnd = hwnd;
                OnWindowDragStarted?.Invoke(hwnd);

                // Tampilkan zone indicators di layar
                _ = ShowZoneWindowsAsync(hwnd);
            }
            else if (eventType == EVENT_SYSTEM_MOVESIZEEND)
            {
                if (!DragModeEnabled) return;

                if (_draggedHwnd == IntPtr.Zero) return;

                // Dapatkan posisi mouse
                GetCursorPos(out POINT pt);

                // Cari zone yang mengandung posisi mouse
                var bounds = GetMonitorBounds(_draggedHwnd);
                var zones = SnapLayoutHelper.GetZones(DragPreset, bounds);
                var hitZone = zones.FirstOrDefault(z => z.ScreenRect.Contains(new System.Windows.Point(pt.X, pt.Y)));

                if (hitZone != null)
                {
                    // Snap window ke zone (fire-and-forget async)
                    _ = SnapWindowAsync(_draggedHwnd, hitZone);
                }

                OnWindowDragEnded?.Invoke(_draggedHwnd, hitZone);
                _draggedHwnd = IntPtr.Zero;

                // Hide zone indicators
                HideZoneWindows();
            }
        }

        #endregion

        #region Zone Window Display (Drag Indicators)

        private async Task ShowZoneWindowsAsync(IntPtr hwnd)
        {
            // Tunggu sebentar agar window selesai render posisi baru
            await Task.Delay(50);

            if (!IsActive || hwnd != _draggedHwnd) return;

            try
            {
                HideZoneWindows();

                var bounds = GetMonitorBounds(hwnd);
                var zones = SnapLayoutHelper.GetZones(DragPreset, bounds);

                foreach (var zone in zones)
                {
                    var zw = new SnapZoneWindow(zone, bounds);
                    zw.Show();
                    _zoneWindows.Add(zw);
                }

                // Start hover tracking timer (60fps)
                _zoneHoverTimer?.Stop();
                _zoneHoverTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                _zoneHoverTimer.Tick += OnZoneHoverTick;
                _zoneHoverTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnapLayout] ShowZoneWindows error: {ex.Message}");
            }
        }

        private void OnZoneHoverTick(object? sender, EventArgs e)
        {
            if (!IsActive || _zoneWindows.Count == 0)
            {
                _zoneHoverTimer?.Stop();
                return;
            }

            GetCursorPos(out POINT pt);
            foreach (var zw in _zoneWindows)
            {
                if (zw.IsVisible)
                    zw.CheckHover(pt.X, pt.Y);
            }
        }

        private void HideZoneWindows()
        {
            _zoneHoverTimer?.Stop();

            foreach (var zw in _zoneWindows)
            {
                try { zw.Close(); } catch { }
            }
            _zoneWindows.Clear();
        }

        #endregion

        #region Hotkey (Keybind Trigger)

        private void RegisterHotkeyInternal()
        {
            if (_hotkeyRegistered) return;
            if (_owner == null) return;

            IntPtr hwnd = new WindowInteropHelper(_owner).Handle;
            if (hwnd == IntPtr.Zero) return;

            if (RegisterHotKey(hwnd, HOTKEY_ID, _hotkeyModifiers | MOD_NOREPEAT, _hotkeyVK))
            {
                _hotkeyRegistered = true;
            }
            else
            {
                // Fallback ke Ctrl+Alt+Z
                uint fallbackMod = MOD_CONTROL | MOD_ALT;
                if (RegisterHotKey(hwnd, HOTKEY_ID, fallbackMod | MOD_NOREPEAT, _hotkeyVK))
                {
                    _hotkeyModifiers = fallbackMod;
                    _hotkeyRegistered = true;
                    Console.WriteLine("[SnapLayout] Hotkey conflict — fallback ke Ctrl+Alt+Z");
                    OnHotkeyConflict?.Invoke("Hotkey conflict dengan aplikasi lain. Fallback ke Ctrl+Alt+Z.");
                }
                else
                {
                    Console.WriteLine("[SnapLayout] Hotkey registration failed completely");
                    OnHotkeyConflict?.Invoke("Gagal register hotkey. Coba ganti kombinasi lain.");
                }
            }
        }

        private void UnregisterHotkeyInternal()
        {
            if (!_hotkeyRegistered) return;
            if (_owner == null) return;

            IntPtr hwnd = new WindowInteropHelper(_owner).Handle;
            if (hwnd != IntPtr.Zero)
                UnregisterHotKey(hwnd, HOTKEY_ID);

            _hotkeyRegistered = false;
        }

        /// <summary>
        /// Ganti kombinasi hotkey.
        /// </summary>
        public void ChangeHotkey(uint modifiers, uint vk)
        {
            _hotkeyModifiers = modifiers;
            _hotkeyVK = vk;

            if (IsActive)
            {
                UnregisterHotkeyInternal();
                RegisterHotkeyInternal();
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                if (KeybindMode)
                {
                    OnHotkeyTriggered?.Invoke();
                    _ = ShowOverlayAsync();
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        private async Task ShowOverlayAsync()
        {
            if (!IsActive || _owner == null) return;

            // Dapatkan foreground window untuk tahu di monitor mana overlay harus tampil
            IntPtr foregroundHwnd = GetForegroundWindow();
            var bounds = foregroundHwnd != IntPtr.Zero
                ? GetMonitorBounds(foregroundHwnd)
                : GetPrimaryWorkArea();

            // Tutup overlay sebelumnya
            try { _overlayWindow?.Close(); } catch { }

            _overlayWindow = new SnapOverlayWindow(ActivePreset, bounds, OnOverlayZoneSelected);
            _overlayWindow.Owner = _owner;
            _overlayWindow.Show();
        }

        private void OnOverlayZoneSelected(SnapZone zone)
        {
            // Tutup overlay
            try { _overlayWindow?.Close(); } catch { }
            _overlayWindow = null;

            // Snap foreground window
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
                _ = SnapWindowAsync(hwnd, zone);
        }

        #endregion

        #region Snap Execution

        /// <summary>
        /// Snap window ke zona tertentu.
        /// </summary>
        public async Task SnapWindowAsync(IntPtr hwnd, SnapZone zone)
        {
            try
            {
                // Cek class name untuk UWP detection
                var className = GetWindowClassName(hwnd);
                if (className == "Windows.UI.Core.CoreWindow")
                {
                    OnSnapFailed?.Invoke($"UWP window ({className}) tidak bisa di-snap via Win32 API.");
                    return;
                }

                // Jika window sedang Maximized, restore dulu dengan delay animasi
                var placement = new WINDOWPLACEMENT();
                placement.length = Marshal.SizeOf(placement);

                if (GetWindowPlacement(hwnd, ref placement) && placement.showCmd == SW_SHOWMAXIMIZED)
                {
                    Win32.Window.ShowWindow(hwnd, SW_RESTORE);
                    await Task.Delay(50);
                }

                int x = (int)zone.ScreenRect.X;
                int y = (int)zone.ScreenRect.Y;
                int w = (int)zone.ScreenRect.Width;
                int h = (int)zone.ScreenRect.Height;

                bool success = Win32.Window.SetWindowPos(
                    hwnd,
                    IntPtr.Zero,
                    x, y, w, h,
                    SWP_SHOWWINDOW | SWP_NOZORDER | SWP_ASYNCWINDOWPOS);

                if (!success)
                {
                    OnSnapFailed?.Invoke($"SetWindowPos gagal untuk hwnd={hwnd}");
                    return;
                }

                OnSnapped?.Invoke(hwnd, zone);
                Console.WriteLine($"[SnapLayout] Snapped {hwnd} → {zone}");
            }
            catch (Exception ex)
            {
                OnSnapFailed?.Invoke($"Snap error: {ex.Message}");
                Debug.WriteLine($"[SnapLayout] SnapWindow error: {ex.Message}");
            }
        }

        private string GetWindowClassName(IntPtr hwnd)
        {
            try
            {
                var sb = new System.Text.StringBuilder(256);
                Win32.Window.GetClassName(hwnd, sb, sb.Capacity);
                return sb.ToString();
            }
            catch { return ""; }
        }

        #endregion

        #region Monitor / DPI Utilities

        /// <summary>
        /// Dapatkan work area bounds dari monitor tempat window berada.
        /// </summary>
        public Rect GetMonitorBounds(IntPtr hwnd)
        {
            try
            {
                Win32.Window.GetWindowRect(hwnd, out Win32.Window.RECT winRect);
                int cx = (winRect.Left + winRect.Right) / 2;
                int cy = (winRect.Top + winRect.Bottom) / 2;

                foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    var wa = screen.WorkingArea;
                    if (cx >= wa.Left && cx <= wa.Right && cy >= wa.Top && cy <= wa.Bottom)
                    {
                        return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
                    }
                }
            }
            catch { }

            return GetPrimaryWorkArea();
        }

        private Rect GetPrimaryWorkArea()
        {
            var wa = SystemParameters.WorkArea;
            return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
        }

        #endregion
    }
}
