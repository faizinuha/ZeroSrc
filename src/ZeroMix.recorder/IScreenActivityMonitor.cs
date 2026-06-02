using System;

namespace ZeroMix.Recorder
{
    /// <summary>
    /// Interface untuk koordinasi pause/resume antara screen recorder dan wallpaper.
    /// GameDetector memicu events ketika fullscreen app terdeteksi/ditutup.
    /// ScreenStudioRecorder dan WallpaperManager subscribe untuk pause/resume.
    /// </summary>
    public interface IScreenActivityMonitor
    {
        /// <summary>
        /// Triggered ketika fullscreen app terdeteksi.
        /// Subscriber harus pause recording/playback.
        /// </summary>
        event EventHandler<FullscreenAppEventArgs>? FullscreenAppDetected;

        /// <summary>
        /// Triggered ketika fullscreen app ditutup.
        /// Subscriber harus resume recording/playback.
        /// </summary>
        event EventHandler<FullscreenAppEventArgs>? FullscreenAppClosed;

        /// <summary>
        /// Check current state: apakah sedang ada fullscreen app?
        /// </summary>
        bool IsFullscreenActive { get; }
    }

    /// <summary>
    /// Event args untuk fullscreen app detection.
    /// </summary>
    public class FullscreenAppEventArgs : EventArgs
    {
        public string AppName { get; set; } = "";
        public IntPtr Handle { get; set; } = IntPtr.Zero;
        public int ScreenIndex { get; set; } = 0; // Which monitor (multi-monitor support)
    }
}
