using BoatGame.Economy;
using BoatGame.Interaction;
using UnityEngine;

namespace BoatGame.Port
{
    [DisallowMultipleComponent]
    public sealed class ResourceMerchant : PortServicePoint
    {
        [Header("Trade")]
        [SerializeField] private TradeDatabase tradeDatabase;

        public TradeDatabase Database
        {
            get
            {
                if (tradeDatabase == null)
                {
                    tradeDatabase = TradeDatabase.CreateRuntimeDefault();
                }

                return tradeDatabase;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            ConfigureService(PortServiceType.Resources, "marchand ressources");
        }

        protected override void OpenService(InteractionSystem interactor, PlayerCurrency currency, ResourceInventory inventory)
        {
            PortUIController.GetOrCreate().OpenResourceShop(this, currency, inventory);
        }

        public bool TryBuy(TradeItem item, PlayerCurrency currency, ResourceInventory inventory, out string message)
        {
            if (item == null || item.kind != TradeItemKind.Resource)
            {
                message = "Article invalide.";
                return false;
            }

            if (currency == null || inventory == null)
            {
                message = "Inventaire introuvable.";
                return false;
            }

            if (!currency.CanSpend(item.coinPrice))
            {
                message = "Pas assez de pieces.";
                return false;
            }

            if (!inventory.CanAdd(item.resourceType, item.quantity))
            {
                message = "Stockage plein.";
                return false;
            }

            currency.TrySpend(item.coinPrice);
            inventory.Add(item.resourceType, item.quantity);
            message = $"{item.displayName} achete.";
            return true;
        }

        public bool TrySell(TradeItem item, PlayerCurrency currency, ResourceInventory inventory, out string message)
        {
            if (item == null || item.kind != TradeItemKind.Resource)
            {
                message = "Article invalide.";
                return false;
            }

            if (currency == null || inventory == null)
            {
                message = "Inventaire introuvable.";
                return false;
            }

            if (!inventory.TrySpend(item.resourceType, item.quantity))
            {
                message = "Ressources insuffisantes.";
                return false;
            }

            currency.AddCoins(item.coinSellValue);
            message = $"{item.displayName} vendu.";
            return true;
        }
    }
}
