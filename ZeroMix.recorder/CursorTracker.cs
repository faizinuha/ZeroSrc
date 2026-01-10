using System;
using System.Runtime.InteropServices;

namespace ZeroMix.Recorder
{
    /// <summary>
    /// Tracks cursor position, clicks, and drag state.
    /// Used to control the virtual camera.
    /// </summary>
    public class CursorTracker
    {
        [DllImport("user32.dll")] 
        private static extern bool GetCursorPos(out POINT lpPoint);
        
        [DllImport("user32.dll")] 
        private static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        public float X { get; private set; }
        public float Y { get; private set; }
        public bool IsLeftClick { get; private set; }
        public bool IsDragging { get; private set; }
        
        private bool _wasClickedLastFrame = false;
        private float _dragStartX, _dragStartY;
        private const float DRAG_THRESHOLD = 10f;

        public void Update()
        {
            GetCursorPos(out var pos);
            X = pos.X;
            Y = pos.Y;

            bool leftDown = (GetAsyncKeyState(0x01) & 0x8000) != 0;

            if (leftDown && !_wasClickedLastFrame)
            {
                // Click started
                _dragStartX = X;
                _dragStartY = Y;
                IsLeftClick = true;
                IsDragging = false;
            }
            else if (leftDown && _wasClickedLastFrame)
            {
                // Held down - check for drag
                float dx = X - _dragStartX;
                float dy = Y - _dragStartY;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                
                if (dist > DRAG_THRESHOLD)
                {
                    IsDragging = true;
                }
                IsLeftClick = true;
            }
            else
            {
                // Released
                IsLeftClick = false;
                IsDragging = false;
            }

            _wasClickedLastFrame = leftDown;
        }
    }
}
