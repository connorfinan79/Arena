using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles MOBA-style input (right-click movement and targeting)
/// Similar to League of Legends control scheme
/// </summary>
public class MOBAInputManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerCharacter playerCharacter;
    
    [Header("Movement Indicator")]
    [SerializeField] private GameObject moveIndicatorPrefab;
    [SerializeField] private float indicatorLifetime = 0.5f;
    
    [Header("Targeting")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float clickRadius = 0.5f;
    
    [Header("Attack Settings")]
    [SerializeField] private bool attackMoveEnabled = true;
    [SerializeField] private float attackMoveRange = 10f;
    
    private TargetingSystem targetingSystem;
    private PathMovement pathMovement;
    private AutoAttack autoAttack;
    
    // Current target
    private BaseCharacter currentTarget;
    private bool isFollowingTarget = false;
    
    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (playerCharacter == null)
        {
            playerCharacter = GetComponent<PlayerCharacter>();
        }
        
        targetingSystem = GetComponent<TargetingSystem>();
        pathMovement = GetComponent<PathMovement>();
        autoAttack = GetComponent<AutoAttack>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
    }
    
    private void Update()
    {
        if (!IsOwner || playerCharacter == null || playerCharacter.IsDead()) return;
        
        HandleInput();
        UpdateTargetFollow();
    }
    
    private void HandleInput()
    {
        // Right-click for movement and targeting
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            HandleRightClick();
        }
        
        // Left-click for abilities (kept for ability system)
        // Q, W, E, R keys still work for abilities
        
        // Stop command (S key or similar)
        if (Input.GetKeyDown(KeyCode.S))
        {
            StopMovement();
        }
        
        // Attack-move (Shift + Right Click or A + Left Click)
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(1))
        {
            HandleAttackMove();
        }
    }
    
    private void HandleRightClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        // Check for enemy targets first (higher priority)
        if (Physics.SphereCast(ray, clickRadius, out RaycastHit enemyHit, 100f, enemyLayer))
        {
            Debug.Log($"Hit enemy: {enemyHit.collider.gameObject.name}, layer: {LayerMask.LayerToName(enemyHit.collider.gameObject.layer)}");
            
            BaseCharacter enemy = enemyHit.collider.GetComponent<BaseCharacter>();
            if (enemy != null && !enemy.IsDead())
            {
                // Target enemy
                SetTarget(enemy);
                return;
            }
            else
            {
                Debug.LogWarning($"Hit enemy layer but no BaseCharacter or is dead");
            }
        }
        
        // Check for ground (movement)
        if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundLayer))
        {
            Debug.Log($"Hit ground at position: {groundHit.point}");
            // Move to position
            MoveToPosition(groundHit.point);
        }
        else
        {
            Debug.LogWarning("Right-click hit nothing!");
        }
    }
    
    private void HandleAttackMove()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            // Move to position and attack anything in range
            if (pathMovement != null)
            {
                pathMovement.MoveToPosition(hit.point);
            }
            
            // Enable attack-move mode
            if (autoAttack != null)
            {
                autoAttack.SetAttackMoveMode(true, attackMoveRange);
            }
            
            ClearTarget();
            SpawnMoveIndicator(hit.point);
        }
    }
    
    private void SetTarget(BaseCharacter target)
    {
        // Clear previous target first
        if (currentTarget != null && targetingSystem != null)
        {
            targetingSystem.ClearTarget();
        }
        
        // Clear previous attack target
        if (autoAttack != null)
        {
            autoAttack.ClearManualTarget();
        }
        
        // Set new target
        currentTarget = target;
        isFollowingTarget = true;
        
        // Show target indicator
        if (targetingSystem != null)
        {
            targetingSystem.SetTarget(target);
        }
        
        // Disable attack-move mode
        if (autoAttack != null)
        {
            autoAttack.SetAttackMoveMode(false, 0);
        }
        
        // Immediately start moving towards target if out of range
        float attackRange = autoAttack != null ? autoAttack.GetAttackConfig().attackRange : 5f;
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        
        if (distanceToTarget > attackRange * 0.9f)
        {
            // Out of range - path to target
            if (pathMovement != null)
            {
                pathMovement.MoveToPosition(target.transform.position);
                Debug.Log($"Pathing to target: {target.gameObject.name}, distance: {distanceToTarget}, range: {attackRange}");
            }
        }
        else
        {
            // In range - attack immediately
            if (autoAttack != null)
            {
                autoAttack.SetManualTarget(target);
                Debug.Log($"In range, attacking: {target.gameObject.name}");
            }
        }
        
        Debug.Log($"Targeted: {target.gameObject.name}");
    }
    
    private void MoveToPosition(Vector3 position)
    {
        ClearTarget();
        
        if (pathMovement != null)
        {
            pathMovement.MoveToPosition(position);
        }
        
        SpawnMoveIndicator(position);
        
        // Disable attack-move mode
        if (autoAttack != null)
        {
            autoAttack.SetAttackMoveMode(false, 0);
        }
    }
    
    private void UpdateTargetFollow()
    {
        if (!isFollowingTarget || currentTarget == null) return;
        
        // Check if target is dead
        if (currentTarget.IsDead())
        {
            ClearTarget();
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        float attackRange = autoAttack != null ? autoAttack.GetAttackConfig().attackRange : 5f;
        
        // Always update pathfinding to follow moving targets
        if (distanceToTarget > attackRange * 0.9f) // 90% of range to avoid jitter
        {
            // Out of range - move closer
            if (pathMovement != null)
            {
                pathMovement.MoveToPosition(currentTarget.transform.position);
            }
            
            // Clear manual target while moving (will re-set when in range)
            if (autoAttack != null)
            {
                autoAttack.ClearManualTarget();
            }
        }
        else
        {
            // In range - stop moving and attack
            if (pathMovement != null)
            {
                pathMovement.StopMovement();
            }
            
            // Set manual target for auto-attack
            if (autoAttack != null)
            {
                autoAttack.SetManualTarget(currentTarget);
            }
        }
    }
    
    private void StopMovement()
    {
        if (pathMovement != null)
        {
            pathMovement.StopMovement();
        }
        
        ClearTarget();
        
        if (autoAttack != null)
        {
            autoAttack.SetAttackMoveMode(false, 0);
        }
    }
    
    private void ClearTarget()
    {
        if (currentTarget != null && targetingSystem != null)
        {
            targetingSystem.ClearTarget();
        }
        
        currentTarget = null;
        isFollowingTarget = false;
        
        if (autoAttack != null)
        {
            autoAttack.ClearManualTarget();
        }
    }
    
    private void SpawnMoveIndicator(Vector3 position)
    {
        if (moveIndicatorPrefab == null) return;
        
        // Spawn indicator slightly above ground
        Vector3 spawnPos = position + Vector3.up * 0.1f;
        GameObject indicator = Instantiate(moveIndicatorPrefab, spawnPos, Quaternion.identity);
        
        // Destroy after lifetime
        Destroy(indicator, indicatorLifetime);
    }
    
    /// <summary>
    /// Get current target
    /// </summary>
    public BaseCharacter GetCurrentTarget()
    {
        return currentTarget;
    }
    
    /// <summary>
    /// Check if following a target
    /// </summary>
    public bool IsFollowingTarget()
    {
        return isFollowingTarget;
    }
}