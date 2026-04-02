using System;
using System.Runtime.InteropServices;

namespace ZeroMix.SleepMode
{
    public static class IdleDetector
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// Mendapatkan durasi waktu sejak input terakhir (keyboard/mouse).
        /// </summary>
        /// <returns>TimeSpan durasi idle.</returns>
        public static TimeSpan GetIdleTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

            if (!GetLastInputInfo(ref lastInputInfo))
            {
                return TimeSpan.Zero;
            }

            // Environment.TickCount mengembalikan milidetik sejak sistem dimulai.
            // dwTime juga dalam basis yang sama.
            uint idleTicks = (uint)Environment.TickCount - lastInputInfo.dwTime;
            return TimeSpan.FromMilliseconds(idleTicks);
        }
    }
}
