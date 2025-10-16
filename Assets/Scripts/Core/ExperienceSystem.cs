using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles experience gain and leveling up
/// </summary>
public class ExperienceSystem : NetworkBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int maxLevel = 18;
    [SerializeField] private float baseXPRequired = 100f;
    [SerializeField] private float xpScalingFactor = 1.5f;
    
    private NetworkVariable<float> currentXP = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<int> currentLevel = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private BaseCharacter character;
    
    public void Initialize(BaseCharacter ownerChar)
    {
        character = ownerChar;
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        currentXP.OnValueChanged += OnXPChanged;
        currentLevel.OnValueChanged += OnLevelChanged;
    }
    
    public override void OnNetworkDespawn()
    {
        currentXP.OnValueChanged -= OnXPChanged;
        currentLevel.OnValueChanged -= OnLevelChanged;
        base.OnNetworkDespawn();
    }
    
    /// <summary>
    /// Add experience (Server only)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddExperienceServerRpc(float amount)
    {
        if (currentLevel.Value >= maxLevel) return;
        
        currentXP.Value += amount;
        
        // Check for level up
        float xpNeeded = GetXPRequiredForLevel(currentLevel.Value);
        while (currentXP.Value >= xpNeeded && currentLevel.Value < maxLevel)
        {
            currentXP.Value -= xpNeeded;
            currentLevel.Value++;
            
            // Level up the character
            if (character != null)
            {
                character.LevelUp(currentLevel.Value);
            }
            
            xpNeeded = GetXPRequiredForLevel(currentLevel.Value);
        }
    }
    
    /// <summary>
    /// Calculate XP required for a specific level
    /// </summary>
    public float GetXPRequiredForLevel(int level)
    {
        return baseXPRequired * Mathf.Pow(xpScalingFactor, level - 1);
    }
    
    /// <summary>
    /// Get current XP progress as percentage (0-1)
    /// </summary>
    public float GetXPProgress()
    {
        if (currentLevel.Value >= maxLevel) return 1f;
        
        float required = GetXPRequiredForLevel(currentLevel.Value);
        return currentXP.Value / required;
    }
    
    // Getters
    public int GetCurrentLevel() => currentLevel.Value;
    public float GetCurrentXP() => currentXP.Value;
    public bool IsMaxLevel() => currentLevel.Value >= maxLevel;
    
    // Network callbacks
    private void OnXPChanged(float oldValue, float newValue)
    {
        // Update UI or visual feedback
    }
    
    private void OnLevelChanged(int oldValue, int newValue)
    {
        // Play level up effects
        if (newValue > oldValue)
        {
            Debug.Log($"{character?.gameObject.name} leveled up to {newValue}!");
            // Add particle effects, sound, etc.
        }
    }
}