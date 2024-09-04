using UnityEngine;
using UnityEngine.InputSystem;
using ONYX;
using UnityEngine.UI;

//  TODO:
//  - Air controll.
//  - Camera controller.
//      - Use the world y postion offset as this should not
//        let the camera go under the world.

public class CarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody CarRigidbody;
    [SerializeField] private Text SpeedText;
    [SerializeField] private Slider BoostSlider;
    [SerializeField] private RectTransform SpedometerNeedle;

    [Header("Booster References")]
    [SerializeField] private float MaxSpeedWitBoost = 50;
    [SerializeField] private AnimationCurve BoostPowerCurve;
    [SerializeField] private BoosterData[] AllBoosterData;

    [Space]
    [SerializeField, ReadOnly] private float BoostAmount = 100;
    [SerializeField] private float BoostUsageSpeed = 1;
    [SerializeField] private float BoostRegenSpeed = 2;
    [SerializeField, ReadOnly] private bool ActivlyBoosting;

    [Space]
    [SerializeField] private float BoostRegenDelay;
    [SerializeField] private bool WaitForBoostRegen;
    [SerializeField, ReadOnly] private float BoostRegenCounter;
    [SerializeField, ReadOnly] private bool WaitingForBoostRegen;

    [Header("Air Controll")]
    [SerializeField] private float AirControllYawStrength = 5000f;
    [SerializeField] private float AirControllPitchStrength = 5000f;
    [SerializeField, ReadOnly] private bool CarInAir;

    [Header("Upright Assist")]
    [SerializeField] private float UprightAssistStrength = 10;
    [SerializeField] private AnimationCurve UprightAssistForceCurve;
    [SerializeField] private float UprightAssistDistance = 2;

    [Header("Suspension Setting")]
    [SerializeField] private LayerMask CarLayer;
    [SerializeField] private float Suspension_RestDistance = 0.6f;
    [SerializeField] private float Suspension_Strength = 500;
    [SerializeField] private float Suspension_Damping = 50;

    [Header("Steer Setting")]
    [SerializeField] private float Steer_MaxAngle = 30;
    [SerializeField] private float Steer_Smoothing = 20;

    [Header("Throttle Setting")]
    [SerializeField] private float RollDamping = 10;
    [SerializeField] private float MaxSpeed = 25;
    [SerializeField] private AnimationCurve ThrottlePowerCurve;

    [Header("Wheel References")]
    [SerializeField] private WheelData[] AllWheelData;

    [Header("Input Debug")]
    [SerializeField, ReadOnly] private float RawThrottleInput;
    [SerializeField, ReadOnly] private float RawSteerInput;
    [SerializeField, ReadOnly] private float RawACYawInput;
    [SerializeField, ReadOnly] private float RawACPitchInput;
    [SerializeField, ReadOnly] private bool RawDriftInput;
    [SerializeField, ReadOnly] private bool RawBoostInput;
    [SerializeField, ReadOnly] private bool RawResetInput;
    [SerializeField, ReadOnly] private bool LastResetInput;

    [System.Serializable]
    public class WheelData
    {
        [Header("References")]
        public Transform Wheel;

        [Header("Setting")]
        public float Acceleration = 50;
        public float Mass = 100;
        [ReadOnly] public float ActiveGrip;
        [Range(0, 1)] public float NormalGrip = 0.2f;
        [Range(0, 1)] public float DriftGrip = 0.05f;
        [Space]
        public float GraphicRotationMultiplier = 150;
        [Space]
        public bool IsDriveWheel;
        public bool IsSteerWheel;
        public bool ReverseSteering;

        [Header("Ground")]
        [ReadOnly] public bool IsWheelGrounded;
        [ReadOnly] public float GroundDistance;
    }

    [System.Serializable]
    public class BoosterData
    {
        public bool IsBoosterActive = true;

        [Header("References")]
        public Transform Booster;

        [Header("Settings")]
        public float BoostForce;
    }

    private void FixedUpdate()
    {
        CarInAir = true;
        foreach (WheelData wheelData in AllWheelData)
        {
            if (wheelData.IsWheelGrounded) CarInAir = false;
            SetWheelActiveGrip(wheelData);
            CheckWheelGround(wheelData);
            SuspensionForWheel(wheelData);
            SteeringForWheel(wheelData);
            UpdateWheelGraphics(wheelData);
        }

        if (CarInAir) AirControll();

        foreach (BoosterData boosterData in AllBoosterData)
        {
            if (!boosterData.IsBoosterActive) return;

            ActivlyBoosting = RawBoostInput && (BoostAmount > 0) && !WaitingForBoostRegen;

            if (ActivlyBoosting)
            {
                Boost(boosterData);

                BoostAmount -= BoostUsageSpeed * Time.deltaTime;

                BoostRegenCounter = BoostRegenDelay;
            }

            if (WaitForBoostRegen)
            {
                if (BoostAmount <= 0)
                {
                    WaitingForBoostRegen = true;
                }
                else if (BoostAmount >= 100)
                {
                    WaitingForBoostRegen = false;
                }
            }

            BoostRegenCounter -= Time.deltaTime;
            if (BoostRegenCounter <= 0)
            {
                BoostAmount += BoostRegenSpeed * Time.deltaTime;
            }

            BoostAmount = Mathf.Clamp(BoostAmount, 0, 100);

            UpdateBoosterGraphics(boosterData);
        }

        AssistUprightness();

        CheckForReset();
        LastResetInput = RawResetInput;

        UpdateGUI();
    }

    private void UpdateGUI()
    {
        SpeedText.text = (Mathf.Round(CarRigidbody.velocity.magnitude * 100) / 100).ToString();

        BoostSlider.interactable = !WaitingForBoostRegen;
        BoostSlider.value = BoostAmount;

        float percentOfMaxBoostSpeed = CarRigidbody.velocity.magnitude / MaxSpeedWitBoost;
        float angle = ((180 * percentOfMaxBoostSpeed) - 90) * -1;
        SpedometerNeedle.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void AirControll()
    {
        float yawForce = RawACYawInput * AirControllYawStrength * Time.deltaTime;
        float pitchForce = RawACPitchInput * AirControllPitchStrength * Time.deltaTime;
        CarRigidbody.AddTorque(transform.up * yawForce + transform.right * pitchForce);
    }

    #region Boosters
    private void Boost(BoosterData boosterData)
    {
        float percentOfMaxBoostSpeed = CarRigidbody.velocity.magnitude / MaxSpeedWitBoost;
        float boostAdjusted = BoostPowerCurve.Evaluate(percentOfMaxBoostSpeed);

        Vector3 boostDirection = boosterData.Booster.forward;
        Vector3 boostVector = boostDirection * boosterData.BoostForce * boostAdjusted * (RawBoostInput ? 1 : 0);

        CarRigidbody.AddForceAtPosition(boostVector, boosterData.Booster.position);
    }

    private void UpdateBoosterGraphics(BoosterData boosterData)
    {
        GameObject boosterEffects = boosterData.Booster.Find("Effects").gameObject;
        boosterEffects.SetActive(ActivlyBoosting);
    }
    #endregion

    #region Wheels
    private void SetWheelActiveGrip(WheelData wheelData)
    {
        wheelData.ActiveGrip = RawDriftInput ? wheelData.DriftGrip : wheelData.NormalGrip;
    }


    private void CheckWheelGround(WheelData wheelData)
    {
        wheelData.IsWheelGrounded = Physics.Raycast(wheelData.Wheel.position, wheelData.Wheel.TransformDirection(Vector3.down), out RaycastHit hit, Suspension_RestDistance, ~CarLayer.value);
        wheelData.GroundDistance = hit.distance;
    }

    private void SuspensionForWheel(WheelData wheelData)
    {
        if (!wheelData.IsWheelGrounded) return;

        Transform wheel = wheelData.Wheel;

        Vector3 springDirection = wheel.up;
        Vector3 wheelWorldVelocity = CarRigidbody.GetPointVelocity(wheel.position);
        float velocity = Vector3.Dot(springDirection, wheelWorldVelocity);

        // Suspension Calculations
        float offset = Suspension_RestDistance - wheelData.GroundDistance;
        float force = (offset * Suspension_Strength) - (velocity * Suspension_Damping);

        Debug.DrawLine(wheel.position, wheel.position + springDirection * force * 0.1f, Color.green);

        CarRigidbody.AddForceAtPosition(springDirection * force, wheel.position);
    }

    private void SteeringForWheel(WheelData wheelData)
    {
        if (!wheelData.IsWheelGrounded) return;

        Transform wheel = wheelData.Wheel;

        Vector3 wheelWorldVelocity = CarRigidbody.GetPointVelocity(wheel.position);

        // Roll
        if (wheelData.IsDriveWheel)
        {
            Vector3 rollDirection = wheel.forward;
            float rollVelocityDot = Vector3.Dot(wheelWorldVelocity, wheel.forward);

            // Adjust roll force by ThrottlePowerCurve
            float percentOfMaxSpeed = CarRigidbody.velocity.magnitude / MaxSpeed;
            float throttleAdjusted = ThrottlePowerCurve.Evaluate(percentOfMaxSpeed);

            float rollForce = 0;
            if (RawThrottleInput != 0)
            {
                rollForce = wheelData.Acceleration * (RawThrottleInput * throttleAdjusted);
            }
            else
            {
                rollForce = -rollVelocityDot * RollDamping;
            }

            CarRigidbody.AddForceAtPosition(rollDirection * rollForce, wheel.position);

            Debug.DrawLine(wheel.position, wheel.position + rollDirection * rollForce * 0.1f, Color.blue);
        }

        // Slide
        Vector3 steerDirection = wheel.right;
        float slideVelocityDot = Vector3.Dot(wheelWorldVelocity, wheel.right);

        float changeInVelocity = -slideVelocityDot * wheelData.ActiveGrip;
        float steerForce = wheelData.Mass * changeInVelocity;

        CarRigidbody.AddForceAtPosition(steerDirection * steerForce, wheel.position);

        Debug.DrawLine(wheel.position, wheel.position + steerDirection * slideVelocityDot * 0.1f, Color.yellow);
        Debug.DrawLine(wheel.position, wheel.position + steerDirection * steerForce * 0.1f, Color.red);
    }

    private void UpdateWheelGraphics(WheelData wheelData)
    {
        // Suspension
        Transform root = wheelData.Wheel.Find("Root");
        if (Physics.Raycast(wheelData.Wheel.position, wheelData.Wheel.TransformDirection(Vector3.down), out RaycastHit hit, Suspension_RestDistance, ~CarLayer.value))
        {
            root.transform.position = hit.point;
        }
        else
        {
            root.transform.localPosition = new Vector3(0, -Suspension_RestDistance, 0);
        }

        // Steer
        if (wheelData.IsSteerWheel)
        {
            wheelData.Wheel.localRotation = Quaternion.Lerp(
                wheelData.Wheel.localRotation,
                Quaternion.Euler(0, RawSteerInput * Steer_MaxAngle * (wheelData.ReverseSteering ? -1 : 1), 0),
                Steer_Smoothing * Time.deltaTime
            );
        }

        // Roll
        if (wheelData.IsDriveWheel && RawDriftInput)
        { // Drifting.
            Transform roll = root.Find("Roll");
            float driftRollSpeed = 10;

            roll.localRotation = roll.localRotation * Quaternion.Euler(driftRollSpeed * wheelData.GraphicRotationMultiplier * Time.deltaTime, 0, 0);
        }
        else
        {
            Transform roll = root.Find("Roll");
            Vector3 wheelVelocity = CarRigidbody.GetPointVelocity(wheelData.Wheel.position);
            float rollVelocityDot = Vector3.Dot(wheelVelocity, wheelData.Wheel.forward);

            roll.localRotation = roll.localRotation * Quaternion.Euler(rollVelocityDot * wheelData.GraphicRotationMultiplier * Time.deltaTime, 0, 0);
        }
    }
    #endregion

    private void CheckForReset()
    {
        if (RawResetInput && !LastResetInput)
        {
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            transform.position = transform.position + new Vector3(0, 3, 0);
        }
    }

    private void AssistUprightness()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, UprightAssistDistance, ~CarLayer))
            return;

        Transform uprightAssistPivot = transform.Find("Upright Assist Pivot");

        Vector3 currentUp = uprightAssistPivot.up;
        Vector3 desiredUp = Vector3.up;

        Vector3 rotationAxis = Vector3.Cross(currentUp, desiredUp);
        float angleDifference = Vector3.Angle(currentUp, desiredUp);

        float maxDistance = uprightAssistPivot.localPosition.y * 2;

        float currentDistance = Vector3.Distance(currentUp, desiredUp);
        float normalizedDistance = currentDistance / maxDistance;

        float forceMultiplier = UprightAssistForceCurve.Evaluate(normalizedDistance);

        Vector3 torque = rotationAxis.normalized * angleDifference * UprightAssistStrength * forceMultiplier;

        CarRigidbody.AddTorque(torque * Time.deltaTime);
    }

    #region Inputs
    public void ThrottleInput(InputAction.CallbackContext ctx) { RawThrottleInput = ctx.ReadValue<float>(); }

    public void SteerInput(InputAction.CallbackContext ctx) { RawSteerInput = ctx.ReadValue<float>(); }

    public void ACYawInput(InputAction.CallbackContext ctx) { RawACYawInput = ctx.ReadValue<float>(); }
    public void ACPitchInput(InputAction.CallbackContext ctx) { RawACPitchInput = ctx.ReadValue<float>(); }

    public void DriftInput(InputAction.CallbackContext ctx) { RawDriftInput = ctx.performed; }
    public void ResetInput(InputAction.CallbackContext ctx) { RawResetInput = ctx.performed; }

    public void BoostInput(InputAction.CallbackContext ctx) { RawBoostInput = ctx.performed; }
    #endregion
}