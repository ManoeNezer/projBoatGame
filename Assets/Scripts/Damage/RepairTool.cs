using BoatGame.Interaction;
using UnityEngine;

namespace BoatGame.Damage
{
    [DisallowMultipleComponent]
    public sealed class RepairTool : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0f)] private float repairCooldown = 0.35f;

        [Header("Feedback")]
        [SerializeField] private AudioSource repairAudio;

        private float nextRepairTime;

        public bool TryRepair(RepairablePart part, InteractionSystem interactor)
        {
            if (part == null || Time.time < nextRepairTime)
            {
                return false;
            }

            bool repaired = part.TryPerformRepair(interactor);
            if (!repaired)
            {
                return false;
            }

            nextRepairTime = Time.time + repairCooldown;
            if (repairAudio != null)
            {
                repairAudio.Play();
            }

            return true;
        }
    }
}
