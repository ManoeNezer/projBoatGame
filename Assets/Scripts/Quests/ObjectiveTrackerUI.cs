using UnityEngine;

namespace BoatGame.Quests
{
    [DisallowMultipleComponent]
    public sealed class ObjectiveTrackerUI : MonoBehaviour
    {
        [SerializeField] private bool visible = true;

        private void OnGUI()
        {
            if (!visible || QuestManager.Instance == null)
            {
                return;
            }

            Quest quest = QuestManager.Instance.ActiveQuest;
            if (quest == null)
            {
                return;
            }

            QuestObjective objective = quest.CurrentObjective;
            Rect area = new Rect(Screen.width - 372f, 28f, 344f, 148f);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label(quest.state == QuestState.Completed ? "CONTRAT ACHEVE" : "CONTRAT ACTIF");
            GUILayout.Label(quest.title);

            if (objective != null)
            {
                Vector3 referencePosition = GetReferencePosition();
                Transform reference = QuestManager.Instance.GetReferenceTransform();
                float distance = objective.DistanceFrom(referencePosition);
                GUILayout.Label(objective.description);
                GUILayout.Label($"{FormatDistance(distance)} - {objective.GetDirectionText(referencePosition, reference)}");
                if (objective.type == QuestObjectiveType.HoldPosition)
                {
                    GUILayout.HorizontalSlider(objective.Hold01, 0f, 1f);
                }
            }
            else if (!string.IsNullOrEmpty(QuestManager.Instance.LastMessage))
            {
                GUILayout.Label(QuestManager.Instance.LastMessage);
            }

            GUILayout.EndArea();
        }

        private static Vector3 GetReferencePosition()
        {
            Transform reference = QuestManager.Instance != null ? QuestManager.Instance.GetReferenceTransform() : null;
            if (reference != null)
            {
                return reference.position;
            }

            Camera camera = Camera.main;
            return camera != null ? camera.transform.position : Vector3.zero;
        }

        private static string FormatDistance(float distance)
        {
            if (distance < 80f)
            {
                return "tout pres";
            }

            if (distance < 300f)
            {
                return "quelques encablures";
            }

            if (distance < 1000f)
            {
                return $"{Mathf.RoundToInt(distance / 50f) * 50} pas de mer";
            }

            return $"{distance / 1000f:0.0} mille de prototype";
        }
    }
}
