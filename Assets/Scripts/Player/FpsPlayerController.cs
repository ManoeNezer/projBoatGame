using BoatGame.Interaction;
using UnityEngine;

namespace BoatGame.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [DisallowMultipleComponent]
    public sealed class FpsPlayerController : MonoBehaviour
    {
        private struct GroundInfo
        {
            public bool isGrounded;
            public Vector3 point;
            public Vector3 normal;
            public Vector3 velocity;
            public Rigidbody body;
        }

        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraRoot;

        [Header("Movement")]
        [SerializeField, Min(0.5f)] private float walkSpeed = 3.2f;
        [SerializeField, Min(0.5f)] private float sprintSpeed = 4.6f;
        [SerializeField, Min(0.1f)] private float acceleration = 18f;
        [SerializeField, Min(0.1f)] private float airAcceleration = 5f;
        [SerializeField, Min(0f)] private float jumpVelocity = 5.1f;
        [SerializeField, Min(1f)] private float gravityMultiplier = 2.2f;
        [SerializeField, Min(0f)] private float groundStickAcceleration = 28f;

        [Header("Moving Rigidbody Ground")]
        [SerializeField, Min(0f)] private float platformVelocitySharpness = 18f;
        [SerializeField, Min(0f)] private float stationFollowSharpness = 24f;

        [Header("Ground Probe")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Range(0f, 75f)] private float maxGroundAngle = 56f;
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.22f;
        [SerializeField, Min(0.01f)] private float groundProbeStart = 0.08f;

        [Header("Look")]
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 1.8f;
        [SerializeField, Min(0f)] private float lookSmoothTime = 0.035f;
        [SerializeField, Range(45f, 89f)] private float maxPitch = 84f;

        [Header("Camera Feel")]
        [SerializeField, Min(0f)] private float cameraSmoothTime = 0.045f;
        [SerializeField, Min(0f)] private float eyeHeight = 1.62f;
        [SerializeField, Min(0f)] private float headBobAmplitude = 0.025f;
        [SerializeField, Min(0f)] private float headBobFrequency = 8.5f;
        [SerializeField, Min(0f)] private float shipSwayRoll = 1.4f;
        [SerializeField, Min(0f)] private float maxCameraRoll = 2.2f;

        [Header("Input")]
        [SerializeField] private KeyCode forwardKey = KeyCode.Z;
        [SerializeField] private KeyCode alternateForwardKey = KeyCode.W;
        [SerializeField] private KeyCode backwardKey = KeyCode.S;
        [SerializeField] private KeyCode leftKey = KeyCode.Q;
        [SerializeField] private KeyCode alternateLeftKey = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.D;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode exitStationKey = KeyCode.E;
        [SerializeField] private KeyCode alternateExitStationKey = KeyCode.Escape;

        private Rigidbody body;
        private CapsuleCollider capsule;
        private GroundInfo ground;
        private Vector3 smoothedPlatformVelocity;
        private Vector3 cameraLocalVelocity;
        private Vector3 defaultCameraLocalPosition;
        private float yaw;
        private float pitch;
        private float smoothedPitch;
        private float pitchVelocity;
        private float headBobTimer;
        private bool jumpQueued;
        private bool isInStation;
        private IPlayerStation activeStation;
        private Rigidbody stationPlatformBody;

        public bool IsInStation => isInStation;
        public Camera PlayerCamera => playerCamera;
        public Rigidbody Body => body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (cameraRoot == null && playerCamera != null)
            {
                cameraRoot = playerCamera.transform.parent != null ? playerCamera.transform.parent : playerCamera.transform;
            }

            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            yaw = transform.eulerAngles.y;
            defaultCameraLocalPosition = new Vector3(0f, eyeHeight, 0f);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Reset()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            body.mass = 75f;
            body.linearDamping = 0f;
            body.angularDamping = 0.05f;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            capsule.height = 1.8f;
            capsule.radius = 0.3f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
        }

        private void Update()
        {
            if (isInStation)
            {
                if (Input.GetKeyDown(exitStationKey) || Input.GetKeyDown(alternateExitStationKey))
                {
                    activeStation?.ExitStation(this);
                }

                return;
            }

            ReadLookInput();
            if (Input.GetKeyDown(jumpKey))
            {
                jumpQueued = true;
            }
        }

        private void FixedUpdate()
        {
            if (isInStation)
            {
                FollowStationAnchor();
                return;
            }

            ProbeGround();
            ApplyMovement();
        }

        private void LateUpdate()
        {
            if (isInStation)
            {
                UpdateStationCamera();
                return;
            }

            UpdateFreeCamera();
        }

        public void Configure(Camera camera, Transform root, LayerMask newGroundMask)
        {
            playerCamera = camera;
            cameraRoot = root;
            groundMask = newGroundMask;
        }

        public void EnterStation(IPlayerStation station)
        {
            if (station == null)
            {
                return;
            }

            activeStation = station;
            stationPlatformBody = station.PlatformBody;
            isInStation = true;
            jumpQueued = false;
            body.linearVelocity = GetAnchorVelocity(station.BodyAnchor);

            if (station.BodyAnchor != null)
            {
                yaw = station.BodyAnchor.eulerAngles.y;
                body.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        public void LeaveStation(IPlayerStation station)
        {
            if (activeStation != station)
            {
                return;
            }

            isInStation = false;
            activeStation = null;
            stationPlatformBody = null;
            yaw = transform.eulerAngles.y;
            pitch = 0f;
            smoothedPitch = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ReadLookInput()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            yaw += mouseX;
            pitch = Mathf.Clamp(pitch - mouseY, -maxPitch, maxPitch);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void ProbeGround()
        {
            ground = default;
            float radius = Mathf.Max(0.05f, capsule.radius * 0.92f);
            Vector3 origin = transform.position + Vector3.up * (capsule.radius + groundProbeStart);
            float distance = capsule.radius + groundProbeStart + groundProbeDistance;

            if (!UnityEngine.Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, distance, groundMask, QueryTriggerInteraction.Ignore))
            {
                smoothedPlatformVelocity = Vector3.Lerp(smoothedPlatformVelocity, Vector3.zero, 1f - Mathf.Exp(-platformVelocitySharpness * Time.fixedDeltaTime));
                return;
            }

            if (Vector3.Angle(hit.normal, Vector3.up) > maxGroundAngle)
            {
                return;
            }

            Vector3 rawPlatformVelocity = hit.rigidbody != null ? hit.rigidbody.GetPointVelocity(hit.point) : Vector3.zero;
            float blend = 1f - Mathf.Exp(-platformVelocitySharpness * Time.fixedDeltaTime);
            smoothedPlatformVelocity = Vector3.Lerp(smoothedPlatformVelocity, rawPlatformVelocity, blend);

            ground.isGrounded = true;
            ground.point = hit.point;
            ground.normal = hit.normal;
            ground.velocity = smoothedPlatformVelocity;
            ground.body = hit.rigidbody;
        }

        private void ApplyMovement()
        {
            Vector2 input = ReadMoveInput();
            bool sprinting = Input.GetKey(sprintKey) && input.y > 0.1f;
            float targetSpeed = sprinting ? sprintSpeed : walkSpeed;

            Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            Vector3 platformVelocity = ground.isGrounded ? ground.velocity : Vector3.zero;
            Vector3 desiredRelativeVelocity = moveDirection * targetSpeed;
            Vector3 velocity = body.linearVelocity;
            Vector3 relativeVelocity = velocity - platformVelocity;
            Vector3 currentRelativePlanar = Vector3.ProjectOnPlane(relativeVelocity, Vector3.up);
            Vector3 velocityDelta = desiredRelativeVelocity - currentRelativePlanar;
            float accel = ground.isGrounded ? acceleration : airAcceleration;
            body.AddForce(Vector3.ClampMagnitude(velocityDelta * accel, accel * targetSpeed), ForceMode.Acceleration);

            if (ground.isGrounded)
            {
                body.AddForce(-ground.normal * groundStickAcceleration, ForceMode.Acceleration);

                if (jumpQueued)
                {
                    Vector3 newVelocity = body.linearVelocity;
                    newVelocity.y = platformVelocity.y + jumpVelocity;
                    body.linearVelocity = newVelocity;
                    jumpQueued = false;
                }
                else if (Vector3.Dot(relativeVelocity, Vector3.up) <= 0.25f)
                {
                    Vector3 newVelocity = body.linearVelocity;
                    newVelocity.y = Mathf.Lerp(newVelocity.y, platformVelocity.y - 0.35f, 0.45f);
                    body.linearVelocity = newVelocity;
                }
            }
            else
            {
                body.AddForce(UnityEngine.Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
                jumpQueued = false;
            }
        }

        private Vector2 ReadMoveInput()
        {
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(forwardKey) || Input.GetKey(alternateForwardKey))
            {
                y += 1f;
            }

            if (Input.GetKey(backwardKey))
            {
                y -= 1f;
            }

            if (Input.GetKey(rightKey))
            {
                x += 1f;
            }

            if (Input.GetKey(leftKey) || Input.GetKey(alternateLeftKey))
            {
                x -= 1f;
            }

            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private void FollowStationAnchor()
        {
            Transform anchor = activeStation?.BodyAnchor;
            if (anchor == null)
            {
                return;
            }

            Vector3 targetPosition = anchor.position;
            Quaternion targetRotation = Quaternion.Euler(0f, anchor.eulerAngles.y, 0f);
            Vector3 anchorVelocity = GetAnchorVelocity(anchor);
            float blend = 1f - Mathf.Exp(-stationFollowSharpness * Time.fixedDeltaTime);

            body.linearVelocity = anchorVelocity;
            body.MovePosition(Vector3.Lerp(body.position, targetPosition, blend));
            body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, blend));
        }

        private Vector3 GetAnchorVelocity(Transform anchor)
        {
            if (anchor == null)
            {
                return Vector3.zero;
            }

            if (stationPlatformBody != null)
            {
                return stationPlatformBody.GetPointVelocity(anchor.position);
            }

            return Vector3.zero;
        }

        private void UpdateFreeCamera()
        {
            if (cameraRoot == null || playerCamera == null)
            {
                return;
            }

            Vector2 input = ReadMoveInput();
            float planarSpeed = Vector3.ProjectOnPlane(body.linearVelocity - (ground.isGrounded ? ground.velocity : Vector3.zero), Vector3.up).magnitude;

            Vector3 bob = Vector3.zero;
            if (ground.isGrounded && input.sqrMagnitude > 0.05f && planarSpeed > 0.2f)
            {
                headBobTimer += Time.deltaTime * headBobFrequency * Mathf.Lerp(0.75f, 1.35f, planarSpeed / sprintSpeed);
                bob.y = Mathf.Sin(headBobTimer) * headBobAmplitude;
                bob.x = Mathf.Cos(headBobTimer * 0.5f) * headBobAmplitude * 0.45f;
            }
            else
            {
                headBobTimer = Mathf.Lerp(headBobTimer, 0f, Time.deltaTime * 4f);
            }

            defaultCameraLocalPosition = new Vector3(0f, eyeHeight, 0f);
            Vector3 targetPosition = defaultCameraLocalPosition + bob;
            cameraRoot.localPosition = Vector3.SmoothDamp(cameraRoot.localPosition, targetPosition, ref cameraLocalVelocity, cameraSmoothTime);

            smoothedPitch = Mathf.SmoothDampAngle(smoothedPitch, pitch, ref pitchVelocity, lookSmoothTime);
            float roll = 0f;
            if (ground.body != null)
            {
                roll = Mathf.Clamp(-Vector3.Dot(ground.body.angularVelocity, transform.forward) * shipSwayRoll, -maxCameraRoll, maxCameraRoll);
            }

            playerCamera.transform.localRotation = Quaternion.Euler(smoothedPitch, 0f, roll);
        }

        private void UpdateStationCamera()
        {
            if (cameraRoot == null || playerCamera == null || activeStation?.CameraAnchor == null)
            {
                return;
            }

            Transform anchor = activeStation.CameraAnchor;
            float blend = 1f - Mathf.Exp(-stationFollowSharpness * Time.deltaTime);
            cameraRoot.position = Vector3.Lerp(cameraRoot.position, anchor.position, blend);
            cameraRoot.rotation = Quaternion.Slerp(cameraRoot.rotation, anchor.rotation, blend);
            playerCamera.transform.localPosition = Vector3.zero;
            playerCamera.transform.localRotation = Quaternion.identity;
        }
    }
}
