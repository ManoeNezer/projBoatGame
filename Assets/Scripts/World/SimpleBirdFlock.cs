using UnityEngine;

namespace BoatGame.World
{
    [DisallowMultipleComponent]
    public sealed class SimpleBirdFlock : MonoBehaviour
    {
        [SerializeField, Min(2f)] private float radius = 26f;
        [SerializeField, Min(0.1f)] private float speed = 0.24f;
        [SerializeField, Min(0f)] private float heightVariation = 3f;

        private Transform[] birds;
        private float[] offsets;

        private void Awake()
        {
            CacheBirds();
        }

        private void OnEnable()
        {
            CacheBirds();
        }

        private void Update()
        {
            if (birds == null || birds.Length != transform.childCount)
            {
                CacheBirds();
            }

            if (birds.Length == 0)
            {
                return;
            }

            float time = Time.time * speed;
            for (int i = 0; i < birds.Length; i++)
            {
                Transform bird = birds[i];
                if (bird == null)
                {
                    continue;
                }

                float angle = time + offsets[i];
                Vector3 local = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle * 1.7f) * heightVariation, Mathf.Sin(angle) * radius * 0.68f);
                bird.localPosition = local;
                Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)).normalized;
                bird.localRotation = Quaternion.LookRotation(tangent, Vector3.up);
            }
        }

        private void CacheBirds()
        {
            int count = transform.childCount;
            birds = new Transform[count];
            offsets = new float[count];
            for (int i = 0; i < count; i++)
            {
                birds[i] = transform.GetChild(i);
                offsets[i] = i * Mathf.PI * 2f / Mathf.Max(1, count);
            }
        }

        public void Configure(float flockRadius, float flockSpeed, float verticalVariation)
        {
            radius = Mathf.Max(2f, flockRadius);
            speed = Mathf.Max(0.1f, flockSpeed);
            heightVariation = Mathf.Max(0f, verticalVariation);
        }
    }
}
