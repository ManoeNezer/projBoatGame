using UnityEngine;

namespace BoatGame.World
{
    public struct WorldRandom
    {
        private uint state;

        public WorldRandom(int seed, Vector2Int coordinate, int salt)
        {
            state = Hash(seed, coordinate.x, coordinate.y, salt);
            if (state == 0u)
            {
                state = 0x9E3779B9u;
            }
        }

        public float Next01()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0x00FFFFFFu) / 16777215f;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            return minInclusive + Mathf.FloorToInt(Next01() * (maxExclusive - minInclusive));
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            return Mathf.Lerp(minInclusive, maxInclusive, Next01());
        }

        public bool Chance(float probability)
        {
            return Next01() <= probability;
        }

        public Vector2 InsideUnitCircle()
        {
            float angle = Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Next01());
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        public static float Value01(int seed, Vector2Int coordinate, int salt)
        {
            uint hash = Hash(seed, coordinate.x, coordinate.y, salt);
            return (hash & 0x00FFFFFFu) / 16777215f;
        }

        public static uint Hash(int seed, int x, int y, int salt)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)seed) * 16777619u;
                h = (h ^ (uint)(x * 73856093)) * 16777619u;
                h = (h ^ (uint)(y * 19349663)) * 16777619u;
                h = (h ^ (uint)(salt * 83492791)) * 16777619u;
                h ^= h >> 16;
                h *= 2246822519u;
                h ^= h >> 13;
                h *= 3266489917u;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
