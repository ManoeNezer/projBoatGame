using System;
using UnityEngine;

namespace BoatGame.World
{
    [Serializable]
    public sealed class WorldGenerationSettings
    {
        [Header("World Scale")]
        [Min(80f)] public float chunkSize = 420f;
        [Min(0f)] public float safeRadiusFromOrigin = 520f;
        [Min(0f)] public float waterlinePadding = 0.05f;

        [Header("Streaming")]
        [Min(0.1f)] public float updateInterval = 0.45f;
        [Range(1, 5)] public int fullChunkRadius = 2;
        [Range(2, 7)] public int silhouetteChunkRadius = 4;

        [Header("Routes")]
        [Range(-2f, 2f)] public float routeSlope = 0.38f;
        [Range(0, 2)] public int routeHalfWidthChunks = 1;
        [Range(2, 8)] public int portSpacingChunks = 3;
        [Range(0, 7)] public int portPhase = 2;
        [Range(1, 6)] public int minPortDistanceChunks = 2;

        [Header("POI Density")]
        [Range(0f, 1f)] public float routePoiChance = 0.76f;
        [Range(0f, 1f)] public float openSeaPoiChance = 0.38f;
        [Range(0f, 1f)] public float largeIslandChance = 0.16f;
        [Range(0f, 1f)] public float shipwreckChance = 0.08f;
        [Range(0f, 1f)] public float dangerChance = 0.1f;

        [Header("Island Shape")]
        [Range(8, 48)] public int islandMeshResolution = 28;
        [Range(6, 18)] public int distantMeshResolution = 10;
        [Min(12f)] public float smallIslandRadius = 58f;
        [Min(20f)] public float largeIslandRadius = 116f;
        [Min(1f)] public float smallIslandHeight = 14f;
        [Min(2f)] public float largeIslandHeight = 34f;

        [Header("Debug")]
        public bool drawChunkGizmos = true;
        public bool drawRouteGizmos = true;
        public bool drawPoiLabels = true;
    }
}
