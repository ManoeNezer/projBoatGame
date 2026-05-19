using BoatGame.Boat;
using UnityEngine;

namespace BoatGame.Port
{
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public sealed class DockingZone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PortManager port;
        [SerializeField] private Transform dockAnchor;

        [Header("Docking")]
        [SerializeField, Min(0.25f)] private float holdDuration = 1.25f;
        [SerializeField, Min(0f)] private float approachDamping = 2.4f;
        [SerializeField, Min(0f)] private float positionSpring = 1.8f;
        [SerializeField, Min(0f)] private float velocityDamping = 3.2f;
        [SerializeField, Min(0f)] private float rotationSpring = 2.2f;
        [SerializeField, Min(0f)] private float angularDamping = 4.5f;
        [SerializeField, Min(0f)] private float maxDockAcceleration = 8f;
        [SerializeField, Min(0f)] private float releaseImpulse = 1.2f;

        [Header("Input")]
        [SerializeField] private KeyCode dockKey = KeyCode.E;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        private BoatHelmController boat;
        private Rigidbody boatBody;
        private float holdTimer;
        private bool docked;
        private float releaseCooldown;

        public bool IsDocked => docked;
        public float Hold01 => Mathf.Clamp01(holdTimer / holdDuration);

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void Awake()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
            if (port == null)
            {
                port = GetComponentInParent<PortManager>();
            }
        }

        private void Update()
        {
            releaseCooldown = Mathf.Max(0f, releaseCooldown - Time.deltaTime);
            if (boat == null)
            {
                holdTimer = 0f;
                return;
            }

            if (docked)
            {
                if (Input.GetKeyDown(dockKey) && releaseCooldown <= 0f)
                {
                    Undock();
                }

                return;
            }

            if (Input.GetKey(dockKey))
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= holdDuration)
                {
                    docked = true;
                    releaseCooldown = 0.75f;
                }
            }
            else
            {
                holdTimer = Mathf.MoveTowards(holdTimer, 0f, Time.deltaTime * 1.8f);
            }
        }

        private void FixedUpdate()
        {
            if (boatBody == null)
            {
                return;
            }

            if (!docked)
            {
                ApplyApproachDamping();
                return;
            }

            ApplyDockingForces();
        }

        private void OnTriggerEnter(Collider other)
        {
            BoatHelmController candidate = GetBoat(other);
            if (candidate == null)
            {
                return;
            }

            boat = candidate;
            boatBody = candidate.Body;
        }

        private void OnTriggerExit(Collider other)
        {
            BoatHelmController candidate = GetBoat(other);
            if (candidate == null || candidate != boat || docked)
            {
                return;
            }

            boat = null;
            boatBody = null;
            holdTimer = 0f;
        }

        public void Configure(PortManager owner, Transform anchor)
        {
            port = owner;
            dockAnchor = anchor;
        }

        private void Undock()
        {
            docked = false;
            holdTimer = 0f;
            releaseCooldown = 1f;
            if (boatBody != null)
            {
                Vector3 direction = dockAnchor != null ? dockAnchor.forward : transform.forward;
                boatBody.AddForce(direction * releaseImpulse, ForceMode.VelocityChange);
            }
        }

        private void ApplyApproachDamping()
        {
            if (boatBody.linearVelocity.sqrMagnitude > 0.04f)
            {
                boatBody.AddForce(-boatBody.linearVelocity * approachDamping, ForceMode.Acceleration);
            }
        }

        private void ApplyDockingForces()
        {
            Vector3 targetPosition = dockAnchor != null ? dockAnchor.position : transform.position;
            Quaternion targetRotation = dockAnchor != null ? dockAnchor.rotation : transform.rotation;
            Vector3 positionError = targetPosition - boatBody.worldCenterOfMass;
            positionError.y *= 0.25f;
            Vector3 acceleration = Vector3.ClampMagnitude(positionError * positionSpring - boatBody.linearVelocity * velocityDamping, maxDockAcceleration);
            boatBody.AddForce(acceleration, ForceMode.Acceleration);

            float yawError = Mathf.DeltaAngle(boatBody.rotation.eulerAngles.y, targetRotation.eulerAngles.y);
            Vector3 angularAcceleration = Vector3.up * (yawError * Mathf.Deg2Rad * rotationSpring) - boatBody.angularVelocity * angularDamping;
            boatBody.AddTorque(angularAcceleration, ForceMode.Acceleration);
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

        private void OnGUI()
        {
            if (boat == null)
            {
                return;
            }

            string text = docked ? "E - larguer les amarres" : $"Maintenir E pour amarrer {Hold01:P0}";
            Rect rect = new Rect(Screen.width * 0.5f - 170f, Screen.height - 150f, 340f, 34f);
            GUI.Box(rect, text);
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = docked ? new Color(0.15f, 1f, 0.35f, 0.45f) : new Color(1f, 0.75f, 0.2f, 0.35f);
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
            Transform anchor = dockAnchor != null ? dockAnchor : transform;
            Gizmos.DrawLine(anchor.position, anchor.position + anchor.forward * 10f);
        }
    }
}
