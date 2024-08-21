using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadeCameraController : MonoBehaviour
{
    [HideInInspector] public static ArcadeCameraController Instance;

    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform cam;
    [SerializeField] private CameraPresetSO defaultCamSettings;

    [Header ("Inputs")]
    [SerializeField] private float steerInput;

    [Header("Debug")]
    [SerializeField] private float moveSmoothing;
    [SerializeField] private float rotationSmoothing;
    [SerializeField] private float lookSmoothing;
    [SerializeField] private float cameraTilt;

    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 lookOffset;

    [SerializeField] private ArcadeCarController arcadeCarController;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadCamPreset(defaultCamSettings);
        arcadeCarController = ArcadeCarController.Instance;
    }

    private void FixedUpdate()
    {
        cam.localPosition = Vector3.Lerp(cam.localPosition, offset, moveSmoothing * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, target.position, moveSmoothing * Time.deltaTime);

        transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, rotationSmoothing * Time.deltaTime);

        Vector3 relativeLookOffset = target.TransformDirection(-lookOffset);
        Quaternion targetRotation = Quaternion.LookRotation(target.position - (cam.position + relativeLookOffset));

        Vector3 cameraTiltAmount = new Vector3(0, 0, -cameraTilt * steerInput * Mathf.Clamp(arcadeCarController.rb.velocity.magnitude, 0, 1));

        cam.rotation = Quaternion.Lerp(cam.rotation, targetRotation * Quaternion.Euler(cameraTiltAmount), lookSmoothing * Time.deltaTime);
    }

    public void LoadCamPreset(CameraPresetSO camPreset)
    {
        this.moveSmoothing = camPreset.moveSmoothing;
        this.rotationSmoothing = camPreset.rotationSmoothing;
        this.lookSmoothing = camPreset.lookSmoothing;
        this.cameraTilt = camPreset.cameraTilt;
        this.offset = camPreset.offset;
        this.lookOffset = camPreset.lookOffset;
    }

    public void SteerInput(InputAction.CallbackContext ctx) { steerInput = ctx.ReadValue<float>(); }
}