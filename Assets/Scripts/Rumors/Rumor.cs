using System;
using BoatGame.World;
using UnityEngine;

namespace BoatGame.Rumors
{
    [Serializable]
    public sealed class Rumor
    {
        public string id;
        public string title;
        [TextArea(2, 5)] public string text;
        public string sourceName;
        public string targetName;
        public Vector3 approximatePosition;
        public bool hasApproximatePosition;
        public float uncertaintyRadius = 220f;
        public MaritimePoiType poiType = MaritimePoiType.OpenWater;

        [SerializeField] private bool revealed;

        public bool Revealed => revealed;

        public Rumor()
        {
        }

        public Rumor(string newTitle, string newText, Vector3 position, bool hasPosition)
        {
            id = Guid.NewGuid().ToString("N");
            title = newTitle;
            text = newText;
            approximatePosition = position;
            hasApproximatePosition = hasPosition;
            revealed = true;
        }

        public void Reveal()
        {
            revealed = true;
        }

        public string GetDistanceText(Vector3 from)
        {
            if (!hasApproximatePosition)
            {
                return "Position incertaine";
            }

            float distance = Vector3.Distance(from, approximatePosition);
            if (distance < 220f)
            {
                return "Tout pres";
            }

            if (distance < 700f)
            {
                return "A courte voile";
            }

            if (distance < 1600f)
            {
                return "A une bonne traversee";
            }

            return "Au-dela de l'horizon";
        }

        public string GetDirectionText(Vector3 from, Transform reference)
        {
            if (!hasApproximatePosition)
            {
                return "Les marins restent vagues";
            }

            Vector3 to = approximatePosition - from;
            to.y = 0f;
            if (to.sqrMagnitude < 0.001f)
            {
                return "Ici meme";
            }

            if (reference == null)
            {
                return CardinalFromDirection(to);
            }

            float angle = Vector3.SignedAngle(reference.forward, to.normalized, Vector3.up);
            float abs = Mathf.Abs(angle);
            if (abs < 20f)
            {
                return "droit devant";
            }

            if (abs > 155f)
            {
                return "dans votre sillage";
            }

            if (angle > 0f)
            {
                return abs < 85f ? "sur tribord" : "par tribord arriere";
            }

            return abs < 85f ? "sur babord" : "par babord arriere";
        }

        private static string CardinalFromDirection(Vector3 direction)
        {
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            angle = Mathf.Repeat(angle + 360f, 360f);
            if (angle < 45f || angle >= 315f)
            {
                return "vers le nord";
            }

            if (angle < 135f)
            {
                return "vers l'est";
            }

            if (angle < 225f)
            {
                return "vers le sud";
            }

            return "vers l'ouest";
        }
    }
}
