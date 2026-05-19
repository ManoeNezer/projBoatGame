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
            return WorldRules.ChoosePoiType(seed, coordinate, settings);
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
