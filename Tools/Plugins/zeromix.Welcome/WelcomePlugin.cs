using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace ZeroMix.Plugins.Welcome
{
    /// <summary>
    /// Welcome screen dengan opsi konfigurasi:
    /// - Mode 1: Hanya saat fresh boot (default lama)
    /// - Mode 2: Setiap kali ZeroMix start (termasuk unlock, sleep wake, dll)
    /// - Mode 3: Disabled
    /// </summary>
    public static class WelcomePlugin
    {
        // Threshold uptime untuk fresh boot detection
        private const double BootThresholdMinutes = 5.0;
        
        // Registry key untuk menyimpan setting
        private const string RegistryPath = @"SOFTWARE\ZeroMix\Welcome";
        
        public enum WelcomeMode
        {
            FreshBootOnly = 0,    // Default: hanya saat fresh boot
            EveryStart = 1,       // Setiap kali ZeroMix start
            Disabled = 2          // Nonaktif
        }

        public static void TryShowWelcome()
        {
            try
            {
                var mode = GetWelcomeMode();
                
                bool shouldShow = mode switch
                {
                    WelcomeMode.FreshBootOnly => IsFreshBoot(),
                    WelcomeMode.EveryStart => true,
                    WelcomeMode.Disabled => false,
                    _ => IsFreshBoot()
                };

                if (!shouldShow) return;

                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    try 
                    { 
                        var window = new WelcomeWindow();
                        window.Show();
                        System.Diagnostics.Debug.WriteLine($"[Welcome] Shown in mode: {mode}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Welcome] Show error: {ex.Message}");
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Welcome] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current welcome mode from registry
        /// </summary>
        public static WelcomeMode GetWelcomeMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
                if (key?.GetValue("Mode") is int mode && Enum.IsDefined(typeof(WelcomeMode), mode))
                {
                    return (WelcomeMode)mode;
                }
            }
            catch { }
            
            return WelcomeMode.FreshBootOnly; // Default
        }

        /// <summary>
        /// Set welcome mode and save to registry
        /// </summary>
        public static void SetWelcomeMode(WelcomeMode mode)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
                key?.SetValue("Mode", (int)mode);
                System.Diagnostics.Debug.WriteLine($"[Welcome] Mode set to: {mode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Welcome] Failed to save mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Cek apakah Windows baru saja boot (uptime < threshold).
        /// Windows+L tidak restart sistem, jadi uptime tetap panjang.
        /// </summary>
        private static bool IsFreshBoot()
        {
            try
            {
                // Environment.TickCount64 = milliseconds sejak Windows boot
                double uptimeMinutes = Environment.TickCount64 / 1000.0 / 60.0;
                System.Diagnostics.Debug.WriteLine($"[Welcome] Uptime: {uptimeMinutes:F1} min");
                return uptimeMinutes < BootThresholdMinutes;
            }
            catch { return false; }
        }

        /// <summary>
        /// Get friendly description of current mode
        /// </summary>
        public static string GetModeDescription(WelcomeMode mode)
        {
            return mode switch
            {
                WelcomeMode.FreshBootOnly => "Hanya saat laptop baru hidup (fresh boot)",
                WelcomeMode.EveryStart => "Setiap kali ZeroMix dibuka (termasuk unlock)",
                WelcomeMode.Disabled => "Nonaktif",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Check if ZeroMix is set to auto-start with Windows
        /// </summary>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                return key?.GetValue("ZeroMix") != null;
            }
            catch { return false; }
        }

        /// <summary>
        /// Enable/disable ZeroMix auto-start with Windows
        /// </summary>
        public static void SetAutoStart(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;

                if (enabled)
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("ZeroMix", $"\"{exePath}\"");
                        System.Diagnostics.Debug.WriteLine("[Welcome] Auto-start enabled");
                    }
                }
                else
                {
                    key.DeleteValue("ZeroMix", false);
                    System.Diagnostics.Debug.WriteLine("[Welcome] Auto-start disabled");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Welcome] Auto-start error: {ex.Message}");
            }
        }
    }
}
