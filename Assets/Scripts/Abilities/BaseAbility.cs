using UnityEngine;

/// <summary>
/// Base class for all abilities
/// Create specific abilities by inheriting from this class
/// </summary>
public abstract class BaseAbility : ScriptableObject
{
    [Header("Ability Info")]
    public string abilityName = "Ability";
    public string description = "Ability description";
    public Sprite icon;
    
    [Header("Ability Stats")]
    public float cooldown = 5f;
    public float manaCost = 0f;
    public float castTime = 0f;
    public float range = 10f;
    
    [Header("Animation")]
    public string animationTrigger = "Ability";
    public GameObject abilityPrefab; // Visual effect or projectile
    
    // Runtime data
    protected float lastUseTime;
    protected BaseCharacter caster;
    
    public virtual void Initialize(BaseCharacter character)
    {
        caster = character;
        lastUseTime = -cooldown; // Can use immediately
    }
    
    /// <summary>
    /// Check if ability can be used
    /// </summary>
    public virtual bool CanUse()
    {
        if (caster == null || caster.IsDead()) return false;
        if (Time.time < lastUseTime + cooldown) return false;
        return true;
    }
    
    /// <summary>
    /// Use the ability at a target position
    /// </summary>
    public virtual void Use(Vector3 targetPosition)
    {
        if (!CanUse()) return;
        
        lastUseTime = Time.time;
        Execute(targetPosition);
    }
    
    /// <summary>
    /// Override this in derived classes to implement ability logic
    /// </summary>
    protected abstract void Execute(Vector3 targetPosition);
    
    /// <summary>
    /// Get remaining cooldown time
    /// </summary>
    public float GetCooldownRemaining()
    {
        float remaining = (lastUseTime + cooldown) - Time.time;
        return Mathf.Max(0, remaining);
    }
    
    /// <summary>
    /// Check if ability is on cooldown
    /// </summary>
    public bool IsOnCooldown()
    {
        return GetCooldownRemaining() > 0;
    }
}