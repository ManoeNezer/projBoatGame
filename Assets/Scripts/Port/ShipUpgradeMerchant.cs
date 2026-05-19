using BoatGame.Boat;
using BoatGame.Economy;
using BoatGame.Interaction;
using BoatGame.Upgrades;
using UnityEngine;

namespace BoatGame.Port
{
    [DisallowMultipleComponent]
    public sealed class ShipUpgradeMerchant : PortServicePoint
    {
        [SerializeField] private BoatUpgradeSystem upgradeSystem;

        public BoatUpgradeSystem UpgradeSystem
        {
            get
            {
                if (upgradeSystem == null)
                {
                    BoatHelmController boat = Object.FindFirstObjectByType<BoatHelmController>();
                    if (boat != null)
                    {
                        upgradeSystem = boat.GetComponent<BoatUpgradeSystem>();
                        if (upgradeSystem == null)
                        {
                            upgradeSystem = boat.gameObject.AddComponent<BoatUpgradeSystem>();
                        }
                    }
                }

                return upgradeSystem;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            ConfigureService(PortServiceType.ShipUpgrades, "chantier naval");
        }

        protected override void OpenService(InteractionSystem interactor, PlayerCurrency currency, ResourceInventory inventory)
        {
            PortUIController.GetOrCreate().OpenUpgradeShop(this, currency, inventory);
        }

        public bool TryBuy(BoatUpgradeType type, PlayerCurrency currency, ResourceInventory inventory, out string message)
        {
            BoatUpgradeSystem system = UpgradeSystem;
            if (system == null)
            {
                message = "Aucun bateau a ameliorer.";
                return false;
            }

            return system.TryPurchase(type, currency, inventory, out message);
        }
    }
}
