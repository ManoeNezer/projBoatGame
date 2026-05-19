using BoatGame.Water;
using UnityEngine;

namespace BoatGame.World
{
    [DisallowMultipleComponent]
    public sealed class OceanSurfaceFollower : MonoBehaviour
    {
        [SerializeField] private float heightOffset = 0.18f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobFrequency = 0.45f;
        [SerializeField] private bool alignToWaterNormal = true;
        [SerializeField] private Vector2 driftDirection;
        [SerializeField, Min(0f)] private float driftSpeed;

        private Vector3 anchorPosition;
        private float phaseOffset;

        private void Awake()
        {
            anchorPosition = transform.position;
            phaseOffset = WorldRandom.Value01(transform.GetInstanceID(), Vector2Int.zero, 73) * Mathf.PI * 2f;
            if (driftDirection.sqrMagnitude > 0.001f)
            {
                driftDirection.Normalize();
            }
        }

        private void LateUpdate()
        {
            WaterManager water = WaterManager.Instance;
            if (water == null)
            {
                return;
            }

            float time = Time.time;
            Vector3 drift = new Vector3(driftDirection.x, 0f, driftDirection.y) * (driftSpeed * time);
            Vector3 query = anchorPosition + drift;
            WaterManager.WaterSample sample = water.GetWaterSample(query);
            float bob = Mathf.Sin(time * bobFrequency * Mathf.PI * 2f + phaseOffset) * bobAmplitude;
            transform.position = sample.Position + Vector3.up * (heightOffset + bob);

            if (alignToWaterNormal)
            {
                Quaternion normalRotation = Quaternion.FromToRotation(transform.up, sample.Normal) * transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, normalRotation, Time.deltaTime * 4f);
            }
        }

        public void Configure(float offset, float amplitude, float frequency, Vector2 drift, float speed, bool align)
        {
            heightOffset = offset;
            bobAmplitude = amplitude;
            bobFrequency = frequency;
            driftDirection = drift.sqrMagnitude > 0.001f ? drift.normalized : Vector2.zero;
            driftSpeed = Mathf.Max(0f, speed);
            alignToWaterNormal = align;
            anchorPosition = transform.position;
        }
    }
}
