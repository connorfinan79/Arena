using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Projectile that travels in a direction and damages targets
/// Supports knockback and max distance
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class Projectile : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool piercing = false;
    [SerializeField] private LayerMask hitLayers;
    
    private float damage;
    private ulong ownerId;
    private NetworkObject ownerNetworkObject;
    private Vector3 direction;
    private float spawnTime;
    private Vector3 startPosition;
    
    // Knockback settings
    private float knockbackForce;
    private float knockbackDuration;
    
    // Distance settings
    private float maxDistance = 20f;
    private float travelSpeed = 15f;
    
    // Impact effects
    private GameObject impactEffectPrefab;
    private AudioClip impactSoundClip;
    
    public void Initialize(
        float dmg, 
        ulong owner, 
        Vector3 dir, 
        NetworkObject shooterNetObj = null,
        float maxDist = 20f,
        float projSpeed = 15f,
        float kbForce = 0f,
        float kbDuration = 0.2f,
        GameObject impactEffect = null,
        AudioClip impactSound = null)
    {
        damage = dmg;
        ownerId = owner;
        ownerNetworkObject = shooterNetObj;
        direction = dir.normalized;
        spawnTime = Time.time;
        startPosition = transform.position;
        maxDistance = maxDist;
        travelSpeed = projSpeed;
        knockbackForce = kbForce;
        knockbackDuration = kbDuration;
        impactEffectPrefab = impactEffect;
        impactSoundClip = impactSound;
        
        // Override speed if provided
        if (travelSpeed > 0)
        {
            speed = travelSpeed;
        }
    }
    
    private void Update()
    {
        if (!IsServer) return;
        
        // Move projectile
        transform.position += direction * speed * Time.deltaTime;
        
        // Check if exceeded max distance
        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            DestroyProjectile();
            return;
        }
        
        // Check lifetime (backup)
        if (Time.time > spawnTime + lifetime)
        {
            DestroyProjectile();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        
        // Check if hit layer is valid
        if (((1 << other.gameObject.layer) & hitLayers) == 0)
        {
            return;
        }
        
        // Try to damage character
        BaseCharacter character = other.GetComponent<BaseCharacter>();
        if (character != null && !character.IsDead())
        {
            // Don't hit the shooter
            NetworkObject targetNetObj = character.GetComponent<NetworkObject>();
            if (targetNetObj != null && ownerNetworkObject != null && targetNetObj == ownerNetworkObject)
            {
                return;
            }
            
            // Check friendly fire
            bool shooterIsPlayer = ownerNetworkObject != null && 
                                  ownerNetworkObject.gameObject.layer == LayerMask.NameToLayer("Player");
            bool targetIsPlayer = other.gameObject.layer == LayerMask.NameToLayer("Player");
            
            if (shooterIsPlayer == targetIsPlayer)
            {
                return; // Friendly fire blocked
            }
            
            // Apply damage
            character.TakeDamageServerRpc(damage, ownerId);
            
            // Apply knockback if configured
            if (knockbackForce > 0)
            {
                KnockbackController knockback = other.GetComponent<KnockbackController>();
                if (knockback != null)
                {
                    knockback.ApplyKnockbackServerRpc(direction, knockbackForce, knockbackDuration);
                }
            }
            
            if (!piercing)
            {
                DestroyProjectile();
            }
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Hit ground
            DestroyProjectile();
        }
    }
    
    private void DestroyProjectile()
    {
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
    
    // Visualize max distance in editor
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && IsServer)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(startPosition, maxDistance);
        }
    }
}