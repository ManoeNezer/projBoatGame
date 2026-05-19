using System;
using BoatGame.Upgrades;
using UnityEngine;

namespace BoatGame.Economy
{
    [Serializable]
    public sealed class TradeItem
    {
        public string id;
        public string displayName;
        public TradeItemKind kind;
        public ResourceType resourceType;
        public BoatUpgradeType upgradeType;
        [Min(0)] public int quantity = 1;
        [Min(0)] public int coinPrice;
        [Min(0)] public int coinSellValue;
        public SerializableResourceAmount[] resourceCosts;

        public string GetCostSummary()
        {
            string summary = coinPrice > 0 ? $"{coinPrice} pieces" : "gratuit";
            if (resourceCosts == null)
            {
                return summary;
            }

            for (int i = 0; i < resourceCosts.Length; i++)
            {
                if (resourceCosts[i].amount <= 0)
                {
                    continue;
                }

                summary += $" + {resourceCosts[i].amount} {GetResourceDisplayName(resourceCosts[i].type)}";
            }

            return summary;
        }

        public static string GetResourceDisplayName(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Fabric:
                    return "tissu";
                case ResourceType.Rope:
                    return "corde";
                case ResourceType.Iron:
                    return "fer";
                default:
                    return "bois";
            }
        }
    }
}
