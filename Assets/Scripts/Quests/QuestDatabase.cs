using System.Collections.Generic;
using BoatGame.Economy;
using BoatGame.Upgrades;
using BoatGame.World;
using UnityEngine;

namespace BoatGame.Quests
{
    [CreateAssetMenu(menuName = "Boat Game/Quests/Quest Database")]
    public sealed class QuestDatabase : ScriptableObject
    {
        [SerializeField] private int generatedQuestCounter;

        public Quest CreateContract(QuestContractType type, string originName, Vector3 originPosition, WorldManager.WorldPoiInfo destination)
        {
            generatedQuestCounter++;
            string id = $"contract_{generatedQuestCounter}_{type}_{destination.Coordinate.x}_{destination.Coordinate.y}";
            Quest quest = new Quest
            {
                id = id,
                type = type,
                originName = originName,
                originPosition = originPosition,
                destinationName = destination.DisplayName,
                destinationPosition = destination.Position,
                state = QuestState.Available
            };

            switch (type)
            {
                case QuestContractType.PortDelivery:
                    BuildDeliveryQuest(quest);
                    break;
                case QuestContractType.ShipwreckRecovery:
                    BuildRecoveryQuest(quest);
                    break;
                case QuestContractType.TreasureHunt:
                    BuildTreasureQuest(quest);
                    break;
                case QuestContractType.DangerousZoneInvestigation:
                    BuildDangerQuest(quest);
                    break;
                case QuestContractType.MaritimeEscort:
                    BuildEscortQuest(quest);
                    break;
                case QuestContractType.PortDiscovery:
                    BuildPortDiscoveryQuest(quest);
                    break;
                default:
                    BuildIslandExplorationQuest(quest);
                    break;
            }

            return quest;
        }

        public static QuestDatabase CreateRuntime()
        {
            return CreateInstance<QuestDatabase>();
        }

        private static void BuildDeliveryQuest(Quest quest)
        {
            quest.title = $"Lettre scellee pour {quest.destinationName}";
            quest.description = "Un officier du port confie un paquet cire. Il doit voyager au sec, loin des regards et des recifs.";
            quest.steps = new List<QuestStep>
            {
                Step("Porter la missive au port indique.", Objective("delivery_reach", QuestObjectiveType.ReachLocation, $"Trouver {quest.destinationName}", quest.destinationPosition, 56f)),
                Step("Remettre la missive au bureau du port.", Objective("delivery_return", QuestObjectiveType.ReturnToPort, "Accoster dans le port de destination", quest.destinationPosition, 42f))
            };
            quest.rewards = CoinAndResourceReward(95, ResourceType.Rope, 2);
        }

        private static void BuildIslandExplorationQuest(Quest quest)
        {
            quest.title = $"Relever les signes de {quest.destinationName}";
            quest.description = "Un cartographe cherche des falaises, plages et silhouettes fiables pour ses cartes de mer.";
            quest.steps = new List<QuestStep>
            {
                Step("Naviguer jusqu'a l'ile et confirmer ses repaires.", Objective("island_reach", QuestObjectiveType.DiscoverLocation, $"Explorer {quest.destinationName}", quest.destinationPosition, 64f)),
                Step("Revenir raconter ce que vous avez vu.", Objective("island_return", QuestObjectiveType.ReturnToPort, $"Retourner a {quest.originName}", quest.originPosition, 48f))
            };
            quest.rewards = CoinAndResourceReward(80, ResourceType.Wood, 4);
        }

        private static void BuildRecoveryQuest(Quest quest)
        {
            quest.title = $"Coffret perdu pres de {quest.destinationName}";
            quest.description = "Une epave a rendu quelques planches a la mer. Un coffret de bord y serait encore pris.";
            quest.steps = new List<QuestStep>
            {
                Step("Trouver l'epave.", Objective("wreck_reach", QuestObjectiveType.ReachLocation, $"Atteindre {quest.destinationName}", quest.destinationPosition, 58f)),
                Step("Recuperer le coffret dans les debris.", Objective("wreck_interact", QuestObjectiveType.InteractAtLocation, "Fouiller les debris marques", quest.destinationPosition + new Vector3(10f, 0f, -8f), 24f)),
                Step("Rapporter le coffret au port.", Objective("wreck_return", QuestObjectiveType.ReturnToPort, $"Retourner a {quest.originName}", quest.originPosition, 48f))
            };
            quest.rewards = CoinAndResourceReward(130, ResourceType.Iron, 3);
        }

