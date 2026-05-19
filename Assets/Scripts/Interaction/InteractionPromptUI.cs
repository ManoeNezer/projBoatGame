using UnityEngine;
using UnityEngine.UI;

namespace BoatGame.Interaction
{
    [DisallowMultipleComponent]
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private Text promptText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float fadeSpeed = 12f;

        private float targetAlpha;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            SetPrompt(string.Empty, false);
        }

        private void Update()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
            }
        }

        public void Configure(Text text, CanvasGroup group)
        {
            promptText = text;
            canvasGroup = group;
            SetPrompt(string.Empty, false);
        }

        public void SetPrompt(string prompt, bool visible)
        {
            if (promptText != null)
            {
                promptText.text = visible ? $"[E] {prompt}" : string.Empty;
            }

            targetAlpha = visible ? 1f : 0f;
            if (!Application.isPlaying && canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
            }
        }
    }
}
