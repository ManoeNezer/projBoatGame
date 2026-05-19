using BoatGame.Weather;
using UnityEngine;

namespace BoatGame.Events
{
    public sealed class FogBankEvent : MaritimeEventBase
    {
        private float radius;
        private Vector3 drift;

        public override string DisplayName => "Banc de brouillard";

        protected override void OnBegin()
        {
            radius = Random.Range(48f, 82f);
            drift = Quaternion.Euler(0f, Random.Range(-30f, 30f), 0f) * (Target != null ? Target.forward : Vector3.forward);
            drift.y = 0f;
            drift = drift.normalized * Random.Range(1.2f, 2.6f);
        }

        protected override void OnTick(float deltaTime)
        {
            Origin += drift * deltaTime;
            if (Target == null)
            {
                return;
            }

            float distance = Vector3.Distance(new Vector3(Target.position.x, 0f, Target.position.z), new Vector3(Origin.x, 0f, Origin.z));
            float intensity = 1f - Mathf.InverseLerp(radius * 0.45f, radius, distance);
            if (intensity > 0f)
            {
                WeatherManager.Instance?.AddFogPulse(Mathf.Clamp01(intensity), 0.75f);
            }
        }

        public override void DrawGizmos()
        {
            Gizmos.color = new Color(0.72f, 0.82f, 0.86f, 0.35f);
            Gizmos.DrawWireSphere(Origin, radius);
        }
    }
}
