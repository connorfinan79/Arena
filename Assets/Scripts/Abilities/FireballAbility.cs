using UnityEngine;

/// <summary>
/// Example ability: Shoots a fireball toward target position
/// Create more abilities by copying this template
/// </summary>
[CreateAssetMenu(fileName = "Fireball Ability", menuName = "Game/Abilities/Fireball")]
public class FireballAbility : BaseAbility
{
    [Header("Fireball Settings")]
    public float damage = 50f;
    public float projectileSpeed = 20f;
    public float explosionRadius = 3f;
    
    protected override void Execute(Vector3 targetPosition)
    {
        if (caster == null) return;
        
        // Calculate direction
        Vector3 spawnPos = caster.transform.position + Vector3.up * 1.5f;
        Vector3 direction = (targetPosition - spawnPos).normalized;
        
        // Spawn projectile if prefab exists
        if (abilityPrefab != null)
        {
            GameObject fireball = Instantiate(abilityPrefab, spawnPos, Quaternion.LookRotation(direction));
            
            // Set up the fireball projectile
            FireballProjectile fireballScript = fireball.GetComponent<FireballProjectile>();
            if (fireballScript != null)
            {
                fireballScript.Initialize(damage, caster.OwnerClientId, direction, projectileSpeed, explosionRadius);
            }
            
            // If it has a NetworkObject, spawn it on network
            Unity.Netcode.NetworkObject netObj = fireball.GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }
        
        // Play animation on caster
        Animator animator = caster.GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(animationTrigger))
        {
            animator.SetTrigger(animationTrigger);
        }
        
        Debug.Log($"{caster.gameObject.name} used Fireball!");
    }
}

/// <summary>
/// Fireball projectile behavior
/// Attach this to your fireball prefab
/// </summary>
public class FireballProjectile : MonoBehaviour
{
    private float damage;
    private ulong ownerId;
    private Vector3 direction;
    private float speed;
    private float explosionRadius;
    private float lifetime = 5f;
    private float spawnTime;
    
    public void Initialize(float dmg, ulong owner, Vector3 dir, float spd, float radius)
    {
        damage = dmg;
        ownerId = owner;
        direction = dir.normalized;
        speed = spd;
        explosionRadius = radius;
        spawnTime = Time.time;
    }
    
    private void Update()
    {
        // Move projectile
        transform.position += direction * speed * Time.deltaTime;
        
        // Destroy after lifetime
        if (Time.time > spawnTime + lifetime)
        {
            Explode();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Hit something - explode
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy") ||
            other.gameObject.layer == LayerMask.NameToLayer("Player") ||
            other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
        }
    }
    
    private void Explode()
    {
        // Deal AOE damage
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        
        foreach (Collider hit in hitColliders)
        {
            BaseCharacter character = hit.GetComponent<BaseCharacter>();
            if (character != null && !character.IsDead() && character.OwnerClientId != ownerId)
            {
                character.TakeDamageServerRpc(damage, ownerId);
            }
        }
        
        // Spawn explosion effect here (particle system, etc.)
        
        // Destroy fireball
        Destroy(gameObject);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}