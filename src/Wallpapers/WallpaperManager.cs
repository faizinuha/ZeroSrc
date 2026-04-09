using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using ZeroMix;

namespace ZeroMix.Wallpapers
{
    /// <summary>
    /// Global manager for Video Wallpapers and Desktop refresh.
    /// Replaces the static functionality previously found in the Wallpapers Window.
    /// </summary>
    public static class WallpaperManager
    {
        private static VideoWallpaperWindow? _videoWallpaperWindow;

        /// <summary>
        /// Launch a video wallpaper dan simpan session ke JSON.
        /// </summary>
        public static void LaunchVideoWallpaper(string videoPath, double volume = 0)
        {
            try
            {
                StopVideoWallpaper();
                _videoWallpaperWindow = new VideoWallpaperWindow(videoPath, volume);
                _videoWallpaperWindow.Show();

                // Simpan session
                WallpaperSession.Save(new WallpaperSession {
                    Name = Path.GetFileNameWithoutExtension(videoPath),
                    WallpaperPath = videoPath,
                    Volume = volume,
                    Type = "video",
                    Active = true
                });

                Debug.WriteLine($"Video wallpaper launched: {videoPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error launching video wallpaper: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop wallpaper dan clear session.
        /// </summary>
        public static void StopVideoWallpaper()
        {
            try
            {
                if (_videoWallpaperWindow != null)
                {
                    _videoWallpaperWindow.Close();
                    _videoWallpaperWindow = null;
                }
                WallpaperSession.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping video wallpaper: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore wallpaper dari session JSON saat app startup.
        /// Dipanggil dari App.xaml.cs → StartMainApp.
        /// </summary>
        public static void RestoreSession()
        {
            try
            {
                var session = WallpaperSession.Load();
                if (session == null || !session.Active) return;
                if (!File.Exists(session.WallpaperPath)) return;

                Debug.WriteLine($"[WallpaperManager] Restoring session: {session.WallpaperPath}");
                LaunchVideoWallpaper(session.WallpaperPath, session.Volume);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WallpaperManager] RestoreSession error: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the desktop by reapplying the current registry wallpaper.
        /// </summary>
        public static void RefreshDesktop()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
                string? wallpaper = key?.GetValue("Wallpaper") as string;
                if (!string.IsNullOrEmpty(wallpaper))
                {
                    NativeMethods.SetWallpaper(wallpaper);
                }
            }
            catch { }
        }

        /// <summary>
        /// Helper to find FFMPEG (Static version)
        /// </summary>
        public static string? FindFFmpeg()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var paths = new[] {
                Path.Combine(baseDir, "Tools", "FFMPEG", "ffmpeg.exe"),
                Path.Combine(baseDir, "FFMPEG", "ffmpeg.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ZeroMix", "Tools", "FFMPEG", "ffmpeg.exe")
            };
            return paths.FirstOrDefault(File.Exists);
        }

        // Native Methods Helper
        private static class NativeMethods
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

            private const int SPI_SETDESKWALLPAPER = 20;
            private const int SPIF_UPDATEINIFILE = 0x01;
            private const int SPIF_SENDCHANGE = 0x02;

            public static void SetWallpaper(string path)
            {
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
        }
    }
}
