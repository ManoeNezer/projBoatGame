using BoatGame.Boat;
using BoatGame.Damage;
using BoatGame.Discovery;
using BoatGame.Economy;
using BoatGame.Events;
using BoatGame.Port;
using BoatGame.Physics;
using BoatGame.Player;
using BoatGame.Quests;
using BoatGame.Rumors;
using BoatGame.Upgrades;
using BoatGame.Weather;
using BoatGame.World;
using UnityEngine;

namespace BoatGame.Debugging
{
    public static class MaritimeDangerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDangerPrototypeSetup()
        {
            BoatHelmController boat = Object.FindFirstObjectByType<BoatHelmController>();
            FpsPlayerController player = Object.FindFirstObjectByType<FpsPlayerController>();

            EnsureManagers();
            if (boat != null)
            {
                EnsureBoatDamageSetup(boat);
                EnsureBoatUpgradeSetup(boat);
            }

            if (player != null)
            {
                EnsurePlayerRepairSetup(player);
                EnsurePlayerEconomySetup(player);
            }

            EnsurePrototypeZones();
            EnsurePortSystems(boat);
        }

        private static void EnsureManagers()
        {
            if (Object.FindFirstObjectByType<WeatherManager>() == null)
            {
                new GameObject("WeatherManager").AddComponent<WeatherManager>();
            }

            if (Object.FindFirstObjectByType<MaritimeEventManager>() == null)
            {
                new GameObject("MaritimeEventManager").AddComponent<MaritimeEventManager>();
            }

            if (Object.FindFirstObjectByType<DangerDebugUI>() == null)
            {
                new GameObject("DangerDebugUI").AddComponent<DangerDebugUI>();
            }

            if (Object.FindFirstObjectByType<PortUIController>() == null)
            {
                new GameObject("PortUIController").AddComponent<PortUIController>();
            }

            if (Object.FindFirstObjectByType<PortDebugTools>() == null)
            {
                new GameObject("PortDebugTools").AddComponent<PortDebugTools>();
            }

            if (Object.FindFirstObjectByType<QuestManager>() == null)
            {
                new GameObject("QuestManager").AddComponent<QuestManager>();
            }

            if (Object.FindFirstObjectByType<RumorManager>() == null)
            {
                new GameObject("RumorManager").AddComponent<RumorManager>();
            }

            if (Object.FindFirstObjectByType<DiscoveryManager>() == null)
            {
                new GameObject("DiscoveryManager").AddComponent<DiscoveryManager>();
            }

            if (Object.FindFirstObjectByType<ContractBoardUI>() == null)
            {
                new GameObject("ContractBoardUI").AddComponent<ContractBoardUI>();
            }

            if (Object.FindFirstObjectByType<RumorUI>() == null)
            {
                new GameObject("RumorUI").AddComponent<RumorUI>();
            }

            if (Object.FindFirstObjectByType<ObjectiveTrackerUI>() == null)
            {
                new GameObject("ObjectiveTrackerUI").AddComponent<ObjectiveTrackerUI>();
            }

            if (Object.FindFirstObjectByType<DiscoveryNotificationUI>() == null)
            {
                new GameObject("DiscoveryNotificationUI").AddComponent<DiscoveryNotificationUI>();
            }

            if (Object.FindFirstObjectByType<QuestDebugTools>() == null)
            {
                new GameObject("QuestDebugTools").AddComponent<QuestDebugTools>();
            }
        }

        private static void EnsureBoatDamageSetup(BoatHelmController boat)
        {
            GameObject boatObject = boat.gameObject;
            FloatingObject floating = boatObject.GetComponent<FloatingObject>();
            RepairResource resources = boatObject.GetComponent<RepairResource>();
            if (resources == null)
            {
                resources = boatObject.AddComponent<RepairResource>();
                resources.Configure(10, 16);
            }

            BoatDamageSystem damage = boatObject.GetComponent<BoatDamageSystem>();
            if (damage == null)
            {
                damage = boatObject.AddComponent<BoatDamageSystem>();
            }

            damage.Configure(boat, floating, resources);

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer < 0)
            {
                interactableLayer = 0;
            }

