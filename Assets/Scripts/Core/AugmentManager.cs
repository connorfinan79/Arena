using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages all active augments/buffs on a character
/// </summary>
public class AugmentManager : NetworkBehaviour
{
    private List<ActiveAugment> activeAugments = new List<ActiveAugment>();
    private BaseCharacter character;
    [SerializeField] private BaseCharacter owner;
    
    private class ActiveAugment
    {
        public Augment augment;
        public float applyTime;
        
        public ActiveAugment(Augment aug)
        {
            augment = aug;
            applyTime = Time.time;
        }
        
        public bool IsExpired()
        {
            if (augment.isPermanent) return false;
            return Time.time > applyTime + augment.duration;
        }
    }
    
    private void Start()
    {
        character = GetComponent<BaseCharacter>();
    }
    
    private void Update()
    {
        if (!IsServer) return;
        
        // Remove expired augments
        activeAugments.RemoveAll(a => a.IsExpired());
    }
    
    /// <summary>
    /// Add an augment to the character
    /// </summary>
    public void AddAugment(Augment augment)
    {
        if (augment == null) return;
        
        if (IsServer)
        {
            activeAugments.Add(new ActiveAugment(augment));
        }
    }
    
    /// <summary>
    /// Remove a specific augment
    /// </summary>
    public void RemoveAugment(Augment augment)
    {
        if (augment == null) return;
        
        if (IsServer)
        {
            activeAugments.RemoveAll(a => a.augment == augment);
        }
    }
    
    /// <summary>
    /// Get total stat bonus from all augments
    /// </summary>
    public float GetStatBonus(string statName)
    {
        float totalBonus = 0f;
        float baseValue = GetBaseStat(statName);
        
        foreach (ActiveAugment active in activeAugments)
        {
            totalBonus += active.augment.GetStatBonus(statName, baseValue);
        }
        
        return totalBonus;
    }
    
    private float GetBaseStat(string statName)
    {
        if (owner == null) return 0f;
        
        switch (statName)
        {
            case "Health": return owner.GetMaxHealth();
            case "Damage": return owner.GetDamage();
            case "MoveSpeed": return owner.GetMoveSpeed();
            case "AttackSpeed": return owner.GetAttackSpeed();
            case "AttackRange": return owner.GetAttackRange();
            case "Armor": return owner.GetArmor();
            default: return 0f;
        }
    }
    
    /// <summary>
    /// Get all active augments
    /// </summary>
    public List<Augment> GetActiveAugments()
    {
        List<Augment> augments = new List<Augment>();
        foreach (ActiveAugment active in activeAugments)
        {
            augments.Add(active.augment);
        }
        return augments;
    }
    
    /// <summary>
    /// Clear all augments
    /// </summary>
    public void ClearAllAugments()
    {
        if (IsServer)
        {
            activeAugments.Clear();
        }
    }
}