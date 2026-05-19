using System.Collections.Generic;
using BoatGame.Boat;
using BoatGame.Damage;
using BoatGame.Discovery;
using BoatGame.Economy;
using BoatGame.Player;
using BoatGame.Port;
using BoatGame.Upgrades;
using BoatGame.Water;
using BoatGame.World;
using UnityEngine;

namespace BoatGame.Quests
{
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Quest Data")]
        [SerializeField] private QuestDatabase database;
        [SerializeField, Min(1)] private int contractsPerPort = 6;
        [SerializeField, Min(250f)] private float minimumContractDistance = 650f;
        [SerializeField, Min(650f)] private float maximumContractDistance = 2300f;

        [Header("Runtime")]
        [SerializeField] private List<Quest> availableContracts = new List<Quest>();
        [SerializeField] private List<Quest> activeQuests = new List<Quest>();
        [SerializeField] private List<Quest> completedQuests = new List<Quest>();
        [SerializeField] private List<Quest> turnedInQuests = new List<Quest>();
        [SerializeField] private List<BoatUpgradeType> unlockedUpgradePlans = new List<BoatUpgradeType>();

        [Header("Markers")]
        [SerializeField] private Material markerMaterial;
        [SerializeField] private bool drawGizmos = true;

        private readonly Dictionary<string, QuestObjectiveMarker> activeMarkers = new Dictionary<string, QuestObjectiveMarker>(8);
        private Transform markerRoot;
        private BoatHelmController boat;
        private FpsPlayerController player;
        private PlayerCurrency currency;
        private ResourceInventory inventory;
        private BoatDamageSystem damageSystem;
        private string lastMessage;
        private int generationSalt;

        public IReadOnlyList<Quest> AvailableContracts => availableContracts;
        public IReadOnlyList<Quest> ActiveQuests => activeQuests;
        public IReadOnlyList<Quest> CompletedQuests => completedQuests;
        public IReadOnlyList<Quest> TurnedInQuests => turnedInQuests;
        public IReadOnlyList<BoatUpgradeType> UnlockedUpgradePlans => unlockedUpgradePlans;
        public string LastMessage => lastMessage;

        public Quest ActiveQuest
        {
            get
            {
                for (int i = 0; i < activeQuests.Count; i++)
                {
                    if (activeQuests[i] != null && (activeQuests[i].state == QuestState.Accepted || activeQuests[i].state == QuestState.InProgress))
                    {
                        return activeQuests[i];
                    }
                }

                return completedQuests.Count > 0 ? completedQuests[0] : null;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (database == null)
            {
                database = QuestDatabase.CreateRuntime();
            }

            ResolveReferences();
            EnsureMarkerRoot();
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
            ResolveReferences();
            Vector3 referencePosition = GetReferencePosition();

            for (int i = activeQuests.Count - 1; i >= 0; i--)
            {
                Quest quest = activeQuests[i];
                if (quest == null)
                {
                    activeQuests.RemoveAt(i);
                    continue;
                }

                bool completed = quest.Tick(referencePosition, Time.deltaTime);
                if (completed || quest.state == QuestState.Completed)
                {
                    activeQuests.RemoveAt(i);
                    if (!completedQuests.Contains(quest))
                    {
                        completedQuests.Add(quest);
                    }

                    lastMessage = $"Contrat acheve: {quest.title}. Retournez au tableau de contrats.";
                }
            }

            RefreshObjectiveMarkers();
        }

        public static QuestManager GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject manager = new GameObject("QuestManager");
            return manager.AddComponent<QuestManager>();
        }

        public IReadOnlyList<Quest> GetContractsForPort(PortManager port)
        {
            GenerateContractsForPort(port);
            return availableContracts;
        }

        public void GenerateContractsForPort(PortManager port)
        {
            ResolveReferences();
            string originName = port != null ? port.PortName : "Port sans nom";
            Vector3 originPosition = port != null ? port.transform.position : GetReferencePosition();
            int existing = 0;

            for (int i = 0; i < availableContracts.Count; i++)
            {
                Quest quest = availableContracts[i];
                if (quest != null && quest.originName == originName)
                {
                    existing++;
                }
            }

            QuestContractType[] types =
            {
                QuestContractType.PortDelivery,
                QuestContractType.IslandExploration,
                QuestContractType.ShipwreckRecovery,
                QuestContractType.TreasureHunt,
                QuestContractType.DangerousZoneInvestigation,
                QuestContractType.MaritimeEscort,
                QuestContractType.PortDiscovery
            };

            while (existing < contractsPerPort)
            {
                QuestContractType type = types[(existing + generationSalt) % types.Length];
                if (TryCreateContract(type, originName, originPosition, out Quest quest))
                {
                    availableContracts.Add(quest);
                    existing++;
                }
                else
                {
                    generationSalt++;
                    break;
                }

                generationSalt++;
            }
        }

