namespace BoatGame.Interaction
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract(InteractionSystem interactor);
        void Interact(InteractionSystem interactor);
        void OnHoverEnter(InteractionSystem interactor);
        void OnHoverExit(InteractionSystem interactor);
    }
}
