using BoatGame.Interaction;
using BoatGame.Player;
using UnityEngine;

namespace BoatGame.Boat
{
    [DisallowMultipleComponent]
    public sealed class HelmStation : InteractableBase, IPlayerStation
    {
        [Header("References")]
        [SerializeField] private BoatHelmController boatController;
        [SerializeField] private Transform bodyAnchor;
        [SerializeField] private Transform cameraAnchor;

        [Header("Input")]
        [SerializeField] private KeyCode leftKey = KeyCode.Q;
        [SerializeField] private KeyCode alternateLeftKey = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.D;

        private FpsPlayerController activePlayer;

        public Transform BodyAnchor => bodyAnchor;
        public Transform CameraAnchor => cameraAnchor;
        public Rigidbody PlatformBody => boatController != null ? boatController.Body : null;
        public override string InteractionPrompt => activePlayer == null ? "Prendre le gouvernail" : "Quitter le gouvernail";

        private void Reset()
        {
            ConfigurePrompt("le gouvernail", "Prendre");
        }

        private void Update()
        {
            if (activePlayer == null || boatController == null)
            {
                return;
            }

            float input = 0f;
            if (Input.GetKey(rightKey))
            {
                input += 1f;
            }

            if (Input.GetKey(leftKey) || Input.GetKey(alternateLeftKey))
            {
                input -= 1f;
            }

            boatController.SetHelmInput(input);
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

            boatController?.SetHelmInput(0f);
            activePlayer.LeaveStation(this);
            activePlayer = null;
        }

        public void Configure(BoatHelmController controller, Transform standAnchor, Transform viewAnchor)
        {
            boatController = controller;
            bodyAnchor = standAnchor;
            cameraAnchor = viewAnchor;
            ConfigurePrompt("le gouvernail", "Prendre");
        }
    }
}
