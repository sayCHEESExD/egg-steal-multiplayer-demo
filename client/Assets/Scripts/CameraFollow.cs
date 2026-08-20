using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 8f;
    public float mouseSensitivity = 3f;
    public float heightOffset = 2f;
    
    private float yaw = 0f;
    private float pitch = 20f;

    private void Start()
    {
        // Locks the mouse cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Read mouse input
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -10f, 60f); // Prevent camera from flipping over

        // Calculate position based on rotation and distance
        Vector3 targetCenter = target.position + (Vector3.up * heightOffset);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        
        transform.position = targetCenter - (rotation * Vector3.forward * distance);
        transform.LookAt(targetCenter);
    }
}