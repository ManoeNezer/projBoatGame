using BoatGame.Environment;
using UnityEngine;

namespace BoatGame.Boat
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class BoatHelmController : MonoBehaviour
    {
        [Header("Force Points")]
        [SerializeField] private Transform sailForcePoint;
        [SerializeField] private Transform rudderForcePoint;

        [Header("Visuals")]
        [SerializeField] private Transform sailVisual;
        [SerializeField] private Transform boomVisual;
        [SerializeField] private Transform rudderVisual;
        [SerializeField] private Transform wheelVisual;

        [Header("Rudder")]
        [SerializeField, Range(5f, 60f)] private float maxRudderAngle = 34f;
        [SerializeField, Min(1f)] private float rudderTurnSpeed = 52f;
        [SerializeField, Min(1f)] private float rudderReturnSpeed = 18f;
        [SerializeField, Min(1f)] private float rudderResponseSpeed = 85f;
        [SerializeField, Min(0f)] private float rudderTorque = 42000f;
        [SerializeField, Min(0f)] private float rudderSideForce = 10500f;

        [Header("Sail")]
        [SerializeField, Range(0f, 1f)] private float sailOpen01 = 0.72f;
        [SerializeField, Range(-85f, 85f)] private float sailAngle = 8f;
        [SerializeField, Min(0.01f)] private float sailHoistSpeed = 0.42f;
        [SerializeField, Min(1f)] private float sailTrimSpeed = 48f;
        [SerializeField, Min(1f)] private float sailResponseSpeed = 75f;
        [SerializeField, Min(0f)] private float sailPropulsionScale = 175f;
        [SerializeField, Min(0f)] private float sailSideForceScale = 48f;

        [Header("Hull Resistance")]
        [SerializeField, Min(0f)] private float lateralResistance = 3.4f;
        [SerializeField, Min(0f)] private float yawResistance = 0.32f;
        [SerializeField, Min(0f)] private float maxForwardSpeed = 9f;

        [Header("Fallback Keyboard")]
        [SerializeField] private bool enableDirectKeyboardFallback;
        [SerializeField] private KeyCode fallbackForwardKey = KeyCode.W;
        [SerializeField] private KeyCode fallbackReverseKey = KeyCode.S;
        [SerializeField] private KeyCode fallbackTurnLeftKey = KeyCode.A;
        [SerializeField] private KeyCode fallbackTurnRightKey = KeyCode.D;

        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;

        private Rigidbody body;
        private float targetRudderAngle;
        private float currentRudderAngle;
        private float targetSailOpen01;
        private float targetSailAngle;
        private float helmInput;
        private float sailHoistInput;
        private float sailTrimInput;
        private int lastHelmInputFrame = -1000;
        private int lastSailInputFrame = -1000;
        private Quaternion sailBaseRotation = Quaternion.identity;
        private Quaternion boomBaseRotation = Quaternion.identity;
        private Quaternion rudderBaseRotation = Quaternion.identity;
        private Quaternion wheelBaseRotation = Quaternion.identity;
        private Vector3 sailBaseScale = Vector3.one;
        private Vector3 sailBaseLocalPosition;
        private Vector3 lastPropulsionForce;
        private Vector3 lastRudderForce;
        private float damageSailEfficiency = 1f;
        private float damageRudderEfficiency = 1f;
        private float damageMastStability = 1f;
        private float weatherSailForceMultiplier = 1f;
        private float weatherRudderForceMultiplier = 1f;
        private float weatherHandlingMultiplier = 1f;
        private float weatherInstability = 0f;
        private float upgradeSailForceMultiplier = 1f;
        private float upgradeRudderForceMultiplier = 1f;
        private float upgradeHandlingMultiplier = 1f;

        public Rigidbody Body => body;
        public float CurrentRudderAngle => currentRudderAngle;
        public float CurrentSailAngle => sailAngle;
        public float SailOpen01 => sailOpen01;
        public Vector3 LastPropulsionForce => lastPropulsionForce;
        public Vector3 LastRudderForce => lastRudderForce;
        public float SailEfficiency => damageSailEfficiency * weatherSailForceMultiplier * upgradeSailForceMultiplier;
        public float RudderEfficiency => damageRudderEfficiency * weatherRudderForceMultiplier * upgradeRudderForceMultiplier;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            targetSailOpen01 = sailOpen01;
            targetSailAngle = sailAngle;
            CacheVisualDefaults();
        }

        private void OnValidate()
        {
            maxRudderAngle = Mathf.Clamp(maxRudderAngle, 5f, 60f);
            sailOpen01 = Mathf.Clamp01(sailOpen01);
            targetSailOpen01 = Mathf.Clamp01(targetSailOpen01);
            sailAngle = Mathf.Clamp(sailAngle, -85f, 85f);
            targetSailAngle = Mathf.Clamp(targetSailAngle, -85f, 85f);
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (enableDirectKeyboardFallback)
            {
                ReadFallbackKeyboard();
            }

            float dt = Time.fixedDeltaTime;
            UpdateRudderState(dt);
            UpdateSailState(dt);
            ApplySailForces();
            ApplyRudderForces();
            ApplyHullResistance();
        }

        private void LateUpdate()
        {
            UpdateVisuals();
        }

        public void SetForcePoints(Transform sailPoint, Transform rudderPoint)
        {
            sailForcePoint = sailPoint;
            rudderForcePoint = rudderPoint;
        }

        public void SetVisuals(Transform sail, Transform boom, Transform rudder, Transform wheel)
        {
            sailVisual = sail;
            boomVisual = boom;
            rudderVisual = rudder;
            wheelVisual = wheel;
            CacheVisualDefaults();
            UpdateVisuals();
        }

        public void SetHelmInput(float input)
        {
            helmInput = Mathf.Clamp(input, -1f, 1f);
            lastHelmInputFrame = Time.frameCount;
        }

        public void SetSailInput(float hoistInput, float trimInput)
        {
            sailHoistInput = Mathf.Clamp(hoistInput, -1f, 1f);
            sailTrimInput = Mathf.Clamp(trimInput, -1f, 1f);
            lastSailInputFrame = Time.frameCount;
        }

        public void ConfigurePrototypeRig(
            Transform sailPoint,
            Transform rudderPoint,
            Transform sail,
            Transform boom,
            Transform rudder,
            Transform wheel)
        {
            SetForcePoints(sailPoint, rudderPoint);
            SetVisuals(sail, boom, rudder, wheel);
            enableDirectKeyboardFallback = false;
            targetSailOpen01 = sailOpen01;
            targetSailAngle = sailAngle;
        }

        public void SetDamageModifiers(float sailEfficiency, float rudderEfficiency, float mastStability)
        {
            damageSailEfficiency = Mathf.Clamp01(sailEfficiency);
            damageRudderEfficiency = Mathf.Clamp01(rudderEfficiency);
            damageMastStability = Mathf.Clamp01(mastStability);
        }

        public void SetWeatherModifiers(float sailForceMultiplier, float rudderForceMultiplier, float handlingMultiplier, float instability)
        {
            weatherSailForceMultiplier = Mathf.Clamp(sailForceMultiplier, 0.15f, 2.5f);
            weatherRudderForceMultiplier = Mathf.Clamp(rudderForceMultiplier, 0.2f, 1.8f);
            weatherHandlingMultiplier = Mathf.Clamp(handlingMultiplier, 0.25f, 1.5f);
            weatherInstability = Mathf.Clamp01(instability);
        }

        public void SetUpgradeModifiers(float sailForceMultiplier, float rudderForceMultiplier, float handlingMultiplier)
        {
            upgradeSailForceMultiplier = Mathf.Clamp(sailForceMultiplier, 0.5f, 3f);
            upgradeRudderForceMultiplier = Mathf.Clamp(rudderForceMultiplier, 0.5f, 3f);
            upgradeHandlingMultiplier = Mathf.Clamp(handlingMultiplier, 0.5f, 2f);
        }

        private void ReadFallbackKeyboard()
        {
            float helm = 0f;
            if (Input.GetKey(fallbackTurnRightKey))
            {
                helm += 1f;
            }

            if (Input.GetKey(fallbackTurnLeftKey))
            {
                helm -= 1f;
            }

            if (Mathf.Abs(helm) > 0.001f)
            {
                SetHelmInput(helm);
            }

            float hoist = 0f;
            if (Input.GetKey(fallbackForwardKey))
            {
                hoist += 1f;
            }

            if (Input.GetKey(fallbackReverseKey))
            {
                hoist -= 1f;
            }

            if (Mathf.Abs(hoist) > 0.001f)
            {
                SetSailInput(hoist, 0f);
            }
        }

        private void UpdateRudderState(float dt)
        {
            bool hasInput = Time.frameCount - lastHelmInputFrame <= 2 && Mathf.Abs(helmInput) > 0.001f;
            float rudderHandling = Mathf.Lerp(0.42f, 1f, damageRudderEfficiency) * weatherHandlingMultiplier * upgradeHandlingMultiplier;
            if (hasInput)
            {
                targetRudderAngle = Mathf.Clamp(targetRudderAngle + helmInput * rudderTurnSpeed * rudderHandling * dt, -maxRudderAngle, maxRudderAngle);
            }
            else
            {
                targetRudderAngle = Mathf.MoveTowards(targetRudderAngle, 0f, rudderReturnSpeed * Mathf.Max(0.45f, rudderHandling) * dt);
                helmInput = 0f;
            }

            currentRudderAngle = Mathf.MoveTowards(currentRudderAngle, targetRudderAngle, rudderResponseSpeed * Mathf.Max(0.35f, rudderHandling) * dt);
        }

        private void UpdateSailState(float dt)
        {
            bool hasInput = Time.frameCount - lastSailInputFrame <= 2;
            float sailHandling = Mathf.Lerp(0.35f, 1f, damageMastStability) * weatherHandlingMultiplier * upgradeHandlingMultiplier;
            if (hasInput)
            {
                targetSailOpen01 = Mathf.Clamp01(targetSailOpen01 + sailHoistInput * sailHoistSpeed * sailHandling * dt);
                targetSailAngle = Mathf.Clamp(targetSailAngle + sailTrimInput * sailTrimSpeed * sailHandling * dt, -85f, 85f);
            }
            else
            {
                sailHoistInput = 0f;
                sailTrimInput = 0f;
            }

            sailOpen01 = Mathf.MoveTowards(sailOpen01, targetSailOpen01, sailHoistSpeed * Mathf.Max(0.35f, sailHandling) * dt * 1.35f);
            sailAngle = Mathf.MoveTowards(sailAngle, targetSailAngle, sailResponseSpeed * Mathf.Max(0.35f, sailHandling) * dt);
        }

        private void ApplySailForces()
        {
            lastPropulsionForce = Vector3.zero;

            WindManager wind = WindManager.Instance;
            if (wind == null || sailOpen01 <= 0.01f)
            {
                return;
            }

            Vector3 forcePoint = GetForcePoint(sailForcePoint);
            Vector3 apparentWind = wind.GetWindVelocity(forcePoint) - body.GetPointVelocity(forcePoint);
            apparentWind.y = 0f;
            float apparentSpeed = apparentWind.magnitude;
            if (apparentSpeed < 0.05f)
            {
                return;
            }

            Vector3 apparentDirection = apparentWind / apparentSpeed;
            Vector3 forward = transform.forward;
            float unstableAngle = sailAngle + Mathf.Sin(Time.time * 5.7f + transform.position.x * 0.03f) * weatherInstability * Mathf.Lerp(3f, 10f, 1f - damageMastStability);
            Vector3 sailNormal = Quaternion.AngleAxis(unstableAngle, transform.up) * forward;
            float catchAmount = Mathf.Abs(Vector3.Dot(apparentDirection, sailNormal));
            float forwardWind = Vector3.Dot(apparentDirection, forward);
            float windUsefulness = Mathf.Clamp01(forwardWind * 0.55f + 0.62f);
            float trimEfficiency = Mathf.SmoothStep(0f, 1f, catchAmount);
            float speedLimiter = 1f - Mathf.InverseLerp(maxForwardSpeed * 0.78f, maxForwardSpeed, Vector3.Dot(body.linearVelocity, forward));
            float rigEfficiency = damageSailEfficiency * Mathf.Lerp(0.45f, 1f, damageMastStability) * weatherSailForceMultiplier * upgradeSailForceMultiplier;
            float efficiency = sailOpen01 * trimEfficiency * windUsefulness * Mathf.Clamp01(speedLimiter) * rigEfficiency;

            Vector3 propulsion = forward * (apparentSpeed * apparentSpeed * sailPropulsionScale * efficiency);
            float sideSign = Vector3.Dot(apparentDirection, transform.right);
            Vector3 sideForce = transform.right * (sideSign * apparentSpeed * apparentSpeed * sailSideForceScale * sailOpen01 * trimEfficiency);

            lastPropulsionForce = propulsion + sideForce;
            body.AddForceAtPosition(lastPropulsionForce, forcePoint, ForceMode.Force);
        }

        private void ApplyRudderForces()
        {
            lastRudderForce = Vector3.zero;
            if (Mathf.Abs(currentRudderAngle) < 0.01f)
            {
                return;
            }

            Vector3 forward = transform.forward;
            float forwardSpeed = Vector3.Dot(body.linearVelocity, forward);
            float speedFactor = Mathf.InverseLerp(0.2f, maxForwardSpeed * 0.75f, Mathf.Abs(forwardSpeed));
            float directionSign = Mathf.Sign(Mathf.Abs(forwardSpeed) > 0.15f ? forwardSpeed : 1f);
            float rudder01 = currentRudderAngle / maxRudderAngle;
            float rudderEfficiency = damageRudderEfficiency * weatherRudderForceMultiplier * upgradeRudderForceMultiplier;

            body.AddTorque(Vector3.up * (rudder01 * rudderTorque * speedFactor * directionSign * rudderEfficiency), ForceMode.Force);
            lastRudderForce = -transform.right * (rudder01 * rudderSideForce * speedFactor * directionSign * rudderEfficiency);
            body.AddForceAtPosition(lastRudderForce, GetForcePoint(rudderForcePoint), ForceMode.Force);
        }

        private void ApplyHullResistance()
        {
            Vector3 right = transform.right;
            Vector3 lateralVelocity = Vector3.Project(body.linearVelocity, right);
            body.AddForce(-lateralVelocity * lateralResistance, ForceMode.Acceleration);

            Vector3 yawVelocity = Vector3.Project(body.angularVelocity, Vector3.up);
            body.AddTorque(-yawVelocity * (yawResistance * body.mass), ForceMode.Force);
        }

        private Vector3 GetForcePoint(Transform point)
        {
            return point != null ? point.position : transform.position;
        }

        private void CacheVisualDefaults()
        {
            if (sailVisual != null)
            {
                sailBaseRotation = sailVisual.localRotation;
                sailBaseScale = sailVisual.localScale;
                sailBaseLocalPosition = sailVisual.localPosition;
            }

            if (boomVisual != null)
            {
                boomBaseRotation = boomVisual.localRotation;
            }

            if (rudderVisual != null)
            {
                rudderBaseRotation = rudderVisual.localRotation;
            }

            if (wheelVisual != null)
            {
                wheelBaseRotation = wheelVisual.localRotation;
            }
        }

        private void UpdateVisuals()
        {
            if (sailVisual != null)
            {
                float visibleHeight = Mathf.Lerp(0.12f, 1f, sailOpen01);
                float flap = Mathf.Sin(Time.time * 7.5f) * weatherInstability * Mathf.Lerp(2f, 8f, 1f - damageMastStability);
                sailVisual.localRotation = sailBaseRotation * Quaternion.Euler(flap * 0.25f, sailAngle + flap, flap * 0.35f);
                sailVisual.localScale = new Vector3(sailBaseScale.x, sailBaseScale.y * visibleHeight, sailBaseScale.z);
                sailVisual.localPosition = sailBaseLocalPosition + Vector3.down * ((1f - visibleHeight) * 0.58f);
            }

            if (boomVisual != null)
            {
                float boomShake = Mathf.Sin(Time.time * 6.2f + 0.4f) * weatherInstability * Mathf.Lerp(1f, 5f, 1f - damageMastStability);
                boomVisual.localRotation = boomBaseRotation * Quaternion.Euler(0f, sailAngle + boomShake, 0f);
            }

            if (rudderVisual != null)
            {
                rudderVisual.localRotation = rudderBaseRotation * Quaternion.Euler(0f, currentRudderAngle, 0f);
            }

            if (wheelVisual != null)
            {
                wheelVisual.localRotation = wheelBaseRotation * Quaternion.Euler(0f, 0f, -currentRudderAngle * 2.8f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            Vector3 sailPoint = GetForcePoint(sailForcePoint);
            Vector3 rudderPoint = GetForcePoint(rudderForcePoint);

            Gizmos.color = new Color(1f, 0.82f, 0.12f, 0.9f);
            Gizmos.DrawLine(sailPoint, sailPoint + lastPropulsionForce * 0.00045f);
            Gizmos.DrawSphere(sailPoint, 0.1f);

            Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.9f);
            Gizmos.DrawLine(rudderPoint, rudderPoint + lastRudderForce * 0.0007f);
            Gizmos.DrawSphere(rudderPoint, 0.08f);

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawLine(transform.position + Vector3.up * 1.2f, transform.position + Vector3.up * 1.2f + transform.forward * 3f);
        }
    }
}
