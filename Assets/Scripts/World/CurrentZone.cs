using UnityEngine;

namespace BoatGame.World
{
    [DisallowMultipleComponent]
    public sealed class CurrentZone : MonoBehaviour
    {
        [Header("Current")]
        [SerializeField, Min(1f)] private float radius = 35f;
        [SerializeField, Min(0f)] private float edgeFalloff = 14f;
        [SerializeField, Range(0f, 360f)] private float directionDegrees = 90f;
        [SerializeField, Min(0f)] private float strength = 2.4f;
        [SerializeField] private float verticalLift;
        [SerializeField] private LayerMask affectedLayers = ~0;

        [Header("Runtime")]
        [SerializeField] private bool active = true;
        [SerializeField, Min(4)] private int maxColliders = 48;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0.1f, 0.85f, 1f, 0.28f);

        private Collider[] overlapBuffer;
        private Rigidbody[] bodyBuffer;

        public float Radius => radius;
        public float Strength => strength;
        public Vector3 CurrentVector => Direction * strength;

        public Vector3 Direction
        {
            get
            {
                Quaternion rotation = Quaternion.Euler(0f, directionDegrees, 0f);
                return (rotation * Vector3.forward).normalized;
            }
        }

        private void Awake()
        {
            EnsureBuffers();
        }

        private void OnValidate()
        {
            radius = Mathf.Max(1f, radius);
            edgeFalloff = Mathf.Max(0f, edgeFalloff);
            directionDegrees = Mathf.Repeat(directionDegrees, 360f);
            strength = Mathf.Max(0f, strength);
            maxColliders = Mathf.Max(4, maxColliders);
        }

        private void FixedUpdate()
        {
            if (!active || strength <= 0f)
            {
                return;
            }

            EnsureBuffers();
            int colliderCount = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, radius, overlapBuffer, affectedLayers, QueryTriggerInteraction.Ignore);
            int bodyCount = 0;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider hit = overlapBuffer[i];
                if (hit == null)
                {
                    continue;
                }

                Rigidbody body = hit.attachedRigidbody;
                if (body == null || body.isKinematic || ContainsBody(bodyBuffer, bodyCount, body))
                {
                    continue;
                }

                if (bodyCount >= bodyBuffer.Length)
                {
                    break;
                }

                bodyBuffer[bodyCount++] = body;
            }

            Vector3 direction = Direction;
            for (int i = 0; i < bodyCount; i++)
            {
                Rigidbody body = bodyBuffer[i];
                bodyBuffer[i] = null;
                float intensity = GetIntensity(body.worldCenterOfMass);
                if (intensity <= 0.001f)
                {
                    continue;
                }

                Vector3 force = direction * (strength * intensity) + Vector3.up * (verticalLift * intensity);
                body.AddForce(force, ForceMode.Acceleration);
            }
        }

        public void Configure(float newRadius, float newDirectionDegrees, float newStrength, float newFalloff)
        {
            radius = Mathf.Max(1f, newRadius);
            directionDegrees = Mathf.Repeat(newDirectionDegrees, 360f);
            strength = Mathf.Max(0f, newStrength);
            edgeFalloff = Mathf.Max(0f, newFalloff);
        }

        public float GetIntensity(Vector3 worldPosition)
        {
            Vector3 flatDelta = worldPosition - transform.position;
            flatDelta.y = 0f;
            float distance = flatDelta.magnitude;
            if (distance >= radius)
            {
                return 0f;
            }

            float fadeStart = Mathf.Max(0f, radius - edgeFalloff);
            if (edgeFalloff <= 0.001f || distance <= fadeStart)
            {
                return 1f;
            }

            return 1f - Mathf.InverseLerp(fadeStart, radius, distance);
        }

        private void EnsureBuffers()
        {
            if (overlapBuffer == null || overlapBuffer.Length != maxColliders)
            {
                overlapBuffer = new Collider[maxColliders];
                bodyBuffer = new Rigidbody[maxColliders];
            }
        }

        private static bool ContainsBody(Rigidbody[] bodies, int count, Rigidbody body)
        {
            for (int i = 0; i < count; i++)
            {
                if (bodies[i] == body)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, radius);
            Vector3 arrowOrigin = transform.position + Vector3.up * 0.6f;
            Vector3 arrowEnd = arrowOrigin + Direction * Mathf.Min(radius * 0.65f, strength * 8f + 4f);
            Gizmos.DrawLine(arrowOrigin, arrowEnd);
            Gizmos.DrawLine(arrowEnd, arrowEnd + Quaternion.AngleAxis(155f, Vector3.up) * Direction * 4f);
            Gizmos.DrawLine(arrowEnd, arrowEnd + Quaternion.AngleAxis(-155f, Vector3.up) * Direction * 4f);
        }
    }
}
