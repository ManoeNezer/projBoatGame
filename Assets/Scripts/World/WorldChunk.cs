using System.Collections.Generic;
using BoatGame.Discovery;
using BoatGame.Economy;
using BoatGame.Port;
using BoatGame.Quests;
using BoatGame.Rumors;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BoatGame.World
{
    [DisallowMultipleComponent]
    public sealed class WorldChunk : MonoBehaviour
    {
        private readonly List<Mesh> generatedMeshes = new List<Mesh>(8);

        private WorldManager manager;
        private Vector2Int coordinate;
        private MaritimePoiType poiType;
        private bool isDistant;
        private Vector3 poiWorldPosition;

        public Vector2Int Coordinate => coordinate;
        public MaritimePoiType PoiType => poiType;
        public bool IsDistant => isDistant;
        public Vector3 PoiWorldPosition => poiWorldPosition;

        public void Build(WorldManager worldManager, Vector2Int chunkCoordinate, bool distant)
        {
            manager = worldManager;
            coordinate = chunkCoordinate;
            isDistant = distant;
            poiType = manager.GetPoiType(coordinate);
            transform.position = manager.GetChunkCenter(coordinate);
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            gameObject.name = $"WorldChunk_{coordinate.x}_{coordinate.y}_{poiType}_{(isDistant ? "LOD" : "Full")}";

            Clear();

            if (poiType == MaritimePoiType.OpenWater)
            {
                if (!isDistant)
                {
                    GenerateOpenWaterAmbience(new WorldRandom(manager.Seed, coordinate, 101));
                }

                return;
            }

            WorldRandom rng = new WorldRandom(manager.Seed, coordinate, 211);
            Vector3 localCenter = GetPoiLocalCenter(ref rng);
            poiWorldPosition = transform.TransformPoint(localCenter);

            if (isDistant)
            {
                GenerateDistantSilhouette(localCenter, rng);
                return;
            }

            CreateDiscoverableLocation(localCenter);

            switch (poiType)
            {
                case MaritimePoiType.SmallIsland:
                    GenerateIsland(localCenter, false, false, rng);
                    break;
                case MaritimePoiType.LargeIsland:
                    GenerateIsland(localCenter, true, false, rng);
                    break;
                case MaritimePoiType.Port:
                    GenerateIsland(localCenter, true, true, rng);
                    break;
                case MaritimePoiType.RockCluster:
                    GenerateRockCluster(localCenter, rng, true);
                    break;
                case MaritimePoiType.Shipwreck:
                    GenerateShipwreck(localCenter, rng);
                    break;
                case MaritimePoiType.DangerZone:
                    GenerateDangerZone(localCenter, rng);
                    break;
            }
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    child.SetActive(false);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            for (int i = 0; i < generatedMeshes.Count; i++)
            {
                Mesh mesh = generatedMeshes[i];
                if (mesh == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(mesh);
                }
                else
                {
                    DestroyImmediate(mesh);
                }
            }

            generatedMeshes.Clear();
        }

        private Vector3 GetPoiLocalCenter(ref WorldRandom rng)
        {
            float maxOffset = manager.Settings.chunkSize * 0.18f;
            Vector2 offset = rng.InsideUnitCircle() * maxOffset;
            return new Vector3(offset.x, 0f, offset.y);
        }

        private void GenerateDistantSilhouette(Vector3 localCenter, WorldRandom rng)
        {
            float radius = poiType == MaritimePoiType.Port || poiType == MaritimePoiType.LargeIsland
                ? manager.Settings.largeIslandRadius * rng.Range(0.85f, 1.18f)
                : manager.Settings.smallIslandRadius * rng.Range(0.75f, 1.25f);

            float height = poiType == MaritimePoiType.Port || poiType == MaritimePoiType.LargeIsland
                ? manager.Settings.largeIslandHeight * 0.55f
                : manager.Settings.smallIslandHeight * 0.55f;

            Mesh mesh = CreateIslandTerrainMesh(localCenter, radius * 1.15f, radius * rng.Range(0.75f, 1.25f), height, manager.Settings.distantMeshResolution);
            GameObject silhouette = CreateMeshObject("DistantSilhouette", mesh, manager.Materials.silhouette, false);
            silhouette.transform.localPosition = Vector3.zero;

            if (poiType == MaritimePoiType.Port)
            {
                Vector3 beaconPosition = localCenter + Vector3.up * (manager.WaterLevel + height + 5f);
                GameObject beacon = CreatePrimitive("DistantBeacon", PrimitiveType.Cylinder, manager.Materials.buildingRoof, false);
                beacon.transform.localPosition = beaconPosition;
                beacon.transform.localScale = new Vector3(2.2f, 8f, 2.2f);
            }
        }

        private void GenerateIsland(Vector3 localCenter, bool large, bool withPort, WorldRandom rng)
        {
            WorldGenerationSettings settings = manager.Settings;
            float baseRadius = large ? settings.largeIslandRadius : settings.smallIslandRadius;
            float baseHeight = large ? settings.largeIslandHeight : settings.smallIslandHeight;
            float radiusX = baseRadius * rng.Range(0.82f, 1.25f);
            float radiusZ = baseRadius * rng.Range(0.72f, 1.18f);
            float height = baseHeight * rng.Range(0.74f, 1.22f);
            Quaternion rotation = Quaternion.Euler(0f, rng.Range(0f, 360f), 0f);
            Vector3 rotatedCenter = localCenter;

            Mesh terrainMesh = CreateIslandTerrainMesh(rotatedCenter, radiusX, radiusZ, height, settings.islandMeshResolution);
            GameObject terrain = CreateMeshObject(large ? "LargeIslandTerrain" : "SmallIslandTerrain", terrainMesh, manager.Materials.islandGrass, true);
            terrain.transform.localRotation = rotation;

            Mesh beachMesh = CreateBeachRingMesh(rotatedCenter, radiusX * 1.05f, radiusZ * 1.05f, radiusX * 0.72f, radiusZ * 0.72f, 72);
            GameObject beach = CreateMeshObject("BeachRing", beachMesh, manager.Materials.beachSand, false);
            beach.transform.localRotation = rotation;

            GenerateShoreRocks(rotatedCenter, radiusX, radiusZ, rng, large ? 15 : 7);

            if (withPort)
            {
                GeneratePort(rotatedCenter, Mathf.Max(radiusX, radiusZ), rng);
            }
            else if (large && rng.Chance(0.65f))
            {
                GenerateBirdFlock(rotatedCenter + Vector3.up * rng.Range(30f, 44f), rng);
            }

            GenerateAmbientDetails(rng, large ? 3 : 2);
        }

        private Mesh CreateIslandTerrainMesh(Vector3 localCenter, float radiusX, float radiusZ, float maxHeight, int resolution)
        {
            int res = Mathf.Max(4, resolution);
            int vertexCount = (res + 1) * (res + 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[res * res * 6];
            float water = manager.WaterLevel;

            for (int z = 0; z <= res; z++)
            {
                for (int x = 0; x <= res; x++)
                {
                    float tx = (float)x / res;
                    float tz = (float)z / res;
                    float px = (tx * 2f - 1f) * radiusX;
                    float pz = (tz * 2f - 1f) * radiusZ;
                    float nx = px / Mathf.Max(0.01f, radiusX);
                    float nz = pz / Mathf.Max(0.01f, radiusZ);
                    float radial = Mathf.Sqrt(nx * nx + nz * nz);
                    Vector3 world = transform.position + localCenter + new Vector3(px, 0f, pz);
                    float noise = FractalNoise(world.x, world.z);
                    float islandMask = Mathf.Clamp01(1f - radial);
                    float plateau = Mathf.SmoothStep(0f, 1f, islandMask);
                    float shoreCut = Mathf.SmoothStep(0.92f, 1f, radial);
                    float cliffBias = Mathf.SmoothStep(0.28f, 0.72f, 1f - radial);
                    float height = water - 0.85f + plateau * maxHeight * Mathf.Lerp(0.42f, 1.12f, noise);
                    height += cliffBias * maxHeight * 0.15f * FractalNoise(world.x + 91f, world.z - 47f);
                    height -= shoreCut * 0.85f;

                    if (radial > 1f)
                    {
                        height = water - 3f;
                    }

                    int index = z * (res + 1) + x;
                    vertices[index] = localCenter + new Vector3(px, height, pz);
                    uvs[index] = new Vector2(tx, tz);
                }
            }

            int triangleIndex = 0;
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int i0 = z * (res + 1) + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + res + 1;
                    int i3 = i2 + 1;
                    triangles[triangleIndex++] = i0;
                    triangles[triangleIndex++] = i2;
                    triangles[triangleIndex++] = i1;
                    triangles[triangleIndex++] = i1;
                    triangles[triangleIndex++] = i2;
                    triangles[triangleIndex++] = i3;
                }
            }

            Mesh mesh = new Mesh { name = $"IslandTerrain_{coordinate.x}_{coordinate.y}" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            generatedMeshes.Add(mesh);
            return mesh;
        }

        private Mesh CreateBeachRingMesh(Vector3 localCenter, float outerX, float outerZ, float innerX, float innerZ, int segments)
        {
            int segmentCount = Mathf.Max(12, segments);
            Vector3[] vertices = new Vector3[segmentCount * 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[segmentCount * 6];
            float water = manager.WaterLevel + manager.Settings.waterlinePadding;

            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i * Mathf.PI * 2f / segmentCount;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                vertices[i * 2] = localCenter + new Vector3(cos * outerX, water, sin * outerZ);
                vertices[i * 2 + 1] = localCenter + new Vector3(cos * innerX, water + 0.18f, sin * innerZ);
                uvs[i * 2] = new Vector2(0f, i / (float)segmentCount);
                uvs[i * 2 + 1] = new Vector2(1f, i / (float)segmentCount);
            }

            int triangleIndex = 0;
            for (int i = 0; i < segmentCount; i++)
            {
                int next = (i + 1) % segmentCount;
                int outerA = i * 2;
                int innerA = outerA + 1;
                int outerB = next * 2;
                int innerB = outerB + 1;
                triangles[triangleIndex++] = outerA;
                triangles[triangleIndex++] = innerA;
                triangles[triangleIndex++] = outerB;
                triangles[triangleIndex++] = outerB;
                triangles[triangleIndex++] = innerA;
                triangles[triangleIndex++] = innerB;
            }

            Mesh mesh = new Mesh { name = $"BeachRing_{coordinate.x}_{coordinate.y}" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            generatedMeshes.Add(mesh);
            return mesh;
        }

        private void GeneratePort(Vector3 islandCenter, float islandRadius, WorldRandom rng)
        {
            float angle = rng.Range(0f, Mathf.PI * 2f);
            Vector3 shoreDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, shoreDirection).normalized;
            Vector3 shore = islandCenter + shoreDirection * (islandRadius * 0.82f);
            float water = manager.WaterLevel;
            Quaternion dockRotation = Quaternion.LookRotation(shoreDirection, Vector3.up);

            CreateBox("MainDock", shore + shoreDirection * 34f + Vector3.up * (water + 0.55f), new Vector3(9f, 0.75f, 86f), dockRotation, manager.Materials.dockWood, true);
            CreateBox("CrossDock", shore + shoreDirection * 70f + Vector3.up * (water + 0.58f), new Vector3(58f, 0.72f, 8f), dockRotation, manager.Materials.dockWood, true);

            for (int i = -2; i <= 2; i++)
            {
                Vector3 postPosition = shore + shoreDirection * (20f + i * 18f) + right * 5.6f + Vector3.up * (water + 0.85f);
                CreateBox($"DockPost_R_{i}", postPosition, new Vector3(1.2f, 3.1f, 1.2f), Quaternion.identity, manager.Materials.dockWood, true);
                CreateBox($"DockPost_L_{i}", postPosition - right * 11.2f, new Vector3(1.2f, 3.1f, 1.2f), Quaternion.identity, manager.Materials.dockWood, true);
            }

            for (int i = 0; i < 5; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 basePosition = shore - shoreDirection * rng.Range(12f, 42f) + right * side * rng.Range(10f, 34f);
                float width = rng.Range(8f, 15f);
                float height = rng.Range(5f, 9f);
                float depth = rng.Range(9f, 16f);
                Quaternion rotation = Quaternion.LookRotation(-shoreDirection, Vector3.up) * Quaternion.Euler(0f, rng.Range(-8f, 8f), 0f);
                CreateBox($"PortBuilding_{i}", basePosition + Vector3.up * (water + 1.4f + height * 0.5f), new Vector3(width, height, depth), rotation, manager.Materials.buildingWall, true);
                CreateBox($"PortRoof_{i}", basePosition + Vector3.up * (water + 1.4f + height + 1.2f), new Vector3(width + 1.5f, 2.2f, depth + 1.5f), rotation, manager.Materials.buildingRoof, false);
            }

            Vector3 beaconBase = shore + right * 42f - shoreDirection * 18f + Vector3.up * (water + 6f);
            GameObject beacon = CreatePrimitive("HarborBeacon", PrimitiveType.Cylinder, manager.Materials.cliffRock, true);
            beacon.transform.localPosition = beaconBase;
            beacon.transform.localScale = new Vector3(2.2f, 6f, 2.2f);
            Light light = beacon.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 110f;
            light.intensity = 3.2f;
            light.color = new Color(1f, 0.72f, 0.42f);

            CreateSafeWaterBuoys(shore + shoreDirection * 115f, shoreDirection, right, rng, 7);
            CreateInteractivePortServices(shore, shoreDirection, right, dockRotation, rng);
            GenerateBirdFlock(islandCenter + Vector3.up * rng.Range(36f, 52f), rng);
        }

        private void CreateInteractivePortServices(Vector3 shore, Vector3 shoreDirection, Vector3 right, Quaternion dockRotation, WorldRandom rng)
        {
            GameObject portRoot = new GameObject("InteractivePort");
            portRoot.layer = manager.WorldLayer;
            portRoot.transform.SetParent(transform, false);
            portRoot.transform.localPosition = Vector3.zero;
            PortManager port = portRoot.AddComponent<PortManager>();

            GameObject portZoneObject = new GameObject("PortZone");
            portZoneObject.layer = manager.WorldLayer;
            portZoneObject.transform.SetParent(portRoot.transform, false);
            portZoneObject.transform.localPosition = shore + shoreDirection * 42f + Vector3.up * (manager.WaterLevel + 2f);
            BoxCollider portCollider = portZoneObject.AddComponent<BoxCollider>();
            portCollider.isTrigger = true;
            portCollider.size = new Vector3(150f, 22f, 160f);
            portZoneObject.AddComponent<PortZone>();

            Transform anchor = new GameObject("DockAnchor").transform;
            anchor.SetParent(portRoot.transform, false);
            anchor.localPosition = shore + shoreDirection * 66f + Vector3.up * (manager.WaterLevel + 0.95f);
            anchor.localRotation = dockRotation;
            anchor.gameObject.layer = manager.WorldLayer;

            GameObject zoneObject = new GameObject("DockingZone");
            zoneObject.layer = manager.WorldLayer;
            zoneObject.transform.SetParent(portRoot.transform, false);
            zoneObject.transform.localPosition = shore + shoreDirection * 66f + Vector3.up * (manager.WaterLevel + 1.2f);
            zoneObject.transform.localRotation = dockRotation;
            BoxCollider dockingCollider = zoneObject.AddComponent<BoxCollider>();
            dockingCollider.isTrigger = true;
            dockingCollider.size = new Vector3(34f, 8f, 38f);
            DockingZone docking = zoneObject.AddComponent<DockingZone>();

            port.Configure($"Port {coordinate.x}:{coordinate.y}", anchor, docking);
            port.RegisterService(CreatePortService<ContractBoard>("ContractBoard", shore + shoreDirection * 62f + Vector3.up * (manager.WaterLevel + 1.25f), manager.Materials.dockWood));
            port.RegisterService(CreatePortService<ResourceMerchant>("ResourceMerchant", shore + shoreDirection * 70f - right * 18f + Vector3.up * (manager.WaterLevel + 1.4f), manager.Materials.buildingWall));
            port.RegisterService(CreatePortService<ShipUpgradeMerchant>("ShipUpgradeMerchant", shore + shoreDirection * 78f + right * 18f + Vector3.up * (manager.WaterLevel + 1.4f), manager.Materials.buildingRoof));
            port.RegisterService(CreatePortService<RepairMerchant>("RepairMerchant", shore + shoreDirection * 88f + Vector3.up * (manager.WaterLevel + 1.4f), manager.Materials.dockWood));
            port.RegisterService(CreatePortService<RumorSource>("RumorSource", shore + shoreDirection * 94f - right * 16f + Vector3.up * (manager.WaterLevel + 1.4f), manager.Materials.buildingWall));

            GameObject calmWater = new GameObject("HarborCalmCurrent");
            calmWater.layer = manager.WorldLayer;
            calmWater.transform.SetParent(portRoot.transform, false);
            calmWater.transform.localPosition = shore + shoreDirection * 74f;
            CurrentZone current = calmWater.AddComponent<CurrentZone>();
            current.Configure(54f, Mathf.Atan2(-shoreDirection.x, -shoreDirection.z) * Mathf.Rad2Deg, 0.35f, 18f);
        }

        private void CreateDiscoverableLocation(Vector3 localCenter)
        {
            GameObject marker = new GameObject($"Discoverable_{poiType}");
            marker.layer = manager.WorldLayer;
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = localCenter + Vector3.up * (manager.WaterLevel + 1.5f);

            DiscoverableLocation discoverable = marker.AddComponent<DiscoverableLocation>();
            discoverable.Configure(
                $"poi_{coordinate.x}_{coordinate.y}_{poiType}",
                manager.GetPoiDisplayName(poiType, coordinate),
                ResolveDiscoveryType(poiType),
                poiType,
                ResolveDiscoveryRadius(poiType),
                ResolveDiscoveryCoins(poiType),
                ResolveDiscoveryResourceType(poiType),
                ResolveDiscoveryResourceAmount(poiType));
        }

        private static DiscoveryType ResolveDiscoveryType(MaritimePoiType type)
        {
            switch (type)
            {
                case MaritimePoiType.Port:
                    return DiscoveryType.Port;
                case MaritimePoiType.Shipwreck:
                    return DiscoveryType.Shipwreck;
                case MaritimePoiType.DangerZone:
                case MaritimePoiType.RockCluster:
                    return DiscoveryType.DangerZone;
                default:
                    return DiscoveryType.Island;
            }
        }

        private static float ResolveDiscoveryRadius(MaritimePoiType type)
        {
            switch (type)
            {
                case MaritimePoiType.Port:
                    return 160f;
                case MaritimePoiType.LargeIsland:
                    return 150f;
                case MaritimePoiType.DangerZone:
                    return 135f;
                case MaritimePoiType.Shipwreck:
                    return 115f;
                default:
                    return 100f;
            }
        }

        private static int ResolveDiscoveryCoins(MaritimePoiType type)
        {
            switch (type)
            {
                case MaritimePoiType.Port:
                    return 18;
                case MaritimePoiType.Shipwreck:
                    return 14;
                case MaritimePoiType.DangerZone:
                    return 16;
                default:
                    return 8;
            }
        }

        private static ResourceType ResolveDiscoveryResourceType(MaritimePoiType type)
        {
            switch (type)
            {
                case MaritimePoiType.Shipwreck:
                case MaritimePoiType.DangerZone:
                    return ResourceType.Iron;
                case MaritimePoiType.Port:
                    return ResourceType.Rope;
                default:
                    return ResourceType.Wood;
            }
        }

        private static int ResolveDiscoveryResourceAmount(MaritimePoiType type)
        {
            switch (type)
            {
                case MaritimePoiType.Shipwreck:
                    return 1;
                case MaritimePoiType.Port:
                    return 1;
                default:
                    return 0;
            }
        }

        private T CreatePortService<T>(string objectName, Vector3 localPosition, Material material) where T : PortServicePoint
        {
            GameObject service = CreatePrimitive(objectName, PrimitiveType.Cylinder, material, false);
            service.layer = LayerMask.NameToLayer("Interactable") >= 0 ? LayerMask.NameToLayer("Interactable") : manager.WorldLayer;
            service.transform.localPosition = localPosition;
            service.transform.localScale = new Vector3(0.8f, 1.25f, 0.8f);
            CapsuleCollider collider = service.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.radius = 0.65f;
            collider.height = 2.2f;
            return service.AddComponent<T>();
        }

        private void GenerateRockCluster(Vector3 localCenter, WorldRandom rng, bool withBuoys)
        {
            int count = rng.Range(8, 16);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = rng.InsideUnitCircle() * rng.Range(12f, 58f);
                Vector3 position = localCenter + new Vector3(offset.x, manager.WaterLevel + rng.Range(0.2f, 4.8f), offset.y);
                CreateRock($"Rock_{i}", position, rng.Range(4f, 14f), rng);
            }

            if (withBuoys)
            {
                Vector3 direction = new Vector3(rng.Range(-1f, 1f), 0f, rng.Range(-1f, 1f)).normalized;
                if (direction.sqrMagnitude < 0.1f)
                {
                    direction = Vector3.forward;
                }

                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
                CreateSafeWaterBuoys(localCenter + direction * 76f, direction, right, rng, 3);
            }
        }

        private void GenerateShipwreck(Vector3 localCenter, WorldRandom rng)
        {
            Quaternion wreckRotation = Quaternion.Euler(0f, rng.Range(0f, 360f), rng.Range(-8f, 8f));
            float water = manager.WaterLevel;

            CreateBox("BrokenHull_A", localCenter + Vector3.up * (water + 0.35f), new Vector3(8f, 1.5f, 34f), wreckRotation * Quaternion.Euler(0f, -11f, 7f), manager.Materials.wreckWood, true);
            CreateBox("BrokenHull_B", localCenter + wreckRotation * new Vector3(7f, 0.2f, 4f) + Vector3.up * water, new Vector3(6f, 1.3f, 26f), wreckRotation * Quaternion.Euler(0f, 16f, -9f), manager.Materials.wreckWood, true);
            CreateBox("FallenMast", localCenter + wreckRotation * new Vector3(-5f, 1.1f, -6f) + Vector3.up * water, new Vector3(1.1f, 1.1f, 46f), wreckRotation * Quaternion.Euler(84f, 0f, 23f), manager.Materials.dockWood, true);

            for (int i = 0; i < 7; i++)
            {
                Vector2 offset = rng.InsideUnitCircle() * rng.Range(8f, 42f);
                GameObject debris = CreateBox($"FloatingDebris_{i}", localCenter + new Vector3(offset.x, water + 0.2f, offset.y), new Vector3(rng.Range(1.2f, 2.6f), 0.35f, rng.Range(3f, 8f)), Quaternion.Euler(rng.Range(-8f, 8f), rng.Range(0f, 360f), rng.Range(-8f, 8f)), manager.Materials.wreckWood, false);
                debris.AddComponent<OceanSurfaceFollower>().Configure(0.12f, 0.08f, rng.Range(0.35f, 0.65f), rng.InsideUnitCircle(), rng.Range(0.02f, 0.08f), true);
            }

            GenerateRockCluster(localCenter + new Vector3(48f, 0f, -32f), rng, false);
            GenerateAmbientDetails(rng, 2);
        }

        private void GenerateDangerZone(Vector3 localCenter, WorldRandom rng)
        {
            GenerateRockCluster(localCenter, rng, false);
            int buoyCount = 8;
            for (int i = 0; i < buoyCount; i++)
            {
                float angle = i * Mathf.PI * 2f / buoyCount;
                Vector3 position = localCenter + new Vector3(Mathf.Cos(angle) * 92f, manager.WaterLevel, Mathf.Sin(angle) * 92f);
                CreateBuoy($"DangerBuoy_{i}", position, i % 2 == 0 ? manager.Materials.buoyRed : manager.Materials.buoyWhite, 1.25f, 3.4f, rng);
            }

            GameObject warningLight = CreatePrimitive("DangerLight", PrimitiveType.Sphere, manager.Materials.buoyRed, false);
            warningLight.transform.localPosition = localCenter + Vector3.up * (manager.WaterLevel + 9f);
            warningLight.transform.localScale = Vector3.one * 2.4f;
            Light light = warningLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 72f;
            light.intensity = 1.7f;
            light.color = new Color(1f, 0.18f, 0.08f);

            GameObject currentObject = new GameObject("DangerCurrentZone");
            currentObject.layer = manager.WorldLayer;
            currentObject.transform.SetParent(transform, false);
            currentObject.transform.localPosition = localCenter;
            CurrentZone currentZone = currentObject.AddComponent<CurrentZone>();
            currentZone.Configure(96f, rng.Range(0f, 360f), rng.Range(2.2f, 4.2f), 28f);
        }

        private void GenerateShoreRocks(Vector3 localCenter, float radiusX, float radiusZ, WorldRandom rng, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = rng.Range(0f, Mathf.PI * 2f);
                float distance = rng.Range(0.82f, 1.05f);
                Vector3 position = localCenter + new Vector3(Mathf.Cos(angle) * radiusX * distance, manager.WaterLevel + rng.Range(0.2f, 2.4f), Mathf.Sin(angle) * radiusZ * distance);
                CreateRock($"ShoreRock_{i}", position, rng.Range(2.2f, 7.5f), rng);
            }
        }

        private void GenerateOpenWaterAmbience(WorldRandom rng)
        {
            if (rng.Chance(0.18f))
            {
                Vector3 position = GetPoiLocalCenter(ref rng);
                CreateBuoy("LonelyBuoy", position, rng.Chance(0.5f) ? manager.Materials.buoyWhite : manager.Materials.buoyRed, 0.9f, 2.6f, rng);
            }

            if (rng.Chance(0.16f))
            {
                GenerateAmbientDetails(rng, 1);
            }
        }

        private void GenerateAmbientDetails(WorldRandom rng, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 local = GetPoiLocalCenter(ref rng) + new Vector3(rng.Range(-25f, 25f), manager.WaterLevel + 0.1f, rng.Range(-25f, 25f));
                GameObject debris = CreateBox($"SeaDebris_{i}", local, new Vector3(rng.Range(0.6f, 1.4f), 0.22f, rng.Range(2.0f, 4.5f)), Quaternion.Euler(0f, rng.Range(0f, 360f), 0f), manager.Materials.wreckWood, false);
                debris.AddComponent<OceanSurfaceFollower>().Configure(0.08f, 0.06f, rng.Range(0.28f, 0.55f), rng.InsideUnitCircle(), rng.Range(0.01f, 0.05f), true);
            }
        }

        private void CreateSafeWaterBuoys(Vector3 start, Vector3 forward, Vector3 right, WorldRandom rng, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float row = i / 2;
                Vector3 position = start + forward * (row * 28f) + right * side * 22f;
                CreateBuoy($"HarborBuoy_{i}", position, side < 0f ? manager.Materials.buoyRed : manager.Materials.buoyWhite, 0.95f, 2.8f, rng);
            }
        }

        private void CreateBuoy(string objectName, Vector3 localPosition, Material material, float radius, float height, WorldRandom rng)
        {
            GameObject buoy = CreatePrimitive(objectName, PrimitiveType.Cylinder, material, false);
            buoy.transform.localPosition = new Vector3(localPosition.x, manager.WaterLevel + 0.3f, localPosition.z);
            buoy.transform.localScale = new Vector3(radius, height * 0.5f, radius);
            buoy.AddComponent<OceanSurfaceFollower>().Configure(0.18f, 0.09f, rng.Range(0.32f, 0.62f), Vector2.zero, 0f, true);
        }

        private void GenerateBirdFlock(Vector3 localCenter, WorldRandom rng)
        {
            GameObject flock = new GameObject("BirdFlock");
            flock.transform.SetParent(transform, false);
            flock.transform.localPosition = localCenter;
            flock.layer = manager.WorldLayer;
            SimpleBirdFlock flockMover = flock.AddComponent<SimpleBirdFlock>();
            flockMover.Configure(rng.Range(18f, 34f), rng.Range(0.18f, 0.34f), rng.Range(1.5f, 4.5f));

            int birdCount = rng.Range(4, 8);
            for (int i = 0; i < birdCount; i++)
            {
                GameObject bird = new GameObject($"Bird_{i}");
                bird.transform.SetParent(flock.transform, false);
                bird.layer = manager.WorldLayer;
                GameObject leftWing = CreatePrimitive("LeftWing", PrimitiveType.Cube, manager.Materials.silhouette, false, bird.transform);
                leftWing.transform.localPosition = new Vector3(-0.22f, 0f, 0f);
                leftWing.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
                leftWing.transform.localScale = new Vector3(0.45f, 0.025f, 0.08f);
                GameObject rightWing = CreatePrimitive("RightWing", PrimitiveType.Cube, manager.Materials.silhouette, false, bird.transform);
                rightWing.transform.localPosition = new Vector3(0.22f, 0f, 0f);
                rightWing.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
                rightWing.transform.localScale = new Vector3(0.45f, 0.025f, 0.08f);
            }
        }

        private GameObject CreateRock(string objectName, Vector3 localPosition, float size, WorldRandom rng)
        {
            GameObject rock = CreatePrimitive(objectName, PrimitiveType.Sphere, manager.Materials.cliffRock, true);
            rock.transform.localPosition = localPosition;
            rock.transform.localRotation = Quaternion.Euler(rng.Range(-12f, 12f), rng.Range(0f, 360f), rng.Range(-12f, 12f));
            rock.transform.localScale = new Vector3(size * rng.Range(0.75f, 1.45f), size * rng.Range(0.45f, 1.25f), size * rng.Range(0.75f, 1.45f));
            return rock;
        }

        private GameObject CreateBox(string objectName, Vector3 localPosition, Vector3 scale, Quaternion rotation, Material material, bool keepCollider)
        {
            GameObject box = CreatePrimitive(objectName, PrimitiveType.Cube, material, keepCollider);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = rotation;
            box.transform.localScale = scale;
            return box;
        }

        private GameObject CreatePrimitive(string objectName, PrimitiveType primitiveType, Material material, bool keepCollider, Transform parentOverride = null)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.layer = manager.WorldLayer;
            primitive.transform.SetParent(parentOverride != null ? parentOverride : transform, false);

            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!keepCollider)
            {
                Collider collider = primitive.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(collider);
                    }
                    else
                    {
                        DestroyImmediate(collider);
                    }
                }
            }

            return primitive;
        }

        private GameObject CreateMeshObject(string objectName, Mesh mesh, Material material, bool withCollider)
        {
            GameObject meshObject = new GameObject(objectName);
            meshObject.layer = manager.WorldLayer;
            meshObject.transform.SetParent(transform, false);
            MeshFilter filter = meshObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            if (withCollider)
            {
                MeshCollider collider = meshObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }

            return meshObject;
        }

        private float FractalNoise(float x, float z)
        {
            float seedOffset = manager.Seed * 0.0137f;
            float n0 = Mathf.PerlinNoise(x * 0.0085f + seedOffset, z * 0.0085f - seedOffset);
            float n1 = Mathf.PerlinNoise(x * 0.021f - seedOffset * 0.37f, z * 0.021f + seedOffset * 0.61f);
            float n2 = Mathf.PerlinNoise(x * 0.047f + 11.7f, z * 0.047f - 4.2f);
            return Mathf.Clamp01(n0 * 0.58f + n1 * 0.29f + n2 * 0.13f);
        }

        private void OnDrawGizmosSelected()
        {
            if (manager == null || manager.Settings == null || !manager.Settings.drawChunkGizmos)
            {
                return;
            }

            Gizmos.color = isDistant ? new Color(0.2f, 0.5f, 1f, 0.25f) : new Color(0.1f, 1f, 0.35f, 0.35f);
            Gizmos.DrawWireCube(transform.position, new Vector3(manager.Settings.chunkSize, 24f, manager.Settings.chunkSize));

            if (poiType != MaritimePoiType.OpenWater)
            {
                Gizmos.color = poiType == MaritimePoiType.Port ? new Color(1f, 0.76f, 0.24f, 0.9f) : new Color(0.72f, 1f, 0.45f, 0.7f);
                Gizmos.DrawSphere(poiWorldPosition, 8f);
            }

#if UNITY_EDITOR
            if (manager.Settings.drawPoiLabels && poiType != MaritimePoiType.OpenWater)
            {
                Handles.Label(poiWorldPosition + Vector3.up * 16f, $"{poiType}\n{coordinate.x}, {coordinate.y}");
            }
#endif
        }
    }
}
