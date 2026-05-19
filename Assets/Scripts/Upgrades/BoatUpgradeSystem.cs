using System.Collections.Generic;
using BoatGame.Boat;
using BoatGame.Damage;
using BoatGame.Economy;
using UnityEngine;

namespace BoatGame.Upgrades
{
    [DisallowMultipleComponent]
    public sealed class BoatUpgradeSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoatHelmController helmController;
        [SerializeField] private BoatDamageSystem damageSystem;
        [SerializeField] private RepairResource repairResource;

        [Header("Upgrades")]
        [SerializeField] private List<BoatUpgradeDefinition> definitions = new List<BoatUpgradeDefinition>();
        [SerializeField] private List<BoatUpgradeType> purchasedUpgrades = new List<BoatUpgradeType>();

        [Header("Visuals")]
        [SerializeField] private Material upgradeMaterial;

        private readonly HashSet<BoatUpgradeType> visualized = new HashSet<BoatUpgradeType>();

        public IReadOnlyList<BoatUpgradeDefinition> Definitions => definitions;

        private void Awake()
        {
            ResolveReferences();
            EnsureDefaultDefinitions();
            ApplyPurchasedUpgrades(null);
        }

        private void OnValidate()
        {
            EnsureDefaultDefinitions();
        }

        public bool HasUpgrade(BoatUpgradeType type)
        {
            return purchasedUpgrades.Contains(type);
        }

