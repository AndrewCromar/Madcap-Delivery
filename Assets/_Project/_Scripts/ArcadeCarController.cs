using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeCarController : MonoBehaviour
{
    [HideInInspector] public static ArcadeCarController Instance;

    [Header("References")]
    [SerializeField] public Rigidbody rb;
    [SerializeField] private Transform root;
    [SerializeField] private TrailRenderer leftDriftTrail;
    [SerializeField] private TrailRenderer rightDriftTrail;
    [SerializeField] private CameraPresetSO camSettings;
    [SerializeField] private CameraPresetSO driftCamSettings;
    [SerializeField] private CameraPresetSO reverseCamSettings;
    [SerializeField] private Transform leftTiltPoint;
    [SerializeField] private Transform rightTiltPoint;
    [SerializeField] private Transform leftWheel;
    [SerializeField] private Transform rightWheel;

    [Header("Settings")]
    [SerializeField] private float acceleration = 1000;
    [SerializeField] private float breakingPower = 5;
    [SerializeField] private float steerSpeed = 50;
    [SerializeField] private float driftSteerSpeed = 100;

    [Space]

    [SerializeField] private float maxSpeed = 20;
    [SerializeField] private float downhillMaxSpeed = 100;
    [SerializeField] private float speedClampSmoothing = 10;

    [Space]

    [SerializeField] private float groundCheckRayDistance = 1;
    [SerializeField] private float groundAlignSmoothing = 10;
    [SerializeField] private LayerMask sphereLayer;

    [Space]

    [SerializeField] private float rootRotationSmoothing = 10;
    [SerializeField] private float driftAngle = 15;
    [SerializeField] private float timeTillDisableDriftCam = 0.15f;

    [Space]

    [SerializeField] private float tiltSmoothing = 10;
    [SerializeField] private float maxTilt = 10;
    [SerializeField] private float driftMaxTilt = 20;

    [Space]

    [SerializeField] private float maxWheelSteer = 45;
    [SerializeField] private float wheelTurnSmoothing = 5;

    [Header("Debug")]
    [SerializeField] private ArcadeCameraController arcadeCameraController;

    [SerializeField] private bool reverse;

    [SerializeField] private bool isGrounded;
    [SerializeField] private RaycastHit groundCheckHit;

    [SerializeField] private float steerAngle;

    [SerializeField] private bool downhill;

    [SerializeField] private bool requestGearChange;
    [SerializeField] private bool lastGearInput;

    [SerializeField] private float rootYRotation;

    [SerializeField] private float actingSteerSpeed;

    [SerializeField] private bool isDrifting;
    [SerializeField] private float timeOfNotDrifting;

    [SerializeField] private float tiltAmount;

    [Header("Inputs")]
    [SerializeField] private bool throttleInput;
    [SerializeField] private bool gearInput;
    [SerializeField] private bool breakInput;
    [SerializeField] private bool driftInput;
    [SerializeField] private float steerInput;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        rb.transform.parent = null;
        arcadeCameraController = ArcadeCameraController.Instance;
    }

    private void Update()
    {
        CheckForRequests();

        HandleDrifting();

        CheckIsGrounded();
        CheckForSteerSpeed();

        AlignCarIfOnGround();
        CheckForDownhill();

        HandleGearChanges();
        HandleSteer();
        HandleThrottle();
        HandleBreak();

        ClampSpeed();

        UpdateGraphics();
        UpdateCameraSettings();

        transform.position = rb.transform.position;
    }

    private void CheckIsGrounded() { isGrounded = Physics.Raycast(new Ray(transform.position, Vector3.down), out groundCheckHit, groundCheckRayDistance, ~sphereLayer); }

    private void AlignCarIfOnGround()
    {
        if (isGrounded)
        {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, groundCheckHit.normal) * transform.rotation;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, groundAlignSmoothing * Time.deltaTime);
        }
    }

    private void HandleDrifting()
    {
        isDrifting = driftInput && steerInput != 0 && !reverse;

        if (!isDrifting) timeOfNotDrifting += Time.deltaTime;
        else timeOfNotDrifting = 0;
    }

    private void CheckForSteerSpeed()
    {
        actingSteerSpeed = isDrifting ? driftSteerSpeed : steerSpeed;
    }

    private void CheckForDownhill()
    {
        float dotProduct = Vector3.Dot(transform.forward, Vector3.down);

        downhill = dotProduct > 0.05;
    }

    private void CheckForRequests()
    {
        if (gearInput && !lastGearInput) requestGearChange = true;
        lastGearInput = gearInput;
    }

    private void HandleGearChanges()
    {
        if (requestGearChange)
        {
            requestGearChange = false;
            reverse = !reverse;
        }
    }

    private void HandleSteer()
    {
        steerAngle += steerInput * actingSteerSpeed * (reverse ? -1 : 1) * (throttleInput ? 1 : 0) * Time.deltaTime;

        Vector3 currentRotation = transform.localRotation.eulerAngles;
        currentRotation.y = steerAngle;
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    private void HandleThrottle()
    {
        if (breakInput) return;
        if (!isGrounded) return;

        rb.AddForce(transform.forward * (throttleInput ? 1 : 0) * (reverse ? -1 : 1) * acceleration * Time.deltaTime);
    }

    private void HandleBreak()
    {
        if (!breakInput) return;

        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, Mathf.Lerp(rb.velocity.z, 0, breakingPower * Time.deltaTime));
    }

    private void ClampSpeed()
    {
        float currentMaxSpeed = downhill ? downhillMaxSpeed : maxSpeed;

        Vector3 velocity = rb.velocity;
        float currentSpeed = velocity.magnitude;

        if (currentSpeed > currentMaxSpeed)
        {
            Vector3 targetVelocity = velocity.normalized * currentMaxSpeed;

            rb.velocity = Vector3.Lerp(velocity, targetVelocity, speedClampSmoothing * Time.deltaTime);
        }
    }

    private void UpdateGraphics()
    {
        // Drift angle
        rootYRotation = (isDrifting ? driftAngle : 0) * steerInput;
        Quaternion targetRotation = Quaternion.Euler(0, rootYRotation, 0);

        root.localRotation = Quaternion.Slerp(root.localRotation, targetRotation, rootRotationSmoothing * Time.deltaTime);

        // Car tilt
        float newTiltAmount = (isDrifting ? driftMaxTilt : maxTilt) * steerInput;
        tiltAmount = Mathf.Lerp(tiltAmount, newTiltAmount + ((2.5f * Mathf.Sin(10 * Time.time) + 1) * steerInput * (isDrifting ? 1 : 0)), tiltSmoothing * Time.deltaTime);

        if (tiltAmount < 0)
        {
            leftTiltPoint.localRotation = Quaternion.Euler(0, 0, 0);
            rightTiltPoint.localRotation = Quaternion.Euler(0, 0, tiltAmount);
        }
        else if (tiltAmount > 0)
        {
            leftTiltPoint.localRotation = Quaternion.Euler(0, 0, tiltAmount);
            rightTiltPoint.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            leftTiltPoint.localRotation = Quaternion.Euler(0, 0, 0);
            rightTiltPoint.localRotation = Quaternion.Euler(0, 0, 0);
        }

        // Drift trail
        leftDriftTrail.emitting = isDrifting && tiltAmount > 0;
        rightDriftTrail.emitting = isDrifting && tiltAmount < 0;

        // Turn wheels
        float wheelAngle = maxWheelSteer * steerInput;
        leftWheel.localRotation = Quaternion.Lerp(leftWheel.localRotation, Quaternion.Euler(0, wheelAngle, 0), wheelTurnSmoothing * Time.deltaTime);
        rightWheel.localRotation = Quaternion.Lerp(rightWheel.localRotation, Quaternion.Euler(0, wheelAngle, 0), wheelTurnSmoothing * Time.deltaTime);
    }

    private void UpdateCameraSettings()
    {
        if (reverse)
        {
            arcadeCameraController.LoadCamPreset(reverseCamSettings);
        }
        else if (isDrifting || timeOfNotDrifting <= timeTillDisableDriftCam)
        {
            arcadeCameraController.LoadCamPreset(driftCamSettings);
        }
        else
        {
            arcadeCameraController.LoadCamPreset(camSettings);
        }
    }

    #region Inputs
    public void ThrottleInput(InputAction.CallbackContext ctx) { throttleInput = ctx.performed; }
    public void GearInput(InputAction.CallbackContext ctx) { gearInput = ctx.performed; }
    public void BreakInput(InputAction.CallbackContext ctx) { breakInput = ctx.performed; }
    public void DriftInput(InputAction.CallbackContext ctx) { driftInput = ctx.performed; }
    public void SteerInput(InputAction.CallbackContext ctx) { steerInput = ctx.ReadValue<float>(); }
    #endregion
}