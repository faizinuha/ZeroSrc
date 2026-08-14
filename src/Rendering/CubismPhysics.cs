using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ZeroMix.Rendering
{
    /// <summary>
    /// Parser .physics3.json + simulasi physics sederhana.
    /// Physics TIDAK ditangani Cubism Core — diimplementasikan di sini dan
    /// diterapkan sebagai override parameter sebelum csmUpdateModel tiap frame.
    ///
    /// Implementasi mengikuti pola algoritma Cubism SDK (input normalization →
    /// pendulum simulation per vertex → output ke parameter), ditulis ulang di C#.
    /// </summary>
    public sealed class CubismPhysics
    {
        public sealed class PhysicsInput
        {
            public string TargetParameter = "";
            public float Weight = 1f;
            public string Type = "Angle"; // Angle | X | Y
            public bool Reflect;
            public float NormalizationMin, NormalizationDefault, NormalizationMax;
        }

        public sealed class PhysicsOutput
        {
            public string TargetParameter = "";
            public int VertexIndex;
            public float Scale = 1f;
            public float Weight = 1f;
            public string Type = "Angle"; // Angle | X | Y
            public bool Reflect;
            public float NormalizationMin, NormalizationDefault, NormalizationMax;
        }

        public sealed class PhysicsVertex
        {
            public float X, Y;
            public float Mobility = 1f;
            public float Delay = 1f;
            public float Acceleration = 1f;
            public float Radius = 0f;
            // state simulasi
            public float PositionX, PositionY;
            public float VelocityX, VelocityY;
            public float LastGravityAngle;
        }

        public sealed class PhysicsSetting
        {
            public List<PhysicsInput> Inputs = new();
            public List<PhysicsOutput> Outputs = new();
            public List<PhysicsVertex> Vertices = new();
        }

        public List<PhysicsSetting> Settings { get; } = new();
        public float GravityX, GravityY;
        public float WindX, WindY;
        public float Fps = 60f;

        private float _lastDeltaSeconds = 1f / 60f;
        private CubismModel? _model;

        public static CubismPhysics? Load(string physicsPath)
        {
            if (!File.Exists(physicsPath)) return null;
            try
            {
                JObject json = JObject.Parse(File.ReadAllText(physicsPath));
                var physics = new CubismPhysics();

                var meta = json["Meta"] as JObject;
                if (meta != null)
                {
                    physics.Fps = meta["Fps"]?.Value<float>() ?? 60f;
                    var gravity = meta["EffectiveForces"]?["Gravity"] as JObject;
                    var wind = meta["EffectiveForces"]?["Wind"] as JObject;
                    if (gravity != null)
                    {
                        physics.GravityX = gravity["X"]?.Value<float>() ?? 0f;
                        physics.GravityY = gravity["Y"]?.Value<float>() ?? -1f;
                    }
                    if (wind != null)
                    {
                        physics.WindX = wind["X"]?.Value<float>() ?? 0f;
                        physics.WindY = wind["Y"]?.Value<float>() ?? 0f;
                    }
                }

                if (json["PhysicsSettings"] is JArray settings)
                {
                    foreach (var s in settings)
                    {
                        var setting = new PhysicsSetting();

                        if (s["Input"] is JArray inputs)
                        {
                            foreach (var i in inputs)
                            {
                                setting.Inputs.Add(new PhysicsInput
                                {
                                    TargetParameter = i["Source"]?["Id"]?.ToString() ?? "",
                                    Weight = i["Weight"]?.Value<float>() ?? 1f,
                                    Type = i["Type"]?.ToString() ?? "Angle",
                                    Reflect = i["Reflect"]?.Value<bool>() ?? false,
                                    NormalizationMin = i["Normalization"]?["Position"]?["Minimum"]?.Value<float>() ?? 0f,
                                    NormalizationDefault = i["Normalization"]?["Position"]?["Default"]?.Value<float>() ?? 0f,
                                    NormalizationMax = i["Normalization"]?["Position"]?["Maximum"]?.Value<float>() ?? 0f,
                                });
                            }
                        }

                        if (s["Output"] is JArray outputs)
                        {
                            foreach (var o in outputs)
                            {
                                setting.Outputs.Add(new PhysicsOutput
                                {
                                    TargetParameter = o["Destination"]?["Id"]?.ToString() ?? "",
                                    VertexIndex = o["VertexIndex"]?.Value<int>() ?? 0,
                                    Scale = o["Scale"]?.Value<float>() ?? 1f,
                                    Weight = o["Weight"]?.Value<float>() ?? 1f,
                                    Type = o["Type"]?.ToString() ?? "Angle",
                                    Reflect = o["Reflect"]?.Value<bool>() ?? false,
                                    NormalizationMin = o["Normalization"]?["Position"]?["Minimum"]?.Value<float>() ?? 0f,
                                    NormalizationDefault = o["Normalization"]?["Position"]?["Default"]?.Value<float>() ?? 0f,
                                    NormalizationMax = o["Normalization"]?["Position"]?["Maximum"]?.Value<float>() ?? 0f,
                                });
                            }
                        }

                        if (s["Vertices"] is JArray vertices)
                        {
                            foreach (var v in vertices)
                            {
                                var p = v["Position"] as JObject;
                                setting.Vertices.Add(new PhysicsVertex
                                {
                                    X = p?["X"]?.Value<float>() ?? 0f,
                                    Y = p?["Y"]?.Value<float>() ?? 0f,
                                    Mobility = v["Mobility"]?.Value<float>() ?? 1f,
                                    Delay = v["Delay"]?.Value<float>() ?? 1f,
                                    Acceleration = v["Acceleration"]?.Value<float>() ?? 1f,
                                    Radius = v["Radius"]?.Value<float>() ?? 0f,
                                    PositionX = p?["X"]?.Value<float>() ?? 0f,
                                    PositionY = p?["Y"]?.Value<float>() ?? 0f,
                                });
                            }
                        }

                        physics.Settings.Add(setting);
                    }
                }

                return physics;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Physics] Load error: {ex.Message}");
                return null;
            }
        }

        public void AttachModel(CubismModel model) => _model = model;

        /// <summary>
        /// Evaluasi physics untuk frame berikutnya. Memakai parameter current dari model
        /// dan meng-override parameter output langsung di model.
        /// </summary>
        public void Evaluate(float deltaSeconds, float totalElapsed)
        {
            if (_model == null || Settings.Count == 0) return;

            // Clamp delta agar stabil
            if (deltaSeconds <= 0f) deltaSeconds = 1f / 60f;
            if (deltaSeconds > 1f / 10f) deltaSeconds = 1f / 10f;
            _lastDeltaSeconds = deltaSeconds;

            foreach (var setting in Settings)
            {
                // 1) Input normalization → nilai input terkombinasi
                float inputValue = 0f;
                float weightSum = 0f;
                foreach (var input in setting.Inputs)
                {
                    int idx = _model.GetParameterIndex(input.TargetParameter);
                    if (idx < 0) continue;
                    float v = _model.GetParameterValue(idx);

                    float normMin = input.NormalizationMin;
                    float normMax = input.NormalizationMax;
                    float normDef = input.NormalizationDefault;
                    if (normMax == normMin) continue;

                    // Normalize ke [-1, 1]
                    float normalized;
                    if (v < normDef)
                        normalized = normDef != normMin ? (v - normDef) / (normDef - normMin) : 0f;
                    else
                        normalized = normMax != normDef ? (v - normDef) / (normMax - normDef) : 0f;

                    if (input.Reflect)
                    {
                        if (input.Type == "Angle") normalized = -normalized;
                        else normalized = -normalized;
                    }

                    inputValue += normalized * input.Weight;
                    weightSum += input.Weight;
                }

                float targetAngle = weightSum > 0f ? inputValue / weightSum : 0f;

                // 2) Pendulum simulation per vertex
                for (int vi = 0; vi < setting.Vertices.Count; vi++)
                {
                    var v = setting.Vertices[vi];
                    if (vi == 0)
                    {
                        // Root vertex — langsung mengikuti input (tanpa delay)
                        v.PositionX = v.X;
                        v.PositionY = v.Y;
                        v.VelocityX = 0f;
                        v.VelocityY = 0f;
                        v.LastGravityAngle = targetAngle;
                        continue;
                    }

                    float dt = _lastDeltaSeconds;

                    // Gaya gravitasi & angin (dalam satuan per detik², di-skala kecil)
                    float gx = GravityX * 9.8f * 60f * 60f * 0.00001f;
                    float gy = GravityY * 9.8f * 60f * 60f * 0.00001f;

                    // Target posisi berdasarkan sudut input + gravitasi
                    float gravityAngle = (float)Math.Atan2(-gy, gx);
                    if (setting.Inputs.Count > 0)
                        gravityAngle += targetAngle * (float)Math.PI / 180f * 1.2f;

                    // Sudut antara vertex saat ini dan root (dari posisi awal)
                    var root = setting.Vertices[0];
                    float baseAngle = (float)Math.Atan2(v.PositionY - root.PositionY, v.PositionX - root.PositionX);

                    // Gaya pemulih menuju baseAngle + input
                    float targetX = v.X + (float)Math.Cos(gravityAngle) * v.Radius * 0.05f;
                    float targetY = v.Y + (float)Math.Sin(gravityAngle) * v.Radius * 0.05f;

                    // Spring sederhana dengan mobility/delay/acceleration
                    float spring = 1f - v.Mobility * 0.999f;
                    float stiffness = 6f * v.Acceleration * (1f - v.Delay * 0.9f + 0.1f);
                    stiffness = Math.Clamp(stiffness, 0.1f, 10f);

                    v.VelocityX += (targetX - v.PositionX) * stiffness * dt * 2f;
                    v.VelocityY += (targetY - v.PositionY) * stiffness * dt * 2f;
                    v.VelocityX *= (1f - v.Mobility * 0.5f);
                    v.VelocityY *= (1f - v.Mobility * 0.5f);

                    v.PositionX += v.VelocityX * dt * 60f * 0.01f;
                    v.PositionY += v.VelocityY * dt * 60f * 0.01f;
                    v.LastGravityAngle = gravityAngle;
                }

                // 3) Output → parameter model
                foreach (var output in setting.Outputs)
                {
                    if (output.VertexIndex < 0 || output.VertexIndex >= setting.Vertices.Count) continue;
                    int paramIdx = _model.GetParameterIndex(output.TargetParameter);
                    if (paramIdx < 0) continue;

                    var v = setting.Vertices[output.VertexIndex];
                    var root = setting.Vertices[0];

                    float value;
                    if (output.Type == "Angle")
                    {
                        float dx = v.PositionX - root.PositionX;
                        float dy = v.PositionY - root.PositionY;
                        value = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
                    }
                    else if (output.Type == "X")
                    {
                        value = v.PositionX;
                    }
                    else // Y
                    {
                        value = v.PositionY;
                    }

                    value *= output.Scale * 0.01f;

                    if (output.Reflect) value = -value;

                    // Normalize ke rentang parameter
                    float pmin = _model.GetParameterMinValue(paramIdx);
                    float pmax = _model.GetParameterMaxValue(paramIdx);
                    if (pmax > pmin)
                    {
                        value = Math.Clamp(value, pmin, pmax);
                    }

                    float current = _model.GetParameterValue(paramIdx);
                    float w = Math.Clamp(output.Weight * 0.01f, 0f, 1f);
                    _model.SetParameterValue(paramIdx, current + (value - current) * w);
                }
            }
        }
    }
}
