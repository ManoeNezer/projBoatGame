using BoatGame.Boat;
using BoatGame.Physics;
using BoatGame.Water;
using BoatGame.Weather;
using UnityEngine;

namespace BoatGame.Damage
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class BoatDamageSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoatHelmController helmController;
        [SerializeField] private FloatingObject floatingObject;
        [SerializeField] private RepairResource repairResource;

        [Header("Integrity")]
        [SerializeField, Range(0f, 1f)] private float hullIntegrity = 1f;
        [SerializeField, Range(0f, 1f)] private float sailIntegrity = 1f;
        [SerializeField, Range(0f, 1f)] private float rudderIntegrity = 1f;
        [SerializeField, Range(0f, 1f)] private float mastIntegrity = 1f;

        [Header("Impact Damage")]
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 3.1f;
        [SerializeField, Min(0f)] private float severeImpactSpeed = 10.5f;
        [SerializeField, Min(0f)] private float impactDamageScale = 0.28f;
        [SerializeField, Min(0f)] private float worldImpactMultiplier = 1.35f;

        [Header("Sea Damage")]
        [SerializeField, Min(0f)] private float stormDamageInterval = 2.3f;
        [SerializeField, Min(0f)] private float stormRigDamageRate = 0.028f;
        [SerializeField, Min(0f)] private float dangerousWaveDamage = 0.035f;
        [SerializeField, Min(0f)] private float waveDamageCooldown = 1.6f;

        [Header("Water Intake")]
        [SerializeField, Range(0f, 1f)] private float internalWater01;
        [SerializeField, Min(0f)] private float leakRate = 0.045f;
        [SerializeField, Min(0f)] private float bilgeDrainRate = 0.025f;
        [SerializeField, Min(0f)] private float waterMassAtFull = 1700f;
        [SerializeField, Min(0f)] private float sinkingDownforceAtFull = 18f;

        [Header("Feedback")]
        [SerializeField] private AudioSource damageAudio;
        [SerializeField] private AudioSource leakAudio;
        [SerializeField] private bool drawGizmos = true;

        private Rigidbody body;
        private float dryMass;
        private float nextStormDamageTime;
        private float nextWaveDamageTime;
        private int worldLayer;
        private float upgradeHullDamageReduction;
        private float upgradeLeakReduction;

        public float HullIntegrity => hullIntegrity;
        public float SailIntegrity => sailIntegrity;
        public float RudderIntegrity => rudderIntegrity;
        public float MastIntegrity => mastIntegrity;
        public float InternalWater01 => internalWater01;
        public RepairResource RepairResource => repairResource;
        public bool IsSinking => internalWater01 >= 0.98f && hullIntegrity < 0.35f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            dryMass = body.mass;
            if (helmController == null)
            {
                helmController = GetComponent<BoatHelmController>();
            }

            if (floatingObject == null)
            {
                floatingObject = GetComponent<FloatingObject>();
            }

            if (repairResource == null)
            {
                repairResource = GetComponent<RepairResource>();
            }

            worldLayer = LayerMask.NameToLayer("World");
        }

        private void Start()
        {
            EnsureRuntimeAudio();
        }

        private void OnValidate()
        {
            hullIntegrity = Mathf.Clamp01(hullIntegrity);
            sailIntegrity = Mathf.Clamp01(sailIntegrity);
            rudderIntegrity = Mathf.Clamp01(rudderIntegrity);
            mastIntegrity = Mathf.Clamp01(mastIntegrity);
            internalWater01 = Mathf.Clamp01(internalWater01);
            minimumImpactSpeed = Mathf.Max(0f, minimumImpactSpeed);
            severeImpactSpeed = Mathf.Max(minimumImpactSpeed + 0.1f, severeImpactSpeed);
            waterMassAtFull = Mathf.Max(0f, waterMassAtFull);
        }

        private void FixedUpdate()
        {
            UpdateSeaDamage();
            UpdateWaterIntake();
            ApplyDamageModifiers();
            UpdateLeakAudio();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.contactCount == 0)
            {
                return;
            }

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < minimumImpactSpeed)
            {
                return;
            }

            float severity = Mathf.InverseLerp(minimumImpactSpeed, severeImpactSpeed, impactSpeed);
            float damage = severity * severity * impactDamageScale;
            if (worldLayer >= 0 && collision.gameObject.layer == worldLayer)
            {
                damage *= worldImpactMultiplier;
            }

            ContactPoint contact = collision.GetContact(0);
            BoatPartType part = ResolveHitPart(contact.point);
            ApplyDamage(part, damage, true);

            if (damage > 0.08f)
            {
                ApplyDamage(BoatPartType.Hull, damage * 0.35f, false);
            }
        }

        public float GetIntegrity(BoatPartType part)
        {
            switch (part)
            {
                case BoatPartType.Sail:
                    return sailIntegrity;
                case BoatPartType.Rudder:
                    return rudderIntegrity;
                case BoatPartType.Mast:
                    return mastIntegrity;
                default:
                    return hullIntegrity;
            }
        }

        public void ApplyDamage(BoatPartType part, float amount, bool playFeedback)
        {
            amount = Mathf.Max(0f, amount);
            if (amount <= 0f)
            {
                return;
            }

            switch (part)
            {
                case BoatPartType.Sail:
                    sailIntegrity = Mathf.Clamp01(sailIntegrity - amount);
                    break;
                case BoatPartType.Rudder:
                    rudderIntegrity = Mathf.Clamp01(rudderIntegrity - amount);
                    break;
                case BoatPartType.Mast:
                    mastIntegrity = Mathf.Clamp01(mastIntegrity - amount);
                    break;
                default:
                    amount *= 1f - Mathf.Clamp01(upgradeHullDamageReduction);
                    hullIntegrity = Mathf.Clamp01(hullIntegrity - amount);
                    break;
            }

            if (playFeedback && damageAudio != null)
            {
                damageAudio.Play();
            }
        }

        public void Repair(BoatPartType part, float amount)
        {
            amount = Mathf.Max(0f, amount);
            switch (part)
            {
                case BoatPartType.Sail:
                    sailIntegrity = Mathf.Clamp01(sailIntegrity + amount);
                    break;
                case BoatPartType.Rudder:
                    rudderIntegrity = Mathf.Clamp01(rudderIntegrity + amount);
                    break;
                case BoatPartType.Mast:
                    mastIntegrity = Mathf.Clamp01(mastIntegrity + amount);
                    break;
                default:
                    hullIntegrity = Mathf.Clamp01(hullIntegrity + amount);
                    if (hullIntegrity > 0.92f)
                    {
                        internalWater01 = Mathf.MoveTowards(internalWater01, 0f, amount * 0.5f);
                    }
                    break;
            }
        }

        public void DrainInternalWater(float amount01)
        {
            internalWater01 = Mathf.Clamp01(internalWater01 - Mathf.Max(0f, amount01));
        }

        public void SetUpgradeModifiers(float hullDamageReduction, float leakReduction)
        {
            upgradeHullDamageReduction = Mathf.Clamp01(hullDamageReduction);
            upgradeLeakReduction = Mathf.Clamp01(leakReduction);
        }

        public void Configure(BoatHelmController helm, FloatingObject floating, RepairResource resources)
        {
            helmController = helm;
            floatingObject = floating;
            repairResource = resources;
        }

        private void UpdateSeaDamage()
        {
            WeatherManager weather = WeatherManager.Instance;
            if (weather != null && weather.Danger01 > 0.72f && Time.time >= nextStormDamageTime)
            {
                nextStormDamageTime = Time.time + stormDamageInterval;
                float rigDamage = stormRigDamageRate * Mathf.InverseLerp(0.72f, 1f, weather.Danger01);
                ApplyDamage(BoatPartType.Sail, rigDamage * Mathf.Lerp(0.35f, 1f, helmController != null ? helmController.SailOpen01 : 0.75f), false);
                ApplyDamage(BoatPartType.Mast, rigDamage * 0.65f, false);
                ApplyDamage(BoatPartType.Rudder, rigDamage * 0.28f, false);
            }

            WaterManager water = WaterManager.Instance;
            if (water == null || Time.time < nextWaveDamageTime)
            {
                return;
            }

            WaterManager.WaterSample sample = water.GetWaterSample(transform.position);
            float verticalImpact = Mathf.Max(0f, -body.linearVelocity.y - sample.Velocity.y);
            float crestStress = Mathf.Clamp01((sample.Crest - 0.88f) / 0.12f);
            float weatherStress = weather != null ? weather.Danger01 : 0f;
            if (crestStress > 0.2f && verticalImpact > 1.3f && weatherStress > 0.35f)
            {
                nextWaveDamageTime = Time.time + waveDamageCooldown;
                float amount = dangerousWaveDamage * crestStress * Mathf.InverseLerp(1.3f, 4.5f, verticalImpact) * Mathf.Lerp(0.5f, 1.3f, weatherStress);
                ApplyDamage(BoatPartType.Hull, amount, true);
                ApplyDamage(BoatPartType.Mast, amount * 0.3f, false);
            }
        }

        private void UpdateWaterIntake()
        {
            float hullDamage = 1f - hullIntegrity;
            if (hullDamage > 0.02f)
            {
                float seaDanger = WeatherManager.Instance != null ? WeatherManager.Instance.Danger01 : 0f;
                internalWater01 += leakRate * (1f - upgradeLeakReduction) * hullDamage * hullDamage * Mathf.Lerp(1f, 2.4f, seaDanger) * Time.fixedDeltaTime;
            }
            else
            {
                internalWater01 -= bilgeDrainRate * Time.fixedDeltaTime;
            }

            internalWater01 = Mathf.Clamp01(internalWater01);
            body.mass = dryMass + internalWater01 * waterMassAtFull;

            if (floatingObject != null)
            {
                float buoyancyScale = Mathf.Lerp(1f, 0.58f, internalWater01);
                float dragScale = Mathf.Lerp(1f, 1.75f, internalWater01);
                floatingObject.SetExternalWaterModifiers(buoyancyScale, dragScale);
            }

            if (internalWater01 > 0.45f)
            {
                body.AddForce(Vector3.down * (sinkingDownforceAtFull * internalWater01), ForceMode.Acceleration);
            }
        }

        private void ApplyDamageModifiers()
        {
            if (helmController == null)
            {
                return;
            }

            float sailEfficiency = Mathf.Lerp(0.18f, 1f, sailIntegrity);
            float rudderEfficiency = Mathf.Lerp(0.22f, 1f, rudderIntegrity);
            float mastStability = Mathf.Lerp(0.18f, 1f, mastIntegrity);
            helmController.SetDamageModifiers(sailEfficiency, rudderEfficiency, mastStability);
        }

        private BoatPartType ResolveHitPart(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            if (local.y > 1.15f)
            {
                return Mathf.Abs(local.x) > 0.55f ? BoatPartType.Sail : BoatPartType.Mast;
            }

            if (local.z < -2.75f)
            {
                return BoatPartType.Rudder;
            }

            return BoatPartType.Hull;
        }

        private void UpdateLeakAudio()
        {
            EnsureRuntimeAudio();
            if (leakAudio == null)
            {
                return;
            }

            float leak = Mathf.Clamp01((1f - hullIntegrity) * 1.35f + internalWater01 * 0.45f);
            leakAudio.volume = Mathf.Lerp(0f, 0.42f, leak);
            if (leak > 0.02f && !leakAudio.isPlaying)
            {
                leakAudio.Play();
            }
            else if (leak <= 0.02f && leakAudio.isPlaying)
            {
                leakAudio.Stop();
            }
        }

        private void EnsureRuntimeAudio()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (damageAudio == null)
            {
                damageAudio = gameObject.AddComponent<AudioSource>();
                damageAudio.clip = CreateImpactClip();
                damageAudio.loop = false;
                damageAudio.playOnAwake = false;
                damageAudio.spatialBlend = 0.85f;
                damageAudio.volume = 0.65f;
            }

            if (leakAudio == null)
            {
                leakAudio = gameObject.AddComponent<AudioSource>();
                leakAudio.clip = CreateLeakClip();
                leakAudio.loop = true;
                leakAudio.playOnAwake = false;
                leakAudio.spatialBlend = 0.65f;
                leakAudio.volume = 0f;
            }
        }

        private static AudioClip CreateImpactClip()
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * 0.42f);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 8f);
                float wood = Mathf.Sin(t * 210f + Mathf.Sin(t * 61f) * 5f);
                float crack = Mathf.Sin(t * 860f) * Mathf.Exp(-t * 22f);
                samples[i] = (wood * 0.38f + crack * 0.22f) * envelope;
            }

            AudioClip clip = AudioClip.Create("ProceduralWoodImpact", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateLeakClip()
        {
            const int sampleRate = 22050;
            int sampleCount = sampleRate;
            float[] samples = new float[sampleCount];
            float filtered = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                filtered = Mathf.Lerp(filtered, Random.Range(-1f, 1f), 0.35f);
                samples[i] = filtered * 0.09f;
            }

            AudioClip clip = AudioClip.Create("ProceduralWaterLeak", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = Color.Lerp(Color.red, Color.green, hullIntegrity);
            Gizmos.DrawWireCube(transform.position + transform.up * 0.1f, new Vector3(3.2f, 1.1f, 7.2f));
            Gizmos.color = Color.Lerp(Color.red, Color.green, sailIntegrity);
            Gizmos.DrawWireSphere(transform.TransformPoint(new Vector3(0f, 1.85f, 0.55f)), 0.55f);
            Gizmos.color = Color.Lerp(Color.red, Color.green, rudderIntegrity);
            Gizmos.DrawWireSphere(transform.TransformPoint(new Vector3(0f, -0.35f, -3.55f)), 0.35f);
            Gizmos.color = Color.Lerp(Color.red, Color.green, mastIntegrity);
            Gizmos.DrawLine(transform.TransformPoint(new Vector3(0f, 0.2f, 0.45f)), transform.TransformPoint(new Vector3(0f, 3.2f, 0.45f)));
        }
    }
}
