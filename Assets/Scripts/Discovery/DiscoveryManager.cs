using System.Collections.Generic;
using BoatGame.Economy;
using BoatGame.Quests;
using BoatGame.Rumors;
using BoatGame.World;
using UnityEngine;

namespace BoatGame.Discovery
{
    [DefaultExecutionOrder(-30)]
    [DisallowMultipleComponent]
    public sealed class DiscoveryManager : MonoBehaviour
    {
        public static DiscoveryManager Instance { get; private set; }

        [Header("Discoveries")]
        [SerializeField] private List<string> discoveredIds = new List<string>();
        [SerializeField, Min(0.5f)] private float notificationDuration = 4.2f;

        private readonly HashSet<string> discoveredSet = new HashSet<string>();
        private string activeNotification;
        private float notificationTimer;

        public string ActiveNotification => activeNotification;
        public float Notification01 => notificationDuration <= 0.01f ? 0f : Mathf.Clamp01(notificationTimer / notificationDuration);
        public IReadOnlyList<string> DiscoveredIds => discoveredIds;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RebuildSet();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
                if (notificationTimer <= 0f)
                {
                    activeNotification = string.Empty;
                }
            }
        }

        public static DiscoveryManager GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject manager = new GameObject("DiscoveryManager");
            return manager.AddComponent<DiscoveryManager>();
        }

        public bool IsDiscovered(string discoveryId)
        {
            if (string.IsNullOrWhiteSpace(discoveryId))
            {
                return false;
            }

            return discoveredSet.Contains(discoveryId);
        }

        public bool RegisterDiscovery(DiscoverableLocation location)
        {
            if (location == null || string.IsNullOrWhiteSpace(location.DiscoveryId))
            {
                return false;
            }

            if (discoveredSet.Contains(location.DiscoveryId))
            {
                return false;
            }

            discoveredSet.Add(location.DiscoveryId);
            discoveredIds.Add(location.DiscoveryId);
            ApplyDiscoveryReward(location);

            activeNotification = BuildNotification(location);
            notificationTimer = notificationDuration;

            QuestManager.Instance?.NotifyLocationDiscovered(location.DiscoveryType, location.PoiType, location.WorldPosition, location.DisplayName);
            RumorManager.Instance?.OnLocationDiscovered(location);
            return true;
        }

        public void ResetDiscoveries()
        {
            discoveredIds.Clear();
            discoveredSet.Clear();
            activeNotification = "Journal de decouvertes efface.";
            notificationTimer = notificationDuration;
        }

        private void ApplyDiscoveryReward(DiscoverableLocation location)
        {
            PlayerCurrency currency = FindFirstObjectByType<PlayerCurrency>();
            ResourceInventory inventory = FindFirstObjectByType<ResourceInventory>();

            if (currency != null && location.CoinReward > 0)
            {
                currency.AddCoins(location.CoinReward);
            }

            if (inventory != null && location.ResourceRewardAmount > 0)
            {
                inventory.Add(location.ResourceRewardType, location.ResourceRewardAmount);
            }
        }

        private static string BuildNotification(DiscoverableLocation location)
        {
            switch (location.DiscoveryType)
            {
                case DiscoveryType.Port:
                    return $"Port decouvert: {location.DisplayName}";
                case DiscoveryType.Shipwreck:
                    return $"Epave decouverte: {location.DisplayName}";
                case DiscoveryType.DangerZone:
                    return $"Danger maritime repere: {location.DisplayName}";
                case DiscoveryType.StrangeStructure:
                    return $"Structure etrange: {location.DisplayName}";
                default:
                    return $"Ile decouverte: {location.DisplayName}";
            }
        }

        private void RebuildSet()
        {
            discoveredSet.Clear();
            for (int i = 0; i < discoveredIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(discoveredIds[i]))
                {
                    discoveredSet.Add(discoveredIds[i]);
                }
            }
        }
    }
}
