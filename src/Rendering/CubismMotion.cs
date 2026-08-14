using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ZeroMix.Rendering
{
    /// <summary>
    /// Evaluator motion3.json dan exp3.json.
    /// Format Segments (dikonfirmasi dari CubismWebFramework Live2D):
    ///   Segments[0..1] = base point (t, v)
    ///   lalu per segmen: [type, ...]:
    ///     type 0 (Linear):  +3 angka → [type, t1, v1]
    ///     type 1 (Bezier):  +7 angka → [type, c1t, c1v, c2t, c2v, t1, v1]
    ///     type 2 (Stepped): +3 angka → [type, t1, v1]
    ///     type 3 (InverseStepped): +3 angka → [type, t1, v1]
    /// </summary>
    public sealed class CubismMotion
    {
        public enum SegmentType
        {
            Linear = 0,
            Bezier = 1,
            Stepped = 2,
            InverseStepped = 3
        }

        public struct MotionPoint
        {
            public float Time;
            public float Value;
            public MotionPoint(float t, float v) { Time = t; Value = v; }
        }

        public sealed class MotionCurve
        {
            public string Target = ""; // Model | Parameter | PartOpacity
            public string Id = "";
            public List<MotionPoint> Points = new();
            public List<SegmentType> Types = new();
            public float FadeInTime = -1f;
            public float FadeOutTime = -1f;
        }

        public float Duration;
        public float Fps = 60f;
        public bool Loop;
        public List<MotionCurve> Curves { get; } = new();

        public static CubismMotion? Load(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                JObject json = JObject.Parse(File.ReadAllText(path));
                var motion = new CubismMotion
                {
                    Duration = json["Meta"]?["Duration"]?.Value<float>() ?? 1f,
                    Fps = json["Meta"]?["Fps"]?.Value<float>() ?? 60f,
                    Loop = json["Meta"]?["Loop"]?.Value<bool>() ?? false,
                };

                if (json["Curves"] is JArray curves)
                {
                    foreach (var c in curves)
                    {
                        var curve = new MotionCurve
                        {
                            Target = c["Target"]?.ToString() ?? "Parameter",
                            Id = c["Id"]?.ToString() ?? "",
                            FadeInTime = c["FadeInTime"]?.Value<float>() ?? -1f,
                            FadeOutTime = c["FadeOutTime"]?.Value<float>() ?? -1f,
                        };

                        if (c["Segments"] is JArray segs)
                        {
                            var arr = segs.Select(s => s.Value<float>()).ToArray();
                            // Base point
                            if (arr.Length >= 2)
                            {
                                curve.Points.Add(new MotionPoint(arr[0], arr[1]));
                                int pos = 2;
                                while (pos < arr.Length)
                                {
                                    int type = (int)arr[pos];
                                    if (type == 1) // Bezier: 7 angka
                                    {
                                        if (pos + 6 >= arr.Length) break;
                                        curve.Points.Add(new MotionPoint(arr[pos + 1], arr[pos + 2]));
                                        curve.Points.Add(new MotionPoint(arr[pos + 3], arr[pos + 4]));
                                        curve.Points.Add(new MotionPoint(arr[pos + 5], arr[pos + 6]));
                                        curve.Types.Add(SegmentType.Bezier);
                                        pos += 7;
                                    }
                                    else // Linear/Stepped/InverseStepped: 3 angka
                                    {
                                        if (pos + 2 >= arr.Length) break;
                                        curve.Points.Add(new MotionPoint(arr[pos + 1], arr[pos + 2]));
                                        curve.Types.Add((SegmentType)type);
                                        pos += 3;
                                    }
                                }
                            }
                        }
                        motion.Curves.Add(curve);
                    }
                }
                return motion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Motion] Load error: {ex.Message}");
                return null;
            }
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static MotionPoint LerpPoint(MotionPoint a, MotionPoint b, float t)
            => new(Lerp(a.Time, b.Time, t), Lerp(a.Value, b.Value, t));

        private static float EvaluateLinear(MotionPoint[] pts, float time)
        {
            float t = (time - pts[0].Time) / (pts[1].Time - pts[0].Time);
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Lerp(pts[0].Value, pts[1].Value, t);
        }

        private static float EvaluateBezier(MotionPoint[] pts, float time)
        {
            // pts: [p0, c1, c2, p3]
            float t = (time - pts[0].Time) / (pts[3].Time - pts[0].Time);
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            var p01 = LerpPoint(pts[0], pts[1], t);
            var p12 = LerpPoint(pts[1], pts[2], t);
            var p23 = LerpPoint(pts[2], pts[3], t);
            var p012 = LerpPoint(p01, p12, t);
            var p123 = LerpPoint(p12, p23, t);
            return LerpPoint(p012, p123, t).Value;
        }

        /// <summary>Evaluasi nilai kurva pada waktu tertentu.</summary>
        public float Evaluate(MotionCurve curve, float time)
        {
            if (curve.Points.Count < 2) return 0f;

            int segmentCount = curve.Types.Count;
            int baseIdx = 0;

            // Cari segmen yang berisi `time`
            for (int i = 0; i < segmentCount; i++)
            {
                int nextPoint = baseIdx + (curve.Types[i] == SegmentType.Bezier ? 3 : 1);
                if (nextPoint >= curve.Points.Count) break;
                if (curve.Points[nextPoint].Time > time)
                {
                    return EvaluateSegment(curve, i, baseIdx, time);
                }
                baseIdx = nextPoint;
            }
            // Di luar jangkauan — pakai nilai titik terakhir
            return curve.Points[curve.Points.Count - 1].Value;
        }

        private float EvaluateSegment(MotionCurve curve, int segmentIndex, int baseIdx, float time)
        {
            int count = curve.Types[segmentIndex] == SegmentType.Bezier ? 4 : 2;
            var pts = new MotionPoint[count];
            for (int i = 0; i < count; i++)
                pts[i] = curve.Points[baseIdx + i];

            switch (curve.Types[segmentIndex])
            {
                case SegmentType.Linear:
                    return EvaluateLinear(pts, time);
                case SegmentType.Bezier:
                    return EvaluateBezier(pts, time);
                case SegmentType.Stepped:
                    return pts[0].Value;
                case SegmentType.InverseStepped:
                    return pts[1].Value;
                default:
                    return EvaluateLinear(pts, time);
            }
        }
    }

    /// <summary>Evaluator exp3.json (expression overlay).</summary>
    public sealed class CubismExpression
    {
        public struct ExpressionParameter
        {
            public string Id;
            public float Value;
            public string Blend; // Add | Multiply | Overwrite
        }

        public List<ExpressionParameter> Parameters { get; } = new();

        public static CubismExpression? Load(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                JObject json = JObject.Parse(File.ReadAllText(path));
                var expr = new CubismExpression();
                if (json["Parameters"] is JArray paramsArr)
                {
                    foreach (var p in paramsArr)
                    {
                        expr.Parameters.Add(new ExpressionParameter
                        {
                            Id = p["Id"]?.ToString() ?? "",
                            Value = p["Value"]?.Value<float>() ?? 0f,
                            Blend = p["Blend"]?.ToString() ?? "Add",
                        });
                    }
                }
                return expr;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Terapkan expression ke model (Add/Multiply/Overwrite sesuai spec).</summary>
        public void Apply(CubismModel model, float weight = 1f)
        {
            foreach (var p in Parameters)
            {
                int idx = model.GetParameterIndex(p.Id);
                if (idx < 0) continue;

                float current = model.GetParameterValue(idx);
                float target = p.Value;

                float result;
                switch (p.Blend)
                {
                    case "Multiply":
                        result = current * (1f + (target - 1f) * weight);
                        break;
                    case "Overwrite":
                        result = current + (target - current) * weight;
                        break;
                    default: // Add
                        result = current + target * weight;
                        break;
                }

                // Clamp ke rentang parameter
                float min = model.GetParameterMinValue(idx);
                float max = model.GetParameterMaxValue(idx);
                if (max > min) result = Math.Clamp(result, min, max);

                model.SetParameterValue(idx, result);
            }
        }
    }
}
