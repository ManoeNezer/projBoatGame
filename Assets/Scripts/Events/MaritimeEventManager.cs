using System.Collections.Generic;
using BoatGame.Boat;
using UnityEngine;

namespace BoatGame.Events
{
    [DefaultExecutionOrder(-20)]
    [DisallowMultipleComponent]
    public sealed class MaritimeEventManager : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform navigationTarget;
        [SerializeField] private bool autoFindBoat = true;

        [Header("Timing")]
        [SerializeField] private bool eventsEnabled = true;
        [SerializeField, Min(3f)] private float minDelay = 28f;
        [SerializeField, Min(3f)] private float maxDelay = 62f;
        [SerializeField, Range(1, 4)] private int maxConcurrentEvents = 2;
        [SerializeField, Min(8f)] private float defaultDuration = 18f;

        [Header("Placement")]
        [SerializeField, Min(10f)] private float spawnDistance = 95f;
        [SerializeField, Min(0f)] private float sideOffset = 48f;

        [Header("Weights")]
        [SerializeField, Min(0f)] private float gustWeight = 1.4f;
        [SerializeField, Min(0f)] private float waveWeight = 1.1f;
        [SerializeField, Min(0f)] private float fogWeight = 0.85f;
        [SerializeField, Min(0f)] private float currentWeight = 0.9f;
        [SerializeField, Min(0f)] private float reefWeight = 0.65f;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        private readonly List<MaritimeEventBase> activeEvents = new List<MaritimeEventBase>(4);
        private Rigidbody targetBody;
        private float nextEventTime;
        private string lastEventName = "Aucun";

        public int ActiveEventCount => activeEvents.Count;
        public string LastEventName => lastEventName;

        private void Start()
        {
            FindTargetIfNeeded();
            ScheduleNextEvent();
        }

        private void Update()
        {
            FindTargetIfNeeded();
            TickEvents();

            if (!eventsEnabled || navigationTarget == null || activeEvents.Count >= maxConcurrentEvents || Time.time < nextEventTime)
            {
                return;
            }

            SpawnRandomEvent();
            ScheduleNextEvent();
        }

        private void OnDisable()
        {
            for (int i = 0; i < activeEvents.Count; i++)
            {
                activeEvents[i].Finish();
            }

            activeEvents.Clear();
        }

        public void SpawnRandomEvent()
        {
            MaritimeEventBase maritimeEvent = CreateWeightedEvent();
            if (maritimeEvent == null || navigationTarget == null)
            {
                return;
            }

            Vector3 origin = GetSpawnOrigin(maritimeEvent);
            float duration = GetDuration(maritimeEvent);
            maritimeEvent.Begin(this, navigationTarget, targetBody, origin, duration);
            activeEvents.Add(maritimeEvent);
            lastEventName = maritimeEvent.DisplayName;
        }

        public void ForceGust()
        {
            ForceEvent(new StormGustEvent());
        }

        public void ForceDangerousWave()
        {
            ForceEvent(new DangerousWaveEvent());
        }

        public void ForceFogBank()
        {
            ForceEvent(new FogBankEvent());
        }

        public void ForceCurrentZone()
        {
            ForceEvent(new CurrentZoneEvent());
        }

        private void ForceEvent(MaritimeEventBase maritimeEvent)
        {
            if (navigationTarget == null)
            {
                FindTargetIfNeeded();
            }

            if (navigationTarget == null)
            {
                return;
            }

            maritimeEvent.Begin(this, navigationTarget, targetBody, GetSpawnOrigin(maritimeEvent), GetDuration(maritimeEvent));
            activeEvents.Add(maritimeEvent);
            lastEventName = maritimeEvent.DisplayName;
        }

        private void TickEvents()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                MaritimeEventBase maritimeEvent = activeEvents[i];
                maritimeEvent.Tick(Time.deltaTime);
                if (!maritimeEvent.IsFinished)
                {
                    continue;
                }

                maritimeEvent.Finish();
                activeEvents.RemoveAt(i);
            }
        }

        private void FindTargetIfNeeded()
        {
            if (!autoFindBoat && navigationTarget != null)
            {
                return;
            }

            if (navigationTarget != null && targetBody != null)
            {
                return;
            }

            BoatHelmController boat = FindFirstObjectByType<BoatHelmController>();
            if (boat != null)
            {
                navigationTarget = boat.transform;
                targetBody = boat.Body;
                return;
            }

            if (Camera.main != null)
            {
                navigationTarget = Camera.main.transform;
                targetBody = null;
            }
        }

        private void ScheduleNextEvent()
        {
            float min = Mathf.Min(minDelay, maxDelay);
            float max = Mathf.Max(minDelay, maxDelay);
            nextEventTime = Time.time + Random.Range(min, max);
        }

        private MaritimeEventBase CreateWeightedEvent()
        {
            float total = gustWeight + waveWeight + fogWeight + currentWeight + reefWeight;
            if (total <= 0.001f)
            {
                return null;
            }

            float roll = Random.value * total;
            if ((roll -= gustWeight) <= 0f)
            {
                return new StormGustEvent();
            }

            if ((roll -= waveWeight) <= 0f)
            {
                return new DangerousWaveEvent();
            }

            if ((roll -= fogWeight) <= 0f)
            {
                return new FogBankEvent();
            }

            if ((roll -= currentWeight) <= 0f)
            {
                return new CurrentZoneEvent();
            }

            return new HiddenReefEvent();
        }

        private Vector3 GetSpawnOrigin(MaritimeEventBase maritimeEvent)
        {
            Vector3 forward = navigationTarget != null ? navigationTarget.forward : Vector3.forward;
            Vector3 right = navigationTarget != null ? navigationTarget.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            float distance = maritimeEvent is DangerousWaveEvent ? spawnDistance * 0.65f : spawnDistance;
            if (maritimeEvent is HiddenReefEvent)
            {
                distance = spawnDistance * 0.5f;
            }

            return navigationTarget.position + forward * distance + right * Random.Range(-sideOffset, sideOffset);
        }

        private float GetDuration(MaritimeEventBase maritimeEvent)
        {
            if (maritimeEvent is StormGustEvent)
            {
                return Random.Range(7f, 12f);
            }

            if (maritimeEvent is DangerousWaveEvent)
            {
                return Random.Range(8f, 13f);
            }

            if (maritimeEvent is FogBankEvent)
            {
                return Random.Range(24f, 42f);
            }

            if (maritimeEvent is CurrentZoneEvent)
            {
                return Random.Range(26f, 44f);
            }

            if (maritimeEvent is HiddenReefEvent)
            {
                return Random.Range(38f, 70f);
            }

            return defaultDuration;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            Vector3 origin = navigationTarget != null ? navigationTarget.position : transform.position;
            Gizmos.color = new Color(1f, 0.72f, 0.18f, 0.28f);
            Gizmos.DrawWireSphere(origin, spawnDistance);

            for (int i = 0; i < activeEvents.Count; i++)
            {
                activeEvents[i].DrawGizmos();
            }
        }
    }
}
