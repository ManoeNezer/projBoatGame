using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoatGame.Quests
{
    [Serializable]
    public sealed class Quest
    {
        public string id;
        public QuestContractType type;
        public string title;
        [TextArea(2, 5)] public string description;
        public string originName;
        public Vector3 originPosition;
        public string destinationName;
        public Vector3 destinationPosition;
        public QuestState state = QuestState.Available;
        public List<QuestStep> steps = new List<QuestStep>();
        public List<QuestReward> rewards = new List<QuestReward>();

        [SerializeField] private int currentStepIndex;

        public int CurrentStepIndex => currentStepIndex;

        public QuestStep CurrentStep
        {
            get
            {
                if (steps == null || steps.Count == 0)
                {
                    return null;
                }

                int index = Mathf.Clamp(currentStepIndex, 0, steps.Count - 1);
                return steps[index];
            }
        }

        public QuestObjective CurrentObjective => CurrentStep?.CurrentObjective;

        public void Accept()
        {
            if (state == QuestState.Available)
            {
                state = QuestState.Accepted;
            }
        }

        public bool Tick(Vector3 referencePosition, float deltaTime)
        {
            if (state != QuestState.Accepted && state != QuestState.InProgress)
            {
                return false;
            }

            state = QuestState.InProgress;
            QuestStep step = CurrentStep;
            QuestObjective objective = step?.CurrentObjective;
            objective?.Tick(referencePosition, deltaTime);

            if (step != null && step.Completed)
            {
                currentStepIndex++;
                if (currentStepIndex >= steps.Count)
                {
                    state = QuestState.Completed;
                    currentStepIndex = Mathf.Max(0, steps.Count - 1);
                    return true;
                }
            }

            return false;
        }

        public bool CompleteObjective(string objectiveId)
        {
            for (int s = 0; s < steps.Count; s++)
            {
                QuestStep step = steps[s];
                if (step == null || step.objectives == null)
                {
                    continue;
                }

                for (int o = 0; o < step.objectives.Count; o++)
                {
                    QuestObjective objective = step.objectives[o];
                    if (objective != null && objective.id == objectiveId)
                    {
                        objective.ForceComplete();
                        return true;
                    }
                }
            }

            return false;
        }

        public string RewardSummary()
        {
            if (rewards == null || rewards.Count == 0)
            {
                return "Renom seulement";
            }

            string summary = rewards[0].DisplayText;
            for (int i = 1; i < rewards.Count; i++)
            {
                summary += $", {rewards[i].DisplayText}";
            }

            return summary;
        }
    }
}
