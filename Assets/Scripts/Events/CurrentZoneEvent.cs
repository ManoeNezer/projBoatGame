using BoatGame.World;
using UnityEngine;

namespace BoatGame.Events
{
    public sealed class CurrentZoneEvent : MaritimeEventBase
    {
        private GameObject zoneObject;
        private CurrentZone zone;

        public override string DisplayName => "Courant marin";

        protected override void OnBegin()
        {
            zoneObject = new GameObject("EventCurrentZone");
            zoneObject.transform.position = Origin;
            zone = zoneObject.AddComponent<CurrentZone>();
            float direction = Target != null ? Target.eulerAngles.y + Random.Range(-90f, 90f) : Random.Range(0f, 360f);
            zone.Configure(Random.Range(44f, 72f), direction, Random.Range(2.0f, 4.4f), Random.Range(14f, 26f));
        }

        protected override void OnTick(float deltaTime)
        {
            if (zoneObject != null && Target != null)
            {
                zoneObject.transform.position += Target.forward * (deltaTime * 0.75f);
            }
        }

        protected override void OnFinish()
        {
            if (zoneObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(zoneObject);
            }
            else
            {
                Object.DestroyImmediate(zoneObject);
            }
        }

        public override void DrawGizmos()
        {
            if (zone == null)
            {
                Gizmos.color = new Color(0.1f, 0.85f, 1f, 0.35f);
                Gizmos.DrawWireSphere(Origin, 52f);
            }
        }
    }
}
