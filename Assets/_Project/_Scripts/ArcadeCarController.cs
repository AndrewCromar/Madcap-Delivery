using UnityEngine;
using UnityEngine.InputSystem;
using EditorAttributes;

public class ArcadeCarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform root;

    [Header("Settings")]
    [SerializeField] private float acceleration = 1000;
    [SerializeField] private float breakingPower = 5;
    [SerializeField] private float steerSpeed = 50;

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
    [SerializeField] private float driftAngle = 45;

    [Header("Debug")]

    [SerializeField] private bool reverse;

    [SerializeField] private bool isGrounded;
    [SerializeField] private RaycastHit groundCheckHit;

    [SerializeField] private float steerAngle;
    [SerializeField] private bool steeringRight;

    [SerializeField] private bool downhill;

    [SerializeField] private bool requestGearChange;
    [SerializeField] private bool lastGearInput;

    [SerializeField] private float rootYRotation;

    [Header("Inputs")]
    [SerializeField] private bool throttleInput;
    [SerializeField] private bool gearInput;
    [SerializeField] private bool breakInput;
    [SerializeField] private bool driftInput;
    [SerializeField] private float steerInput;

    private void Start()
    {
        rb.transform.parent = null;
    }

    private void Update()
    {
        CheckForRequests();

        CheckIsGrounded();

        AlignCarIfOnGround();
        CheckForDownhill();

        HandleGearChanges();
        HandleSteer();
        HandleThrottle();
        HandleBreak();

        ClampSpeed();

        UpdateGraphics();

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
        steerAngle += steerInput * steerSpeed * (reverse ? -1 : 1) * (throttleInput ? 1 : 0) * Time.deltaTime;

        if (steerInput > 0 && !reverse)
        {
            steeringRight = true;
        }
        else if (steerInput < 0 && !reverse)
        {
            steeringRight = false;
        }

        Vector3 currentRotation = transform.rotation.eulerAngles;
        currentRotation.y = steerAngle;
        transform.rotation = Quaternion.Euler(currentRotation);
    }

    // TODO:
    // - Remove "steeringRight" variable.

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
        rootYRotation = (driftInput ? driftAngle : 0) * steerInput;
        Debug.Log(rootYRotation);

        float smoothedYRotation = Mathf.Lerp(root.localRotation.eulerAngles.y, rootYRotation, rootRotationSmoothing * Time.deltaTime);
        Debug.Log(root.localRotation.eulerAngles.y);
        Debug.Log(smoothedYRotation);
        root.localRotation = Quaternion.Euler(0, smoothedYRotation, 0);
    }

    #region Inputs
    public void ThrottleInput(InputAction.CallbackContext ctx) { throttleInput = ctx.performed; }
    public void GearInput(InputAction.CallbackContext ctx) { gearInput = ctx.performed; }
    public void BreakInput(InputAction.CallbackContext ctx) { breakInput = ctx.performed; }
    public void DriftInput(InputAction.CallbackContext ctx) { driftInput = ctx.performed; }
    public void SteerInput(InputAction.CallbackContext ctx) { steerInput = ctx.ReadValue<float>(); }
    #endregion
}