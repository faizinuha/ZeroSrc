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

        // Velocity untuk SmoothDamp (Ease In-Out)
        private float _velX = 0;
        private float _velY = 0;
        private float _velZoom = 0;

        // Tuning untuk FEELS PREMIUM (Target: Ease In-Out ala Screen Studio)
        private const float SMOOTH_TIME_ACTIVE = 0.22f; // Slower follow to avoid jarring jumps
        private const float SMOOTH_TIME_IDLE = 0.40f;   // Very smooth back to center
        private const float SMOOTH_TIME_TYPE = 0.18f;   // Responsive but smooth for typing
        private const float ZOOM_SMOOTH_TIME = 0.20f;   // Softer zoom transition
        
        private const float ZOOM_IDLE = 1.0f;
        private const float ZOOM_TYPE = 1.30f;          // More noticeable typing zoom
        private const float ZOOM_CLICK = 1.30f;         // Same as typing, avoids "too close" feel
        private const float ZOOM_DRAG = 1.60f;          // Moderate drag zoom

        private int _screenWidth;
        private int _screenHeight;

        public VirtualCamera(int screenWidth, int screenHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            X = screenWidth / 2f;
            Y = screenHeight / 2f;
        }

        public void Update(CursorTracker cursor, bool isZoomEnabled = true)
        {
            float deltaTime = 1f / 60f; // Asumsi loop 60fps

            if (!isZoomEnabled)
            {
                _targetZoom = 1.0f;
                _zoomSustainTicks = 0;
            }
            else
            {
                // 1. Zoom Logic dengan PRIORITY: DRAG > CLICK > TYPE > IDLE
            if (cursor.IsDragging) 
            {
                // Drag Zoom (2.0) selalu prioritas
                _targetZoom = ZOOM_DRAG;
                _zoomSustainTicks = 80; 
            }
            else if (cursor.IsLeftClick) 
            {
                // Click Zoom (1.7)
                if (_targetZoom < ZOOM_DRAG)
                {
                    _targetZoom = ZOOM_CLICK;
                }
                
                if (_zoomSustainTicks < 40)
                {
                    _zoomSustainTicks = 50; 
                }
            }
            else if (cursor.IsTyping)
            {
                // Type Zoom (1.15) - Stay zoomed longer to avoid jitters
                _targetZoom = ZOOM_TYPE;
                _zoomSustainTicks = 60;  // 1 second sustain at 60fps
            }
            else 
            {
                // Hitung mundur sustain
                if (_zoomSustainTicks > 0) 
                {
                    _zoomSustainTicks--;
                }
                else 
                {
                    _targetZoom = ZOOM_IDLE;
                }
            }
        }

            // Zoom dengan Ease In-Out
            Zoom = SmoothDamp(Zoom, _targetZoom, ref _velZoom, ZOOM_SMOOTH_TIME, deltaTime);

            // 2. Camera Move dengan SMART BEHAVIOR
            // Priority: DRAG > CLICK > TYPE, each dengan different responsiveness
            float interactionWeight = 0f;
            float currentSmoothTime = SMOOTH_TIME_IDLE;
            
            if (cursor.IsDragging)
            {
                // Drag: follow cursor aggressively
                interactionWeight = 1.0f;
                currentSmoothTime = SMOOTH_TIME_ACTIVE;
            }
            else if (cursor.IsLeftClick)
            {
                // Click: medium follow
                interactionWeight = 0.7f;
                currentSmoothTime = SMOOTH_TIME_ACTIVE;
            }
            else if (cursor.IsTyping)
            {
                // Typing: quick responsive follow (mirip Screen Studio)
                interactionWeight = 0.5f;
                currentSmoothTime = SMOOTH_TIME_TYPE;
            }
            else
            {
                // Idle: cinematic slow pan back to center
                float cinematicWeight = Math.Clamp((Zoom - 1.0f) / 0.35f, 0f, 1f);
                interactionWeight = cinematicWeight;
                currentSmoothTime = SMOOTH_TIME_IDLE;
            }
            
            float targetX = (cursor.X * interactionWeight) + ((_screenWidth / 2f) * (1f - interactionWeight));
            float targetY = (cursor.Y * interactionWeight) + ((_screenHeight / 2f) * (1f - interactionWeight));

            X = SmoothDamp(X, targetX, ref _velX, currentSmoothTime, deltaTime);
            Y = SmoothDamp(Y, targetY, ref _velY, currentSmoothTime, deltaTime);

            // 3. CLAMPING (Mencegah black area di tepi monitor)
            float viewW = _screenWidth / Zoom;
            float viewH = _screenHeight / Zoom;
            float minX = viewW / 2f;
            float maxX = _screenWidth - (viewW / 2f);
            float minY = viewH / 2f;
            float maxY = _screenHeight - (viewH / 2f);

            X = Math.Clamp(X, minX, maxX);
            Y = Math.Clamp(Y, minY, maxY);

            // 4. Force Reset on Normal
            if (interactionWeight < 0.01f && Math.Abs(Zoom - 1.0f) < 0.001f)
            {
                X = _screenWidth / 2f;
                Y = _screenHeight / 2f;
                Zoom = 1.0f;
            }
        }

        // Fungsi SmoothDamp (Ease In-Out ala Game Engine)
        private float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float deltaTime)
        {
            smoothTime = Math.Max(0.0001f, smoothTime);
            float num = 2f / smoothTime;
            float num2 = num * deltaTime;
            float num3 = 1f / (1f + num2 + 0.48f * num2 * num2 + 0.235f * num2 * num2 * num2);
            float num4 = current - target;
            float num7 = 1000f * smoothTime; // Max speed clamp
            num4 = Math.Clamp(num4, -num7, num7);
            float targetOriginal = target;
            target = current - num4;
            float num8 = (currentVelocity + num * num4) * deltaTime;
            currentVelocity = (currentVelocity - num * num8) * num3;
            float result = target + (num4 + num8) * num3;
            if (targetOriginal - current > 0f == result > targetOriginal)
            {
                result = targetOriginal;
                currentVelocity = (result - targetOriginal) / deltaTime;
            }
            return result;
        }

        public void Reset()
        {
            X = _screenWidth / 2f;
            Y = _screenHeight / 2f;
            Zoom = 1.0f;
            _targetZoom = 1.0f;
            _zoomSustainTicks = 0;
            _velX = _velY = _velZoom = 0;
        }
    }
}
