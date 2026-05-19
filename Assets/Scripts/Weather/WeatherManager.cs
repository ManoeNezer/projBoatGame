using System;
using System.Collections.Generic;
using BoatGame.Boat;
using BoatGame.Environment;
using BoatGame.Water;
using UnityEngine;

namespace BoatGame.Weather
{
    [DefaultExecutionOrder(-130)]
    [DisallowMultipleComponent]
    public sealed class WeatherManager : MonoBehaviour
    {
        [Serializable]
        private struct WeatherProfile
        {
            [Range(0f, 3f)] public float windMultiplier;
            public float windDirectionOffset;
            [Range(0.2f, 3f)] public float waveHeightMultiplier;
            [Range(0.4f, 2.2f)] public float waveFrequencyMultiplier;
            [Range(0.3f, 2.4f)] public float waveSpeedMultiplier;
            [Range(0f, 1f)] public float fogIntensity;
            [Range(0f, 1f)] public float rainIntensity;
            [Range(0f, 1f)] public float dangerIntensity;
            [Range(0.25f, 1.35f)] public float sailHandlingMultiplier;
            [Range(0.25f, 1.8f)] public float sailForceMultiplier;
            [Range(0.25f, 1.2f)] public float rudderMultiplier;
        }

        public static WeatherManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private WeatherState initialState = WeatherState.Calm;
        [SerializeField] private WeatherState targetState = WeatherState.Calm;
        [SerializeField, Min(0.1f)] private float transitionSharpness = 0.45f;
        [SerializeField] private bool autoCycleWeather = true;
        [SerializeField, Min(5f)] private float minStateDuration = 65f;
        [SerializeField, Min(5f)] private float maxStateDuration = 155f;

        [Header("Target")]
        [SerializeField] private Transform weatherTarget;
        [SerializeField] private bool autoFindBoat = true;

        [Header("Profiles")]
        [SerializeField] private WeatherProfile calm = new WeatherProfile
        {
            windMultiplier = 1f,
            waveHeightMultiplier = 1f,
            waveFrequencyMultiplier = 1f,
            waveSpeedMultiplier = 1f,
            sailHandlingMultiplier = 1f,
            sailForceMultiplier = 1f,
            rudderMultiplier = 1f
        };
        [SerializeField] private WeatherProfile strongWind = new WeatherProfile
        {
            windMultiplier = 1.45f,
            windDirectionOffset = 12f,
            waveHeightMultiplier = 1.18f,
            waveFrequencyMultiplier = 1.08f,
            waveSpeedMultiplier = 1.12f,
            fogIntensity = 0.05f,
            rainIntensity = 0.05f,
            dangerIntensity = 0.18f,
            sailHandlingMultiplier = 0.88f,
            sailForceMultiplier = 1.18f,
            rudderMultiplier = 0.95f
        };
        [SerializeField] private WeatherProfile rain = new WeatherProfile
        {
            windMultiplier = 1.18f,
            windDirectionOffset = -8f,
            waveHeightMultiplier = 1.08f,
            waveFrequencyMultiplier = 1.04f,
            waveSpeedMultiplier = 1.06f,
            fogIntensity = 0.22f,
            rainIntensity = 0.72f,
            dangerIntensity = 0.18f,
            sailHandlingMultiplier = 0.9f,
            sailForceMultiplier = 0.96f,
            rudderMultiplier = 0.98f
        };
        [SerializeField] private WeatherProfile fog = new WeatherProfile
        {
            windMultiplier = 0.72f,
            windDirectionOffset = 5f,
            waveHeightMultiplier = 0.92f,
            waveFrequencyMultiplier = 0.9f,
            waveSpeedMultiplier = 0.86f,
            fogIntensity = 0.82f,
            rainIntensity = 0.05f,
            dangerIntensity = 0.26f,
            sailHandlingMultiplier = 0.96f,
            sailForceMultiplier = 0.88f,
            rudderMultiplier = 1f
        };
        [SerializeField] private WeatherProfile storm = new WeatherProfile
        {
            windMultiplier = 2.25f,
            windDirectionOffset = 24f,
            waveHeightMultiplier = 1.95f,
            waveFrequencyMultiplier = 1.34f,
            waveSpeedMultiplier = 1.34f,
            fogIntensity = 0.66f,
            rainIntensity = 1f,
            dangerIntensity = 1f,
            sailHandlingMultiplier = 0.52f,
            sailForceMultiplier = 1.38f,
            rudderMultiplier = 0.78f
        };
        [SerializeField] private WeatherProfile dangerousSea = new WeatherProfile
        {
            windMultiplier = 1.62f,
            windDirectionOffset = -18f,
            waveHeightMultiplier = 2.2f,
            waveFrequencyMultiplier = 1.2f,
            waveSpeedMultiplier = 1.2f,
            fogIntensity = 0.28f,
            rainIntensity = 0.28f,
            dangerIntensity = 0.82f,
            sailHandlingMultiplier = 0.68f,
            sailForceMultiplier = 1.08f,
            rudderMultiplier = 0.86f
        };

