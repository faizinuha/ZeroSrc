using System;
using System.Diagnostics;
using System.Windows;

namespace ZeroMix.Plugins.Welcome
{
    /// <summary>
    /// Tampilkan Welcome screen hanya saat startup/restart — BUKAN saat Windows+L.
    ///
    /// Cara bedain:
    /// - Startup/restart → uptime Windows pendek (< 3 menit)
    /// - Windows+L unlock → uptime sudah lama, app tidak di-restart
    /// </summary>
    public static class WelcomePlugin
    {
        // Threshold uptime — kalau Windows baru nyala < 3 menit, anggap fresh boot
        private const double BootThresholdMinutes = 5.0;

        public static void TryShowWelcome()
        {
            try
            {
                if (!IsFreshBoot()) return;

                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    try { new WelcomeWindow().Show(); }
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
    }
}
