using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ZeroMix
{
    // Extension methods untuk Wallpapers class
    public partial class Wallpapers
    {
        // --- VIDEO WALLPAPER CONTROL METHODS ---
        
        /// <summary>
        /// Launch video wallpaper window dengan video yang sudah di-optimize
        /// </summary>
        public void LaunchVideoWallpaper(string videoPath, double volume = 0)
        {
            try
            {
                // Stop existing video wallpaper if any
                StopVideoWallpaper();

                // Create and show video wallpaper window
                _videoWallpaperWindow = new VideoWallpaperWindow(videoPath, volume);
                _videoWallpaperWindow.Show();

                StatusLabel.Text = $"✅ Video wallpaper is now playing: {System.IO.Path.GetFileName(videoPath)}";
                
                Debug.WriteLine($"Video wallpaper launched: {videoPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error launching video wallpaper: {ex.Message}");
                StatusLabel.Text = "❌ Failed to launch video wallpaper";
            }
        }

        /// <summary>
        /// Stop dan close video wallpaper window
        /// </summary>
        public static void StopVideoWallpaper()
        {
            try
            {
                if (_videoWallpaperWindow != null)
                {
                    _videoWallpaperWindow.Close();
                    _videoWallpaperWindow = null;
                    Debug.WriteLine("Video wallpaper stopped");
                    
                    // Force desktop refresh to ensure wallpaper is visible
                    RefreshDesktop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping video wallpaper: {ex.Message}");
            }
        }

        public static void LaunchVideoWallpaperStatic(string videoPath, double volume = 0)
        {
            StopVideoWallpaper();
            _videoWallpaperWindow = new VideoWallpaperWindow(videoPath, volume);
            _videoWallpaperWindow.Show();
        }

        public static string? OptimizeVideoForWallpaperStatic(string inputPath)
        {
            try
            {
                var ffmpegPath = FindFFmpegStatic();
                if (string.IsNullOrEmpty(ffmpegPath)) return null;

                var roamingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix", "Temp");
                Directory.CreateDirectory(roamingDir);
                
                var fileName = Path.GetFileNameWithoutExtension(inputPath);
                var outputPath = Path.Combine(roamingDir, $"{fileName}_optimized.mp4");

                if (File.Exists(outputPath)) return outputPath; // Cache hit

                var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

                var arguments = $"-i \"{inputPath}\" -vf \"scale={screenWidth}:{screenHeight}:force_original_aspect_ratio=increase,crop={screenWidth}:{screenHeight},fps=60\" -c:v libx264 -preset fast -crf 20 -tune film -pix_fmt yuv420p -an -movflags +faststart -y \"{outputPath}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit(90000);

                return File.Exists(outputPath) ? outputPath : null;
            }
            catch { return null; }
        }

        public static string? FindFFmpegStatic()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var paths = new[] {
                Path.Combine(baseDir, "FFMPEG", "ffmpeg.exe"),
                @"C:\ZeroMix\ZeroMix\FFMPEG\ffmpeg.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ZeroMix", "FFMPEG", "ffmpeg.exe")
            };
            return paths.FirstOrDefault(File.Exists);
        }

        public static void RefreshDesktop()
        {
            try
            {
                // Trigger wallpaper refresh via SystemParametersInfo
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
                string? wallpaper = key?.GetValue("Wallpaper") as string;
                if (!string.IsNullOrEmpty(wallpaper))
                {
                    NativeMethods.SetWallpaper(wallpaper);
                }
            }
            catch { }
        }
    }
}
