using UnityEngine;

namespace BoatGame.Weather
{
    [DefaultExecutionOrder(250)]
    [DisallowMultipleComponent]
    public sealed class WeatherCameraFeedback : MonoBehaviour
    {
        [Header("Shake")]
        [SerializeField, Min(0f)] private float maxPositionShake = 0.035f;
        [SerializeField, Min(0f)] private float maxRotationShake = 1.15f;
        [SerializeField, Min(0.01f)] private float noiseFrequency = 1.6f;

        [Header("Wet Lens")]
        [SerializeField] private bool drawWetOverlay = true;
        [SerializeField] private Color overlayColor = new Color(0.58f, 0.78f, 0.86f, 0.12f);

        private Texture2D overlayTexture;

        private void LateUpdate()
        {
            WeatherManager weather = WeatherManager.Instance;
            if (weather == null)
            {
                return;
            }

            float danger = weather.Danger01;
            if (danger <= 0.001f)
            {
                return;
            }

            float time = Time.time * noiseFrequency;
            float x = Mathf.PerlinNoise(time, 2.1f) * 2f - 1f;
            float y = Mathf.PerlinNoise(7.3f, time * 1.17f) * 2f - 1f;
            float r = Mathf.PerlinNoise(time * 0.77f, 15.9f) * 2f - 1f;

            transform.localPosition += new Vector3(x, y * 0.65f, 0f) * (maxPositionShake * danger);
            transform.localRotation *= Quaternion.Euler(y * maxRotationShake * danger * 0.35f, x * maxRotationShake * danger * 0.25f, r * maxRotationShake * danger);
        }

        private void OnGUI()
        {
            WeatherManager weather = WeatherManager.Instance;
            if (!drawWetOverlay || weather == null)
            {
                return;
            }

            float wetness = Mathf.Clamp01(weather.Rain01 * 0.5f + weather.Danger01 * 0.18f);
            if (wetness <= 0.01f)
            {
                return;
            }

            if (overlayTexture == null)
            {
                overlayTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                overlayTexture.SetPixel(0, 0, Color.white);
                overlayTexture.Apply();
            }

            Color previous = GUI.color;
            GUI.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, overlayColor.a * wetness);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), overlayTexture);
            GUI.color = previous;
        }
    }
}