        public bool AcceptQuest(Quest quest)
        {
            if (quest == null || quest.state != QuestState.Available)
            {
                lastMessage = "Ce contrat n'est plus disponible.";
                return false;
            }

            quest.Accept();
            availableContracts.Remove(quest);
            if (!activeQuests.Contains(quest))
            {
                activeQuests.Add(quest);
            }

            quest.CurrentObjective?.Reveal();
            RefreshObjectiveMarkers();
            lastMessage = $"Contrat accepte: {quest.title}";
            return true;
        }

        public bool CompleteObjective(string questId, string objectiveId)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                Quest quest = activeQuests[i];
                if (quest == null || quest.id != questId)
                {
                    continue;
                }

                if (!quest.CompleteObjective(objectiveId))
                {
                    continue;
                }

                RemoveMarker(questId, objectiveId);
                quest.Tick(GetReferencePosition(), 0f);
                quest.CurrentObjective?.Reveal();
                lastMessage = $"Objectif accompli: {quest.title}";
                return true;
            }

            return false;
        }

        public void NotifyLocationDiscovered(DiscoveryType discoveryType, MaritimePoiType poiType, Vector3 position, string locationName)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                Quest quest = activeQuests[i];
                QuestObjective objective = quest?.CurrentObjective;
                if (objective == null || objective.type != QuestObjectiveType.DiscoverLocation)
                {
                    continue;
                }

                float radius = Mathf.Max(objective.completionRadius * 1.35f, 120f);
                bool locationMatch = Vector3.Distance(objective.targetPosition, position) <= radius;
                bool nameMatch = !string.IsNullOrWhiteSpace(locationName) && locationName == objective.targetName;
                if (locationMatch || nameMatch)
                {
                    objective.ForceComplete();
                    quest.Tick(GetReferencePosition(), 0f);
                    lastMessage = $"Decouverte liee au contrat: {quest.title}";
                }
            }
        }

        public bool TurnInQuest(Quest quest)
        {
            if (quest == null || quest.state != QuestState.Completed)
            {
                lastMessage = "Aucun contrat acheve a rendre.";
                return false;
            }

            ResolveReferences();
            for (int i = 0; i < quest.rewards.Count; i++)
            {
                quest.rewards[i]?.Apply(currency, inventory, damageSystem, this);
            }

            completedQuests.Remove(quest);
            if (!turnedInQuests.Contains(quest))
            {
                turnedInQuests.Add(quest);
            }

            quest.state = QuestState.TurnedIn;
            lastMessage = $"Recompense recue: {quest.RewardSummary()}";
            return true;
        }

        public void UnlockUpgradePlan(BoatUpgradeType upgradePlan)
        {
            if (!unlockedUpgradePlans.Contains(upgradePlan))
            {
                unlockedUpgradePlans.Add(upgradePlan);
                lastMessage = $"Plan d'amelioration obtenu: {upgradePlan}";
            }
        }

        public void RevealActiveDestination()
        {
            Quest quest = ActiveQuest;
            QuestObjective objective = quest?.CurrentObjective;
            if (objective == null)
            {
                lastMessage = "Aucun objectif actif.";
                return;
            }

            objective.Reveal();
            lastMessage = $"Destination revelee: {objective.description}";
        }

        public void CompleteCurrentObjectiveDebug()
        {
            Quest quest = ActiveQuest;
            QuestObjective objective = quest?.CurrentObjective;
            if (quest == null || objective == null)
            {
                lastMessage = "Aucun objectif actif a completer.";
                return;
            }

            CompleteObjective(quest.id, objective.id);
        }

        public Quest GenerateDebugContract()
        {
            PortManager nearestPort = PortManager.FindNearest(GetReferencePosition(), 600f);
            GenerateContractsForPort(nearestPort);
            for (int i = 0; i < availableContracts.Count; i++)
            {
                if (nearestPort == null || availableContracts[i].originName == nearestPort.PortName)
                {
                    lastMessage = $"Contrat debug genere: {availableContracts[i].title}";
                    return availableContracts[i];
                }
            }

            lastMessage = "Impossible de generer un contrat debug.";
            return null;
        }

        public void GiveActiveRewardDebug()
        {
            Quest quest = ActiveQuest;
            if (quest == null)
            {
                lastMessage = "Aucun contrat actif pour la recompense debug.";
                return;
            }

            ResolveReferences();
            for (int i = 0; i < quest.rewards.Count; i++)
            {
                quest.rewards[i]?.Apply(currency, inventory, damageSystem, this);
            }

            lastMessage = $"Recompense debug donnee: {quest.RewardSummary()}";
        }

        public void SpawnQuestPoiNear()
        {
            WorldManager world = WorldManager.Instance;
            if (world == null)
            {
                lastMessage = "Aucun WorldManager pour spawn un POI.";
                return;
            }

            WorldManager.WorldPoiInfo poi = world.EnsurePoiNear(GetReferencePosition(), MaritimePoiType.Shipwreck, 450f, 950f);
            lastMessage = $"POI de quete garanti: {poi.DisplayName}";
        }

        public void ResetQuests()
        {
            availableContracts.Clear();
            activeQuests.Clear();
            completedQuests.Clear();
            turnedInQuests.Clear();
            unlockedUpgradePlans.Clear();
            ClearMarkers();
            lastMessage = "Contrats reinitialises.";
        }

        private bool TryCreateContract(QuestContractType type, string originName, Vector3 originPosition, out Quest quest)
        {
            quest = null;
            WorldManager world = WorldManager.Instance;
            if (world == null || database == null)
            {
                return false;
            }

            WorldManager.WorldPoiInfo destination = ResolveDestinationForType(world, originPosition, type);
            quest = database.CreateContract(type, originName, originPosition, destination);
            return quest != null;
        }

        private WorldManager.WorldPoiInfo ResolveDestinationForType(WorldManager world, Vector3 originPosition, QuestContractType type)
        {
            switch (type)
            {
                case QuestContractType.PortDelivery:
                case QuestContractType.PortDiscovery:
                case QuestContractType.MaritimeEscort:
                    return world.EnsurePoiNear(originPosition, MaritimePoiType.Port, minimumContractDistance, maximumContractDistance);
                case QuestContractType.ShipwreckRecovery:
                    return world.EnsurePoiNear(originPosition, MaritimePoiType.Shipwreck, minimumContractDistance, maximumContractDistance);
                case QuestContractType.DangerousZoneInvestigation:
                    return world.EnsurePoiNear(originPosition, MaritimePoiType.DangerZone, minimumContractDistance, maximumContractDistance);
                case QuestContractType.TreasureHunt:
                    return ResolveAny(world, originPosition, new[] { MaritimePoiType.SmallIsland, MaritimePoiType.LargeIsland }, MaritimePoiType.SmallIsland);
                default:
                    return ResolveAny(world, originPosition, new[] { MaritimePoiType.LargeIsland, MaritimePoiType.SmallIsland }, MaritimePoiType.LargeIsland);
            }
        }

        private WorldManager.WorldPoiInfo ResolveAny(WorldManager world, Vector3 originPosition, MaritimePoiType[] allowedTypes, MaritimePoiType fallbackType)
        {
            if (world.TryFindPoi(originPosition, allowedTypes, minimumContractDistance, maximumContractDistance, out WorldManager.WorldPoiInfo poi))
            {
                return poi;
            }

            return world.EnsurePoiNear(originPosition, fallbackType, minimumContractDistance, maximumContractDistance);
        }

        private void RefreshObjectiveMarkers()
        {
            EnsureMarkerRoot();
            for (int i = 0; i < activeQuests.Count; i++)
            {
                Quest quest = activeQuests[i];
                QuestObjective objective = quest?.CurrentObjective;
                if (objective == null)
                {
                    continue;
                }

                objective.Reveal();
                if (objective.type == QuestObjectiveType.InteractAtLocation)
                {
                    EnsureMarker(quest, objective);
                }
            }
        }

        private void EnsureMarker(Quest quest, QuestObjective objective)
        {
            string key = GetMarkerKey(quest.id, objective.id);
            if (activeMarkers.TryGetValue(key, out QuestObjectiveMarker existing) && existing != null && existing.gameObject.activeInHierarchy)
            {
                return;
            }

            Vector3 position = objective.targetPosition;
            WaterManager water = WaterManager.Instance;
            position.y = water != null ? water.GetWaterHeight(position) + 1.15f : Mathf.Max(position.y, 1.15f);

            GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerObject.name = $"QuestMarker_{quest.id}_{objective.id}";
            markerObject.layer = LayerMask.NameToLayer("Interactable") >= 0 ? LayerMask.NameToLayer("Interactable") : 0;
            markerObject.transform.SetParent(markerRoot, true);
            markerObject.transform.position = position;
            markerObject.transform.localScale = new Vector3(1.1f, 0.45f, 1.1f);

            Collider collider = markerObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Renderer renderer = markerObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetMarkerMaterial();
            }

            QuestObjectiveMarker marker = markerObject.AddComponent<QuestObjectiveMarker>();
            marker.Configure(quest.id, objective.id, true, objective.description);
            activeMarkers[key] = marker;
        }

        private void RemoveMarker(string questId, string objectiveId)
        {
            string key = GetMarkerKey(questId, objectiveId);
            if (!activeMarkers.TryGetValue(key, out QuestObjectiveMarker marker))
            {
                return;
            }

            if (marker != null)
            {
                Destroy(marker.gameObject);
            }

            activeMarkers.Remove(key);
        }

        private void ClearMarkers()
        {
            foreach (KeyValuePair<string, QuestObjectiveMarker> pair in activeMarkers)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }

            activeMarkers.Clear();
        }

        private Material GetMarkerMaterial()
        {
            if (markerMaterial != null)
            {
                return markerMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            markerMaterial = new Material(shader) { name = "RuntimeQuestMarkerMaterial" };
            if (markerMaterial.HasProperty("_BaseColor"))
            {
                markerMaterial.SetColor("_BaseColor", new Color(0.9f, 0.68f, 0.22f, 0.92f));
            }
            else if (markerMaterial.HasProperty("_Color"))
            {
                markerMaterial.SetColor("_Color", new Color(0.9f, 0.68f, 0.22f, 0.92f));
            }

            return markerMaterial;
        }

        private void EnsureMarkerRoot()
        {
            if (markerRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("QuestObjectiveMarkers");
            markerRoot = root.transform;
        }

        private Vector3 GetReferencePosition()
        {
            ResolveReferences();
            if (boat != null)
            {
                return boat.transform.position;
            }

            if (player != null)
            {
                return player.transform.position;
            }

            Camera camera = Camera.main;
            return camera != null ? camera.transform.position : Vector3.zero;
        }

        public Transform GetReferenceTransform()
        {
            ResolveReferences();
            if (boat != null)
            {
                return boat.transform;
            }

            if (player != null)
            {
                return player.transform;
            }

            Camera camera = Camera.main;
            return camera != null ? camera.transform : null;
        }

        private void ResolveReferences()
        {
            if (boat == null)
            {
                boat = FindFirstObjectByType<BoatHelmController>();
            }

            if (player == null)
            {
                player = FindFirstObjectByType<FpsPlayerController>();
            }

            if (currency == null)
            {
                currency = player != null ? player.GetComponent<PlayerCurrency>() : FindFirstObjectByType<PlayerCurrency>();
            }

            if (inventory == null)
            {
                inventory = player != null ? player.GetComponent<ResourceInventory>() : FindFirstObjectByType<ResourceInventory>();
            }

            if (damageSystem == null && boat != null)
            {
                damageSystem = boat.GetComponent<BoatDamageSystem>();
            }
        }

        private static string GetMarkerKey(string questId, string objectiveId)
        {
            return $"{questId}:{objectiveId}";
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Quest quest = ActiveQuest;
            QuestObjective objective = quest?.CurrentObjective;
            if (objective == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.78f, 0.18f, 0.55f);
            Gizmos.DrawWireSphere(objective.targetPosition, objective.completionRadius);
            Gizmos.DrawLine(transform.position, objective.targetPosition);
        }
    }
}
