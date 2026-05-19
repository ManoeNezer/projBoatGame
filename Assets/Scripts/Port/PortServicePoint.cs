using BoatGame.Economy;
using BoatGame.Interaction;
using UnityEngine;

namespace BoatGame.Port
{
    public abstract class PortServicePoint : InteractableBase
    {
        [Header("Port Service")]
        [SerializeField] private PortManager port;
        [SerializeField] private PortServiceType serviceType;
        [SerializeField] private string serviceName = "Service";

        public PortManager Port => port;
        public PortServiceType ServiceType => serviceType;
        public override string InteractionPrompt => $"Parler: {serviceName}";

        protected virtual void Awake()
        {
            if (port == null)
            {
                port = GetComponentInParent<PortManager>();
            }
        }

        public override void Interact(InteractionSystem interactor)
        {
            ResolvePlayerEconomy(interactor, out PlayerCurrency currency, out ResourceInventory inventory);
            OpenService(interactor, currency, inventory);
        }

        public void ConfigurePort(PortManager owner)
        {
            port = owner;
        }

        protected void ConfigureService(PortServiceType type, string displayName)
        {
            serviceType = type;
            serviceName = displayName;
            ConfigurePrompt(displayName, "Parler");
        }

        protected abstract void OpenService(InteractionSystem interactor, PlayerCurrency currency, ResourceInventory inventory);

        private static void ResolvePlayerEconomy(InteractionSystem interactor, out PlayerCurrency currency, out ResourceInventory inventory)
        {
            GameObject player = interactor != null && interactor.PlayerController != null ? interactor.PlayerController.gameObject : null;
            if (player == null)
            {
                currency = Object.FindFirstObjectByType<PlayerCurrency>();
                inventory = Object.FindFirstObjectByType<ResourceInventory>();
                return;
            }

            currency = player.GetComponent<PlayerCurrency>();
            if (currency == null)
            {
                currency = player.AddComponent<PlayerCurrency>();
            }

            inventory = player.GetComponent<ResourceInventory>();
            if (inventory == null)
            {
                inventory = player.AddComponent<ResourceInventory>();
            }
        }
    }
}
