using UnityEngine;

namespace BoatGame.Quests
{
    public static class ContractEntryUI
    {
        public static bool DrawAvailable(Quest quest)
        {
            if (quest == null)
            {
                return false;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(quest.title);
            GUILayout.Label(quest.description);
            GUILayout.Label($"Destination: {quest.destinationName}");
            GUILayout.Label($"Recompense: {quest.RewardSummary()}");
            bool clicked = GUILayout.Button("Accepter le contrat", GUILayout.Height(30f));
            GUILayout.EndVertical();
            return clicked;
        }

        public static bool DrawCompleted(Quest quest)
        {
            if (quest == null)
            {
                return false;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"{quest.title} - acheve");
            GUILayout.Label($"Recompense: {quest.RewardSummary()}");
            bool clicked = GUILayout.Button("Rendre le contrat", GUILayout.Height(30f));
            GUILayout.EndVertical();
            return clicked;
        }

        public static void DrawActive(Quest quest)
        {
            if (quest == null)
            {
                return;
            }

            QuestObjective objective = quest.CurrentObjective;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"{quest.title} - {quest.state}");
            if (quest.CurrentStep != null)
            {
                GUILayout.Label(quest.CurrentStep.description);
            }

            if (objective != null)
            {
                GUILayout.Label(objective.description);
                if (objective.type == QuestObjectiveType.HoldPosition)
                {
                    GUILayout.HorizontalSlider(objective.Hold01, 0f, 1f);
                }
            }

            GUILayout.EndVertical();
        }
    }
}
