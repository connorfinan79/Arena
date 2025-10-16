using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Enemy AI character
/// Handles basic AI behavior: patrol, chase, attack
/// </summary>
public class EnemyCharacter : BaseCharacter
{
    [Header("AI Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private LayerMask playerLayer;
    
    // Event for enemy spawner to track deaths
    public System.Action OnEnemyDestroyed;
    
    private Transform currentTarget;
    private float lastAttackTime;
    private AutoAttack autoAttack;
    
    protected override void Awake()
    {
        base.Awake();
        autoAttack = GetComponent<AutoAttack>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            if (autoAttack != null)
            {
                autoAttack.Initialize(this, null);
                autoAttack.SetAutoTarget(true);
            }
        }
    }
    
    protected override void Update()
    {
        if (!IsServer) return; // AI only runs on server
        if (isDead) return;
        
        base.Update();
        HandleAI();
    }
    
    private void HandleAI()
    {
        // Find nearest player
        currentTarget = FindNearestPlayer();
        
        if (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            
            if (distanceToTarget <= attackRange)
            {
                // In attack range - stop and attack
                AttackTarget();
                
                // Look at target
                Vector3 direction = (currentTarget.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    transform.forward = direction;
                }
            }
            else if (distanceToTarget <= detectionRange)
            {
                // Chase target
                Vector3 direction = (currentTarget.position - transform.position).normalized;
                Move(direction);
                
                // Look at target
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    transform.forward = direction;
                }
            }
        }
    }
    
    private Transform FindNearestPlayer()
    {
        // Find all colliders in detection range on player layer
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);
        
        Transform nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider col in colliders)
        {
            BaseCharacter character = col.GetComponent<BaseCharacter>();
            if (character != null && !character.IsDead())
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = col.transform;
                }
            }
        }
        
        return nearest;
    }
    
    private void AttackTarget()
    {
        if (currentTarget == null) return;
        if (Time.time < lastAttackTime + (1f / attackSpeed)) return;
        
        lastAttackTime = Time.time;
        
        // Get target character component
        BaseCharacter targetChar = currentTarget.GetComponent<BaseCharacter>();
        if (targetChar != null)
        {
            // Apply damage
            targetChar.TakeDamageServerRpc(damage, OwnerClientId);
        }
    }
    
    protected override void OnDeath(ulong killerId)
    {
        base.OnDeath(killerId);
        
        // Grant experience to killer
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killerId, out NetworkObject killerObj))
        {
            ExperienceSystem killerExp = killerObj.GetComponent<ExperienceSystem>();
            if (killerExp != null)
            {
                killerExp.AddExperienceServerRpc(50f); // Grant 50 XP for killing enemy
            }
        }
        
        // Destroy after delay
        Invoke(nameof(DestroyEnemy), 2f);
    }
    
    private void DestroyEnemy()
    {
        if (IsServer)
        {
            // Notify spawner that this enemy is being destroyed
            OnEnemyDestroyed?.Invoke();
            
            // Despawn from network
            GetComponent<NetworkObject>().Despawn();
        }
    }
    
    // Visualization for debugging
    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}