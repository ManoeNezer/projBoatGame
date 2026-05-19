using BoatGame.Boat;
using BoatGame.Discovery;
using BoatGame.Port;
using BoatGame.Quests;
using BoatGame.Rumors;
using UnityEngine;

namespace BoatGame.Debugging
{
    [DisallowMultipleComponent]
    public sealed class QuestDebugTools : MonoBehaviour
    {
        [SerializeField] private bool showPanel = true;
        [SerializeField] private KeyCode generateContractKey = KeyCode.F11;
        [SerializeField] private KeyCode completeObjectiveKey = KeyCode.F12;

        private void Update()
        {
            if (Input.GetKeyDown(generateContractKey))
            {
                QuestManager.GetOrCreate().GenerateDebugContract();
            }

            if (Input.GetKeyDown(completeObjectiveKey))
            {
                QuestManager.GetOrCreate().CompleteCurrentObjectiveDebug();
            }
        }

        private void OnGUI()
        {
            if (!showPanel)
            {
                return;
            }

            QuestManager questManager = QuestManager.GetOrCreate();
            Rect area = new Rect(18f, Screen.height - 262f, 430f, 122f);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("Quest debug: F11 contrat, F12 objectif");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Contrat"))
            {
                questManager.GenerateDebugContract();
            }

            if (GUILayout.Button("Completer"))
            {
                questManager.CompleteCurrentObjectiveDebug();
            }

            if (GUILayout.Button("Reveler"))
            {
                questManager.RevealActiveDestination();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Recompense"))
            {
                questManager.GiveActiveRewardDebug();
            }

            if (GUILayout.Button("POI proche"))
            {
                questManager.SpawnQuestPoiNear();
            }

            if (GUILayout.Button("Reset"))
            {
                questManager.ResetQuests();
                DiscoveryManager.Instance?.ResetDiscoveries();
                RumorManager.Instance?.ResetRumors();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Spawn port proche"))
            {
                SpawnPortNearBoat();
            }

            if (!string.IsNullOrEmpty(questManager.LastMessage))
            {
                GUILayout.Label(questManager.LastMessage);
            }
            GUILayout.EndArea();
        }

        private static void SpawnPortNearBoat()
        {
            BoatHelmController boat = FindFirstObjectByType<BoatHelmController>();
            Vector3 basePosition = boat != null ? boat.transform.position : Vector3.zero;
            Quaternion rotation = boat != null ? Quaternion.Euler(0f, boat.transform.eulerAngles.y + 180f, 0f) : Quaternion.identity;
            Vector3 position = basePosition + (boat != null ? boat.transform.forward : Vector3.forward) * 150f + Vector3.right * 40f;
            PortManager.CreateRuntimePort(position, rotation);
        }
    }
}
