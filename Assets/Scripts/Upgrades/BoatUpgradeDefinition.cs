using System;
using BoatGame.Economy;
using UnityEngine;

namespace BoatGame.Upgrades
{
    [Serializable]
    public sealed class BoatUpgradeDefinition
    {
        public BoatUpgradeType type;
        public string displayName;
        [TextArea(2, 4)] public string description;
        [Min(0)] public int coinCost;
        public SerializableResourceAmount[] resourceCosts;

        [Header("Stats")]
        [Min(0f)] public float sailForceBonus;
        [Min(0f)] public float rudderForceBonus;
        [Min(0f)] public float handlingBonus;
        [Range(0f, 0.85f)] public float hullDamageReduction;
        [Range(0f, 0.85f)] public float leakReduction;
        [Min(0)] public int storageCapacityBonus;
        [Min(0)] public int repairPatchCapacityBonus;

        public string CostSummary
        {
            get
            {
                string summary = coinCost > 0 ? $"{coinCost} pieces" : "gratuit";
                if (resourceCosts == null)
                {
                    return summary;
                }

                for (int i = 0; i < resourceCosts.Length; i++)
                {
                    if (resourceCosts[i].amount > 0)
                    {
                        summary += $" + {resourceCosts[i].amount} {TradeItem.GetResourceDisplayName(resourceCosts[i].type)}";
                    }
                }

                return summary;
            }
        }
    }
}
