using UnityEngine;

namespace BoatGame.Interaction
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [SerializeField] private string displayName = "Interact";
        [SerializeField] private string verb = "Utiliser";
        [SerializeField] private bool isInteractable = true;

        public virtual string InteractionPrompt => $"{verb} {displayName}";

        public virtual bool CanInteract(InteractionSystem interactor)
        {
            return isInteractable && enabled && gameObject.activeInHierarchy;
        }

        public abstract void Interact(InteractionSystem interactor);

        public virtual void OnHoverEnter(InteractionSystem interactor)
        {
        }

        public virtual void OnHoverExit(InteractionSystem interactor)
        {
        }

        public void ConfigurePrompt(string newDisplayName, string newVerb)
        {
            displayName = newDisplayName;
            verb = newVerb;
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;
        }
    }
}