        public BoatUpgradeDefinition GetDefinition(BoatUpgradeType type)
        {
            EnsureDefaultDefinitions();
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].type == type)
                {
                    return definitions[i];
                }
            }

            return null;
        }

        public bool TryPurchase(BoatUpgradeType type, PlayerCurrency currency, ResourceInventory inventory, out string message)
        {
            ResolveReferences();
            BoatUpgradeDefinition definition = GetDefinition(type);
            if (definition == null)
            {
                message = "Amelioration inconnue.";
                return false;
            }

            if (HasUpgrade(type))
            {
                message = "Cette amelioration est deja installee.";
                return false;
            }

            if (currency == null || inventory == null)
            {
                message = "Aucune economie joueur trouvee.";
                return false;
            }

            if (!currency.CanSpend(definition.coinCost) || !inventory.CanSpend(definition.resourceCosts))
            {
                message = "Ressources insuffisantes.";
                return false;
            }

            currency.TrySpend(definition.coinCost);
            inventory.TrySpend(definition.resourceCosts);
            purchasedUpgrades.Add(type);
            ApplyPurchasedUpgrades(inventory);
            message = $"{definition.displayName} installee.";
            return true;
        }

        public void ResetPurchases(ResourceInventory inventory)
        {
            purchasedUpgrades.Clear();
            visualized.Clear();
            RemoveUpgradeVisuals();
            ApplyPurchasedUpgrades(inventory);
        }

        public void ApplyPurchasedUpgrades(ResourceInventory inventory)
        {
            ResolveReferences();

            float sailMultiplier = 1f;
            float rudderMultiplier = 1f;
            float handlingMultiplier = 1f;
            float hullReduction = 0f;
            float leakReduction = 0f;
            int storageBonus = 0;
            int repairStorageBonus = 0;

            for (int i = 0; i < purchasedUpgrades.Count; i++)
            {
                BoatUpgradeDefinition definition = GetDefinition(purchasedUpgrades[i]);
                if (definition == null)
                {
                    continue;
                }

                sailMultiplier += definition.sailForceBonus;
                rudderMultiplier += definition.rudderForceBonus;
                handlingMultiplier += definition.handlingBonus;
                hullReduction = Mathf.Max(hullReduction, definition.hullDamageReduction);
                leakReduction = Mathf.Max(leakReduction, definition.leakReduction);
                storageBonus += definition.storageCapacityBonus;
                repairStorageBonus += definition.repairPatchCapacityBonus;
                EnsureVisual(definition.type);
            }

            if (helmController != null)
            {
                helmController.SetUpgradeModifiers(sailMultiplier, rudderMultiplier, handlingMultiplier);
            }

            if (damageSystem != null)
            {
                damageSystem.SetUpgradeModifiers(hullReduction, leakReduction);
            }

            if (repairResource != null)
            {
                repairResource.SetMaxWoodPatches(16 + repairStorageBonus, false);
            }

            if (inventory != null)
            {
                inventory.SetCapacityBonus(storageBonus);
            }
        }

        private void ResolveReferences()
        {
            if (helmController == null)
            {
                helmController = GetComponent<BoatHelmController>();
            }

            if (damageSystem == null)
            {
                damageSystem = GetComponent<BoatDamageSystem>();
            }

            if (repairResource == null)
            {
                repairResource = GetComponent<RepairResource>();
            }
        }

        private void EnsureDefaultDefinitions()
        {
            if (definitions == null)
            {
                definitions = new List<BoatUpgradeDefinition>();
            }

            if (definitions.Count > 0)
            {
                return;
            }

            definitions.Add(new BoatUpgradeDefinition
            {
                type = BoatUpgradeType.ReinforcedSail,
                displayName = "Voile renforcee",
                description = "Tissu epais et coutures de corde. Meilleure propulsion et voile plus stable.",
                coinCost = 80,
                resourceCosts = new[] { new SerializableResourceAmount(ResourceType.Fabric, 4), new SerializableResourceAmount(ResourceType.Rope, 2) },
                sailForceBonus = 0.18f,
                handlingBonus = 0.04f
            });
            definitions.Add(new BoatUpgradeDefinition
            {
                type = BoatUpgradeType.ImprovedRudder,
                displayName = "Gouvernail ameliore",
                description = "Ferrures neuves et palette plus efficace. Le bateau repond mieux.",
                coinCost = 70,
                resourceCosts = new[] { new SerializableResourceAmount(ResourceType.Iron, 3), new SerializableResourceAmount(ResourceType.Wood, 2) },
                rudderForceBonus = 0.24f,
                handlingBonus = 0.05f
            });
            definitions.Add(new BoatUpgradeDefinition
            {
                type = BoatUpgradeType.ReinforcedHull,
                displayName = "Coque renforcee",
                description = "Bordage supplementaire. Moins de degats de collision et moins d'infiltration.",
                coinCost = 110,
                resourceCosts = new[] { new SerializableResourceAmount(ResourceType.Wood, 6), new SerializableResourceAmount(ResourceType.Iron, 4) },
                hullDamageReduction = 0.28f,
                leakReduction = 0.25f
            });
            definitions.Add(new BoatUpgradeDefinition
            {
                type = BoatUpgradeType.ExpandedStorage,
                displayName = "Stockage augmente",
                description = "Caisses et rances sur le pont. Plus de ressources et de planches de reparation.",
                coinCost = 60,
                resourceCosts = new[] { new SerializableResourceAmount(ResourceType.Wood, 4), new SerializableResourceAmount(ResourceType.Rope, 2) },
                storageCapacityBonus = 12,
                repairPatchCapacityBonus = 8
            });
            definitions.Add(new BoatUpgradeDefinition
            {
                type = BoatUpgradeType.SmallCannon,
                displayName = "Petit canon",
                description = "Canon placeholder fixe sur le pont. Visuel installe pour le futur combat.",
                coinCost = 130,
                resourceCosts = new[] { new SerializableResourceAmount(ResourceType.Iron, 6), new SerializableResourceAmount(ResourceType.Wood, 3) }
            });
            definitions.Add(new BoatUpgradeDefinition
            {
                type = BoatUpgradeType.UpperDeck,
                displayName = "Pont superieur",
                description = "Petite plateforme arriere. Espace de pont supplementaire et silhouette plus noble.",
                coinCost = 160,
                resourceCosts = new[] { new SerializableResourceAmount(ResourceType.Wood, 8), new SerializableResourceAmount(ResourceType.Iron, 3) },
                handlingBonus = 0.02f
            });
        }

        private void EnsureVisual(BoatUpgradeType type)
        {
            if (visualized.Contains(type) || transform.Find($"Upgrade_{type}") != null)
            {
                visualized.Add(type);
                return;
            }

            GameObject root = new GameObject($"Upgrade_{type}");
            root.transform.SetParent(transform, false);
            root.layer = gameObject.layer;
            visualized.Add(type);

            switch (type)
            {
                case BoatUpgradeType.ReinforcedSail:
                    CreateBox(root.transform, "SailBand_A", new Vector3(0f, 2.06f, 0.49f), new Vector3(2.45f, 0.08f, 0.06f), Quaternion.identity, false);
                    CreateBox(root.transform, "SailBand_B", new Vector3(0f, 1.55f, 0.49f), new Vector3(2.2f, 0.08f, 0.06f), Quaternion.identity, false);
                    break;
                case BoatUpgradeType.ImprovedRudder:
                    CreateBox(root.transform, "RudderBrace", new Vector3(0f, -0.12f, -3.42f), new Vector3(0.72f, 0.12f, 0.18f), Quaternion.identity, false);
                    break;
                case BoatUpgradeType.ReinforcedHull:
                    CreateBox(root.transform, "HullArmor_L", new Vector3(-1.74f, -0.08f, -0.2f), new Vector3(0.12f, 0.42f, 5.1f), Quaternion.identity, true);
                    CreateBox(root.transform, "HullArmor_R", new Vector3(1.74f, -0.08f, -0.2f), new Vector3(0.12f, 0.42f, 5.1f), Quaternion.identity, true);
                    break;
                case BoatUpgradeType.ExpandedStorage:
                    CreateBox(root.transform, "StorageCrate_A", new Vector3(-0.74f, 0.5f, -0.75f), new Vector3(0.58f, 0.42f, 0.58f), Quaternion.Euler(0f, 8f, 0f), true);
                    CreateBox(root.transform, "StorageCrate_B", new Vector3(0.72f, 0.5f, -0.58f), new Vector3(0.52f, 0.38f, 0.52f), Quaternion.Euler(0f, -12f, 0f), true);
                    break;
                case BoatUpgradeType.SmallCannon:
                    CreateCannon(root.transform);
                    break;
                case BoatUpgradeType.UpperDeck:
                    CreateBox(root.transform, "UpperDeckPlatform", new Vector3(0f, 0.92f, -2.15f), new Vector3(2.25f, 0.16f, 1.45f), Quaternion.identity, true);
                    CreateBox(root.transform, "UpperDeckPost_L", new Vector3(-0.96f, 0.68f, -1.55f), new Vector3(0.12f, 0.72f, 0.12f), Quaternion.identity, true);
                    CreateBox(root.transform, "UpperDeckPost_R", new Vector3(0.96f, 0.68f, -1.55f), new Vector3(0.12f, 0.72f, 0.12f), Quaternion.identity, true);
                    break;
            }
        }

        private void CreateCannon(Transform parent)
        {
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "SmallCannonBarrel";
            barrel.transform.SetParent(parent, false);
            barrel.transform.localPosition = new Vector3(0.85f, 0.68f, 0.75f);
            barrel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            barrel.transform.localScale = new Vector3(0.16f, 0.72f, 0.16f);
            barrel.layer = gameObject.layer;
            ApplyMaterial(barrel);
            Collider collider = barrel.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyUnityObject(collider);
            }

            CreateBox(parent, "SmallCannonBase", new Vector3(0.85f, 0.48f, 0.75f), new Vector3(0.55f, 0.18f, 0.42f), Quaternion.identity, true);
        }

        private GameObject CreateBox(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, bool keepCollider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = objectName;
            box.layer = gameObject.layer;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = localRotation;
            box.transform.localScale = localScale;
            ApplyMaterial(box);

            if (!keepCollider)
            {
                Collider collider = box.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyUnityObject(collider);
                }
            }

            return box;
        }

        private void ApplyMaterial(GameObject target)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            if (upgradeMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                upgradeMaterial = new Material(shader) { name = "RuntimeUpgradeMaterial" };
                if (upgradeMaterial.HasProperty("_BaseColor"))
                {
                    upgradeMaterial.SetColor("_BaseColor", new Color(0.42f, 0.29f, 0.16f));
                }
                else if (upgradeMaterial.HasProperty("_Color"))
                {
                    upgradeMaterial.SetColor("_Color", new Color(0.42f, 0.29f, 0.16f));
                }
            }

            renderer.sharedMaterial = upgradeMaterial;
        }

        private void RemoveUpgradeVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (!child.name.StartsWith("Upgrade_", System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void DestroyUnityObject(Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
