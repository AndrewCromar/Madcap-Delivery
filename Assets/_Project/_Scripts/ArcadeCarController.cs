using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeCarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform root;
    [SerializeField] private TrailRenderer[] driftTrails;
    [SerializeField] private CameraPresetSO camSettings;
    [SerializeField] private CameraPresetSO driftCamSettings;

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

    [Header("Debug")]
    [SerializeField] private CameraController cameraController;

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

    [Header("Inputs")]
    [SerializeField] private bool throttleInput;
    [SerializeField] private bool gearInput;
    [SerializeField] private bool breakInput;
    [SerializeField] private bool driftInput;
    [SerializeField] private float steerInput;

    private void Start()
    {
        rb.transform.parent = null;
        cameraController = CameraController.Instance;
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
        isDrifting = driftInput && steerInput != 0;

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

        Vector3 currentRotation = transform.rotation.eulerAngles;
        currentRotation.y = steerAngle;
        transform.rotation = Quaternion.Euler(currentRotation);
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
        rootYRotation = (isDrifting ? driftAngle : 0) * steerInput;
        Quaternion targetRotation = Quaternion.Euler(0, rootYRotation, 0);

        root.localRotation = Quaternion.Slerp(root.localRotation, targetRotation, rootRotationSmoothing * Time.deltaTime);

        foreach (TrailRenderer driftTrail in driftTrails) driftTrail.emitting = isDrifting;
    }

    private void UpdateCameraSettings()
    {
        if (isDrifting || timeOfNotDrifting <= timeTillDisableDriftCam)
        {
            cameraController.LoadCamPreset(driftCamSettings);
        }
        else
        {
            cameraController.LoadCamPreset(camSettings);
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