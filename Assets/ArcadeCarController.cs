using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeCarController : MonoBehaviour {
    [Header ("References")]
    [SerializeField] private Rigidbody rb;

    [Header ("Settings")]
    [SerializeField] private float breakingPower = 5;

    [Header ("Debug")]
    [SerializeField] private bool reverse;

    [SerializeField] private bool requestGearChange;
    [SerializeField] private bool lastGearInput;

    [Header ("Inputs")]
    [SerializeField] private bool throttleInput;
    [SerializeField] private bool gearInput;
    [SerializeField] private bool breakInput;
    [SerializeField] private float steerInput;

    private void Start(){
        rb.transform.parent = null;
    }

    private void Update(){
        CheckForRequests();

        HandleGearChanges();
        HandleThrottle();
        HandleBreak();

        transform.position = rb.transform.position;
    }

    private void CheckForRequests(){
        if(gearInput && !lastGearInput) requestGearChange = true;
        lastGearInput = gearInput;
    }

    private void HandleGearChanges(){
        if(requestGearChange){
            requestGearChange = false;
            reverse = !reverse;
        }
    }

    private void HandleThrottle(){
        if(breakInput) return;

        rb.AddForce(transform.forward * (throttleInput ? 1 : 0) * (reverse ? -1 : 1));
    }

    private void HandleBreak(){
        if(!breakInput) return;

        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, Mathf.Lerp(rb.velocity.z, 0, breakingPower * Time.deltaTime));
    }

    public void ThrottleInput(InputAction.CallbackContext ctx){ throttleInput = ctx.performed; }
    public void GearInput(InputAction.CallbackContext ctx){ gearInput = ctx.performed; }
    public void BreakInput(InputAction.CallbackContext ctx){ breakInput = ctx.performed; }
    public void SteerInput(InputAction.CallbackContext ctx){ steerInput = ctx.ReadValue<float>(); }
}