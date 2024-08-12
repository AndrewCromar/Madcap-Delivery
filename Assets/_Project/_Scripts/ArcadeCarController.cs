using UnityEngine;
using UnityEngine.InputSystem;
using EditorAttributes;

public class ArcadeCarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Settings")]
    [SerializeField] private float acceleration = 1000;
    [SerializeField] private float breakingPower = 5;
    [SerializeField] private float steerSpeed = 50;

    [SerializeField] private float maxSpeed = 20;
    [SerializeField] private float downhillMaxSpeed = 100;
    [SerializeField] private float speedClampSmoothing = 10;

    [SerializeField] private float groundCheckRayDistance = 1;
    [SerializeField] private float groundAlignSmoothing = 10;
    [SerializeField] private LayerMask sphereLayer;

    [Header("Debug")]
    [SerializeField, VerticalGroup(nameof(field01), nameof(field02), nameof(field03), nameof(field04))]
    private Void groupHolder;
    
    [SerializeField] private bool reverse;

    [SerializeField] private bool isGrounded;
    [SerializeField] private RaycastHit groundCheckHit;

    [SerializeField] private float steerAngle;

    [SerializeField] private bool downhill;

    [SerializeField] private bool requestGearChange;
    [SerializeField] private bool lastGearInput;

    [Header("Inputs")]
    [SerializeField] private bool throttleInput;
    [SerializeField] private bool gearInput;
    [SerializeField] private bool breakInput;
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
        steerAngle += steerInput * steerSpeed * (reverse ? -1 : 1) * Time.deltaTime;

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

    public void ThrottleInput(InputAction.CallbackContext ctx) { throttleInput = ctx.performed; }
    public void GearInput(InputAction.CallbackContext ctx) { gearInput = ctx.performed; }
    public void BreakInput(InputAction.CallbackContext ctx) { breakInput = ctx.performed; }
    public void SteerInput(InputAction.CallbackContext ctx) { steerInput = ctx.ReadValue<float>(); }
}