using System;
using System.Collections.Generic;

namespace BoatGame.Quests
{
    [Serializable]
    public sealed class QuestStep
    {
        public string description;
        public List<QuestObjective> objectives = new List<QuestObjective>();

        public bool Completed
        {
            get
            {
                if (objectives == null || objectives.Count == 0)
                {
                    return true;
                }

                for (int i = 0; i < objectives.Count; i++)
                {
                    if (objectives[i] != null && !objectives[i].Completed)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public QuestObjective CurrentObjective
        {
            get
            {
                if (objectives == null)
                {
                    return null;
                }

                for (int i = 0; i < objectives.Count; i++)
                {
                    if (objectives[i] != null && !objectives[i].Completed)
                    {
                        return objectives[i];
                    }
                }

                return objectives.Count > 0 ? objectives[objectives.Count - 1] : null;
            }
        }
    }
}
