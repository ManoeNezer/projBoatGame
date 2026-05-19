using BoatGame.Economy;
using BoatGame.Interaction;
using BoatGame.Port;
using UnityEngine;

namespace BoatGame.Rumors
{
    [DisallowMultipleComponent]
    public sealed class RumorSource : PortServicePoint
    {
        protected override void Awake()
        {
            base.Awake();
            ConfigureService(PortServiceType.Rumors, "maitre des rumeurs");
        }

        protected override void OpenService(InteractionSystem interactor, PlayerCurrency currency, ResourceInventory inventory)
        {
            RumorUI.GetOrCreate().Open(this, currency, inventory);
        }
    }
}
