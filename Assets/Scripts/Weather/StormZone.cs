using System.Collections.Generic;
using UnityEngine;

namespace BoatGame.Weather
{
    [DisallowMultipleComponent]
    public sealed class StormZone : MonoBehaviour
    {
        private static readonly List<StormZone> ActiveZones = new List<StormZone>(16);

        [Header("Zone")]
        [SerializeField, Min(8f)] private float radius = 115f;
        [SerializeField, Min(0f)] private float innerRadius = 38f;
        [SerializeField, Range(0f, 1f)] private float intensity = 1f;

        [Header("Forces")]
        [SerializeField, Range(0f, 360f)] private float pushDirectionDegrees = 55f;
        [SerializeField, Min(0f)] private float boatPushAcceleration = 2.8f;
        [SerializeField, Min(0f)] private float swirlAcceleration = 0.65f;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color outerColor = new Color(0.18f, 0.22f, 0.28f, 0.36f);
        [SerializeField] private Color innerColor = new Color(0.55f, 0.12f, 0.08f, 0.42f);

        public float Radius => radius;
        public float Intensity => intensity;

        public Vector3 PushDirection
        {
            get
            {
                Quaternion rotation = Quaternion.Euler(0f, pushDirectionDegrees, 0f);
                return (rotation * Vector3.forward).normalized;
            }
        }

        private void OnEnable()
        {
            if (!ActiveZones.Contains(this))
            {
                ActiveZones.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveZones.Remove(this);
        }

        private void OnValidate()
        {
            radius = Mathf.Max(8f, radius);
            innerRadius = Mathf.Clamp(innerRadius, 0f, radius - 0.1f);
            intensity = Mathf.Clamp01(intensity);
            pushDirectionDegrees = Mathf.Repeat(pushDirectionDegrees, 360f);
            boatPushAcceleration = Mathf.Max(0f, boatPushAcceleration);
            swirlAcceleration = Mathf.Max(0f, swirlAcceleration);
        }

        public float GetIntensity(Vector3 worldPosition)
        {
            Vector3 delta = worldPosition - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance >= radius)
            {
                return 0f;
            }

            if (distance <= innerRadius)
            {
                return intensity;
            }

            float fade = 1f - Mathf.InverseLerp(innerRadius, radius, distance);
            return Mathf.SmoothStep(0f, 1f, fade) * intensity;
        }

        public Vector3 GetPushAcceleration(Vector3 worldPosition)
        {
            float zoneIntensity = GetIntensity(worldPosition);
            if (zoneIntensity <= 0.001f)
            {
                return Vector3.zero;
            }

            Vector3 radial = worldPosition - transform.position;
            radial.y = 0f;
            Vector3 tangent = radial.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, radial.normalized) : Vector3.zero;
            return (PushDirection * boatPushAcceleration + tangent * swirlAcceleration) * zoneIntensity;
        }

        public void Configure(float newRadius, float newInnerRadius, float newIntensity, float newPushDirectionDegrees, float newBoatPushAcceleration, float newSwirlAcceleration)
        {
            radius = Mathf.Max(8f, newRadius);
            innerRadius = Mathf.Clamp(newInnerRadius, 0f, radius - 0.1f);
            intensity = Mathf.Clamp01(newIntensity);
            pushDirectionDegrees = Mathf.Repeat(newPushDirectionDegrees, 360f);
            boatPushAcceleration = Mathf.Max(0f, newBoatPushAcceleration);
            swirlAcceleration = Mathf.Max(0f, newSwirlAcceleration);
        }

        public static float SampleCombinedIntensity(Vector3 worldPosition, out Vector3 pushAcceleration)
        {
            float strongest = 0f;
            pushAcceleration = Vector3.zero;

            for (int i = ActiveZones.Count - 1; i >= 0; i--)
            {
                StormZone zone = ActiveZones[i];
                if (zone == null || !zone.isActiveAndEnabled)
                {
                    ActiveZones.RemoveAt(i);
                    continue;
                }

                float zoneIntensity = zone.GetIntensity(worldPosition);
                if (zoneIntensity <= 0f)
                {
                    continue;
                }

                strongest = Mathf.Max(strongest, zoneIntensity);
                pushAcceleration += zone.GetPushAcceleration(worldPosition);
            }

            return Mathf.Clamp01(strongest);
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = outerColor;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = innerColor;
            Gizmos.DrawWireSphere(transform.position, innerRadius);

            Vector3 origin = transform.position + Vector3.up * 2f;
            Vector3 push = PushDirection * Mathf.Min(radius * 0.45f, boatPushAcceleration * 14f + 8f);
            Gizmos.DrawLine(origin, origin + push);
        }
    }
}
