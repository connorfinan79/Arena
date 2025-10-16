using UnityEngine;

/// <summary>
/// Represents a stat modification/buff
/// Can be temporary or permanent
/// </summary>
[CreateAssetMenu(fileName = "New Augment", menuName = "Game/Augment")]
public class Augment : ScriptableObject
{
    [Header("Augment Info")]
    public string augmentName = "Augment";
    public string description = "Augment description";
    public Sprite icon;
    
    [Header("Stat Modifications")]
    public StatModification[] statModifications;
    
    [Header("Duration")]
    public bool isPermanent = true;
    public float duration = 0f; // If not permanent
    
    [System.Serializable]
    public class StatModification
    {
        public StatType statType;
        public ModificationType modificationType;
        public float value;
    }
    
    public enum StatType
    {
        Health,
        Damage,
        MoveSpeed,
        AttackSpeed,
        AttackRange,
        Armor
    }
    
    public enum ModificationType
    {
        Flat,       // Add flat amount
        Percentage  // Multiply by percentage (e.g., 0.1 = 10% increase)
    }
    
    /// <summary>
    /// Get the bonus value for a specific stat
    /// </summary>
    public float GetStatBonus(string stat, float baseValue)
    {
        float totalBonus = 0f;
        
        foreach (StatModification mod in statModifications)
        {
            if (mod.statType.ToString() == stat)
            {
                if (mod.modificationType == ModificationType.Flat)
                {
                    totalBonus += mod.value;
                }
                else // Percentage
                {
                    totalBonus += baseValue * mod.value;
                }
            }
        }
        
        return totalBonus;
    }
}