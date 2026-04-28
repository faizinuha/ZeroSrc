using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace zeromix.CatGatekeeper
{
    public class CatGatekeeperService : IDisposable
    {
        private DispatcherTimer? _timer;
        private int _secondsActive = 0;
        private bool _isBreakActive = false;
        
        public int UsageLimitMinutes { get; set; } = 60; // Default 60 menit
        public int BreakTimeMinutes { get; set; } = 5;   // Default 5 menit break
        
        public event Action? OnBreakTimeReached;
        public event Action? OnBreakEnded;
        
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        
        public void Start()
        {
            if (_timer != null) return;
            
            _secondsActive = 0;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTimerTick;
            _timer.Start();
            
            Console.WriteLine("[CatGatekeeper] Service started");
        }
        
        public void Stop()
        {
            _timer?.Stop();
            _timer = null;
            _secondsActive = 0;
            Console.WriteLine("[CatGatekeeper] Service stopped");
        }
        
        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_isBreakActive) return;
            
            // Check if user is active (mouse/keyboard input in last 5 seconds)
            if (IsUserActive())
            {
                _secondsActive++;
                
                // Check if reached usage limit
                if (_secondsActive >= UsageLimitMinutes * 60)
                {
                    _isBreakActive = true;
                    _secondsActive = 0;
                    OnBreakTimeReached?.Invoke();
                    Console.WriteLine("[CatGatekeeper] Break time reached!");
                }
            }
        }
        
        private bool IsUserActive()
        {
            LASTINPUTINFO lastInput = new LASTINPUTINFO();
            lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);
            
            if (GetLastInputInfo(ref lastInput))
            {
                uint idleTime = (uint)Environment.TickCount - lastInput.dwTime;
                return idleTime < 5000; // Active if input within last 5 seconds
            }
            
            return false;
        }
        
        public void EndBreak()
        {
            _isBreakActive = false;
            _secondsActive = 0;
            OnBreakEnded?.Invoke();
            Console.WriteLine("[CatGatekeeper] Break ended, timer reset");
        }
        
        public int GetCurrentSeconds() => _secondsActive;
        public bool IsBreakActive() => _isBreakActive;
        
        public void Dispose()
        {
            Stop();
        }
    }
}
