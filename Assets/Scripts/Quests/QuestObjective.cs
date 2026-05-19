using System;
using UnityEngine;

namespace BoatGame.Quests
{
    [Serializable]
    public sealed class QuestObjective
    {
        public string id;
        public string description;
        public QuestObjectiveType type;
        public Vector3 targetPosition;
        public string targetName;
        [Min(2f)] public float completionRadius = 38f;
        [Min(0f)] public float requiredHoldSeconds;

        [SerializeField] private bool completed;
        [SerializeField] private float holdTimer;
        [SerializeField] private bool revealed;

        public bool Completed => completed;
        public bool Revealed => revealed;
        public float Hold01 => requiredHoldSeconds <= 0.001f ? 1f : Mathf.Clamp01(holdTimer / requiredHoldSeconds);

        public void Reveal()
        {
            revealed = true;
        }

        public void ResetProgress()
        {
            completed = false;
            holdTimer = 0f;
        }

        public void ForceComplete()
        {
            completed = true;
            holdTimer = requiredHoldSeconds;
        }

        public bool Tick(Vector3 referencePosition, float deltaTime)
        {
            if (completed)
            {
                return true;
            }

            if (type == QuestObjectiveType.InteractAtLocation)
            {
                return false;
            }

            float distance = Vector3.Distance(referencePosition, targetPosition);
            bool inRange = distance <= completionRadius;

            if (type == QuestObjectiveType.HoldPosition)
            {
                if (inRange)
                {
                    holdTimer += deltaTime;
                    if (holdTimer >= requiredHoldSeconds)
                    {
                        completed = true;
                    }
                }
                else
                {
                    holdTimer = Mathf.MoveTowards(holdTimer, 0f, deltaTime * 1.5f);
                }

                return completed;
            }

            if (inRange)
            {
                completed = true;
            }

            return completed;
        }

        public float DistanceFrom(Vector3 position)
        {
            return Vector3.Distance(position, targetPosition);
        }

        public string GetDirectionText(Vector3 from, Transform reference)
        {
            Vector3 to = targetPosition - from;
            to.y = 0f;
            if (to.sqrMagnitude < 0.001f)
            {
                return "tout pres";
            }

            if (reference == null)
            {
                return CardinalFromDirection(to);
            }

            float angle = Vector3.SignedAngle(reference.forward, to.normalized, Vector3.up);
            float abs = Mathf.Abs(angle);
            if (abs < 18f)
            {
                return "droit devant";
            }

            if (abs > 150f)
            {
                return "dans votre sillage";
            }

            if (angle > 0f)
            {
                return abs < 80f ? "sur tribord" : "loin sur tribord arriere";
            }

            return abs < 80f ? "sur babord" : "loin sur babord arriere";
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