        [Header("Feedback")]
        [SerializeField] private bool createRuntimeRain = true;
        [SerializeField] private bool createRuntimeAudio = true;
        [SerializeField, Min(0f)] private float baseFogDensity = 0.0038f;
        [SerializeField] private Color baseFogColor = new Color(0.42f, 0.62f, 0.68f);
        [SerializeField] private Color stormFogColor = new Color(0.18f, 0.22f, 0.25f);

        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;

        private readonly List<WaterManager.GerstnerWave> baseWaves = new List<WaterManager.GerstnerWave>(WaterManager.MaxWaveCount);
        private readonly List<WaterManager.GerstnerWave> workingWaves = new List<WaterManager.GerstnerWave>(WaterManager.MaxWaveCount);

        private WeatherProfile currentProfile;
        private float baseWindStrength = 8.5f;
        private float baseWindDirection;
        private float nextAutoChangeTime;
        private float stormZoneIntensity;
        private Vector3 stormPushAcceleration;
        private float windPulse01;
        private float windPulseTimer;
        private float windPulseDirectionOffset;
        private float wavePulse01;
        private float wavePulseTimer;
        private float fogPulse01;
        private float fogPulseTimer;
        private BoatHelmController boat;
        private Rigidbody boatBody;
        private ParticleSystem rainSystem;
        private AudioSource windAudio;
        private AudioSource rainAudio;
        private AudioSource creakAudio;

        public WeatherState TargetState => targetState;
        public float StormIntensity01 => stormZoneIntensity;
        public float Danger01 => Mathf.Clamp01(currentProfile.dangerIntensity + stormZoneIntensity * 0.65f + wavePulse01 * 0.35f);
        public float Rain01 => Mathf.Clamp01(currentProfile.rainIntensity);
        public float Fog01 => Mathf.Clamp01(Mathf.Max(currentProfile.fogIntensity, fogPulse01));
        public float WindMultiplier => currentProfile.windMultiplier + windPulse01 * 0.8f;
        public Vector3 StormPushAcceleration => stormPushAcceleration;

        private void OnEnable()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Multiple WeatherManager instances found. Disabling duplicate on {name}.", this);
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            targetState = initialState;
            currentProfile = GetProfile(targetState);
            CaptureBaseEnvironment();
            FindTargetIfNeeded();
            ScheduleNextWeather();
            EnsureRuntimeFeedback();
        }

        private void Update()
        {
            FindTargetIfNeeded();
            CaptureBaseEnvironmentIfNeeded();
            UpdateAutoCycle();
            UpdateStormInfluence();
            UpdateTransientPulses();
            UpdateProfile(Time.deltaTime);
            ApplyEnvironment();
            UpdateFeedback();
            ApplyBoatHandlingModifiers();
        }

        private void FixedUpdate()
        {
            if (boatBody != null && stormPushAcceleration.sqrMagnitude > 0.0001f)
            {
                boatBody.AddForce(stormPushAcceleration, ForceMode.Acceleration);
            }
        }

        public void SetWeather(WeatherState state)
        {
            targetState = state;
            ScheduleNextWeather();
        }

