using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles knockback effects on characters
/// Attach to any character that can be knocked back
/// </summary>
public class KnockbackController : NetworkBehaviour
{
    private CharacterController characterController;
    private BaseCharacter character;
    
    private Vector3 knockbackVelocity;
    private float knockbackEndTime;
    private bool isKnockedBack = false;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        character = GetComponent<BaseCharacter>();
    }
    
    private void Update()
    {
        if (!IsServer) return;
        
        // Apply knockback movement
        if (isKnockedBack && Time.time < knockbackEndTime)
        {
            // Gradually reduce knockback
            float remainingTime = knockbackEndTime - Time.time;
            float t = remainingTime / 0.2f; // Normalize
            
            Vector3 movement = knockbackVelocity * t * Time.deltaTime;
            movement.y = -2f; // Gravity
            
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(movement);
            }
        }
        else if (isKnockedBack)
        {
            // Knockback finished
            isKnockedBack = false;
            knockbackVelocity = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Apply knockback to this character
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyKnockbackServerRpc(Vector3 direction, float force, float duration)
    {
        if (character != null && character.IsDead()) return;
        
        knockbackVelocity = direction.normalized * force;
        knockbackEndTime = Time.time + duration;
        isKnockedBack = true;
        
        // Sync to clients for visual feedback
        ApplyKnockbackClientRpc(direction, force, duration);
    }
    
    [ClientRpc]
    private void ApplyKnockbackClientRpc(Vector3 direction, float force, float duration)
    {
        // Visual feedback on clients (particles, camera shake, etc.)
        // The actual physics happens on server
    }
    
    /// <summary>
    /// Check if currently being knocked back
    /// </summary>
    public bool IsKnockedBack()
    {
        return isKnockedBack;
    }
    
    /// <summary>
    /// Cancel current knockback
    /// </summary>
    public void CancelKnockback()
    {
        isKnockedBack = false;
        knockbackVelocity = Vector3.zero;
    }
}