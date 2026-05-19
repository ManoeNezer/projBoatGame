using UnityEngine;

namespace BoatGame.Events
{
    public sealed class HiddenReefEvent : MaritimeEventBase
    {
        private readonly GameObject[] reefs = new GameObject[5];

        public override string DisplayName => "Recifs caches";

        protected override void OnBegin()
        {
            int worldLayer = LayerMask.NameToLayer("World");
            Vector3 right = Target != null ? Target.right : Vector3.right;
            Vector3 forward = Target != null ? Target.forward : Vector3.forward;
            int count = Random.Range(3, reefs.Length + 1);
            for (int i = 0; i < count; i++)
            {
                GameObject reef = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                reef.name = $"HiddenReef_{i + 1:00}";
                reef.layer = worldLayer >= 0 ? worldLayer : 0;
                reef.transform.position = Origin + right * Random.Range(-24f, 24f) + forward * Random.Range(-12f, 18f) + Vector3.down * Random.Range(0.45f, 0.95f);
                reef.transform.rotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
                reef.transform.localScale = new Vector3(Random.Range(1.5f, 3.2f), Random.Range(0.35f, 0.9f), Random.Range(1.5f, 3.2f));
                reefs[i] = reef;
            }
        }

        protected override void OnTick(float deltaTime)
        {
        }

        protected override void OnFinish()
        {
            for (int i = 0; i < reefs.Length; i++)
            {
                if (reefs[i] == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(reefs[i]);
                }
                else
                {
                    Object.DestroyImmediate(reefs[i]);
                }

                reefs[i] = null;
            }
        }

        public override void DrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.22f, 0.08f, 0.45f);
            Gizmos.DrawWireSphere(Origin, 28f);
        }
    }
}
