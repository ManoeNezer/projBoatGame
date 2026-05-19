using System.Collections.Generic;
using BoatGame.Boat;
using BoatGame.Player;
using BoatGame.Water;
using UnityEngine;

namespace BoatGame.World
{
    [DefaultExecutionOrder(-80)]
    [DisallowMultipleComponent]
    public sealed class WorldManager : MonoBehaviour
    {
        public static WorldManager Instance { get; private set; }

        [Header("World")]
        [SerializeField] private int seed = 177013;
        [SerializeField] private Transform streamingTarget;
        [SerializeField] private bool autoFindTarget = true;
        [SerializeField] private WorldGenerationSettings settings = new WorldGenerationSettings();
        [SerializeField] private WorldMaterialSet materials = new WorldMaterialSet();

        [Header("Runtime")]
        [SerializeField] private ChunkStreamer streamer;

        private int worldLayer;
        private readonly Dictionary<Vector2Int, MaritimePoiType> guaranteedPois = new Dictionary<Vector2Int, MaritimePoiType>(16);

        public readonly struct WorldPoiInfo
        {
            public WorldPoiInfo(Vector2Int coordinate, MaritimePoiType type, Vector3 position, string displayName)
            {
                Coordinate = coordinate;
                Type = type;
                Position = position;
                DisplayName = displayName;
            }

            public Vector2Int Coordinate { get; }
            public MaritimePoiType Type { get; }
            public Vector3 Position { get; }
            public string DisplayName { get; }
        }

        public int Seed => seed;
        public Transform StreamingTarget => streamingTarget;
        public WorldGenerationSettings Settings => settings;
        public WorldMaterialSet Materials => materials;
        public int WorldLayer => worldLayer >= 0 ? worldLayer : 0;
        public float WaterLevel => WaterManager.Instance != null ? WaterManager.Instance.WaterLevel : 0f;

        private void Awake()
        {
            RegisterSingleton();
            ResolveLayer();
            EnsureStreamer();
            EnsureFallbackMaterials();
        }

        private void OnEnable()
        {
            RegisterSingleton();
            ResolveLayer();
            EnsureStreamer();
        }

        private void Start()
        {
            FindTargetIfNeeded();
            streamer.ForceRefresh();
        }

        private void Update()
        {
            if (autoFindTarget && streamingTarget == null)
            {
                FindTargetIfNeeded();
            }
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Configure(Transform target, WorldMaterialSet materialSet, int worldSeed)
        {
            streamingTarget = target;
            seed = worldSeed;
            if (materialSet != null)
            {
                materials = materialSet;
            }

            ResolveLayer();
            EnsureStreamer();
            EnsureFallbackMaterials();
        }

        public Vector2Int WorldToChunk(Vector3 position)
        {
            float chunkSize = Mathf.Max(1f, settings.chunkSize);
            return new Vector2Int(
                Mathf.FloorToInt(position.x / chunkSize),
                Mathf.FloorToInt(position.z / chunkSize));
        }

        public Vector3 GetChunkCenter(Vector2Int coordinate)
        {
            return WorldRules.GetChunkCenter(coordinate, settings.chunkSize);
        }

        public MaritimePoiType GetPoiType(Vector2Int coordinate)
        {
            if (guaranteedPois.TryGetValue(coordinate, out MaritimePoiType guaranteedType))
            {
                return guaranteedType;
            }

            return WorldRules.ChoosePoiType(seed, coordinate, settings);
        }

        public bool TryFindPoi(Vector3 origin, MaritimePoiType[] allowedTypes, float minDistance, float maxDistance, out WorldPoiInfo poi)
        {
            poi = default;
            if (allowedTypes == null || allowedTypes.Length == 0)
            {
                return false;
            }

            Vector2Int center = WorldToChunk(origin);
            float chunkSize = Mathf.Max(1f, settings.chunkSize);
            int radius = Mathf.Max(1, Mathf.CeilToInt(maxDistance / chunkSize));
            float minSqr = minDistance * minDistance;
            float maxSqr = maxDistance * maxDistance;
            float bestSqr = float.MaxValue;
            bool found = false;

            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    Vector2Int coordinate = new Vector2Int(center.x + x, center.y + z);
                    MaritimePoiType type = GetPoiType(coordinate);
                    if (!ContainsType(allowedTypes, type))
                    {
                        continue;
                    }

                    Vector3 position = GetChunkCenter(coordinate);
                    float sqr = (position - origin).sqrMagnitude;
                    if (sqr < minSqr || sqr > maxSqr || sqr >= bestSqr)
                    {
                        continue;
                    }

                    bestSqr = sqr;
                    poi = new WorldPoiInfo(coordinate, type, position, GetPoiDisplayName(type, coordinate));
                    found = true;
                }
            }

