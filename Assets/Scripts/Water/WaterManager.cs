using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoatGame.Water
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class WaterManager : MonoBehaviour
    {
        public const int MaxWaveCount = 8;

        private static readonly int WaterWaveCountId = Shader.PropertyToID("_WaterWaveCount");
        private static readonly int WaterLevelId = Shader.PropertyToID("_WaterLevel");
        private static readonly int OceanTimeId = Shader.PropertyToID("_OceanTime");
        private static readonly int WaterWaveDirectionId = Shader.PropertyToID("_WaterWaveDirection");
        private static readonly int WaterWaveAmplitudeId = Shader.PropertyToID("_WaterWaveAmplitude");
        private static readonly int WaterWaveWavelengthId = Shader.PropertyToID("_WaterWaveWavelength");
        private static readonly int WaterWaveSpeedId = Shader.PropertyToID("_WaterWaveSpeed");
        private static readonly int WaterWaveSteepnessId = Shader.PropertyToID("_WaterWaveSteepness");

        [Serializable]
        public struct GerstnerWave
        {
            public string name;
            public bool enabled;
            public Vector2 direction;
            [Min(0f)] public float amplitude;
            [Min(0.1f)] public float wavelength;
            [Min(0f)] public float speed;
            [Range(0f, 1f)] public float steepness;

            public GerstnerWave(string name, Vector2 direction, float amplitude, float wavelength, float speed, float steepness)
            {
                this.name = name;
                this.enabled = true;
                this.direction = direction;
                this.amplitude = amplitude;
                this.wavelength = wavelength;
                this.speed = speed;
                this.steepness = steepness;
            }

            public bool IsUsable => enabled && amplitude > 0.0001f && wavelength > 0.1f && direction.sqrMagnitude > 0.0001f;

            public void Sanitize()
            {
                amplitude = Mathf.Max(0f, amplitude);
                wavelength = Mathf.Max(0.1f, wavelength);
                speed = Mathf.Max(0f, speed);
                steepness = Mathf.Clamp01(steepness);
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Vector2.right;
                }
                else
                {
                    direction.Normalize();
                }
            }
        }

        public readonly struct WaterSample
        {
            public WaterSample(Vector3 position, Vector3 sourcePosition, Vector3 normal, Vector3 displacement, Vector3 velocity, float crest)
            {
                Position = position;
                SourcePosition = sourcePosition;
                Normal = normal;
                Displacement = displacement;
                Velocity = velocity;
                Crest = crest;
            }

            public Vector3 Position { get; }
            public Vector3 SourcePosition { get; }
            public Vector3 Normal { get; }
            public Vector3 Displacement { get; }
            public Vector3 Velocity { get; }
            public float Crest { get; }
            public float Height => Position.y;
        }

        public static WaterManager Instance { get; private set; }

        [Header("Surface")]
        [SerializeField] private float waterLevel;
        [SerializeField] private List<GerstnerWave> waves = new List<GerstnerWave>();

        [Header("Sampling")]
        [SerializeField, Range(1, 8)] private int inverseIterations = 4;
        [SerializeField, Range(0.5f, 1f)] private float inverseRelaxation = 0.85f;
        [SerializeField] private bool useHorizontalDisplacement = true;

        [Header("Current")]
        [SerializeField] private Vector2 currentDirection = new Vector2(0.8f, 0.25f);
        [SerializeField, Min(0f)] private float currentSpeed = 0.18f;

        [Header("Shader Globals")]
        [SerializeField] private bool pushShaderGlobals = true;

        [Header("Debug")]
        [SerializeField] private bool drawDebugGrid = true;
        [SerializeField, Min(2f)] private float debugGridSize = 36f;
        [SerializeField, Min(1f)] private float debugGridSpacing = 6f;
        [SerializeField, Min(0.1f)] private float debugNormalLength = 1.5f;

        private readonly Vector4[] shaderDirections = new Vector4[MaxWaveCount];
        private readonly float[] shaderAmplitudes = new float[MaxWaveCount];
        private readonly float[] shaderWavelengths = new float[MaxWaveCount];
        private readonly float[] shaderSpeeds = new float[MaxWaveCount];
        private readonly float[] shaderSteepness = new float[MaxWaveCount];

        private int activeWaveCount;

        public float WaterLevel
        {
            get => waterLevel;
            set
            {
                waterLevel = value;
                PushShaderGlobals();
            }
        }

        public IReadOnlyList<GerstnerWave> Waves => waves;
        public int ActiveWaveCount => activeWaveCount;

        public Vector3 CurrentVelocity
        {
            get
            {
                if (currentDirection.sqrMagnitude < 0.0001f || currentSpeed <= 0f)
                {
                    return Vector3.zero;
                }

                Vector2 direction = currentDirection.normalized;
                return new Vector3(direction.x, 0f, direction.y) * currentSpeed;
            }
        }

        private void Reset()
        {
            waterLevel = transform.position.y;
            waves = CreatePrototypeWaves();
            currentDirection = new Vector2(0.8f, 0.25f);
            currentSpeed = 0.18f;
            SanitizeWaves();
        }

        private void OnEnable()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Multiple WaterManager instances found. Disabling duplicate on {name}.", this);
                enabled = false;
                return;
            }

            Instance = this;
            SanitizeWaves();
            PushShaderGlobals();
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnValidate()
        {
            inverseIterations = Mathf.Clamp(inverseIterations, 1, 8);
            inverseRelaxation = Mathf.Clamp(inverseRelaxation, 0.5f, 1f);
            currentSpeed = Mathf.Max(0f, currentSpeed);
            SanitizeWaves();
            PushShaderGlobals();
        }

        private void Update()
        {
            if (pushShaderGlobals)
            {
                PushShaderGlobals();
            }
        }

        private void FixedUpdate()
        {
            if (pushShaderGlobals)
            {
                PushShaderGlobals();
            }
        }

        public static List<GerstnerWave> CreatePrototypeWaves()
        {
            return new List<GerstnerWave>
            {
                new GerstnerWave("Primary swell", new Vector2(0.86f, 0.28f), 1.05f, 42f, 5.6f, 0.58f),
                new GerstnerWave("Cross swell", new Vector2(0.22f, 0.98f), 0.55f, 23f, 4.1f, 0.44f),
                new GerstnerWave("Long roll", new Vector2(-0.54f, 0.84f), 0.42f, 58f, 6.2f, 0.35f),
                new GerstnerWave("Short chop", new Vector2(-0.9f, 0.18f), 0.18f, 11f, 3.8f, 0.26f)
            };
        }

        public void UsePrototypeWaves()
        {
            waves = CreatePrototypeWaves();
            SanitizeWaves();
            PushShaderGlobals();
        }

        public void ReplaceWaves(IEnumerable<GerstnerWave> replacementWaves)
        {
            waves.Clear();
            if (replacementWaves != null)
            {
                foreach (GerstnerWave wave in replacementWaves)
                {
                    if (waves.Count >= MaxWaveCount)
                    {
                        break;
                    }

                    waves.Add(wave);
                }
            }

            SanitizeWaves();
            PushShaderGlobals();
        }

        public float GetWaterHeight(Vector3 position)
        {
            return GetWaterSample(position).Height;
        }

        public Vector3 GetWaterNormal(Vector3 position)
        {
            return GetWaterSample(position).Normal;
        }

        public Vector3 GetWaterVelocity(Vector3 position)
        {
            return GetWaterSample(position).Velocity + CurrentVelocity;
        }

        public WaterSample GetWaterSample(Vector3 position)
        {
            return GetWaterSample(position, GetSimulationTime());
        }

        public WaterSample GetWaterSample(Vector3 position, float time)
        {
            Vector2 queryXZ = new Vector2(position.x, position.z);
            Vector2 sourceXZ = SolveSourcePosition(queryXZ, time);
            return EvaluateSurface(sourceXZ, time);
        }

        public Vector3 GetDisplacedSurfacePoint(Vector3 position)
        {
            return GetWaterSample(position).Position;
        }

        private float GetSimulationTime()
        {
            return Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        }

        private Vector2 SolveSourcePosition(Vector2 queryXZ, float time)
        {
            if (!useHorizontalDisplacement || activeWaveCount == 0)
            {
                return queryXZ;
            }

            Vector2 sourceXZ = queryXZ;
            for (int i = 0; i < inverseIterations; i++)
            {
                Vector3 displacement = EvaluateDisplacement(sourceXZ, time);
                Vector2 displacedXZ = sourceXZ + new Vector2(displacement.x, displacement.z);
                sourceXZ += (queryXZ - displacedXZ) * inverseRelaxation;
            }

            return sourceXZ;
        }

        private Vector3 EvaluateDisplacement(Vector2 sourceXZ, float time)
        {
            Vector3 displacement = Vector3.zero;
            int count = Mathf.Max(1, activeWaveCount);

            for (int i = 0; i < waves.Count; i++)
            {
                GerstnerWave wave = waves[i];
                if (!wave.IsUsable)
                {
                    continue;
                }

                Vector2 direction = wave.direction.normalized;
                float waveNumber = Mathf.PI * 2f / wave.wavelength;
                float phase = waveNumber * (Vector2.Dot(direction, sourceXZ) - wave.speed * time);
                float sin = Mathf.Sin(phase);
                float cos = Mathf.Cos(phase);
                float horizontalAmplitude = wave.steepness / (waveNumber * count);

                displacement.x += direction.x * horizontalAmplitude * cos;
                displacement.y += wave.amplitude * sin;
                displacement.z += direction.y * horizontalAmplitude * cos;
            }

            return displacement;
        }

        private WaterSample EvaluateSurface(Vector2 sourceXZ, float time)
        {
            Vector3 displacement = Vector3.zero;
            Vector3 velocity = Vector3.zero;
            Vector3 tangentX = Vector3.right;
            Vector3 tangentZ = Vector3.forward;
            float crest = 0f;
            float amplitudeSum = 0f;
            int count = Mathf.Max(1, activeWaveCount);

            for (int i = 0; i < waves.Count; i++)
            {
                GerstnerWave wave = waves[i];
                if (!wave.IsUsable)
                {
                    continue;
                }

                Vector2 direction = wave.direction.normalized;
                float waveNumber = Mathf.PI * 2f / wave.wavelength;
                float phase = waveNumber * (Vector2.Dot(direction, sourceXZ) - wave.speed * time);
                float sin = Mathf.Sin(phase);
                float cos = Mathf.Cos(phase);
                float steepnessShare = wave.steepness / count;
                float horizontalAmplitude = steepnessShare / waveNumber;
                float phaseVelocity = -waveNumber * wave.speed;

                displacement.x += direction.x * horizontalAmplitude * cos;
                displacement.y += wave.amplitude * sin;
                displacement.z += direction.y * horizontalAmplitude * cos;

                velocity.x += direction.x * horizontalAmplitude * -sin * phaseVelocity;
                velocity.y += wave.amplitude * cos * phaseVelocity;
                velocity.z += direction.y * horizontalAmplitude * -sin * phaseVelocity;

                tangentX.x += -direction.x * direction.x * steepnessShare * sin;
                tangentX.y += direction.x * wave.amplitude * waveNumber * cos;
                tangentX.z += -direction.x * direction.y * steepnessShare * sin;

                tangentZ.x += -direction.x * direction.y * steepnessShare * sin;
                tangentZ.y += direction.y * wave.amplitude * waveNumber * cos;
                tangentZ.z += -direction.y * direction.y * steepnessShare * sin;

                crest += (sin * 0.5f + 0.5f) * wave.amplitude;
                amplitudeSum += wave.amplitude;
            }

            Vector3 normal = Vector3.Cross(tangentZ, tangentX).normalized;
            if (normal.y < 0f)
            {
                normal = -normal;
            }

            float normalizedCrest = amplitudeSum > 0.0001f ? Mathf.Clamp01(crest / amplitudeSum) : 0f;
            Vector3 sourcePosition = new Vector3(sourceXZ.x, waterLevel, sourceXZ.y);
            Vector3 surfacePosition = sourcePosition + displacement;
            surfacePosition.y = waterLevel + displacement.y;

            return new WaterSample(surfacePosition, sourcePosition, normal, displacement, velocity, normalizedCrest);
        }

        private void SanitizeWaves()
        {
            if (waves == null)
            {
                waves = new List<GerstnerWave>();
            }

            if (waves.Count > MaxWaveCount)
            {
                waves.RemoveRange(MaxWaveCount, waves.Count - MaxWaveCount);
            }

            activeWaveCount = 0;
            for (int i = 0; i < waves.Count; i++)
            {
                GerstnerWave wave = waves[i];
                wave.Sanitize();
                waves[i] = wave;
                if (wave.IsUsable)
                {
                    activeWaveCount++;
                }
            }
        }

        private void PushShaderGlobals()
        {
            if (!pushShaderGlobals)
            {
                return;
            }

            SanitizeWaves();
            Array.Clear(shaderDirections, 0, shaderDirections.Length);
            Array.Clear(shaderAmplitudes, 0, shaderAmplitudes.Length);
            Array.Clear(shaderWavelengths, 0, shaderWavelengths.Length);
            Array.Clear(shaderSpeeds, 0, shaderSpeeds.Length);
            Array.Clear(shaderSteepness, 0, shaderSteepness.Length);

            int writeIndex = 0;
            for (int i = 0; i < waves.Count && writeIndex < MaxWaveCount; i++)
            {
                GerstnerWave wave = waves[i];
                if (!wave.IsUsable)
                {
                    continue;
                }

                Vector2 direction = wave.direction.normalized;
                shaderDirections[writeIndex] = new Vector4(direction.x, direction.y, 0f, 0f);
                shaderAmplitudes[writeIndex] = wave.amplitude;
                shaderWavelengths[writeIndex] = wave.wavelength;
                shaderSpeeds[writeIndex] = wave.speed;
                shaderSteepness[writeIndex] = wave.steepness;
                writeIndex++;
            }

            activeWaveCount = writeIndex;
            Shader.SetGlobalInt(WaterWaveCountId, activeWaveCount);
            Shader.SetGlobalFloat(WaterLevelId, waterLevel);
            Shader.SetGlobalFloat(OceanTimeId, GetSimulationTime());
            Shader.SetGlobalVectorArray(WaterWaveDirectionId, shaderDirections);
            Shader.SetGlobalFloatArray(WaterWaveAmplitudeId, shaderAmplitudes);
            Shader.SetGlobalFloatArray(WaterWaveWavelengthId, shaderWavelengths);
            Shader.SetGlobalFloatArray(WaterWaveSpeedId, shaderSpeeds);
            Shader.SetGlobalFloatArray(WaterWaveSteepnessId, shaderSteepness);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGrid)
            {
                return;
            }

            SanitizeWaves();
            float half = debugGridSize * 0.5f;
            Vector3 origin = transform.position;
            Gizmos.color = new Color(0.1f, 0.55f, 1f, 0.45f);

            for (float x = -half; x <= half + 0.001f; x += debugGridSpacing)
            {
                for (float z = -half; z <= half + 0.001f; z += debugGridSpacing)
                {
                    Vector3 query = new Vector3(origin.x + x, waterLevel, origin.z + z);
                    WaterSample sample = GetWaterSample(query);
                    Gizmos.DrawSphere(sample.Position, 0.08f);
                    Gizmos.DrawLine(sample.Position, sample.Position + sample.Normal * debugNormalLength);
                }
            }
        }
    }
}
