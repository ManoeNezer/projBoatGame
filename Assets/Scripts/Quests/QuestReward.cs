using System;
using BoatGame.Damage;
using BoatGame.Economy;
using BoatGame.Rumors;
using BoatGame.Upgrades;
using UnityEngine;

namespace BoatGame.Quests
{
    [Serializable]
    public sealed class QuestReward
    {
        public QuestRewardKind kind;
        [Min(0)] public int amount;
        public ResourceType resourceType;
        public BoatUpgradeType upgradePlan;
        public string rumorText;

        public string DisplayText
        {
            get
            {
                switch (kind)
                {
                    case QuestRewardKind.Resource:
                        return $"{amount} {TradeItem.GetResourceDisplayName(resourceType)}";
                    case QuestRewardKind.UpgradePlan:
                        return $"Plan: {upgradePlan}";
                    case QuestRewardKind.FreeRepair:
                        return "Reparation gratuite";
                    case QuestRewardKind.Rumor:
                        return "Rumeur";
                    default:
                        return $"{amount} pieces";
                }
            }
        }

        public void Apply(PlayerCurrency currency, ResourceInventory inventory, BoatDamageSystem damageSystem, QuestManager questManager)
        {
            switch (kind)
            {
                case QuestRewardKind.Resource:
                    inventory?.Add(resourceType, amount);
                    break;
                case QuestRewardKind.UpgradePlan:
                    questManager?.UnlockUpgradePlan(upgradePlan);
                    break;
                case QuestRewardKind.FreeRepair:
                    if (damageSystem != null)
                    {
                        damageSystem.Repair(BoatPartType.Hull, 1f);
                        damageSystem.Repair(BoatPartType.Sail, 1f);
                        damageSystem.Repair(BoatPartType.Rudder, 1f);
                        damageSystem.Repair(BoatPartType.Mast, 1f);
                        damageSystem.DrainInternalWater(1f);
                    }
                    break;
                case QuestRewardKind.Rumor:
                    RumorManager.Instance?.AddRumor(new Rumor("Rumeur gagnee", string.IsNullOrEmpty(rumorText) ? "Un vieux marin a laisse quelques mots sur une route oubliee." : rumorText, Vector3.zero, false));
                    break;
                default:
                    currency?.AddCoins(amount);
                    break;
            }
        }
    }
}
