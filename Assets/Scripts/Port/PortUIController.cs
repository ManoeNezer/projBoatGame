using System;
using System.Collections.Generic;
using BoatGame.Damage;
using BoatGame.Economy;
using BoatGame.Upgrades;
using UnityEngine;

namespace BoatGame.Port
{
    [DisallowMultipleComponent]
    public sealed class PortUIController : MonoBehaviour
    {
        private enum ScreenMode
        {
            Closed,
            Resources,
            Upgrades,
            Repairs
        }

        private static PortUIController instance;

        [SerializeField] private bool showDebugHeader = true;

        private ScreenMode mode = ScreenMode.Closed;
        private ResourceMerchant resourceMerchant;
        private ShipUpgradeMerchant upgradeMerchant;
        private RepairMerchant repairMerchant;
        private PlayerCurrency currency;
        private ResourceInventory inventory;
        private string message;
        private string pendingTitle;
        private string pendingCost;
        private Action pendingAction;
        private Vector2 scroll;

        public static PortUIController Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (mode != ScreenMode.Closed && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public static PortUIController GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject ui = new GameObject("PortUIController");
            return ui.AddComponent<PortUIController>();
        }

        public void OpenResourceShop(ResourceMerchant merchant, PlayerCurrency playerCurrency, ResourceInventory playerInventory)
        {
            resourceMerchant = merchant;
            upgradeMerchant = null;
            repairMerchant = null;
            Open(ScreenMode.Resources, playerCurrency, playerInventory, "Marchand de ressources");
        }

        public void OpenUpgradeShop(ShipUpgradeMerchant merchant, PlayerCurrency playerCurrency, ResourceInventory playerInventory)
        {
            upgradeMerchant = merchant;
            resourceMerchant = null;
            repairMerchant = null;
            Open(ScreenMode.Upgrades, playerCurrency, playerInventory, "Chantier naval");
        }

        public void OpenRepairShop(RepairMerchant merchant, PlayerCurrency playerCurrency, ResourceInventory playerInventory)
        {
            repairMerchant = merchant;
            resourceMerchant = null;
            upgradeMerchant = null;
            Open(ScreenMode.Repairs, playerCurrency, playerInventory, "Reparations du port");
        }

