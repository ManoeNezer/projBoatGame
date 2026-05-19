using UnityEngine;

namespace BoatGame.Environment
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class WindManager : MonoBehaviour
    {
        public static WindManager Instance { get; private set; }

        [Header("Wind")]
        [SerializeField, Range(0f, 360f)] private float directionDegrees = 35f;
        [SerializeField, Min(0f)] private float strength = 8.5f;
        [SerializeField, Range(0f, 0.5f)] private float gustAmount = 0.12f;
        [SerializeField, Min(0.01f)] private float gustFrequency = 0.08f;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField, Min(2f)] private float gizmoLength = 12f;
        [SerializeField] private Color gizmoColor = new Color(0.55f, 0.9f, 1f, 0.9f);

        public float DirectionDegrees
        {
            get => directionDegrees;
            set => directionDegrees = Mathf.Repeat(value, 360f);
        }

        public float BaseStrength
        {
            get => strength;
            set => strength = Mathf.Max(0f, value);
        }

        public Vector3 WindDirection
        {
            get
            {
                Quaternion rotation = Quaternion.Euler(0f, directionDegrees, 0f);
                return (rotation * Vector3.forward).normalized;
            }
        }

        public float CurrentStrength
        {
            get
            {
                float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
                float gust = Mathf.Sin(time * gustFrequency * Mathf.PI * 2f) * gustAmount;
                float secondaryGust = Mathf.Sin(time * gustFrequency * 1.73f + 2.1f) * gustAmount * 0.35f;
                return strength * Mathf.Max(0f, 1f + gust + secondaryGust);
            }
        }

        public Vector3 WindVelocity => WindDirection * CurrentStrength;

        private void OnEnable()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Multiple WindManager instances found. Disabling duplicate on {name}.", this);
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

        private void OnValidate()
        {
            directionDegrees = Mathf.Repeat(directionDegrees, 360f);
            strength = Mathf.Max(0f, strength);
            gustFrequency = Mathf.Max(0.01f, gustFrequency);
        }

        public Vector3 GetWindVelocity(Vector3 position)
        {
            return WindVelocity;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            Vector3 origin = transform.position + Vector3.up * 3f;
            Vector3 direction = WindDirection;
            Vector3 end = origin + direction * gizmoLength;

            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(origin, end);
            DrawArrowHead(end, direction, gizmoLength * 0.16f);
        }

        private void DrawArrowHead(Vector3 tip, Vector3 direction, float size)
        {
            Quaternion left = Quaternion.AngleAxis(155f, Vector3.up);
            Quaternion right = Quaternion.AngleAxis(-155f, Vector3.up);
            Gizmos.DrawLine(tip, tip + left * direction * size);
            Gizmos.DrawLine(tip, tip + right * direction * size);
        }
    }
}
