using UnityEngine;

/// <summary>
/// Top-down MOBA-style camera that follows the player
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 15, -10);
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float rotationX = 45f;
    
    [Header("Camera Bounds (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds = new Vector2(-50, -50);
    [SerializeField] private Vector2 maxBounds = new Vector2(50, 50);
    
    [Header("Zoom Settings")]
    [SerializeField] private bool allowZoom = true;
    [SerializeField] private float minZoom = 10f;
    [SerializeField] private float maxZoom = 25f;
    [SerializeField] private float zoomSpeed = 2f;
    
    private Transform target;
    private float currentZoom;
    
    private void Start()
    {
        // Set initial rotation
        transform.rotation = Quaternion.Euler(rotationX, 0, 0);
        currentZoom = offset.y;
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        // Handle zoom
        if (allowZoom)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                currentZoom -= scroll * zoomSpeed;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }
        }
        
        // Calculate desired position
        Vector3 adjustedOffset = offset;
        adjustedOffset.y = currentZoom;
        adjustedOffset.z = -currentZoom * 0.7f; // Maintain angle
        
        Vector3 desiredPosition = target.position + adjustedOffset;
        
        // Apply bounds if enabled
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minBounds.y, maxBounds.y);
        }
        
        // Smoothly move camera
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}