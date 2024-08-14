using UnityEngine;

public class CameraController : MonoBehaviour
{
    [HideInInspector] public static CameraController Instance;

    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform cam;
    [SerializeField] private CameraPresetSO defaultCamSettings;

    [Header("Debug")]
    [SerializeField] private float moveSmoothing;
    [SerializeField] private float rotationSmoothing;
    [SerializeField] private float lookSmoothing;

    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 lookOffset;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadCamPreset(defaultCamSettings);
    }

    private void FixedUpdate()
    {
        cam.localPosition = Vector3.Lerp(cam.localPosition, offset, moveSmoothing * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, target.position, moveSmoothing * Time.deltaTime);

        transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, rotationSmoothing * Time.deltaTime);

        Vector3 relativeLookOffset = target.TransformDirection(-lookOffset);
        Quaternion targetRotation = Quaternion.LookRotation(target.position - (cam.position + relativeLookOffset));

        cam.rotation = Quaternion.Lerp(cam.rotation, targetRotation, lookSmoothing * Time.deltaTime);
    }

    public void LoadCamPreset(CameraPresetSO camPreset)
    {
        this.moveSmoothing = camPreset.moveSmoothing;
        this.rotationSmoothing = camPreset.rotationSmoothing;
        this.lookSmoothing = camPreset.lookSmoothing;
        this.offset = camPreset.offset;
        this.lookOffset = camPreset.lookOffset;
    }
}