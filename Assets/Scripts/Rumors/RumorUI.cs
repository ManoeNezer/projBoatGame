using System.Collections.Generic;
using BoatGame.Boat;
using BoatGame.Economy;
using UnityEngine;

namespace BoatGame.Rumors
{
    [DisallowMultipleComponent]
    public sealed class RumorUI : MonoBehaviour
    {
        private static RumorUI instance;

        private RumorSource source;
        private PlayerCurrency currency;
        private ResourceInventory inventory;
        private bool open;
        private Vector2 scroll;
        private string message;

        public static RumorUI Instance => instance;

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

        public static RumorUI GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject ui = new GameObject("RumorUI");
            return ui.AddComponent<RumorUI>();
        }

        public void Open(RumorSource newSource, PlayerCurrency playerCurrency, ResourceInventory playerInventory)
        {
            source = newSource;
            currency = playerCurrency;
            inventory = playerInventory;
            open = true;
            scroll = Vector2.zero;
            message = "Les nouvelles de quai sentent le sel et la fumee.";
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

            Rect area = new Rect(Screen.width * 0.5f - 300f, 82f, 600f, Mathf.Min(580f, Screen.height - 124f));
            GUILayout.BeginArea(area, GUI.skin.window);
            GUILayout.Label("RUMEURS DE PORT");
            GUILayout.Label(currency != null ? $"Pieces: {currency.Coins}" : "Pieces: ?");
            GUILayout.Label(inventory != null ? inventory.GetSummary() : "Ressources: ?");
            if (!string.IsNullOrEmpty(message))
            {
                GUILayout.Box(message);
            }

            scroll = GUILayout.BeginScrollView(scroll);
            DrawRumors();
            GUILayout.EndScrollView();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fermer", GUILayout.Width(120f), GUILayout.Height(30f)))
            {
                Close();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawRumors()
        {
            RumorManager manager = RumorManager.GetOrCreate();
            IReadOnlyList<Rumor> rumors = manager.GetRumorsForPort(source != null ? source.Port : null);
            Vector3 referencePosition = GetReferencePosition(out Transform referenceTransform);

            for (int i = 0; i < rumors.Count; i++)
            {
                Rumor rumor = rumors[i];
                if (rumor == null)
                {
                    continue;
                }

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(rumor.title);
                GUILayout.Label(rumor.text);
                if (rumor.hasApproximatePosition)
                {
                    GUILayout.Label($"{rumor.GetDistanceText(referencePosition)} - {rumor.GetDirectionText(referencePosition, referenceTransform)}");
                }

                GUI.enabled = !rumor.Revealed;
                if (GUILayout.Button(rumor.Revealed ? "Deja notee" : "Noter dans le journal", GUILayout.Height(28f)))
                {
                    manager.RevealRumor(rumor);
                    message = "Rumeur ajoutee au journal de bord.";
                }

                GUI.enabled = true;
                GUILayout.EndVertical();
            }

            if (manager.KnownRumors.Count > 0)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Journal de rumeurs");
                for (int i = 0; i < manager.KnownRumors.Count; i++)
                {
                    Rumor rumor = manager.KnownRumors[i];
                    if (rumor == null)
                    {
                        continue;
                    }

                    GUILayout.Box($"{rumor.title}\n{rumor.GetDistanceText(referencePosition)} - {rumor.GetDirectionText(referencePosition, referenceTransform)}");
                }
            }
        }

        private static Vector3 GetReferencePosition(out Transform referenceTransform)
        {
            BoatHelmController boat = FindFirstObjectByType<BoatHelmController>();
            if (boat != null)
            {
                referenceTransform = boat.transform;
                return boat.transform.position;
            }

            Camera camera = Camera.main;
            referenceTransform = camera != null ? camera.transform : null;
            return camera != null ? camera.transform.position : Vector3.zero;
        }
    }
}
