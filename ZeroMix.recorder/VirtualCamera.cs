using System;

namespace ZeroMix.Recorder
{
    public class VirtualCamera
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Zoom { get; private set; } = 1.0f;

        private float _targetZoom = 1.0f;
        
        // Physics parameters
        private const float ZOOM_SPEED = 0.08f;
        
        // Zoom levels
        private const float ZOOM_IDLE = 1.0f;
        private const float ZOOM_CLICK = 1.25f;
        private const float ZOOM_DRAG = 1.35f;

        private int _screenWidth;
        private int _screenHeight;

        public VirtualCamera(int screenWidth, int screenHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            X = screenWidth / 2f;
            Y = screenHeight / 2f;
        }

        public void Update(CursorTracker cursor)
        {
            // 1. Zoom Logic
            if (cursor.IsDragging) _targetZoom = ZOOM_DRAG;
            else if (cursor.IsLeftClick) _targetZoom = ZOOM_CLICK;
            else _targetZoom = ZOOM_IDLE;

            Zoom += (_targetZoom - Zoom) * ZOOM_SPEED;

            // 2. Smooth Follow (Linear Lerp - Anti Jitter)
            bool isActive = Zoom > 1.02f;
            float targetX = isActive ? cursor.X : (_screenWidth / 2f);
            float targetY = isActive ? cursor.Y : (_screenHeight / 2f);

            float speed = isActive ? 0.08f : 0.05f; 
            X += (targetX - X) * speed;
            Y += (targetY - Y) * speed;

            // 3. CLAMPING (Mencegah black area di tepi monitor)
            float viewW = _screenWidth / Zoom;
            float viewH = _screenHeight / Zoom;
            float minX = viewW / 2f;
            float maxX = _screenWidth - (viewW / 2f);
            float minY = viewH / 2f;
            float maxY = _screenHeight - (viewH / 2f);

            X = Math.Clamp(X, minX, maxX);
            Y = Math.Clamp(Y, minY, maxY);

            // 4. Hard Lock for Idle
            if (!isActive && Math.Abs(Zoom - 1.0f) < 0.01f && Math.Abs(X - _screenWidth/2f) < 1.0f)
            {
                X = _screenWidth / 2f;
                Y = _screenHeight / 2f;
                Zoom = 1.0f;
            }
        }

        public void Reset()
        {
            X = _screenWidth / 2f;
            Y = _screenHeight / 2f;
            Zoom = 1.0f;
            _targetZoom = 1.0f;
        }
    }
}