        public void Close()
        {
            mode = ScreenMode.Closed;
            pendingAction = null;
            pendingTitle = string.Empty;
            pendingCost = string.Empty;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Open(ScreenMode screen, PlayerCurrency playerCurrency, ResourceInventory playerInventory, string openingMessage)
        {
            currency = playerCurrency;
            inventory = playerInventory;
            mode = screen;
            message = openingMessage;
            pendingAction = null;
            scroll = Vector2.zero;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
            if (mode == ScreenMode.Closed)
            {
                return;
            }

            Rect area = new Rect(Screen.width * 0.5f - 310f, 72f, 620f, Mathf.Min(620f, Screen.height - 110f));
            GUILayout.BeginArea(area, GUI.skin.window);
            DrawHeader();
            scroll = GUILayout.BeginScrollView(scroll);

            switch (mode)
            {
                case ScreenMode.Resources:
                    DrawResourceShop();
                    break;
                case ScreenMode.Upgrades:
                    DrawUpgradeShop();
                    break;
                case ScreenMode.Repairs:
                    DrawRepairShop();
                    break;
            }

            GUILayout.EndScrollView();
            DrawPendingConfirmation();
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fermer", GUILayout.Width(130f), GUILayout.Height(30f)))
            {
                Close();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawHeader()
        {
            if (showDebugHeader)
            {
                GUILayout.Label("PORT");
            }

            GUILayout.Label(currency != null ? $"Pieces: {currency.Coins}" : "Pieces: ?");
            GUILayout.Label(inventory != null ? inventory.GetSummary() : "Ressources: ?");
            if (!string.IsNullOrEmpty(message))
            {
                GUILayout.Box(message);
            }
        }

        private void DrawResourceShop()
        {
            if (resourceMerchant == null)
            {
                GUILayout.Label("Aucun marchand de ressources.");
                return;
            }

            GUILayout.Label("Acheter / vendre");
            IReadOnlyList<TradeItem> items = resourceMerchant.Database.Items;
            for (int i = 0; i < items.Count; i++)
            {
                TradeItem item = items[i];
                if (item == null || item.kind != TradeItemKind.Resource)
                {
                    continue;
                }

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"{item.displayName} x{item.quantity}\nAchat {item.coinPrice} pieces / Vente {item.coinSellValue} pieces", GUILayout.Width(340f));
                if (GUILayout.Button("Acheter", GUILayout.Width(95f), GUILayout.Height(42f)))
                {
                    BeginConfirmation($"Acheter {item.displayName}", $"{item.coinPrice} pieces", () => Try(() => resourceMerchant.TryBuy(item, currency, inventory, out message)));
                }

                if (GUILayout.Button("Vendre", GUILayout.Width(95f), GUILayout.Height(42f)))
                {
                    BeginConfirmation($"Vendre {item.displayName}", $"{item.coinSellValue} pieces gagnees", () => Try(() => resourceMerchant.TrySell(item, currency, inventory, out message)));
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawUpgradeShop()
        {
            BoatUpgradeSystem system = upgradeMerchant != null ? upgradeMerchant.UpgradeSystem : null;
            if (upgradeMerchant == null || system == null)
            {
                GUILayout.Label("Aucun chantier naval disponible.");
                return;
            }

            GUILayout.Label("Ameliorations bateau");
            IReadOnlyList<BoatUpgradeDefinition> definitions = system.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                BoatUpgradeDefinition definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                bool purchased = system.HasUpgrade(definition.type);
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"{definition.displayName} {(purchased ? "(installee)" : string.Empty)}");
                GUILayout.Label(definition.description);
                GUILayout.Label(definition.CostSummary);
                GUI.enabled = !purchased;
                if (GUILayout.Button(purchased ? "Installee" : "Acheter", GUILayout.Height(30f)))
                {
                    BoatUpgradeType type = definition.type;
                    BeginConfirmation(definition.displayName, definition.CostSummary, () => Try(() => upgradeMerchant.TryBuy(type, currency, inventory, out message)));
                }

                GUI.enabled = true;
                GUILayout.EndVertical();
            }
        }

        private void DrawRepairShop()
        {
            if (repairMerchant == null || repairMerchant.DamageSystem == null)
            {
                GUILayout.Label("Aucun reparateur disponible.");
                return;
            }

            BoatDamageSystem damage = repairMerchant.DamageSystem;
            DrawRepairButton("Reparer coque", BoatPartType.Hull, damage.HullIntegrity);
            DrawRepairButton("Reparer voile", BoatPartType.Sail, damage.SailIntegrity);
            DrawRepairButton("Reparer gouvernail", BoatPartType.Rudder, damage.RudderIntegrity);
            DrawRepairButton("Reparer mat", BoatPartType.Mast, damage.MastIntegrity);

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"Vider l'eau interne: {damage.InternalWater01:P0}", GUILayout.Width(360f));
            int price = repairMerchant.DrainWaterPrice;
            if (GUILayout.Button($"{price} pieces", GUILayout.Width(120f), GUILayout.Height(32f)))
            {
                BeginConfirmation("Vider la cale", $"{price} pieces", () => Try(() => repairMerchant.TryDrainWater(currency, out message)));
            }
            GUILayout.EndHorizontal();
        }

        private void DrawRepairButton(string label, BoatPartType part, float integrity)
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"{label}: {integrity:P0}", GUILayout.Width(360f));
            int price = repairMerchant.GetRepairPrice(part);
            GUI.enabled = integrity < 0.995f;
            if (GUILayout.Button($"{price} pieces", GUILayout.Width(120f), GUILayout.Height(32f)))
            {
                BeginConfirmation(label, $"{price} pieces", () => Try(() => repairMerchant.TryRepair(part, currency, out message)));
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void BeginConfirmation(string title, string cost, Action action)
        {
            pendingTitle = title;
            pendingCost = cost;
            pendingAction = action;
            message = "Confirmer l'achat.";
        }

        private void DrawPendingConfirmation()
        {
            if (pendingAction == null)
            {
                return;
            }

            GUILayout.Space(8f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"Confirmer: {pendingTitle}");
            GUILayout.Label(pendingCost);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Confirmer", GUILayout.Height(30f)))
            {
                Action action = pendingAction;
                pendingAction = null;
                action.Invoke();
            }

            if (GUILayout.Button("Annuler", GUILayout.Height(30f)))
            {
                pendingAction = null;
                message = "Achat annule.";
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void Try(Func<bool> purchase)
        {
            if (!purchase())
            {
                return;
            }
        }
    }
}
