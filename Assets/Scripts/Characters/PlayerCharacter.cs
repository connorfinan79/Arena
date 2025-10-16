using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Player-controlled character
/// Handles player input and movement
/// </summary>
public class PlayerCharacter : BaseCharacter
{
    [Header("Player Settings")]
    [SerializeField] private Camera playerCamera;
    
    private AutoAttack autoAttack;
    
    protected override void Awake()
    {
        base.Awake();
        autoAttack = GetComponent<AutoAttack>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            // Find and assign camera
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
            
            // Set up camera to follow this player
            PlayerCamera camScript = playerCamera?.GetComponent<PlayerCamera>();
            if (camScript != null)
            {
                camScript.SetTarget(transform);
            }
            
            // Initialize auto attack
            if (autoAttack != null)
            {
                autoAttack.Initialize(this, playerCamera);
            }
        }
        else
        {
            // Disable components for non-owner clients
            if (autoAttack != null)
                autoAttack.enabled = false;
        }
    }
    
    protected override void Update()
    {
        if (!IsOwner) return;
        
        // Movement is now handled by PathMovement component (click-to-move)
        // WASD movement removed for MOBA controls
        
        base.Update();
        HandleAbilityInput();
    }
    
    protected override void HandleMovement()
    {
        // Movement now handled by MOBAInputManager and PathMovement
        // This method can be left empty or removed
    }
    
    private void HandleAbilityInput()
    {
        if (abilityManager == null) return;
        
        // Q ability
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Vector3 cursorPos = GetCursorWorldPosition();
            abilityManager.UseAbility(0, cursorPos);
        }
        
        // W ability
        if (Input.GetKeyDown(KeyCode.W))
        {
            Vector3 cursorPos = GetCursorWorldPosition();
            abilityManager.UseAbility(1, cursorPos);
        }
        
        // E ability
        if (Input.GetKeyDown(KeyCode.E))
        {
            Vector3 cursorPos = GetCursorWorldPosition();
            abilityManager.UseAbility(2, cursorPos);
        }
        
        // R ability (Ultimate)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Vector3 cursorPos = GetCursorWorldPosition();
            abilityManager.UseAbility(3, cursorPos);
        }
    }
    
    private Vector3 GetCursorWorldPosition()
    {
        if (playerCamera == null) return transform.position;
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        return transform.position;
    }
    
    protected override void OnDeath(ulong killerId)
    {
        base.OnDeath(killerId);
        
        // Disable controls
        if (IsOwner)
        {
            if (autoAttack != null)
                autoAttack.enabled = false;
        }
        
        // Play death animation or effects here
        // After delay, respawn or return to menu
    }
    
    /// <summary>
    /// Respawn the player
    /// </summary>
    [ServerRpc]
    public void RespawnServerRpc()
    {
        isDead = false;
        currentHealth = maxHealth;
        networkHealth.Value = currentHealth;
        
        // Reset position (customize as needed)
        transform.position = Vector3.zero;
        
        // Re-enable controls
        RespawnClientRpc();
    }
    
    [ClientRpc]
    private void RespawnClientRpc()
    {
        if (IsOwner && autoAttack != null)
        {
            autoAttack.enabled = true;
        }
    }
}