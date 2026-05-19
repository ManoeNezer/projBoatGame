using BoatGame.Interaction;
using UnityEngine;

namespace BoatGame.Quests
{
    [DisallowMultipleComponent]
    public sealed class QuestObjectiveMarker : InteractableBase
    {
        [SerializeField] private string questId;
        [SerializeField] private string objectiveId;
        [SerializeField] private bool requiresInteraction = true;
        [SerializeField, Min(1f)] private float autoCompleteRadius = 18f;
        [SerializeField] private bool drawGizmos = true;

        private Transform target;

        public override string InteractionPrompt => base.InteractionPrompt;

        private void Awake()
        {
            ConfigurePrompt("le repere", "Examiner");
        }

        private void Update()
        {
            if (requiresInteraction)
            {
                return;
            }

            if (target == null)
            {
                Camera camera = Camera.main;
                target = camera != null ? camera.transform : null;
            }

            if (target != null && Vector3.Distance(target.position, transform.position) <= autoCompleteRadius)
            {
                Complete();
            }
        }

        public override void Interact(InteractionSystem interactor)
        {
            Complete();
        }

        public void Configure(string ownerQuestId, string ownerObjectiveId, bool interactionRequired, string prompt)
        {
            questId = ownerQuestId;
            objectiveId = ownerObjectiveId;
            requiresInteraction = interactionRequired;
            ConfigurePrompt(prompt, interactionRequired ? "Examiner" : "Observer");
        }

        private void Complete()
        {
            if (QuestManager.Instance != null && QuestManager.Instance.CompleteObjective(questId, objectiveId))
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.65f);
            Gizmos.DrawWireSphere(transform.position, autoCompleteRadius);
        }
    }
}
