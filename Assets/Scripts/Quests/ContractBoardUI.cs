using System.Collections.Generic;
using BoatGame.Economy;
using UnityEngine;

namespace BoatGame.Quests
{
    [DisallowMultipleComponent]
    public sealed class ContractBoardUI : MonoBehaviour
    {
        private static ContractBoardUI instance;

        private ContractBoard board;
        private PlayerCurrency currency;
        private ResourceInventory inventory;
        private bool open;
        private Vector2 scroll;
        private string message;

        public static ContractBoardUI Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (open && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public static ContractBoardUI GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject ui = new GameObject("ContractBoardUI");
            return ui.AddComponent<ContractBoardUI>();
        }

        public void Open(ContractBoard newBoard, PlayerCurrency playerCurrency, ResourceInventory playerInventory)
        {
            board = newBoard;
            currency = playerCurrency;
            inventory = playerInventory;
            open = true;
            scroll = Vector2.zero;
            message = "Les contrats disponibles sont marques a la cire du port.";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            open = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnGUI()
        {
            if (!open)
            {
                return;
            }

            QuestManager manager = QuestManager.GetOrCreate();
            Rect area = new Rect(Screen.width * 0.5f - 340f, 64f, 680f, Mathf.Min(660f, Screen.height - 96f));
            GUILayout.BeginArea(area, GUI.skin.window);
            GUILayout.Label("TABLEAU DE CONTRATS");
            GUILayout.Label(board != null && board.Port != null ? board.Port.PortName : "Port");
            GUILayout.Label(currency != null ? $"Pieces: {currency.Coins}" : "Pieces: ?");
            GUILayout.Label(inventory != null ? inventory.GetSummary() : "Ressources: ?");
            if (!string.IsNullOrEmpty(message))
            {
                GUILayout.Box(message);
            }

            scroll = GUILayout.BeginScrollView(scroll);
            DrawCompleted(manager);
            DrawActive(manager);
            DrawAvailable(manager);
            GUILayout.EndScrollView();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Actualiser les offres", GUILayout.Height(30f)))
            {
                manager.GenerateContractsForPort(board != null ? board.Port : null);
                message = "Le tableau est a jour.";
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fermer", GUILayout.Width(120f), GUILayout.Height(30f)))
            {
                Close();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawCompleted(QuestManager manager)
        {
            IReadOnlyList<Quest> completed = manager.CompletedQuests;
            if (completed.Count <= 0)
            {
                return;
            }

            GUILayout.Label("Contrats a rendre");
            for (int i = 0; i < completed.Count; i++)
            {
                Quest quest = completed[i];
                if (ContractEntryUI.DrawCompleted(quest))
                {
                    manager.TurnInQuest(quest);
                    message = manager.LastMessage;
                    return;
                }
            }
        }

        private void DrawActive(QuestManager manager)
        {
            IReadOnlyList<Quest> active = manager.ActiveQuests;
            if (active.Count <= 0)
            {
                return;
            }

            GUILayout.Label("Contrats actifs");
            for (int i = 0; i < active.Count; i++)
            {
                ContractEntryUI.DrawActive(active[i]);
            }
        }

        private void DrawAvailable(QuestManager manager)
        {
            IReadOnlyList<Quest> available = manager.GetContractsForPort(board != null ? board.Port : null);
            GUILayout.Label("Offres disponibles");
            bool drewAny = false;
            for (int i = 0; i < available.Count; i++)
            {
                Quest quest = available[i];
                if (board != null && board.Port != null && quest != null && quest.originName != board.Port.PortName)
                {
                    continue;
                }

                drewAny = true;
                if (ContractEntryUI.DrawAvailable(quest))
                {
                    manager.AcceptQuest(quest);
                    message = manager.LastMessage;
                    return;
                }
            }

            if (!drewAny)
            {
                GUILayout.Box("Aucun contrat pour ce port. Revenez apres la prochaine maree.");
            }
        }
    }
}
