using UnityEngine;

/// <summary>
/// ScriptableObject that holds base stats for characters
/// Create instances via: Assets > Create > Game > Character Stats
/// </summary>
[CreateAssetMenu(fileName = "New Character Stats", menuName = "Game/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float healthRegen = 1f;
    public float moveSpeed = 5f;
    public float baseDamage = 10f;
    public float attackSpeed = 1f; // Attacks per second
    public float attackRange = 5f;
    public float armor = 0f;
    
    [Header("Level Scaling")]
    public float healthPerLevel = 10f;
    public float damagePerLevel = 2f;
    public float armorPerLevel = 1f;
    
    [Header("Character Info")]
    public string characterName = "Character";
    public string description = "A character description";
    
    /// <summary>
    /// Calculate stat value at specific level
    /// </summary>
    public float GetHealthAtLevel(int level)
    {
        return maxHealth + (healthPerLevel * (level - 1));
    }
    
    public float GetDamageAtLevel(int level)
    {
        return baseDamage + (damagePerLevel * (level - 1));
    }
    
    public float GetArmorAtLevel(int level)
    {
        return armor + (armorPerLevel * (level - 1));
    }
}