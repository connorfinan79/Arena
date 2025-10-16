using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles automatic attacking with support for melee and ranged
/// Includes knockback support, sound effects, and MOBA-style manual targeting
/// </summary>
public class AutoAttack : NetworkBehaviour
{
    [Header("Attack Configuration")]
    [SerializeField] private AutoAttackConfig attackConfig;
    
    [Header("Spawn Point")]
    [SerializeField] private Transform projectileSpawnPoint;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackAnimationTrigger = "Attack";
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    
    private BaseCharacter character;
    private Camera playerCamera;
    private CharacterAnimationController animController;
    private float lastAttackTime;
    private bool isAutoTargeting = false;
    
    // Manual targeting (for MOBA controls)
    private BaseCharacter manualTarget;
    private bool hasManualTarget = false;
    
    // Attack-move mode
    private bool attackMoveMode = false;
    private float attackMoveRange = 10f;
    
    public void Initialize(BaseCharacter ownerChar, Camera cam)
    {
        character = ownerChar;
        playerCamera = cam;
        animController = character.GetComponent<CharacterAnimationController>();
        
        // Create spawn point if none exists
        if (projectileSpawnPoint == null)
        {
            GameObject spawnPoint = new GameObject("AttackSpawnPoint");
            spawnPoint.transform.SetParent(transform);
            spawnPoint.transform.localPosition = new Vector3(0, 1.5f, 0.5f);
            projectileSpawnPoint = spawnPoint.transform;
        }
        
        // Setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.maxDistance = 20f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }
        
        // Create default config if none assigned
        if (attackConfig == null)
        {
            attackConfig = new AutoAttackConfig();
        }
    }
    
    public void SetAutoTarget(bool enabled)
    {
        isAutoTargeting = enabled;
    }
    
    private void Update()
    {
        if (!IsOwner && !IsServer) return;
        if (character == null || character.IsDead()) return;
        
        // Check if we can attack
        float attackInterval = 1f / character.GetAttackSpeed();
        if (Time.time < lastAttackTime + attackInterval) return;
        
        // Priority 1: Manual target (right-click targeting)
        if (IsOwner && hasManualTarget && manualTarget != null)
        {
            PerformManualTargetAttack();
        }
        // Priority 2: Attack-move mode
        else if (IsOwner && attackMoveMode)
        {
            PerformAttackMoveAttack();
        }
        // Priority 3: AI auto-targeting (enemies only)
        else if (IsServer && isAutoTargeting)
        {
            PerformEnemyAutoAttack();
        }
    }
    
    private void PerformManualTargetAttack()
    {
        if (manualTarget == null || manualTarget.IsDead())
        {
            ClearManualTarget();
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, manualTarget.transform.position);
        
        // Check if in range
        if (distanceToTarget <= attackConfig.attackRange)
        {
            Vector3 direction = (manualTarget.transform.position - transform.position).normalized;
            
            if (attackConfig.attackType == AttackType.Melee)
            {
                AttackServerRpc(manualTarget.NetworkObjectId, manualTarget.transform.position, direction);
            }
            else
            {
                AttackServerRpc(manualTarget.NetworkObjectId, manualTarget.transform.position, direction);
            }
        }
    }
    
