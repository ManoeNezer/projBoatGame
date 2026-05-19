using BoatGame.Damage;
using BoatGame.Events;
using BoatGame.Weather;
using UnityEngine;

namespace BoatGame.Debugging
{
    [DisallowMultipleComponent]
    public sealed class DangerDebugUI : MonoBehaviour
    {
        [SerializeField] private bool show = true;
        [SerializeField] private BoatDamageSystem damageSystem;
        [SerializeField] private MaritimeEventManager eventManager;

        private GUIStyle labelStyle;

        private void Update()
        {
            if (damageSystem == null)
            {
                damageSystem = FindFirstObjectByType<BoatDamageSystem>();
            }

            if (eventManager == null)
            {
                eventManager = FindFirstObjectByType<MaritimeEventManager>();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                show = !show;
            }
        }

        private void OnGUI()
        {
            if (!show)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    normal = { textColor = new Color(0.92f, 0.96f, 0.92f, 0.95f) }
                };
            }

            GUILayout.BeginArea(new Rect(18f, 18f, 320f, 190f), GUI.skin.box);
            GUILayout.Label("DANGER MARITIME", labelStyle);

            WeatherManager weather = WeatherManager.Instance;
            if (weather != null)
            {
                GUILayout.Label($"Meteo: {weather.TargetState}  Danger {weather.Danger01:0.00}", labelStyle);
                GUILayout.Label($"Pluie {weather.Rain01:0.00}  Brouillard {weather.Fog01:0.00}  Tempete {weather.StormIntensity01:0.00}", labelStyle);
            }

            if (damageSystem != null)
            {
                GUILayout.Space(4f);
                GUILayout.Label($"Coque {damageSystem.HullIntegrity:P0}  Eau interne {damageSystem.InternalWater01:P0}", labelStyle);
                GUILayout.Label($"Voile {damageSystem.SailIntegrity:P0}  Gouvernail {damageSystem.RudderIntegrity:P0}  Mat {damageSystem.MastIntegrity:P0}", labelStyle);

                RepairResource resource = damageSystem.RepairResource;
                if (resource != null)
                {
                    GUILayout.Label($"Ressources bois: {resource.CurrentWoodPatches}/{resource.MaxWoodPatches}", labelStyle);
                }
            }

            if (eventManager != null)
            {
                GUILayout.Space(4f);
                GUILayout.Label($"Evenement: {eventManager.LastEventName} ({eventManager.ActiveEventCount})", labelStyle);
            }

            GUILayout.Label("F3: masquer/afficher", labelStyle);
            GUILayout.EndArea();
        }
    }
}
