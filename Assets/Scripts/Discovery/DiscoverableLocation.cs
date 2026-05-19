using BoatGame.Boat;
using BoatGame.Economy;
using BoatGame.Player;
using BoatGame.World;
using UnityEngine;

namespace BoatGame.Discovery
{
    [DisallowMultipleComponent]
    public sealed class DiscoverableLocation : MonoBehaviour
    {
        [Header("Discovery")]
        [SerializeField] private string discoveryId;
        [SerializeField] private string displayName = "Lieu sans nom";
        [SerializeField] private DiscoveryType discoveryType = DiscoveryType.Island;
        [SerializeField] private MaritimePoiType poiType = MaritimePoiType.OpenWater;
        [SerializeField, Min(12f)] private float discoveryRadius = 90f;
        [SerializeField] private bool discovered;

        [Header("Reward")]
        [SerializeField, Min(0)] private int coinReward = 8;
        [SerializeField] private ResourceType resourceRewardType = ResourceType.Wood;
        [SerializeField, Min(0)] private int resourceRewardAmount;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        private Transform target;
        private float nextTargetSearchTime;

        public string DiscoveryId => discoveryId;
        public string DisplayName => displayName;
        public DiscoveryType DiscoveryType => discoveryType;
        public MaritimePoiType PoiType => poiType;
        public float DiscoveryRadius => discoveryRadius;
        public int CoinReward => coinReward;
        public ResourceType ResourceRewardType => resourceRewardType;
        public int ResourceRewardAmount => resourceRewardAmount;
        public bool Discovered => discovered;
        public Vector3 WorldPosition => transform.position;
        public bool HasWorldPosition => true;

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(discoveryId))
            {
                discoveryId = $"{discoveryType}_{Mathf.RoundToInt(transform.position.x)}_{Mathf.RoundToInt(transform.position.z)}";
            }

            EnsureTrigger();
        }

        private void Update()
        {
            if (discovered)
            {
                return;
            }

            if (target == null || Time.time >= nextTargetSearchTime)
            {
                nextTargetSearchTime = Time.time + 0.8f;
                target = ResolveTarget();
            }

            if (target != null && Vector3.Distance(target.position, transform.position) <= discoveryRadius)
            {
                Discover();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (discovered)
            {
                return;
            }

            if (other.GetComponentInParent<BoatHelmController>() != null || other.GetComponentInParent<FpsPlayerController>() != null)
            {
                Discover();
            }
        }

        public void Configure(string id, string newDisplayName, DiscoveryType type, MaritimePoiType maritimeType, float radius, int coins, ResourceType rewardType, int rewardAmount)
        {
            discoveryId = id;
            displayName = string.IsNullOrWhiteSpace(newDisplayName) ? "Lieu sans nom" : newDisplayName;
            discoveryType = type;
            poiType = maritimeType;
            discoveryRadius = Mathf.Max(12f, radius);
            coinReward = Mathf.Max(0, coins);
            resourceRewardType = rewardType;
            resourceRewardAmount = Mathf.Max(0, rewardAmount);
            EnsureTrigger();
        }

        private void Discover()
        {
            if (discovered)
            {
                return;
            }

            discovered = true;
            DiscoveryManager.GetOrCreate().RegisterDiscovery(this);
        }

        private void EnsureTrigger()
        {
            SphereCollider sphere = GetComponent<SphereCollider>();
            if (sphere == null)
            {
                return;
            }

            sphere.isTrigger = true;
            sphere.radius = discoveryRadius;
        }

        private static Transform ResolveTarget()
        {
            BoatHelmController boat = FindFirstObjectByType<BoatHelmController>();
            if (boat != null)
            {
                return boat.transform;
            }

            FpsPlayerController player = FindFirstObjectByType<FpsPlayerController>();
            if (player != null)
            {
                return player.transform;
            }

            Camera camera = Camera.main;
            return camera != null ? camera.transform : null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = discovered ? new Color(0.3f, 0.85f, 0.45f, 0.42f) : new Color(1f, 0.82f, 0.24f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, discoveryRadius);
        }
    }
}
