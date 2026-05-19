using BoatGame.Boat;
using BoatGame.Damage;
using BoatGame.Economy;
using BoatGame.Interaction;
using UnityEngine;

namespace BoatGame.Port
{
    [DisallowMultipleComponent]
    public sealed class RepairMerchant : PortServicePoint
    {
        [SerializeField] private BoatDamageSystem damageSystem;

        [Header("Prices")]
        [SerializeField, Min(0)] private int hullRepairPrice = 45;
        [SerializeField, Min(0)] private int sailRepairPrice = 35;
        [SerializeField, Min(0)] private int rudderRepairPrice = 30;
        [SerializeField, Min(0)] private int mastRepairPrice = 40;
        [SerializeField, Min(0)] private int drainWaterPrice = 25;

        public BoatDamageSystem DamageSystem
        {
            get
            {
                if (damageSystem == null)
                {
                    BoatHelmController boat = Object.FindFirstObjectByType<BoatHelmController>();
                    if (boat != null)
                    {
                        damageSystem = boat.GetComponent<BoatDamageSystem>();
                    }
                }

                return damageSystem;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            ConfigureService(PortServiceType.Repairs, "reparateur naval");
        }

        protected override void OpenService(InteractionSystem interactor, PlayerCurrency currency, ResourceInventory inventory)
        {
            PortUIController.GetOrCreate().OpenRepairShop(this, currency, inventory);
        }

        public int GetRepairPrice(BoatPartType part)
        {
            switch (part)
            {
                case BoatPartType.Sail:
                    return sailRepairPrice;
                case BoatPartType.Rudder:
                    return rudderRepairPrice;
                case BoatPartType.Mast:
                    return mastRepairPrice;
                default:
                    return hullRepairPrice;
            }
        }

        public int DrainWaterPrice => drainWaterPrice;

        public bool TryRepair(BoatPartType part, PlayerCurrency currency, out string message)
        {
            BoatDamageSystem damage = DamageSystem;
            if (damage == null || currency == null)
            {
                message = "Service indisponible.";
                return false;
            }

            int price = GetRepairPrice(part);
            if (damage.GetIntegrity(part) >= 0.995f)
            {
                message = "Cette partie est deja intacte.";
                return false;
            }

            if (!currency.TrySpend(price))
            {
                message = "Pas assez de pieces.";
                return false;
            }

            damage.Repair(part, 1f);
            message = "Reparation terminee.";
            return true;
        }

        public bool TryDrainWater(PlayerCurrency currency, out string message)
        {
            BoatDamageSystem damage = DamageSystem;
            if (damage == null || currency == null)
            {
                message = "Service indisponible.";
                return false;
            }

            if (damage.InternalWater01 <= 0.01f)
            {
                message = "La cale est deja seche.";
                return false;
            }

            if (!currency.TrySpend(drainWaterPrice))
            {
                message = "Pas assez de pieces.";
                return false;
            }

            damage.DrainInternalWater(1f);
            message = "Cale videe.";
            return true;
        }
    }
}