        public void AddWindGust(float intensity, float directionOffset, float duration)
        {
            windPulse01 = Mathf.Max(windPulse01, Mathf.Clamp01(intensity));
            windPulseDirectionOffset = directionOffset;
            windPulseTimer = Mathf.Max(windPulseTimer, Mathf.Max(0.1f, duration));
        }

        public void AddWavePulse(float intensity, float duration)
        {
            wavePulse01 = Mathf.Max(wavePulse01, Mathf.Clamp01(intensity));
            wavePulseTimer = Mathf.Max(wavePulseTimer, Mathf.Max(0.1f, duration));
        }

        public void AddFogPulse(float intensity, float duration)
        {
            fogPulse01 = Mathf.Max(fogPulse01, Mathf.Clamp01(intensity));
            fogPulseTimer = Mathf.Max(fogPulseTimer, Mathf.Max(0.1f, duration));
        }

        private void CaptureBaseEnvironment()
        {
            WindManager wind = WindManager.Instance;
            if (wind != null)
            {
                baseWindStrength = Mathf.Max(0.1f, wind.BaseStrength);
                baseWindDirection = wind.DirectionDegrees;
            }

            baseFogDensity = RenderSettings.fogDensity > 0f ? RenderSettings.fogDensity : baseFogDensity;
            baseFogColor = RenderSettings.fogColor;
            CaptureBaseWaves();
        }

        private void CaptureBaseEnvironmentIfNeeded()
        {
            if (baseWaves.Count == 0)
            {
                CaptureBaseWaves();
            }
        }

        private void CaptureBaseWaves()
        {
            WaterManager water = WaterManager.Instance;
            if (water == null)
            {
                return;
            }

            baseWaves.Clear();
            IReadOnlyList<WaterManager.GerstnerWave> waves = water.Waves;
            for (int i = 0; i < waves.Count && i < WaterManager.MaxWaveCount; i++)
            {
                baseWaves.Add(waves[i]);
            }
        }

        private void FindTargetIfNeeded()
        {
            if (!autoFindBoat && weatherTarget != null)
            {
                return;
            }

            if (boat == null)
            {
                boat = FindFirstObjectByType<BoatHelmController>();
                if (boat != null)
                {
                    boatBody = boat.Body;
                }
            }

            if (weatherTarget == null && boat != null)
            {
                weatherTarget = boat.transform;
            }

            if (weatherTarget == null && Camera.main != null)
            {
                weatherTarget = Camera.main.transform;
            }
        }

        private void UpdateAutoCycle()
        {
            if (!autoCycleWeather || Time.time < nextAutoChangeTime)
            {
                return;
            }

            int stateCount = Enum.GetValues(typeof(WeatherState)).Length;
            WeatherState next = targetState;
            for (int i = 0; i < 5 && next == targetState; i++)
            {
                next = (WeatherState)UnityEngine.Random.Range(0, stateCount);
            }

            SetWeather(next);
        }

        private void ScheduleNextWeather()
        {
            float minDuration = Mathf.Min(minStateDuration, maxStateDuration);
            float maxDuration = Mathf.Max(minStateDuration, maxStateDuration);
            nextAutoChangeTime = Time.time + UnityEngine.Random.Range(minDuration, maxDuration);
        }

        private void UpdateStormInfluence()
        {
            if (weatherTarget == null)
            {
                stormZoneIntensity = 0f;
                stormPushAcceleration = Vector3.zero;
                return;
            }

            stormZoneIntensity = StormZone.SampleCombinedIntensity(weatherTarget.position, out stormPushAcceleration);
        }

        private void UpdateTransientPulses()
        {
            DecayPulse(ref windPulse01, ref windPulseTimer);
            DecayPulse(ref wavePulse01, ref wavePulseTimer);
            DecayPulse(ref fogPulse01, ref fogPulseTimer);
        }