            return found;
        }

        public WorldPoiInfo EnsurePoiNear(Vector3 origin, MaritimePoiType requestedType, float minDistance, float maxDistance)
        {
            MaritimePoiType[] types = { requestedType };
            if (TryFindPoi(origin, types, minDistance, maxDistance, out WorldPoiInfo existing))
            {
                return existing;
            }

            Vector2Int center = WorldToChunk(origin);
            float chunkSize = Mathf.Max(1f, settings.chunkSize);
            int minRadius = Mathf.Max(1, Mathf.FloorToInt(minDistance / chunkSize));
            int maxRadius = Mathf.Max(minRadius + 1, Mathf.CeilToInt(maxDistance / chunkSize));

            for (int radius = minRadius; radius <= maxRadius; radius++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) != radius)
                        {
                            continue;
                        }

                        Vector2Int coordinate = new Vector2Int(center.x + x, center.y + z);
                        if (Vector2.Distance(new Vector2(coordinate.x, coordinate.y), Vector2.zero) < 1.5f)
                        {
                            continue;
                        }

                        guaranteedPois[coordinate] = requestedType;
                        Vector3 position = GetChunkCenter(coordinate);
                        streamer?.ForceRefresh();
                        return new WorldPoiInfo(coordinate, requestedType, position, GetPoiDisplayName(requestedType, coordinate));
                    }
                }
            }

            Vector2Int fallback = new Vector2Int(center.x + maxRadius, center.y);
            guaranteedPois[fallback] = requestedType;
            return new WorldPoiInfo(fallback, requestedType, GetChunkCenter(fallback), GetPoiDisplayName(requestedType, fallback));
        }

        public string GetPoiDisplayName(MaritimePoiType type, Vector2Int coordinate)
        {
            string prefix;
            switch (type)
            {
                case MaritimePoiType.Port:
                    prefix = "Port";
                    break;
                case MaritimePoiType.LargeIsland:
                    prefix = "Grande ile";
                    break;
                case MaritimePoiType.SmallIsland:
                    prefix = "Ile";
                    break;
                case MaritimePoiType.Shipwreck:
                    prefix = "Epave";
                    break;
                case MaritimePoiType.DangerZone:
                    prefix = "Passe dangereuse";
                    break;
                case MaritimePoiType.RockCluster:
                    prefix = "Recifs";
                    break;
                default:
                    prefix = "Mer ouverte";
                    break;
            }

            return $"{prefix} {Mathf.Abs(coordinate.x * 17 + coordinate.y * 31) % 997:000}";
        }

        private static bool ContainsType(MaritimePoiType[] allowedTypes, MaritimePoiType type)
        {
            for (int i = 0; i < allowedTypes.Length; i++)
            {
                if (allowedTypes[i] == type)
                {
                    return true;
                }
            }

            return false;
        }

        private void RegisterSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Multiple WorldManager instances found. Disabling duplicate on {name}.", this);
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void EnsureStreamer()
        {
            if (streamer == null)
            {
                streamer = GetComponent<ChunkStreamer>();
            }

            if (streamer == null)
            {
                streamer = gameObject.AddComponent<ChunkStreamer>();
            }

            streamer.Configure(this);
        }

        private void FindTargetIfNeeded()
        {
            if (streamingTarget != null)
            {
                return;
            }

            BoatHelmController boat = FindFirstObjectByType<BoatHelmController>();
            if (boat != null)
            {
                streamingTarget = boat.transform;
                return;
            }

            FpsPlayerController player = FindFirstObjectByType<FpsPlayerController>();
            if (player != null)
            {
                streamingTarget = player.transform;
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                streamingTarget = mainCamera.transform;
            }
        }

        private void ResolveLayer()
        {
            worldLayer = LayerMask.NameToLayer("World");
            if (worldLayer < 0)
            {
                worldLayer = 0;
            }
        }

        private void EnsureFallbackMaterials()
        {
            materials ??= new WorldMaterialSet();
            materials.islandGrass ??= CreateRuntimeMaterial("RuntimeIslandGrass", new Color(0.16f, 0.32f, 0.15f), 0.42f, 0f);
            materials.beachSand ??= CreateRuntimeMaterial("RuntimeBeachSand", new Color(0.62f, 0.53f, 0.37f), 0.58f, 0f);
            materials.cliffRock ??= CreateRuntimeMaterial("RuntimeCliffRock", new Color(0.28f, 0.28f, 0.25f), 0.32f, 0f);
            materials.dockWood ??= CreateRuntimeMaterial("RuntimeDockWood", new Color(0.28f, 0.16f, 0.08f), 0.38f, 0f);
            materials.buildingWall ??= CreateRuntimeMaterial("RuntimeBuildingWall", new Color(0.46f, 0.38f, 0.28f), 0.5f, 0f);
            materials.buildingRoof ??= CreateRuntimeMaterial("RuntimeBuildingRoof", new Color(0.23f, 0.08f, 0.06f), 0.36f, 0f);
            materials.buoyRed ??= CreateRuntimeMaterial("RuntimeBuoyRed", new Color(0.62f, 0.08f, 0.06f), 0.28f, 0f);
            materials.buoyWhite ??= CreateRuntimeMaterial("RuntimeBuoyWhite", new Color(0.86f, 0.83f, 0.72f), 0.42f, 0f);
            materials.wreckWood ??= CreateRuntimeMaterial("RuntimeWreckWood", new Color(0.17f, 0.1f, 0.06f), 0.34f, 0f);
            materials.silhouette ??= CreateRuntimeMaterial("RuntimeIslandSilhouette", new Color(0.08f, 0.12f, 0.11f), 0.65f, 0f);
        }

        private static Material CreateRuntimeMaterial(string materialName, Color color, float smoothness, float metallic)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader) { name = materialName };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            return material;
        }

        private void OnDrawGizmosSelected()
        {
            if (settings == null || !settings.drawRouteGizmos)
            {
                return;
            }

            float chunkSize = Mathf.Max(1f, settings.chunkSize);
            Vector3 reference = streamingTarget != null ? streamingTarget.position : transform.position;
            int currentX = Mathf.FloorToInt(reference.x / chunkSize);
            int minX = currentX - settings.silhouetteChunkRadius - 2;
            int maxX = currentX + settings.silhouetteChunkRadius + 2;

            Gizmos.color = new Color(0.35f, 0.75f, 1f, 0.32f);
            Vector3 previous = Vector3.zero;
            bool hasPrevious = false;
            for (int x = minX; x <= maxX; x++)
            {
                int z = Mathf.RoundToInt(x * settings.routeSlope);
                Vector3 point = new Vector3((x + 0.5f) * chunkSize, WaterLevel + 0.35f, (z + 0.5f) * chunkSize);
                Gizmos.DrawWireSphere(point, 12f);
                if (hasPrevious)
                {
                    Gizmos.DrawLine(previous, point);
                }

                previous = point;
                hasPrevious = true;
            }
        }
    }
}
