using System.Collections.Generic;
using UnityEngine;

namespace BoatGame.Economy
{
    [CreateAssetMenu(menuName = "Boat Game/Economy/Trade Database")]
    public sealed class TradeDatabase : ScriptableObject
    {
        [SerializeField] private List<TradeItem> items = new List<TradeItem>();

        public IReadOnlyList<TradeItem> Items => items;

        public TradeItem Find(string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].id == id)
                {
                    return items[i];
                }
            }

            return null;
        }

        public static TradeDatabase CreateRuntimeDefault()
        {
            TradeDatabase database = CreateInstance<TradeDatabase>();
            database.items = new List<TradeItem>
            {
                CreateResource("wood_bundle", "Lot de bois", ResourceType.Wood, 5, 18, 9),
                CreateResource("fabric_roll", "Rouleau de tissu", ResourceType.Fabric, 3, 24, 12),
                CreateResource("rope_coil", "Rouleau de corde", ResourceType.Rope, 3, 20, 10),
                CreateResource("iron_ingots", "Lingots de fer", ResourceType.Iron, 2, 34, 17)
            };
            return database;
        }

        private static TradeItem CreateResource(string id, string name, ResourceType type, int quantity, int price, int sellValue)
        {
            return new TradeItem
            {
                id = id,
                displayName = name,
                kind = TradeItemKind.Resource,
                resourceType = type,
                quantity = quantity,
                coinPrice = price,
                coinSellValue = sellValue
            };
        }
    }
}
