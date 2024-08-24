using UnityEngine;
using UnityEngine.InputSystem;
using ONYX;

public class CarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody CarRigidbody;
    [SerializeField] private LayerMask CarLayer;

    [Header("Suspension Setting")]
    [SerializeField] private float Suspension_RestDistance = 0.6f;
    [SerializeField] private float Suspension_Strength = 500;
    [SerializeField] private float Suspension_Damping = 50;

    [Header("Steer Setting")]
    [SerializeField] private float Steer_MaxAngle = 30;
    [SerializeField] private float Steer_Smoothing = 20;

    [Header("Throttle Setting")]
    [SerializeField] private float RollDamping = 10;

    [Header("Wheel References")]
    [SerializeField] private WheelData[] AllWheelData;

    [Header("Input Debug")]
    [SerializeField, ReadOnly] private float RawThrottleInput;
    [SerializeField, ReadOnly] private float RawSteerInput;
    [SerializeField, ReadOnly] private bool RawDriftInput;

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

    private void Update()
    {
        foreach (WheelData wheelData in AllWheelData)
        {
            // Set wheel grip.
            SetWheelActiveGrip(wheelData);

            // Wheel Ground Data
            CheckWheelGround(wheelData);

            // Suspension
            SuspensionForWheel(wheelData);

            // Steering
            SteeringForWheel(wheelData);

            // Wheel Graphics
            UpdateWheelGraphics(wheelData);
        }
    }

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


            float rollForce = 0;
            if (RawThrottleInput != 0)
            {
                rollForce = wheelData.Acceleration * RawThrottleInput;
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
        if(wheelData.IsDriveWheel && RawDriftInput) { // Drifting.
            Transform roll = root.Find("Roll");
            float driftRollSpeed = 10;

            roll.localRotation = roll.localRotation * Quaternion.Euler(driftRollSpeed * wheelData.GraphicRotationMultiplier * Time.deltaTime, 0, 0);
        } else {
            Transform roll = root.Find("Roll");
            Vector3 wheelVelocity = CarRigidbody.GetPointVelocity(wheelData.Wheel.position);
            float rollVelocityDot = Vector3.Dot(wheelVelocity, wheelData.Wheel.forward);

            roll.localRotation = roll.localRotation * Quaternion.Euler(rollVelocityDot * wheelData.GraphicRotationMultiplier * Time.deltaTime, 0, 0);
        }
    }

    public void ThrottleInput(InputAction.CallbackContext ctx) { RawThrottleInput = ctx.ReadValue<float>(); }
    public void SteerInput(InputAction.CallbackContext ctx) { RawSteerInput = ctx.ReadValue<float>(); }
    public void DriftInput(InputAction.CallbackContext ctx) { RawDriftInput = ctx.performed; }
}