        private static void BuildTreasureQuest(Quest quest)
        {
            quest.title = $"Carte humide de {quest.destinationName}";
            quest.description = "Une carte griffee parle de pierres noires, d'une plage basse et d'un coffre enterre hors de vue.";
            quest.steps = new List<QuestStep>
            {
                Step("Suivre les marques jusqu'a la cote.", Objective("treasure_reach", QuestObjectiveType.ReachLocation, $"Approcher {quest.destinationName}", quest.destinationPosition, 60f)),
                Step("Creuser au repere de fortune.", Objective("treasure_interact", QuestObjectiveType.InteractAtLocation, "Fouiller le repere de sable", quest.destinationPosition + new Vector3(-14f, 0f, 12f), 22f)),
                Step("Ramener la preuve au port.", Objective("treasure_return", QuestObjectiveType.ReturnToPort, $"Retourner a {quest.originName}", quest.originPosition, 48f))
            };
            quest.rewards = new List<QuestReward>
            {
                new QuestReward { kind = QuestRewardKind.Coins, amount = 165 },
                new QuestReward { kind = QuestRewardKind.Resource, resourceType = ResourceType.Fabric, amount = 3 },
                new QuestReward { kind = QuestRewardKind.UpgradePlan, upgradePlan = BoatUpgradeType.SmallCannon }
            };
        }

        private static void BuildDangerQuest(Quest quest)
        {
            quest.title = $"Veille sur {quest.destinationName}";
            quest.description = "Des feux rouges ont ete apercus entre les lames. Le port paie pour savoir si la passe est praticable.";
            QuestObjective hold = Objective("danger_hold", QuestObjectiveType.HoldPosition, "Observer la zone sans fuir", quest.destinationPosition, 70f);
            hold.requiredHoldSeconds = 8f;
            quest.steps = new List<QuestStep>
            {
                Step("Entrer dans la zone signalee.", Objective("danger_reach", QuestObjectiveType.ReachLocation, $"Trouver {quest.destinationName}", quest.destinationPosition, 76f)),
                Step("Tenir le cap assez longtemps pour juger le danger.", hold),
                Step("Rapporter l'avertissement au port.", Objective("danger_return", QuestObjectiveType.ReturnToPort, $"Retourner a {quest.originName}", quest.originPosition, 48f))
            };
            quest.rewards = CoinAndResourceReward(145, ResourceType.Wood, 5);
        }

        private static void BuildEscortQuest(Quest quest)
        {
            quest.title = $"Route sure vers {quest.destinationName}";
            quest.description = "Un sloop marchand part derriere vous. Tenez une route claire et ouvrez la passe.";
            QuestObjective hold = Objective("escort_hold", QuestObjectiveType.HoldPosition, "Garder la route ouverte", quest.destinationPosition, 82f);
            hold.requiredHoldSeconds = 10f;
            quest.steps = new List<QuestStep>
            {
                Step("Ouvrir la route jusqu'au repere convenu.", Objective("escort_reach", QuestObjectiveType.ReachLocation, $"Naviguer vers {quest.destinationName}", quest.destinationPosition, 82f)),
                Step("Rester dans la passe jusqu'au passage du convoi.", hold),
                Step("Revenir toucher votre solde.", Objective("escort_return", QuestObjectiveType.ReturnToPort, $"Retourner a {quest.originName}", quest.originPosition, 48f))
            };
            quest.rewards = CoinAndResourceReward(120, ResourceType.Rope, 4);
        }

        private static void BuildPortDiscoveryQuest(Quest quest)
        {
            quest.title = $"Chercher {quest.destinationName}";
            quest.description = "Une fumee de lanterne aurait ete vue derriere la brume. Trouvez le port et revenez avec sa position.";
            quest.steps = new List<QuestStep>
            {
                Step("Trouver le port mentionne par les marins.", Objective("port_reach", QuestObjectiveType.DiscoverLocation, $"Decouvrir {quest.destinationName}", quest.destinationPosition, 72f)),
                Step("Revenir avec la route en memoire.", Objective("port_return", QuestObjectiveType.ReturnToPort, $"Retourner a {quest.originName}", quest.originPosition, 48f))
            };
            quest.rewards = new List<QuestReward>
            {
                new QuestReward { kind = QuestRewardKind.Coins, amount = 110 },
                new QuestReward { kind = QuestRewardKind.Rumor, rumorText = "Au nord d'une passe rouge, les rochers chantent avant la tempete." }
            };
        }

        private static QuestStep Step(string description, QuestObjective objective)
        {
            return new QuestStep
            {
                description = description,
                objectives = new List<QuestObjective> { objective }
            };
        }

        private static QuestObjective Objective(string id, QuestObjectiveType type, string description, Vector3 target, float radius)
        {
            return new QuestObjective
            {
                id = id,
                type = type,
                description = description,
                targetName = description,
                targetPosition = target,
                completionRadius = radius
            };
        }

        private static List<QuestReward> CoinAndResourceReward(int coins, ResourceType type, int amount)
        {
            return new List<QuestReward>
            {
                new QuestReward { kind = QuestRewardKind.Coins, amount = coins },
                new QuestReward { kind = QuestRewardKind.Resource, resourceType = type, amount = amount }
            };
        }
    }
}
