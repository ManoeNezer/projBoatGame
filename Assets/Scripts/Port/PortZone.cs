using BoatGame.Boat;
using UnityEngine;

namespace BoatGame.Port
{
    [DisallowMultipleComponent]
    public sealed class PortZone : MonoBehaviour
    {
        [SerializeField] private PortManager port;
        [SerializeField] private bool boatInside;
        [SerializeField] private bool drawGizmos = true;

        public PortManager Port => port;
        public bool BoatInside => boatInside;

        private void Awake()
        {
            if (port == null)
            {
                port = GetComponentInParent<PortManager>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (GetBoat(other) != null)
            {
                boatInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (GetBoat(other) != null)
            {
                boatInside = false;
            }
        }

        private static BoatHelmController GetBoat(Collider other)
        {
            if (other == null)
            {
                return null;
            }

            Rigidbody attached = other.attachedRigidbody;
            return attached != null ? attached.GetComponent<BoatHelmController>() : other.GetComponentInParent<BoatHelmController>();
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = boatInside ? new Color(0.2f, 1f, 0.45f, 0.35f) : new Color(0.35f, 0.8f, 1f, 0.2f);
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
    }
}
