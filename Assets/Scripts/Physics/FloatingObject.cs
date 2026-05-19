using System.Collections.Generic;
using BoatGame.Water;
using UnityEngine;

namespace BoatGame.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class FloatingObject : MonoBehaviour
    {
        [Header("Float Points")]
        [SerializeField] private List<Transform> floatPoints = new List<Transform>();
        [SerializeField, Min(0.05f)] private float pointSubmergeDepth = 0.85f;

        [Header("Buoyancy")]
        [SerializeField, Min(0f)] private float buoyancyMultiplier = 1.12f;
        [SerializeField, Min(0f)] private float waterDrag = 1.6f;
        [SerializeField, Min(0f)] private float verticalDamping = 3.8f;
        [SerializeField, Min(0f)] private float angularDamping = 0.75f;
        [SerializeField, Min(0f)] private float maxPointForce = 65000f;

        [Header("Mass Distribution")]
        [SerializeField] private bool overrideCenterOfMass = true;
        [SerializeField] private Vector3 localCenterOfMass = new Vector3(0f, -0.35f, 0f);

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField, Min(0.02f)] private float gizmoRadius = 0.12f;

        private Rigidbody body;
        private float lastWetness;
        private Vector3 lastCenterOfBuoyancy;
        private float externalBuoyancyMultiplier = 1f;
        private float externalWaterDragMultiplier = 1f;

        public float Wetness => lastWetness;
        public Vector3 CenterOfBuoyancy => lastCenterOfBuoyancy;
        public IReadOnlyList<Transform> FloatPoints => floatPoints;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ApplyCenterOfMass();
        }

        private void Reset()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 4f;
        }

        private void OnValidate()
        {
            pointSubmergeDepth = Mathf.Max(0.05f, pointSubmergeDepth);
            buoyancyMultiplier = Mathf.Max(0f, buoyancyMultiplier);
            waterDrag = Mathf.Max(0f, waterDrag);
            verticalDamping = Mathf.Max(0f, verticalDamping);
            angularDamping = Mathf.Max(0f, angularDamping);
            maxPointForce = Mathf.Max(0f, maxPointForce);

            if (!Application.isPlaying)
            {
                body = GetComponent<Rigidbody>();
                ApplyCenterOfMass();
            }
        }

        private void FixedUpdate()
        {
            WaterManager water = WaterManager.Instance;
            if (water == null)
            {
                lastWetness = 0f;
                return;
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            int pointCount = Mathf.Max(1, floatPoints.Count);
            float massPerPoint = body.mass / pointCount;
            float submergenceSum = 0f;
            Vector3 weightedBuoyancyCenter = Vector3.zero;

            for (int i = 0; i < pointCount; i++)
            {
                Transform point = floatPoints.Count > 0 ? floatPoints[i] : transform;
                if (point == null)
                {
                    continue;
                }

                Vector3 pointPosition = point.position;
                WaterManager.WaterSample sample = water.GetWaterSample(pointPosition);
                float depth = sample.Height - pointPosition.y;
                if (depth <= 0f)
                {
                    continue;
                }

                float submergence = Mathf.Clamp01(depth / pointSubmergeDepth);
                submergence = submergence * submergence * (3f - 2f * submergence);

                Vector3 buoyancyForce = -UnityEngine.Physics.gravity * (massPerPoint * buoyancyMultiplier * externalBuoyancyMultiplier * submergence);
                Vector3 waterVelocity = sample.Velocity + water.CurrentVelocity;
                Vector3 pointVelocity = body.GetPointVelocity(pointPosition);
                Vector3 relativeVelocity = pointVelocity - waterVelocity;
                Vector3 dragForce = -relativeVelocity * (waterDrag * externalWaterDragMultiplier * massPerPoint * submergence);
                Vector3 verticalDampingForce = -Vector3.Project(relativeVelocity, Vector3.up) * (verticalDamping * massPerPoint * submergence);
                Vector3 force = buoyancyForce + dragForce + verticalDampingForce;

                if (maxPointForce > 0f)
                {
                    force = Vector3.ClampMagnitude(force, maxPointForce);
                }

                body.AddForceAtPosition(force, pointPosition, ForceMode.Force);
                submergenceSum += submergence;
                weightedBuoyancyCenter += pointPosition * submergence;
            }

            lastWetness = Mathf.Clamp01(submergenceSum / pointCount);
            if (submergenceSum > 0.0001f)
            {
                lastCenterOfBuoyancy = weightedBuoyancyCenter / submergenceSum;
                body.AddTorque(-body.angularVelocity * (angularDamping * body.mass * lastWetness), ForceMode.Force);
            }
            else
            {
                lastCenterOfBuoyancy = transform.position;
            }
        }

        public void SetFloatPoints(IEnumerable<Transform> points)
        {
            floatPoints.Clear();
            if (points == null)
            {
                return;
            }

            foreach (Transform point in points)
            {
                if (point != null && !floatPoints.Contains(point))
                {
                    floatPoints.Add(point);
                }
            }
        }

        public void Configure(float submergeDepth, float buoyancy, float drag, float verticalDamp, float angularDamp, float forceLimit, Vector3 centerOfMass)
        {
            pointSubmergeDepth = Mathf.Max(0.05f, submergeDepth);
            buoyancyMultiplier = Mathf.Max(0f, buoyancy);
            waterDrag = Mathf.Max(0f, drag);
            verticalDamping = Mathf.Max(0f, verticalDamp);
            angularDamping = Mathf.Max(0f, angularDamp);
            maxPointForce = Mathf.Max(0f, forceLimit);
            localCenterOfMass = centerOfMass;
            overrideCenterOfMass = true;
            ApplyCenterOfMass();
        }

        public void SetExternalWaterModifiers(float buoyancyScale, float dragScale)
        {
            externalBuoyancyMultiplier = Mathf.Clamp(buoyancyScale, 0.25f, 2.5f);
            externalWaterDragMultiplier = Mathf.Clamp(dragScale, 0.25f, 4f);
        }

        private void ApplyCenterOfMass()
        {
            if (overrideCenterOfMass && body != null)
            {
                body.centerOfMass = localCenterOfMass;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            WaterManager water = WaterManager.Instance;
            int pointCount = Mathf.Max(1, floatPoints.Count);

            for (int i = 0; i < pointCount; i++)
            {
                Transform point = floatPoints.Count > 0 ? floatPoints[i] : transform;
                if (point == null)
                {
                    continue;
                }

                Vector3 position = point.position;
                float submergence = 0f;
                Vector3 surface = position;

                if (water != null)
                {
                    WaterManager.WaterSample sample = water.GetWaterSample(position);
                    surface = sample.Position;
                    submergence = Mathf.Clamp01((sample.Height - position.y) / pointSubmergeDepth);
                }

                Gizmos.color = Color.Lerp(new Color(0.9f, 0.2f, 0.1f, 0.85f), new Color(0.1f, 0.9f, 0.35f, 0.85f), submergence);
                Gizmos.DrawSphere(position, gizmoRadius);

                if (water != null)
                {
                    Gizmos.color = new Color(0.1f, 0.55f, 1f, 0.5f);
                    Gizmos.DrawLine(position, surface);
                }
            }

            if (Application.isPlaying && lastWetness > 0.001f)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.9f);
                Gizmos.DrawWireSphere(lastCenterOfBuoyancy, gizmoRadius * 1.5f);
            }
        }
    }
}
