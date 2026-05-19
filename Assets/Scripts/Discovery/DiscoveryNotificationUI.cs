using UnityEngine;

namespace BoatGame.Discovery
{
    [DisallowMultipleComponent]
    public sealed class DiscoveryNotificationUI : MonoBehaviour
    {
        [SerializeField] private bool visible = true;

        private void OnGUI()
        {
            if (!visible || DiscoveryManager.Instance == null || string.IsNullOrEmpty(DiscoveryManager.Instance.ActiveNotification))
            {
                return;
            }

            float alpha = Mathf.SmoothStep(0f, 1f, DiscoveryManager.Instance.Notification01);
            Color previous = GUI.color;
            GUI.color = new Color(0.92f, 0.96f, 0.88f, alpha);
            Rect area = new Rect(Screen.width * 0.5f - 240f, 126f, 480f, 58f);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label(DiscoveryManager.Instance.ActiveNotification);
            GUILayout.EndArea();
            GUI.color = previous;
        }
    }
}
