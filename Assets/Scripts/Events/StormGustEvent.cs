using BoatGame.Weather;
using UnityEngine;

namespace BoatGame.Events
{
    public sealed class StormGustEvent : MaritimeEventBase
    {
        private Vector3 gustDirection;
        private float strength;
        private float directionOffset;

        public override string DisplayName => "Rafale de vent";

        protected override void OnBegin()
        {
            float angle = Random.Range(-70f, 70f);
            directionOffset = angle;
            gustDirection = Quaternion.Euler(0f, angle, 0f) * (Target != null ? Target.forward : Vector3.forward);
            gustDirection.y = 0f;
            gustDirection.Normalize();
            strength = Random.Range(2.2f, 4.8f);
        }

        protected override void OnTick(float deltaTime)
        {
            float pulse = Bell01;
            WeatherManager.Instance?.AddWindGust(pulse, directionOffset, 0.65f);
            if (TargetBody != null)
            {
                TargetBody.AddForce(gustDirection * (strength * pulse), ForceMode.Acceleration);
                TargetBody.AddTorque(Vector3.up * (Vector3.Dot(gustDirection, Target.right) * strength * 0.55f * pulse), ForceMode.Acceleration);
            }
        }

        public override void DrawGizmos()
        {
            Gizmos.color = new Color(0.65f, 0.95f, 1f, 0.65f);
            Gizmos.DrawLine(Origin + Vector3.up * 3f, Origin + Vector3.up * 3f + gustDirection * (strength * 7f));
        }
    }
}
