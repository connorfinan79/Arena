using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles click-to-move pathfinding using Unity's NavMesh
/// Uses character stats for movement speed
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PathMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float stoppingDistance = 0.1f;
    
    [Header("Pathfinding")]
    [SerializeField] private bool useNavMesh = false; // Default to false since NavMesh requires package
    
    private CharacterController characterController;
    private BaseCharacter character;
    private CharacterAnimationController animController;
    
    private Vector3 targetPosition;
    private bool isMoving = false;
    private NavMeshPath navPath;
    private int currentPathIndex = 0;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        character = GetComponent<BaseCharacter>();
        animController = GetComponent<CharacterAnimationController>();
        
        if (useNavMesh)
        {
            navPath = new NavMeshPath();
        }
    }
    
    private void Update()
    {
        if (!IsOwner) return;
        
        if (isMoving)
        {
            UpdateMovement();
        }
    }
    
    /// <summary>
    /// Move to a target position
    /// </summary>
    public void MoveToPosition(Vector3 position)
    {
        targetPosition = position;
        isMoving = true;
        currentPathIndex = 0;
        
        Debug.Log($"PathMovement.MoveToPosition: Moving to {position}, UseNavMesh: {useNavMesh}");
        
        if (useNavMesh && navPath != null)
        {
            // Calculate NavMesh path
            bool hasPath = NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, navPath);
            
            Debug.Log($"NavMesh.CalculatePath: {hasPath}, Status: {navPath.status}, Corners: {navPath.corners.Length}");
            
            if (navPath.status == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning("Invalid NavMesh path calculated - falling back to direct movement");
                // Don't stop moving, just use direct path instead
            }
        }
    }
    
    /// <summary>
    /// Stop all movement
    /// </summary>
    public void StopMovement()
    {
        isMoving = false;
        currentPathIndex = 0;
    }
    
    private void UpdateMovement()
    {
        if (character != null && character.IsDead())
        {
            StopMovement();
            return;
        }
        
        Vector3 destination;
        
        if (useNavMesh && navPath != null && navPath.corners.Length > 0)
        {
            // Follow NavMesh path
            if (currentPathIndex >= navPath.corners.Length)
            {
                StopMovement();
                return;
            }
            
            destination = navPath.corners[currentPathIndex];
        }
        else
        {
            // Direct movement (no pathfinding)
            destination = targetPosition;
        }
        
        // Calculate direction
        Vector3 direction = (destination - transform.position);
        direction.y = 0; // Keep on ground plane
        float distance = direction.magnitude;
        
        // Check if reached waypoint
        if (distance < stoppingDistance)
        {
            if (useNavMesh && navPath != null && currentPathIndex < navPath.corners.Length - 1)
            {
                // Move to next waypoint
                currentPathIndex++;
            }
            else
            {
                // Reached final destination
                StopMovement();
            }
            return;
        }
        
        direction.Normalize();
        
        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Get movement speed from character stats (not local variable)
        float moveSpeed = character != null ? character.GetMoveSpeed() : 5f;
        
        // Move character
        Vector3 movement = direction * moveSpeed * Time.deltaTime;
        movement.y = -2f; // Gravity
        
        if (characterController != null && characterController.enabled)
        {
            characterController.Move(movement);
        }
        
        // Update animation controller if needed
        if (animController != null)
        {
            animController.SetMovementDirection(direction);
        }
    }
    
    /// <summary>
    /// Check if currently moving
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
    
    /// <summary>
    /// Get current target position
    /// </summary>
    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }
    
    /// <summary>
    /// Visualize path in editor
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!isMoving) return;
        
        if (useNavMesh && navPath != null && navPath.corners.Length > 0)
        {
            // Draw NavMesh path
            Gizmos.color = Color.cyan;
            for (int i = 0; i < navPath.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(navPath.corners[i], navPath.corners[i + 1]);
                Gizmos.DrawSphere(navPath.corners[i], 0.1f);
            }
        }
        else
        {
            // Draw direct path
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        
        // Draw target position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(targetPosition, 0.3f);
    }
}