using UnityEngine;

[CreateAssetMenu(fileName = "CameraPreset", menuName = "CameraSettings/CameraPreset", order = 1)]
public class CameraPresetSO : ScriptableObject
{
    public float moveSmoothing = 5;
    public float rotationSmoothing = 5;
    public float lookSmoothing = 5;

    public Vector3 offset = new Vector3(0, 2.5f, -7.5f);
    public Vector3 lookOffset = new Vector3(0, 0, 0);
}