        private static void DecayPulse(ref float value, ref float timer)
        {
            if (timer <= 0f)
            {
                value = Mathf.MoveTowards(value, 0f, Time.deltaTime * 0.65f);
                return;
            }

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                value = Mathf.Min(value, 0.65f);
            }
        }

        private void UpdateProfile(float dt)
        {
            WeatherProfile desired = GetProfile(targetState);
            WeatherProfile stormDesired = storm;
            desired = LerpProfile(desired, stormDesired, stormZoneIntensity);

            float blend = 1f - Mathf.Exp(-transitionSharpness * dt);
            currentProfile = LerpProfile(currentProfile, desired, blend);
        }

        private void ApplyEnvironment()
        {
            WindManager wind = WindManager.Instance;
            if (wind != null)
            {
                wind.BaseStrength = baseWindStrength * Mathf.Max(0.05f, currentProfile.windMultiplier + windPulse01 * 0.85f);
                wind.DirectionDegrees = baseWindDirection + currentProfile.windDirectionOffset + windPulseDirectionOffset * windPulse01;
            }

            ApplyWaterProfile();

            float fog = Fog01;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = Mathf.Lerp(baseFogDensity, 0.024f, fog);
            RenderSettings.fogColor = Color.Lerp(baseFogColor, stormFogColor, Mathf.Clamp01(fog * 0.85f + Danger01 * 0.25f));
        }

        private void ApplyWaterProfile()
        {
            WaterManager water = WaterManager.Instance;
            if (water == null || baseWaves.Count == 0)
            {
                return;
            }

            workingWaves.Clear();
            float heightMultiplier = Mathf.Max(0.05f, currentProfile.waveHeightMultiplier + wavePulse01 * 0.75f);
            float frequencyMultiplier = Mathf.Max(0.1f, currentProfile.waveFrequencyMultiplier + wavePulse01 * 0.28f);
            float speedMultiplier = Mathf.Max(0.1f, currentProfile.waveSpeedMultiplier + wavePulse01 * 0.18f);

            for (int i = 0; i < baseWaves.Count && i < WaterManager.MaxWaveCount; i++)
            {
                WaterManager.GerstnerWave wave = baseWaves[i];
                wave.amplitude *= heightMultiplier;
                wave.wavelength = Mathf.Max(2f, wave.wavelength / frequencyMultiplier);
                wave.speed *= speedMultiplier;
                wave.steepness = Mathf.Clamp01(wave.steepness * Mathf.Lerp(1f, 1.22f, Danger01));
                workingWaves.Add(wave);
            }

            water.ReplaceWaves(workingWaves);
        }

        private void ApplyBoatHandlingModifiers()
        {
            if (boat == null)
            {
                return;
            }

            boat.SetWeatherModifiers(
                currentProfile.sailForceMultiplier,
                currentProfile.rudderMultiplier,
                currentProfile.sailHandlingMultiplier,
                Mathf.Clamp01(Danger01 * 0.85f + currentProfile.rainIntensity * 0.15f));
        }

        private WeatherProfile GetProfile(WeatherState state)
        {
            switch (state)
            {
                case WeatherState.StrongWind:
                    return strongWind;
                case WeatherState.Rain:
                    return rain;
                case WeatherState.Fog:
                    return fog;
                case WeatherState.Storm:
                    return storm;
                case WeatherState.DangerousSea:
                    return dangerousSea;
                default:
                    return calm;
            }
        }

        private static WeatherProfile LerpProfile(WeatherProfile a, WeatherProfile b, float t)
        {
            t = Mathf.Clamp01(t);
            return new WeatherProfile
            {
                windMultiplier = Mathf.Lerp(a.windMultiplier, b.windMultiplier, t),
                windDirectionOffset = Mathf.LerpAngle(a.windDirectionOffset, b.windDirectionOffset, t),
                waveHeightMultiplier = Mathf.Lerp(a.waveHeightMultiplier, b.waveHeightMultiplier, t),
                waveFrequencyMultiplier = Mathf.Lerp(a.waveFrequencyMultiplier, b.waveFrequencyMultiplier, t),
                waveSpeedMultiplier = Mathf.Lerp(a.waveSpeedMultiplier, b.waveSpeedMultiplier, t),
                fogIntensity = Mathf.Lerp(a.fogIntensity, b.fogIntensity, t),
                rainIntensity = Mathf.Lerp(a.rainIntensity, b.rainIntensity, t),
                dangerIntensity = Mathf.Lerp(a.dangerIntensity, b.dangerIntensity, t),
                sailHandlingMultiplier = Mathf.Lerp(a.sailHandlingMultiplier, b.sailHandlingMultiplier, t),
                sailForceMultiplier = Mathf.Lerp(a.sailForceMultiplier, b.sailForceMultiplier, t),
                rudderMultiplier = Mathf.Lerp(a.rudderMultiplier, b.rudderMultiplier, t)
            };
        }

        private void EnsureRuntimeFeedback()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (createRuntimeRain && rainSystem == null)
            {
                GameObject rainObject = new GameObject("RuntimeRain");
                rainObject.transform.SetParent(transform, false);
                rainSystem = rainObject.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = rainSystem.main;
                main.loop = true;
                main.startLifetime = 1.2f;
                main.startSpeed = 22f;
                main.startSize = 0.025f;
                main.maxParticles = 1400;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                ParticleSystem.ShapeModule shape = rainSystem.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(36f, 2f, 36f);
                ParticleSystem.EmissionModule emission = rainSystem.emission;
                emission.rateOverTime = 0f;
                rainSystem.Play();
            }

            if (createRuntimeAudio && windAudio == null)
            {
                GameObject audioObject = new GameObject("WeatherAudio");
                audioObject.transform.SetParent(transform, false);
                windAudio = CreateLoopSource(audioObject, "WindLoop", CreateNoiseClip("ProceduralWind", 1.8f, 0.35f), 0.25f, 0.75f);
                rainAudio = CreateLoopSource(audioObject, "RainLoop", CreateNoiseClip("ProceduralRain", 0.9f, 0.72f), 0f, 1.35f);
                creakAudio = CreateLoopSource(audioObject, "HullCreakLoop", CreateCreakClip(), 0f, 0.55f);
            }
        }

        private void UpdateFeedback()
        {
            EnsureRuntimeFeedback();

            if (rainSystem != null)
            {
                if (weatherTarget != null)
                {
                    rainSystem.transform.position = weatherTarget.position + Vector3.up * 16f;
                }

                ParticleSystem.EmissionModule emission = rainSystem.emission;
                emission.rateOverTime = Mathf.Lerp(0f, 850f, Rain01);
            }

            if (windAudio != null)
            {
                windAudio.volume = Mathf.Lerp(0.08f, 0.58f, Mathf.Clamp01(WindMultiplier / 2.4f));
                windAudio.pitch = Mathf.Lerp(0.75f, 1.28f, Danger01);
            }

            if (rainAudio != null)
            {
                rainAudio.volume = Mathf.Lerp(0f, 0.48f, Rain01);
            }

            if (creakAudio != null)
            {
                creakAudio.volume = Mathf.Lerp(0.02f, 0.28f, Danger01);
                creakAudio.pitch = Mathf.Lerp(0.7f, 1.05f, Danger01);
            }
        }

        private static AudioSource CreateLoopSource(GameObject root, string sourceName, AudioClip clip, float volume, float pitch)
        {
            AudioSource source = root.AddComponent<AudioSource>();
            source.name = sourceName;
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
            return source;
        }

        private static AudioClip CreateNoiseClip(string clipName, float seconds, float brightness)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * seconds);
            float[] samples = new float[sampleCount];
            float last = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float noise = UnityEngine.Random.Range(-1f, 1f);
                last = Mathf.Lerp(last, noise, brightness);
                samples[i] = last * 0.18f;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateCreakClip()
        {
            const int sampleRate = 22050;
            int sampleCount = sampleRate * 2;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin(t * Mathf.PI * 1.6f))), 4f);
                samples[i] = Mathf.Sin(t * 155f + Mathf.Sin(t * 31f) * 2.4f) * envelope * 0.1f;
            }

            AudioClip clip = AudioClip.Create("ProceduralHullCreak", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            Vector3 origin = weatherTarget != null ? weatherTarget.position : transform.position;
            Gizmos.color = Color.Lerp(new Color(0.4f, 0.8f, 1f, 0.5f), new Color(1f, 0.12f, 0.04f, 0.7f), Danger01);
            Gizmos.DrawWireSphere(origin, Mathf.Lerp(8f, 28f, Danger01));
            Gizmos.DrawLine(origin + Vector3.up * 2f, origin + Vector3.up * 2f + stormPushAcceleration * 6f);
        }
    }
}
