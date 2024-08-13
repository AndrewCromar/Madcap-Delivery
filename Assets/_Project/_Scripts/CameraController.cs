using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform cam;

    [Header("Settigs")]
    [SerializeField] private float moveSmoothing = 5;
    [SerializeField] private float rotationSmoothing = 5;
    [SerializeField] private float lookSmoothing = 5;

    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, -7.5f);

    private void FixedUpdate()
    {
        cam.localPosition = Vector3.Lerp(cam.localPosition, offset, moveSmoothing * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, target.position, moveSmoothing * Time.deltaTime);

        transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, rotationSmoothing * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(target.position - cam.position);
        cam.rotation = Quaternion.Lerp(cam.rotation, targetRotation, lookSmoothing * Time.deltaTime);
    }
}