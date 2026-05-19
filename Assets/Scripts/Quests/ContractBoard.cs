using BoatGame.Economy;
using BoatGame.Interaction;
using BoatGame.Port;
using UnityEngine;

namespace BoatGame.Quests
{
    [DisallowMultipleComponent]
    public sealed class ContractBoard : PortServicePoint
    {
        protected override void Awake()
        {
            base.Awake();
            ConfigureService(PortServiceType.Contracts, "tableau de contrats");
        }

        protected override void OpenService(InteractionSystem interactor, PlayerCurrency currency, ResourceInventory inventory)
        {
            ContractBoardUI.GetOrCreate().Open(this, currency, inventory);
        }
    }
}