            Material repairMaterial = CreateRuntimeRepairMaterial();
            EnsureRepairPoint(boat.transform, "HullRepairPoint", new Vector3(0.95f, 0.62f, 1.55f), new Vector3(0.42f, 0.22f, 0.42f), damage, resources, BoatPartType.Hull, repairMaterial, interactableLayer);
            EnsureRepairPoint(boat.transform, "SailRepairPoint", new Vector3(-0.92f, 0.68f, 0.55f), new Vector3(0.38f, 0.22f, 0.38f), damage, resources, BoatPartType.Sail, repairMaterial, interactableLayer);
            EnsureRepairPoint(boat.transform, "RudderRepairPoint", new Vector3(0.72f, 0.62f, -2.85f), new Vector3(0.36f, 0.22f, 0.36f), damage, resources, BoatPartType.Rudder, repairMaterial, interactableLayer);
            EnsureRepairPoint(boat.transform, "MastRepairPoint", new Vector3(0.35f, 0.62f, 0.42f), new Vector3(0.36f, 0.22f, 0.36f), damage, resources, BoatPartType.Mast, repairMaterial, interactableLayer);
        }

        private static void EnsureBoatUpgradeSetup(BoatHelmController boat)
        {
            if (boat.GetComponent<BoatUpgradeSystem>() == null)
            {
                boat.gameObject.AddComponent<BoatUpgradeSystem>();
            }
        }

        private static void EnsurePlayerRepairSetup(FpsPlayerController player)
        {
            if (player.GetComponent<RepairTool>() == null)
            {
                player.gameObject.AddComponent<RepairTool>();
            }

            Camera camera = player.PlayerCamera != null ? player.PlayerCamera : Camera.main;
            if (camera != null && camera.GetComponent<WeatherCameraFeedback>() == null)
            {
                camera.gameObject.AddComponent<WeatherCameraFeedback>();
            }
        }

        private static void EnsurePlayerEconomySetup(FpsPlayerController player)
        {
            if (player.GetComponent<PlayerCurrency>() == null)
            {
                PlayerCurrency currency = player.gameObject.AddComponent<PlayerCurrency>();
                currency.Configure(120);
            }

            if (player.GetComponent<ResourceInventory>() == null)
            {
                player.gameObject.AddComponent<ResourceInventory>();
            }
        }

        private static void EnsurePrototypeZones()
        {
            if (Object.FindFirstObjectByType<StormZone>() == null)
            {
                GameObject stormObject = new GameObject("PrototypeStormZone");
                stormObject.transform.position = new Vector3(220f, 0f, 135f);
                StormZone storm = stormObject.AddComponent<StormZone>();
                storm.Configure(145f, 52f, 1f, 235f, 3.2f, 0.85f);
            }

            if (Object.FindFirstObjectByType<CurrentZone>() == null)
            {
                GameObject currentObject = new GameObject("PrototypeCurrentZone");
                currentObject.transform.position = new Vector3(95f, 0f, 70f);
                CurrentZone current = currentObject.AddComponent<CurrentZone>();
                current.Configure(68f, 118f, 3.1f, 20f);
            }
        }

        private static void EnsurePortSystems(BoatHelmController boat)
        {
            if (Object.FindFirstObjectByType<PortManager>() != null)
            {
                return;
            }

            Vector3 origin = boat != null ? boat.transform.position : Vector3.zero;
            Quaternion rotation = boat != null ? Quaternion.Euler(0f, boat.transform.eulerAngles.y + 180f, 0f) : Quaternion.identity;
            Vector3 position = origin + (boat != null ? boat.transform.forward : Vector3.forward) * 165f + Vector3.right * 45f;
            PortManager.CreateRuntimePort(position, rotation);
        }

        private static void EnsureRepairPoint(Transform parent, string name, Vector3 localPosition, Vector3 localScale, BoatDamageSystem damage, RepairResource resources, BoatPartType part, Material material, int layer)
        {
            Transform existing = parent.Find(name);
            RepairablePart repairable;
            if (existing != null)
            {
                repairable = existing.GetComponent<RepairablePart>();
                if (repairable == null)
                {
                    repairable = existing.gameObject.AddComponent<RepairablePart>();
                }
            }
            else
            {
                GameObject point = GameObject.CreatePrimitive(PrimitiveType.Cube);
                point.name = name;
                point.layer = layer;
                point.transform.SetParent(parent, false);
                point.transform.localPosition = localPosition;
                point.transform.localScale = localScale;
                Collider collider = point.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                }

                Renderer renderer = point.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                }

                repairable = point.AddComponent<RepairablePart>();
            }

            repairable.Configure(damage, resources, part, part == BoatPartType.Hull ? 0.34f : 0.28f, 1);
        }

        private static Material CreateRuntimeRepairMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader) { name = "RuntimeRepairPointMaterial" };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", new Color(0.8f, 0.36f, 0.16f, 0.8f));
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", new Color(0.8f, 0.36f, 0.16f, 0.8f));
            }

            return material;
        }
    }
}
