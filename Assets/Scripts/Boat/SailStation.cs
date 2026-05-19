using BoatGame.Interaction;
using BoatGame.Player;
using UnityEngine;

namespace BoatGame.Boat
{
    [DisallowMultipleComponent]
    public sealed class SailStation : InteractableBase, IPlayerStation
    {
        [Header("References")]
        [SerializeField] private BoatHelmController boatController;
        [SerializeField] private Transform bodyAnchor;
        [SerializeField] private Transform cameraAnchor;

        [Header("Input")]
        [SerializeField] private KeyCode raiseKey = KeyCode.Z;
        [SerializeField] private KeyCode alternateRaiseKey = KeyCode.W;
        [SerializeField] private KeyCode lowerKey = KeyCode.S;
        [SerializeField] private KeyCode trimLeftKey = KeyCode.Q;
        [SerializeField] private KeyCode alternateTrimLeftKey = KeyCode.A;
        [SerializeField] private KeyCode trimRightKey = KeyCode.D;

        private FpsPlayerController activePlayer;

        public Transform BodyAnchor => bodyAnchor;
        public Transform CameraAnchor => cameraAnchor;
        public Rigidbody PlatformBody => boatController != null ? boatController.Body : null;
        public override string InteractionPrompt => activePlayer == null ? "Regler la voile" : "Quitter la voile";

        private void Reset()
        {
            ConfigurePrompt("la voile", "Regler");
        }

        private void Update()
        {
            if (activePlayer == null || boatController == null)
            {
                return;
            }

            float hoist = 0f;
            if (Input.GetKey(raiseKey) || Input.GetKey(alternateRaiseKey))
            {
                hoist += 1f;
            }

            if (Input.GetKey(lowerKey))
            {
                hoist -= 1f;
            }

            float trim = 0f;
            if (Input.GetKey(trimRightKey))
            {
                trim += 1f;
            }

            if (Input.GetKey(trimLeftKey) || Input.GetKey(alternateTrimLeftKey))
            {
                trim -= 1f;
            }

            boatController.SetSailInput(hoist, trim);
        }

        public override bool CanInteract(InteractionSystem interactor)
        {
            return base.CanInteract(interactor) && (activePlayer == null || activePlayer == interactor.PlayerController);
        }

        public override void Interact(InteractionSystem interactor)
        {
            if (interactor == null || interactor.PlayerController == null)
            {
                return;
            }

            if (activePlayer == interactor.PlayerController)
            {
                ExitStation(activePlayer);
                return;
            }

            if (activePlayer != null)
            {
                return;
            }

            activePlayer = interactor.PlayerController;
            activePlayer.EnterStation(this);
        }

        public void ExitStation(FpsPlayerController player)
        {
            if (activePlayer != player)
            {
                return;
            }

            boatController?.SetSailInput(0f, 0f);
            activePlayer.LeaveStation(this);
            activePlayer = null;
        }

        public void Configure(BoatHelmController controller, Transform standAnchor, Transform viewAnchor)
        {
            boatController = controller;
            bodyAnchor = standAnchor;
            cameraAnchor = viewAnchor;
            ConfigurePrompt("la voile", "Regler");
        }
    }
}
