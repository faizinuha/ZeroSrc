using System;
using System.Diagnostics;

namespace ZeroMix
{
    // Extension methods untuk Wallpapers class
    public partial class Wallpapers
    {
        // --- VIDEO WALLPAPER CONTROL METHODS ---
        
        /// <summary>
        /// Launch video wallpaper window dengan video yang sudah di-optimize
        /// </summary>
        public void LaunchVideoWallpaper(string videoPath)
        {
            try
            {
                // Stop existing video wallpaper if any
                StopVideoWallpaper();

                // Create and show video wallpaper window
                _videoWallpaperWindow = new VideoWallpaperWindow(videoPath);
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
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping video wallpaper: {ex.Message}");
            }
        }
    }
}
