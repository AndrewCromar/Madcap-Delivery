using UnityEngine;

public class CameraController : MonoBehaviour {
    [Header ("References")] 
    [SerializeField] private Transform target;
    [SerializeField] private Transform cam;

    [Header ("Settigs")] 
    [SerializeField] private float moveSmoothing; 
    [SerializeField] private float rotationSmoothing;
    [SerializeField] private float lookSmoothing; 

    [SerializeField] private Vector3 offset;

    private void Start(){
    }

    private void Update() {
        cam.localPosition = Vector3.Lerp(cam.localPosition, offset, moveSmoothing * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, target.position, moveSmoothing * Time.deltaTime);

        transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, rotationSmoothing * Time.deltaTime);

    }

    private void LateUpdate(){
        Quaternion targetRotation = Quaternion.LookRotation(target.position - cam.position);
        cam.rotation = Quaternion.Lerp(cam.rotation, targetRotation, lookSmoothing * Time.deltaTime);
    }
}