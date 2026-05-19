using BoatGame.Boat;
using BoatGame.Economy;
using BoatGame.Port;
using BoatGame.Upgrades;
using UnityEngine;

namespace BoatGame.Debugging
{
    [DisallowMultipleComponent]
    public sealed class PortDebugTools : MonoBehaviour
    {
        [SerializeField] private bool showHints = true;
        [SerializeField] private KeyCode giveResourcesKey = KeyCode.F6;
        [SerializeField] private KeyCode openUpgradeShopKey = KeyCode.F7;
        [SerializeField] private KeyCode openResourceShopKey = KeyCode.F8;
        [SerializeField] private KeyCode spawnPortKey = KeyCode.F9;
        [SerializeField] private KeyCode resetUpgradesKey = KeyCode.F10;

        private string lastMessage;

        private void Update()
        {
            if (Input.GetKeyDown(giveResourcesKey))
            {
                GiveResources();
            }

            if (Input.GetKeyDown(openUpgradeShopKey))
            {
                OpenUpgradeShop();
            }

            if (Input.GetKeyDown(openResourceShopKey))
            {
                OpenResourceShop();
            }

            if (Input.GetKeyDown(spawnPortKey))
            {
                SpawnPortNearBoat();
            }

            if (Input.GetKeyDown(resetUpgradesKey))
            {
                ResetUpgrades();
            }
        }

        private void GiveResources()
        {
            ResolveEconomy(out PlayerCurrency currency, out ResourceInventory inventory);
            currency.AddCoins(250);
            inventory.AddDebugBundle();
            lastMessage = "Debug: ressources et pieces ajoutees.";
        }

        private void OpenUpgradeShop()
        {
            ResolveEconomy(out PlayerCurrency currency, out ResourceInventory inventory);
            ShipUpgradeMerchant merchant = Object.FindFirstObjectByType<ShipUpgradeMerchant>();
            if (merchant == null)
            {
                SpawnPortNearBoat();
                merchant = Object.FindFirstObjectByType<ShipUpgradeMerchant>();
            }

            if (merchant != null)
            {
                PortUIController.GetOrCreate().OpenUpgradeShop(merchant, currency, inventory);
                lastMessage = "Debug: chantier naval ouvert.";
            }
        }

        private void OpenResourceShop()
        {
            ResolveEconomy(out PlayerCurrency currency, out ResourceInventory inventory);
            ResourceMerchant merchant = Object.FindFirstObjectByType<ResourceMerchant>();
            if (merchant == null)
            {
                SpawnPortNearBoat();
                merchant = Object.FindFirstObjectByType<ResourceMerchant>();
            }

            if (merchant != null)
            {
                PortUIController.GetOrCreate().OpenResourceShop(merchant, currency, inventory);
                lastMessage = "Debug: marchand ressources ouvert.";
            }
        }

        private void SpawnPortNearBoat()
        {
            BoatHelmController boat = Object.FindFirstObjectByType<BoatHelmController>();
            Vector3 basePosition = boat != null ? boat.transform.position : Vector3.zero;
            Quaternion rotation = boat != null ? Quaternion.Euler(0f, boat.transform.eulerAngles.y + 180f, 0f) : Quaternion.identity;
            Vector3 position = basePosition + (boat != null ? boat.transform.forward : Vector3.forward) * 145f + Vector3.right * 34f;
            PortManager.CreateRuntimePort(position, rotation);
            lastMessage = "Debug: port proche cree.";
        }

        private void ResetUpgrades()
        {
            ResolveEconomy(out _, out ResourceInventory inventory);
            BoatUpgradeSystem upgradeSystem = Object.FindFirstObjectByType<BoatUpgradeSystem>();
            if (upgradeSystem != null)
            {
                upgradeSystem.ResetPurchases(inventory);
                lastMessage = "Debug: achats bateau reinitialises.";
            }
        }

        private static void ResolveEconomy(out PlayerCurrency currency, out ResourceInventory inventory)
        {
            FpsPlayerControllerSafe(out GameObject player);
            currency = player != null ? player.GetComponent<PlayerCurrency>() : Object.FindFirstObjectByType<PlayerCurrency>();
            inventory = player != null ? player.GetComponent<ResourceInventory>() : Object.FindFirstObjectByType<ResourceInventory>();

            if (player != null && currency == null)
            {
                currency = player.AddComponent<PlayerCurrency>();
            }

            if (player != null && inventory == null)
            {
                inventory = player.AddComponent<ResourceInventory>();
            }

            if (currency == null)
            {
                currency = new GameObject("PlayerCurrency").AddComponent<PlayerCurrency>();
            }

            if (inventory == null)
            {
                inventory = new GameObject("ResourceInventory").AddComponent<ResourceInventory>();
            }
        }

        private static void FpsPlayerControllerSafe(out GameObject player)
        {
            BoatGame.Player.FpsPlayerController controller = Object.FindFirstObjectByType<BoatGame.Player.FpsPlayerController>();
            player = controller != null ? controller.gameObject : null;
        }

        private void OnGUI()
        {
            if (!showHints)
            {
                return;
            }

            Rect rect = new Rect(18f, Screen.height - 132f, 420f, 112f);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Port debug: F6 ressources, F7 upgrades, F8 ressources shop, F9 spawn port, F10 reset upgrades");
            if (!string.IsNullOrEmpty(lastMessage))
            {
                GUILayout.Label(lastMessage);
            }
            GUILayout.EndArea();
        }
    }
}
