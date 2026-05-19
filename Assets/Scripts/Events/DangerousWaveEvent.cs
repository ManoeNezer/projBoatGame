using BoatGame.Damage;
using BoatGame.Weather;
using UnityEngine;

namespace BoatGame.Events
{
    public sealed class DangerousWaveEvent : MaritimeEventBase
    {
        private Vector3 direction;
        private Vector3 currentCenter;
        private float speed;
        private float width;
        private float force;
        private bool hasHit;

        public override string DisplayName => "Vague dangereuse";

        protected override void OnBegin()
        {
            direction = Target != null ? -Target.forward : Vector3.back;
            direction = Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f) * direction;
            direction.y = 0f;
            direction.Normalize();
            currentCenter = Origin + direction * 85f;
            speed = Random.Range(16f, 24f);
            width = Random.Range(18f, 30f);
            force = Random.Range(6f, 10f);
            hasHit = false;
        }

        protected override void OnTick(float deltaTime)
        {
            currentCenter -= direction * (speed * deltaTime);
            WeatherManager.Instance?.AddWavePulse(Bell01, 0.45f);

            if (Target == null || TargetBody == null)
            {
                return;
            }

            Vector3 toBoat = Target.position - currentCenter;
            float along = Vector3.Dot(toBoat, direction);
            float side = Vector3.ProjectOnPlane(toBoat - direction * along, Vector3.up).magnitude;
            if (!hasHit && Mathf.Abs(along) < 5.5f && side < width)
            {
                hasHit = true;
                TargetBody.AddForce((direction * force + Vector3.up * (force * 0.45f)), ForceMode.VelocityChange);
                BoatDamageSystem damage = Target.GetComponent<BoatDamageSystem>();
                if (damage != null)
                {
                    damage.ApplyDamage(BoatPartType.Hull, 0.045f, true);
                    damage.ApplyDamage(BoatPartType.Mast, 0.018f, false);
                }
            }
        }

        public override void DrawGizmos()
        {
            Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.55f);
            Gizmos.DrawWireCube(currentCenter + Vector3.up * 0.8f, new Vector3(width * 2f, 2f, 5f));
            Gizmos.DrawLine(currentCenter + Vector3.up * 1.6f, currentCenter + Vector3.up * 1.6f - direction * 18f);
        }
    }
}
