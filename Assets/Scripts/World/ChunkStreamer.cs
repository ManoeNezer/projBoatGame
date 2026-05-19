using System.Collections.Generic;
using UnityEngine;

namespace BoatGame.World
{
    [DisallowMultipleComponent]
    public sealed class ChunkStreamer : MonoBehaviour
    {
        [SerializeField] private WorldManager manager;

        private readonly Dictionary<Vector2Int, WorldChunk> chunks = new Dictionary<Vector2Int, WorldChunk>(96);
        private readonly HashSet<Vector2Int> wantedCoordinates = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> coordinatesToRemove = new List<Vector2Int>(32);
        private readonly Stack<WorldChunk> pooledChunks = new Stack<WorldChunk>(32);
        private float nextRefreshTime;
        private Vector2Int lastCenterCoordinate = new Vector2Int(int.MinValue, int.MinValue);

        public IReadOnlyDictionary<Vector2Int, WorldChunk> LoadedChunks => chunks;

        private void Awake()
        {
            if (manager == null)
            {
                manager = GetComponent<WorldManager>();
            }
        }

        private void Update()
        {
            if (manager == null || manager.StreamingTarget == null)
            {
                return;
            }

            if (Time.time < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.time + manager.Settings.updateInterval;
            Refresh();
        }

        public void Configure(WorldManager worldManager)
        {
            manager = worldManager;
        }

        public void ForceRefresh()
        {
            nextRefreshTime = 0f;
            Refresh();
        }

        private void Refresh()
        {
            if (manager == null || manager.StreamingTarget == null)
            {
                return;
            }

            WorldGenerationSettings settings = manager.Settings;
            Vector2Int center = manager.WorldToChunk(manager.StreamingTarget.position);
            if (center == lastCenterCoordinate && wantedCoordinates.Count > 0 && Time.time < nextRefreshTime - settings.updateInterval * 0.5f)
            {
                return;
            }

            lastCenterCoordinate = center;
            wantedCoordinates.Clear();

            int fullRadius = Mathf.Max(1, settings.fullChunkRadius);
            int silhouetteRadius = Mathf.Max(fullRadius, settings.silhouetteChunkRadius);

            for (int z = -silhouetteRadius; z <= silhouetteRadius; z++)
            {
                for (int x = -silhouetteRadius; x <= silhouetteRadius; x++)
                {
                    Vector2Int coordinate = new Vector2Int(center.x + x, center.y + z);
                    wantedCoordinates.Add(coordinate);
                    bool distant = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) > fullRadius;
                    LoadOrUpdateChunk(coordinate, distant);
                }
            }

            coordinatesToRemove.Clear();
            foreach (KeyValuePair<Vector2Int, WorldChunk> pair in chunks)
            {
                if (!wantedCoordinates.Contains(pair.Key))
                {
                    coordinatesToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < coordinatesToRemove.Count; i++)
            {
                UnloadChunk(coordinatesToRemove[i]);
            }
        }

        private void LoadOrUpdateChunk(Vector2Int coordinate, bool distant)
        {
            if (chunks.TryGetValue(coordinate, out WorldChunk chunk))
            {
                if (chunk.IsDistant != distant || chunk.PoiType != manager.GetPoiType(coordinate))
                {
                    chunk.Build(manager, coordinate, distant);
                }

                return;
            }

            chunk = GetChunkShell();
            chunk.Build(manager, coordinate, distant);
            chunks.Add(coordinate, chunk);
        }

        private WorldChunk GetChunkShell()
        {
            WorldChunk chunk;
            if (pooledChunks.Count > 0)
            {
                chunk = pooledChunks.Pop();
                chunk.gameObject.SetActive(true);
                return chunk;
            }

            GameObject chunkObject = new GameObject("WorldChunk");
            chunkObject.transform.SetParent(transform, false);
            return chunkObject.AddComponent<WorldChunk>();
        }

        private void UnloadChunk(Vector2Int coordinate)
        {
            if (!chunks.TryGetValue(coordinate, out WorldChunk chunk))
            {
                return;
            }

            chunks.Remove(coordinate);
            chunk.Clear();
            chunk.gameObject.SetActive(false);
            pooledChunks.Push(chunk);
        }

        private void OnDrawGizmosSelected()
        {
            if (manager == null || manager.Settings == null || !manager.Settings.drawChunkGizmos)
            {
                return;
            }

            foreach (KeyValuePair<Vector2Int, WorldChunk> pair in chunks)
            {
                WorldChunk chunk = pair.Value;
                if (chunk == null)
                {
                    continue;
                }

                Gizmos.color = chunk.IsDistant ? new Color(0.25f, 0.55f, 1f, 0.14f) : new Color(0.1f, 1f, 0.45f, 0.22f);
                Gizmos.DrawWireCube(chunk.transform.position, new Vector3(manager.Settings.chunkSize, 20f, manager.Settings.chunkSize));
            }
        }
    }
}
