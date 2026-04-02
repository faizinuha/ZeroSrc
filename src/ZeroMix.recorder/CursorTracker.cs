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
        public bool IsTyping { get; private set; }
        
        private bool _wasClickedLastFrame = false;
        private float _dragStartX, _dragStartY;
        private const float DRAG_THRESHOLD = 10f;
        
        // Keyboard tracking
        private bool _wasKeyPressedLastFrame = false;
        private int _typeActiveTicks = 0;
        private const int TYPE_SUSTAIN_TICKS = 45;  // 0.75s sustain at 60fps
        
        // List of keys to ignore (modifiers, function keys)
        private static readonly int[] IgnoredKeys = new[]
        {
            0x10,  // Shift
            0x11,  // Ctrl
            0x12,  // Alt
            0x5B,  // Windows
            0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B,  // F1-F12
            0x90,  // Numlock
            0x91,  // Scroll Lock
            0x14,  // Caps Lock
        };

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
            
            // Keyboard tracking - detect meaningful keystroke
            bool keyPressed = DetectAnyKeyPress();
            
            if (keyPressed && !_wasKeyPressedLastFrame)
            {
                // Key pressed start
                _typeActiveTicks = TYPE_SUSTAIN_TICKS;
                IsTyping = true;
            }
            else if (keyPressed)
            {
                // Key held or continuously pressed
                _typeActiveTicks = TYPE_SUSTAIN_TICKS;
                IsTyping = true;
            }
            else
            {
                // Count down sustain timer
                if (_typeActiveTicks > 0)
                {
                    _typeActiveTicks--;
                    IsTyping = true;
                }
                else
                {
                    IsTyping = false;
                }
            }
            
            _wasKeyPressedLastFrame = keyPressed;
        }
        
        private bool DetectAnyKeyPress()
        {
            // Check all printable keys (A-Z, 0-9, Space, etc)
            // Range 0x30-0x5A covers: 0-9, A-Z
            for (int key = 0x30; key <= 0x5A; key++)
            {
                if ((GetAsyncKeyState(key) & 0x8000) != 0)
                {
                    return true;
                }
            }
            
            // Check Space, Enter, Backspace, Delete, Tab
            int[] extraKeys = { 0x20, 0x0D, 0x08, 0x2E, 0x09 };
            foreach (int key in extraKeys)
            {
                if ((GetAsyncKeyState(key) & 0x8000) != 0)
                {
                    return true;
                }
            }
            
            // Check numpad keys
            for (int key = 0x60; key <= 0x69; key++)
            {
                if ((GetAsyncKeyState(key) & 0x8000) != 0)
                {
                    return true;
                }
            }
            
            // Check special chars (,.<>?/";'[]{}\|- etc)
            int[] specialKeys = { 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF, 0xC0, 0xDB, 0xDC, 0xDD };
            foreach (int key in specialKeys)
            {
                if ((GetAsyncKeyState(key) & 0x8000) != 0)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
