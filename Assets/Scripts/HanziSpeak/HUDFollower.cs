using UnityEngine;

public class HudFollower : MonoBehaviour
{
    public Transform parentTransform;
    public Transform cameraTransform;
    public float followDistance = 2f;
    public float fixedY = 1.5f;
    public float smoothTime = 0.2f;

    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Get horizontal forward vector (ignore pitch)
        Vector3 flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;

        // Desired position
        Vector3 desiredPosition = cameraTransform.position + flatForward * followDistance;
        desiredPosition.y = fixedY;

        parentTransform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // Y-only LookAt toward camera (for horizontal rotation only)
        Vector3 lookAt = cameraTransform.position;
        lookAt.y = transform.position.y;

        transform.LookAt(lookAt);
        transform.Rotate(0, 180f, 0f); // Flip UI to face user
    }
}