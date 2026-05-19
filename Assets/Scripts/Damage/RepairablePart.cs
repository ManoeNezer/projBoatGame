using BoatGame.Interaction;
using UnityEngine;

namespace BoatGame.Damage
{
    [DisallowMultipleComponent]
    public sealed class RepairablePart : InteractableBase
    {
        [Header("Repair")]
        [SerializeField] private BoatDamageSystem damageSystem;
        [SerializeField] private RepairResource repairResource;
        [SerializeField] private BoatPartType partType = BoatPartType.Hull;
        [SerializeField, Range(0.01f, 1f)] private float repairAmount = 0.26f;
        [SerializeField, Min(0)] private int woodPatchCost = 1;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer feedbackRenderer;
        [SerializeField] private Color healthyColor = new Color(0.38f, 0.72f, 0.36f, 0.65f);
        [SerializeField] private Color damagedColor = new Color(0.9f, 0.22f, 0.08f, 0.75f);

        public BoatPartType PartType => partType;
        public float RepairAmount => repairAmount;
        public int WoodPatchCost => woodPatchCost;

        public override string InteractionPrompt
        {
            get
            {
                float integrity = damageSystem != null ? damageSystem.GetIntegrity(partType) : 1f;
                string partName = GetPartDisplayName(partType);
                if (integrity >= 0.995f)
                {
                    return $"{partName} intact";
                }

                int resources = repairResource != null ? repairResource.CurrentWoodPatches : 0;
                if (resources < woodPatchCost)
                {
                    return $"Ressources insuffisantes ({partName})";
                }

                return $"Reparer {partName} - bois x{woodPatchCost}";
            }
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigurePrompt(GetPartDisplayName(partType), "Reparer");
            UpdateFeedback();
        }

        private void Update()
        {
            UpdateFeedback();
        }

        public override bool CanInteract(InteractionSystem interactor)
        {
            ResolveReferences();
            return base.CanInteract(interactor) && damageSystem != null && damageSystem.GetIntegrity(partType) < 0.995f;
        }

        public override void Interact(InteractionSystem interactor)
        {
            RepairTool tool = interactor != null && interactor.PlayerController != null
                ? interactor.PlayerController.GetComponent<RepairTool>()
                : null;

            if (tool != null)
            {
                tool.TryRepair(this, interactor);
                return;
            }

            TryPerformRepair(interactor);
        }

        public bool TryPerformRepair(InteractionSystem interactor)
        {
            ResolveReferences();
            if (damageSystem == null || repairResource == null)
            {
                return false;
            }

            if (damageSystem.GetIntegrity(partType) >= 0.995f || !repairResource.TrySpend(woodPatchCost))
            {
                return false;
            }

            damageSystem.Repair(partType, repairAmount);
            UpdateFeedback();
            return true;
        }

        public void Configure(BoatDamageSystem damage, RepairResource resources, BoatPartType part, float amount, int cost)
        {
            damageSystem = damage;
            repairResource = resources;
            partType = part;
            repairAmount = Mathf.Clamp(amount, 0.01f, 1f);
            woodPatchCost = Mathf.Max(0, cost);
            ConfigurePrompt(GetPartDisplayName(partType), "Reparer");
        }

        private void ResolveReferences()
        {
            if (damageSystem == null)
            {
                damageSystem = GetComponentInParent<BoatDamageSystem>();
            }

            if (repairResource == null && damageSystem != null)
            {
                repairResource = damageSystem.RepairResource;
            }

            if (repairResource == null)
            {
                repairResource = GetComponentInParent<RepairResource>();
            }

            if (feedbackRenderer == null)
            {
                feedbackRenderer = GetComponentInChildren<Renderer>();
            }
        }

        private void UpdateFeedback()
        {
            if (feedbackRenderer == null || damageSystem == null)
            {
                return;
            }

            float integrity = damageSystem.GetIntegrity(partType);
            Color color = Color.Lerp(damagedColor, healthyColor, integrity);
            Material material = feedbackRenderer.material;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static string GetPartDisplayName(BoatPartType part)
        {
            switch (part)
            {
                case BoatPartType.Sail:
                    return "voile";
                case BoatPartType.Rudder:
                    return "gouvernail";
                case BoatPartType.Mast:
                    return "mat";
                default:
                    return "coque";
            }
        }

        private void OnDrawGizmosSelected()
        {
            ResolveReferences();
            float integrity = damageSystem != null ? damageSystem.GetIntegrity(partType) : 1f;
            Gizmos.color = Color.Lerp(Color.red, Color.green, integrity);
            Gizmos.DrawWireSphere(transform.position, 0.38f);
        }
    }
}
