using BoatGame.Water;
using UnityEngine;

namespace BoatGame.Debugging
{
    [ExecuteAlways]
    public sealed class WaterDebugProbe : MonoBehaviour
    {
        [SerializeField, Min(2f)] private float size = 40f;
        [SerializeField, Min(1f)] private float spacing = 5f;
        [SerializeField, Min(0.1f)] private float normalLength = 1.25f;
        [SerializeField] private bool drawNormals = true;
        [SerializeField] private bool drawSurfaceDots = true;

        private void OnDrawGizmos()
        {
            WaterManager water = WaterManager.Instance;
            if (water == null)
            {
                return;
            }

            float half = size * 0.5f;
            for (float x = -half; x <= half + 0.001f; x += spacing)
            {
                for (float z = -half; z <= half + 0.001f; z += spacing)
                {
                    Vector3 query = transform.position + new Vector3(x, 0f, z);
                    WaterManager.WaterSample sample = water.GetWaterSample(query);

                    if (drawSurfaceDots)
                    {
                        Gizmos.color = new Color(0.05f, 0.5f, 1f, 0.45f);
                        Gizmos.DrawSphere(sample.Position, 0.06f);
                    }

                    if (drawNormals)
                    {
                        Gizmos.color = new Color(0.1f, 1f, 0.75f, 0.75f);
                        Gizmos.DrawLine(sample.Position, sample.Position + sample.Normal * normalLength);
                    }
                }
            }
        }
    }
}
