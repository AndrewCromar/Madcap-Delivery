using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform TargetReference;
    [SerializeField] private Transform CameraReference;

    [Header("Position Settings")]
    [SerializeField] private Vector3 offset;

    [Header("Smoothing Settings")]
    [SerializeField] private float SmoothingMove = 2.5f;
    [SerializeField] private float SmoothingLook = 5;

    private void FixedUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, TargetReference.position, SmoothingMove * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, TargetReference.rotation, SmoothingMove * Time.deltaTime);

        Vector3 desiredPosition = TargetReference.TransformPoint(offset);

        CameraReference.position = Vector3.Lerp(CameraReference.position, desiredPosition, SmoothingMove * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(TargetReference.position - CameraReference.position);
        CameraReference.rotation = Quaternion.Slerp(CameraReference.rotation, targetRotation, SmoothingLook * Time.deltaTime);
    }
}
