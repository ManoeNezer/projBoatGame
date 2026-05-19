using UnityEngine;

namespace BoatGame.World
{
    public static class WorldRules
    {
        public static bool IsOnMainRoute(Vector2Int coordinate, WorldGenerationSettings settings)
        {
            int routeZ = Mathf.RoundToInt(coordinate.x * settings.routeSlope);
            return Mathf.Abs(coordinate.y - routeZ) <= settings.routeHalfWidthChunks;
        }

        public static bool IsForcedPort(Vector2Int coordinate, WorldGenerationSettings settings)
        {
            if (!IsOnMainRoute(coordinate, settings))
            {
                return false;
            }

            int period = Mathf.Max(2, settings.portSpacingChunks);
            int phase = Mathf.Clamp(settings.portPhase, 0, period - 1);
            return PositiveModulo(coordinate.x, period) == phase;
        }

        public static bool HasNearbyForcedPort(Vector2Int coordinate, WorldGenerationSettings settings)
        {
            int radius = Mathf.Max(1, settings.minPortDistanceChunks);
            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    Vector2Int other = new Vector2Int(coordinate.x + x, coordinate.y + z);
                    if (IsForcedPort(other, settings))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static MaritimePoiType ChoosePoiType(int seed, Vector2Int coordinate, WorldGenerationSettings settings)
        {
            Vector2 center = new Vector2(
                (coordinate.x + 0.5f) * settings.chunkSize,
                (coordinate.y + 0.5f) * settings.chunkSize);

            if (center.magnitude < settings.safeRadiusFromOrigin)
            {
                return MaritimePoiType.OpenWater;
            }

            if (IsForcedPort(coordinate, settings))
            {
                return MaritimePoiType.Port;
            }

            bool route = IsOnMainRoute(coordinate, settings);
            float chance = route ? settings.routePoiChance : settings.openSeaPoiChance;
            float roll = WorldRandom.Value01(seed, coordinate, 11);
            if (roll > chance)
            {
                return MaritimePoiType.OpenWater;
            }

            float typeRoll = WorldRandom.Value01(seed, coordinate, 29);
            if (!HasNearbyForcedPort(coordinate, settings) && route && typeRoll < 0.05f)
            {
                return MaritimePoiType.Port;
            }

            if (typeRoll < settings.shipwreckChance)
            {
                return MaritimePoiType.Shipwreck;
            }

            if (typeRoll < settings.shipwreckChance + settings.dangerChance)
            {
                return MaritimePoiType.DangerZone;
            }

            if (typeRoll < settings.shipwreckChance + settings.dangerChance + settings.largeIslandChance)
            {
                return MaritimePoiType.LargeIsland;
            }

            if (typeRoll > 0.82f)
            {
                return MaritimePoiType.RockCluster;
            }

            return MaritimePoiType.SmallIsland;
        }

        public static Vector3 GetChunkCenter(Vector2Int coordinate, float chunkSize)
        {
            return new Vector3((coordinate.x + 0.5f) * chunkSize, 0f, (coordinate.y + 0.5f) * chunkSize);
        }

        public static int PositiveModulo(int value, int modulo)
        {
            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }
    }
}
