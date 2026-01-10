using System;

namespace ZeroMix.Recorder
{
    public class VirtualCamera
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Zoom { get; private set; } = 1.0f;

        private float _targetZoom = 1.0f;
        private int _zoomSustainTicks = 0; 

        // --- INI INTI TUGASNYA (ANGKA SAKTI) ---
        private const float ZOOM_SPEED = 0.22f;          // Lebih smooth tapi tetep cepet
        private const float FOLLOW_SPEED_ACTIVE = 0.35f; // Nempel banget ke kursor pas zoom
        private const float FOLLOW_SPEED_IDLE = 0.06f;   // Pelan-pelan balik ke tengah (Cinematic)
        
        private const float ZOOM_IDLE = 1.0f;
        private const float ZOOM_CLICK = 1.70f;          // Angka pilihan Kakak (Deep Zoom)
        private const float ZOOM_DRAG = 2.00f;           // Angka pilihan Kakak (V-Deep)
        // ---------------------------------------

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
            // 1. Zoom Logic dengan STICKY SUSTAIN 
            if (cursor.IsDragging) 
            {
                _targetZoom = ZOOM_DRAG;
                _zoomSustainTicks = 80; 
            }
            else if (cursor.IsLeftClick) 
            {
                _targetZoom = ZOOM_CLICK;
                _zoomSustainTicks = 50; 
            }
            else 
            {
                if (_zoomSustainTicks > 0) _zoomSustainTicks--;
                else _targetZoom = ZOOM_IDLE;
            }

            Zoom += (_targetZoom - Zoom) * ZOOM_SPEED;

            // 2. Follow Logic
            bool isInteracting = _targetZoom > 1.01f || Zoom > 1.01f;
            float targetX = isInteracting ? cursor.X : (_screenWidth / 2f);
            float targetY = isInteracting ? cursor.Y : (_screenHeight / 2f);

            float currentSpeed = isInteracting ? FOLLOW_SPEED_ACTIVE : FOLLOW_SPEED_IDLE;
            X += (targetX - X) * currentSpeed;
            Y += (targetY - Y) * currentSpeed;

            // 3. CLAMPING (Hard Lock boundary)
            float viewW = _screenWidth / Zoom;
            float viewH = _screenHeight / Zoom;
            float minX = viewW / 2f;
            float maxX = _screenWidth - (viewW / 2f);
            float minY = viewH / 2f;
            float maxY = _screenHeight - (viewH / 2f);

            X = Math.Clamp(X, minX, maxX);
            Y = Math.Clamp(Y, minY, maxY);

            // 4. Force Reset on Normal
            if (!isInteracting && Math.Abs(Zoom - 1.0f) < 0.005f)
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
            _zoomSustainTicks = 0;
        }
    }
}
