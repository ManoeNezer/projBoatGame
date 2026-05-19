using BoatGame.Player;
using UnityEngine;

namespace BoatGame.Interaction
{
    [DisallowMultipleComponent]
    public sealed class InteractionSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private FpsPlayerController playerController;
        [SerializeField] private InteractionPromptUI promptUI;

        [Header("Raycast")]
        [SerializeField, Min(0.5f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        [Header("Input")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private IInteractable hoveredInteractable;

        public FpsPlayerController PlayerController => playerController;
        public IInteractable HoveredInteractable => hoveredInteractable;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (playerController == null)
            {
                playerController = GetComponent<FpsPlayerController>();
            }
        }

        private void Update()
        {
            if (playerController != null && playerController.IsInStation)
            {
                SetHovered(null);
                return;
            }

            IInteractable interactable = FindInteractable();
            SetHovered(interactable);

            if (interactable != null && Input.GetKeyDown(interactKey) && interactable.CanInteract(this))
            {
                interactable.Interact(this);
            }
        }

        public void Configure(Camera camera, FpsPlayerController player, InteractionPromptUI prompt, LayerMask mask)
        {
            playerCamera = camera;
            playerController = player;
            promptUI = prompt;
            interactionMask = mask;
        }

        private IInteractable FindInteractable()
        {
            if (playerCamera == null)
            {
                return null;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!UnityEngine.Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, triggerInteraction))
            {
                return null;
            }

            return FindInteractableInParents(hit.collider.transform);
        }

        private IInteractable FindInteractableInParents(Transform source)
        {
            Transform current = source;
            while (current != null)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IInteractable interactable && interactable.CanInteract(this))
                    {
                        return interactable;
                    }
                }

                current = current.parent;
            }

            return null;
        }

        private void SetHovered(IInteractable interactable)
        {
            if (hoveredInteractable == interactable)
            {
                UpdatePrompt();
                return;
            }

            hoveredInteractable?.OnHoverExit(this);
            hoveredInteractable = interactable;
            hoveredInteractable?.OnHoverEnter(this);
            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            bool visible = hoveredInteractable != null && hoveredInteractable.CanInteract(this);
            promptUI?.SetPrompt(visible ? hoveredInteractable.InteractionPrompt : string.Empty, visible);
        }
    }
}
