using UnityEngine;

/// <summary>
/// Defines different auto-attack types
/// </summary>
public enum AttackType
{
    Melee,      // Close range, instant hit
    Ranged      // Spawns projectile
}

/// <summary>
/// Configuration for auto-attack behavior
/// Attach to CharacterStats or create separate ScriptableObject
/// </summary>
[System.Serializable]
public class AutoAttackConfig
{
    [Header("Attack Type")]
    public AttackType attackType = AttackType.Ranged;
    
    [Header("Range Settings")]
    [Tooltip("Maximum range for melee attacks or projectile spawn range")]
    public float attackRange = 5f;
    
    [Tooltip("For ranged: How far projectile travels before despawning")]
    public float projectileMaxDistance = 20f;
    
    [Tooltip("For ranged: Projectile speed")]
    public float projectileSpeed = 15f;
    
    [Header("Knockback Settings")]
    [Tooltip("Amount of knockback force (0 = none, 10 = strong)")]
    public float knockbackForce = 0f;
    
    [Tooltip("Duration of knockback in seconds")]
    public float knockbackDuration = 0.2f;
    
    [Tooltip("Should knockback interrupt movement?")]
    public bool knockbackInterruptsMovement = true;
    
    [Header("Visual/Audio")]
    [Tooltip("Projectile prefab (for ranged attacks)")]
    public GameObject projectilePrefab;
    
    [Tooltip("Impact effect when projectile/attack hits target")]
    public GameObject impactEffect;
    
    [Tooltip("Melee hit effect (particle system)")]
    public GameObject meleeHitEffect;
    
    [Tooltip("Attack sound")]
    public AudioClip attackSound;
    
    [Tooltip("Impact sound when hit connects")]
    public AudioClip impactSound;
    
    [Header("Melee-Specific Settings")]
    [Tooltip("Melee attacks hit arc angle (360 = all around)")]
    public float meleeArcAngle = 90f;
    
    [Tooltip("Can melee attack hit multiple targets?")]
    public bool meleeCleave = false;
    
    [Tooltip("Maximum targets for cleave")]
    public int maxCleaveTargets = 3;
}