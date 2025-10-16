using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Base character class that all characters (player and enemy) inherit from
/// Handles core stats, health, movement, and network synchronization
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class BaseCharacter : NetworkBehaviour
{
    [Header("Character Configuration")]
    [SerializeField] protected CharacterStats baseStats;
    
    [Header("UI")]
    [SerializeField] private GameObject healthBarPrefab;
    private HealthBarUI healthBar;
    
    [Header("Runtime Stats")]
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float damage;
    [SerializeField] protected float attackSpeed;
    [SerializeField] protected float attackRange;
    [SerializeField] protected float armor;
    
    // Network variables for syncing across clients
    protected NetworkVariable<float> networkHealth = new NetworkVariable<float>(
        100f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    
    protected NetworkVariable<int> networkLevel = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    // Components
    protected CharacterController controller;
    protected AbilityManager abilityManager;
    protected AugmentManager augmentManager;
    protected ExperienceSystem expSystem;
    
    // State
    protected bool isDead = false;
    protected Vector3 moveDirection = Vector3.zero;
    
    protected virtual void Awake()
    {
        controller = GetComponent<CharacterController>();
        abilityManager = GetComponent<AbilityManager>();
        augmentManager = GetComponent<AugmentManager>();
        expSystem = GetComponent<ExperienceSystem>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            InitializeStats();
            networkHealth.Value = currentHealth;
        }
        
        // Subscribe to network variable changes
        networkHealth.OnValueChanged += OnHealthChanged;
        networkLevel.OnValueChanged += OnLevelChanged;
        
        // Initialize components
        if (abilityManager != null)
            abilityManager.Initialize(this);
            
        if (expSystem != null)
            expSystem.Initialize(this);
            
        // Spawn health bar (everyone sees health bars)
        if (healthBarPrefab != null)
        {
            GameObject healthBarObj = Instantiate(healthBarPrefab);
            healthBar = healthBarObj.GetComponent<HealthBarUI>();
            if (healthBar != null)
            {
                healthBar.Initialize(this);
            }
        }
    }
    
    public override void OnNetworkDespawn()
    {
        networkHealth.OnValueChanged -= OnHealthChanged;
        networkLevel.OnValueChanged -= OnLevelChanged;
        
        // Destroy health bar
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }
        
        base.OnNetworkDespawn();
    }
    
    protected virtual void InitializeStats()
    {
        if (baseStats == null)
        {
            Debug.LogWarning($"{gameObject.name} has no CharacterStats assigned!");
            // Set default values
            maxHealth = 100f;
            moveSpeed = 5f;
            damage = 10f;
            attackSpeed = 1f;
            attackRange = 5f;
            armor = 0f;
        }
        else
        {
            maxHealth = baseStats.GetHealthAtLevel(1);
            moveSpeed = baseStats.moveSpeed;
            damage = baseStats.GetDamageAtLevel(1);
            attackSpeed = baseStats.attackSpeed;
            attackRange = baseStats.attackRange;
            armor = baseStats.GetArmorAtLevel(1);
        }
        
        currentHealth = maxHealth;
    }
    
    protected virtual void Update()
    {
        if (!IsOwner) return;
        
        HandleMovement();
    }
    
    protected virtual void HandleMovement()
    {
        // Override in derived classes
    }
    
    /// <summary>
    /// Move the character (call this from derived classes)
    /// </summary>
    protected void Move(Vector3 direction)
    {
        if (controller.enabled && !isDead)
        {
            Vector3 move = direction * moveSpeed * Time.deltaTime;
            move.y = -2f; // Gravity
            controller.Move(move);
        }
    }
    
    /// <summary>
    /// Apply damage to this character
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damageAmount, ulong attackerId)
    {
        if (isDead) return;
        
        // Calculate damage reduction from armor
        float damageReduction = armor / (armor + 100);
        float actualDamage = damageAmount * (1 - damageReduction);
        
        currentHealth = Mathf.Max(0, currentHealth - actualDamage);
        networkHealth.Value = currentHealth;
        
        if (currentHealth <= 0)
        {
            Die(attackerId);
        }
    }
    
    /// <summary>
    /// Heal this character
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(float healAmount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        networkHealth.Value = currentHealth;
    }
    
    protected virtual void Die(ulong killerId)
    {
        isDead = true;

        // Trigger death animation
        CharacterAnimationController animController = GetComponent<CharacterAnimationController>();
        if (animController != null)
        {
            animController.PlayDeathAnimation();
        }

        OnDeath(killerId);
    }
    
    protected virtual void OnDeath(ulong killerId)
    {
        // Override in derived classes for death behavior
        Debug.Log($"{gameObject.name} died!");
    }
    
    /// <summary>
    /// Level up the character
    /// </summary>
    public virtual void LevelUp(int newLevel)
    {
        if (!IsServer) return;
        
        networkLevel.Value = newLevel;
        
        if (baseStats != null)
        {
            maxHealth = baseStats.GetHealthAtLevel(newLevel);
            damage = baseStats.GetDamageAtLevel(newLevel);
            armor = baseStats.GetArmorAtLevel(newLevel);
            
            // Heal to full on level up
            currentHealth = maxHealth;
            networkHealth.Value = currentHealth;
        }
    }
    
    // Network callbacks
    protected virtual void OnHealthChanged(float oldValue, float newValue)
    {
        currentHealth = newValue;
    }
    
    protected virtual void OnLevelChanged(int oldValue, int newValue)
    {
        // Visual feedback for level up
    }
    
    // Getters for stats (can be modified by augments)
    public float GetMaxHealth() => maxHealth;
    public float GetCurrentHealth() => currentHealth;
    public float GetMoveSpeed() => moveSpeed;
    public float GetDamage() => damage;
    public float GetAttackSpeed() => attackSpeed;
    public float GetAttackRange() => attackRange;
    public float GetArmor() => armor;
    public bool IsDead() => isDead;
    public int GetLevel() => networkLevel.Value;
    
    // Get base stat value for augment calculations
    public float GetBaseStat(string statName)
    {
        switch (statName)
        {
            case "Health": return maxHealth;
            case "Damage": return damage;
            case "MoveSpeed": return moveSpeed;
            case "AttackSpeed": return attackSpeed;
            case "AttackRange": return attackRange;
            case "Armor": return armor;
            default: return 0f;
        }
    }
}