    private void PerformAttackMoveAttack()
    {
        // Find nearest enemy in attack-move range
        Collider[] colliders = Physics.OverlapSphere(transform.position, attackMoveRange, LayerMask.GetMask("Enemy"));
        
        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider col in colliders)
        {
            BaseCharacter targetChar = col.GetComponent<BaseCharacter>();
            if (targetChar != null && !targetChar.IsDead())
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance && distance <= attackConfig.attackRange)
                {
                    nearestDistance = distance;
                    nearestTarget = col.transform;
                }
            }
        }
        
        if (nearestTarget != null)
        {
            BaseCharacter targetChar = nearestTarget.GetComponent<BaseCharacter>();
            Vector3 direction = (nearestTarget.position - transform.position).normalized;
            
            if (attackConfig.attackType == AttackType.Melee)
            {
                AttackServerRpc(targetChar.NetworkObjectId, nearestTarget.position, direction);
            }
            else
            {
                AttackServerRpc(targetChar.NetworkObjectId, nearestTarget.position, direction);
            }
        }
    }
    
    private void PerformEnemyAutoAttack()
    {
        // Find nearest player
        Collider[] colliders = Physics.OverlapSphere(
            transform.position, 
            attackConfig.attackRange, 
            LayerMask.GetMask("Player")
        );
        
        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider col in colliders)
        {
            BaseCharacter targetChar = col.GetComponent<BaseCharacter>();
            if (targetChar != null && !targetChar.IsDead())
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = col.transform;
                }
            }
        }
        
        if (nearestTarget != null)
        {
            BaseCharacter targetChar = nearestTarget.GetComponent<BaseCharacter>();
            if (targetChar != null)
            {
                Vector3 direction = (nearestTarget.position - transform.position).normalized;
                
                if (attackConfig.attackType == AttackType.Melee)
                {
                    PerformMeleeAttack(direction);
                }
                else
                {
                    AttackServerRpc(targetChar.NetworkObjectId, nearestTarget.position, direction);
                }
            }
        }
    }
    
    private void PerformMeleeAttack(Vector3 direction)
    {
        // Find all potential targets in range
        Collider[] hits = Physics.OverlapSphere(transform.position, attackConfig.attackRange);
        int targetsHit = 0;
        
        foreach (Collider hit in hits)
        {
            // Skip if we've hit max targets (for cleave)
            if (!attackConfig.meleeCleave && targetsHit >= 1) break;
            if (attackConfig.meleeCleave && targetsHit >= attackConfig.maxCleaveTargets) break;
            
            BaseCharacter targetChar = hit.GetComponent<BaseCharacter>();
            if (targetChar == null || targetChar.IsDead()) continue;
            
            // Don't hit self
            NetworkObject targetNetObj = targetChar.GetComponent<NetworkObject>();
            NetworkObject selfNetObj = character.GetComponent<NetworkObject>();
            if (targetNetObj == selfNetObj) continue;
            
            // Check if target is in attack arc
            Vector3 toTarget = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(direction, toTarget);
            
            if (angle <= attackConfig.meleeArcAngle / 2f)
            {
                // Valid target - check if it's an enemy
                bool attackerIsPlayer = gameObject.layer == LayerMask.NameToLayer("Player");
                bool targetIsPlayer = hit.gameObject.layer == LayerMask.NameToLayer("Player");
                
                if (attackerIsPlayer != targetIsPlayer) // Can only hit enemies
                {
                    AttackServerRpc(targetChar.NetworkObjectId, hit.transform.position, toTarget);
                    targetsHit++;
                }
            }
        }
    }
    
    [ServerRpc]
    private void AttackServerRpc(ulong targetNetworkId, Vector3 targetPosition, Vector3 direction)
    {
        lastAttackTime = Time.time;
        
        // Play animation
        if (animController != null)
        {
            animController.PlayAttackAnimation();
        }
        
        // Play attack sound
        PlayAttackSoundClientRpc();
        
        // Execute attack immediately (no queue)
        if (attackConfig.attackType == AttackType.Ranged)
        {
            SpawnProjectile(targetPosition, direction);
        }
        else // Melee
        {
            if (targetNetworkId != 0)
            {
                ApplyMeleeDamage(targetNetworkId, direction);
            }
        }
        
        // Sync animation to clients
        PlayAttackAnimationClientRpc();
    }
    
    private void ApplyMeleeDamage(ulong targetNetworkId, Vector3 direction)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject targetObj))
        {
            BaseCharacter targetChar = targetObj.GetComponent<BaseCharacter>();
            if (targetChar != null)
            {
                Vector3 hitPosition = targetObj.transform.position;
                
                // Apply damage (even if dead for ragdoll effects)
                if (!targetChar.IsDead())
                {
                    targetChar.TakeDamageServerRpc(character.GetDamage(), character.OwnerClientId);
                }
                
                // Apply knockback regardless of death state (for ragdoll/visual effect)
                if (attackConfig.knockbackForce > 0)
                {
                    KnockbackController knockback = targetObj.GetComponent<KnockbackController>();
                    if (knockback != null)
                    {
                        knockback.ApplyKnockbackServerRpc(
                            direction, 
                            attackConfig.knockbackForce, 
                            attackConfig.knockbackDuration
                        );
                    }
                }
                
                // Spawn impact effect (preferred over melee hit effect)
                if (attackConfig.impactEffect != null)
                {
                    SpawnImpactEffectClientRpc(hitPosition, direction);
                }
                else if (attackConfig.meleeHitEffect != null)
                {
                    SpawnHitEffectClientRpc(hitPosition);
                }
                
                // Play impact sound
                if (attackConfig.impactSound != null)
                {
                    PlayImpactSoundClientRpc(hitPosition);
                }
            }
        }
    }
    
    [ClientRpc]
    private void PlayAttackAnimationClientRpc()
    {
        if (animator != null && !IsServer)
        {
            animator.SetTrigger(attackAnimationTrigger);
        }
    }
    
    [ClientRpc]
    private void PlayAttackSoundClientRpc()
    {
        if (attackConfig != null && attackConfig.attackSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(attackConfig.attackSound, 1f);
            }
            else
            {
                // Fallback: Play at position
                AudioSource.PlayClipAtPoint(attackConfig.attackSound, transform.position, 1f);
            }
        }
    }
    
    [ClientRpc]
    private void SpawnHitEffectClientRpc(Vector3 position)
    {
        if (attackConfig.meleeHitEffect != null)
        {
            GameObject effect = Instantiate(attackConfig.meleeHitEffect, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    [ClientRpc]
    private void SpawnImpactEffectClientRpc(Vector3 position, Vector3 direction)
    {
        if (attackConfig.impactEffect != null)
        {
            Quaternion rotation = direction != Vector3.zero ? Quaternion.LookRotation(direction) : Quaternion.identity;
            GameObject effect = Instantiate(attackConfig.impactEffect, position, rotation);
            Destroy(effect, 2f);
        }
    }
    
    [ClientRpc]
    private void PlayImpactSoundClientRpc(Vector3 position)
    {
        if (attackConfig.impactSound != null)
        {
            AudioSource.PlayClipAtPoint(attackConfig.impactSound, position, 1f);
        }
    }
    
    private void SpawnProjectile(Vector3 targetPosition, Vector3 direction)
    {
        if (attackConfig.projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned for ranged attack!");
            return;
        }
        
        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position + Vector3.up;
        
        GameObject projectile = Instantiate(
            attackConfig.projectilePrefab, 
            spawnPos, 
            Quaternion.LookRotation(direction)
        );
        
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();
        
        if (netObj != null)
        {
            netObj.Spawn();
            
            // Initialize projectile with impact effects
            Projectile projScript = projectile.GetComponent<Projectile>();
            if (projScript != null)
            {
                NetworkObject shooterNetObj = character.GetComponent<NetworkObject>();
                projScript.Initialize(
                    character.GetDamage(), 
                    character.OwnerClientId, 
                    direction, 
                    shooterNetObj,
                    attackConfig.projectileMaxDistance,
                    attackConfig.projectileSpeed,
                    attackConfig.knockbackForce,
                    attackConfig.knockbackDuration,
                    attackConfig.impactEffect,
                    attackConfig.impactSound
                );
            }
        }
    }
    
    private Vector3 GetCursorWorldPosition()
    {
        if (playerCamera == null) return transform.position + transform.forward * 5f;
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        return transform.position + transform.forward * 5f;
    }
    
    /// <summary>
    /// Change attack configuration at runtime
    /// </summary>
    public void SetAttackConfig(AutoAttackConfig newConfig)
    {
        attackConfig = newConfig;
    }
    
    /// <summary>
    /// Get current attack configuration
    /// </summary>
    public AutoAttackConfig GetAttackConfig()
    {
        return attackConfig;
    }
    
    // ============ MOBA CONTROL METHODS ============
    
    /// <summary>
    /// Set a manual target for attacking (right-click targeting)
    /// </summary>
    public void SetManualTarget(BaseCharacter target)
    {
        manualTarget = target;
        hasManualTarget = target != null;
        attackMoveMode = false; // Disable attack-move when manually targeting
    }
    
    /// <summary>
    /// Clear manual target
    /// </summary>
    public void ClearManualTarget()
    {
        manualTarget = null;
        hasManualTarget = false;
    }
    
    /// <summary>
    /// Enable/disable attack-move mode (shift + right click)
    /// </summary>
    public void SetAttackMoveMode(bool enabled, float range)
    {
        attackMoveMode = enabled;
        attackMoveRange = range;
        
        if (enabled)
        {
            ClearManualTarget(); // Clear manual target when entering attack-move
        }
    }
}