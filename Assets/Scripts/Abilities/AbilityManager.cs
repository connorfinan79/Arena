using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages all abilities on a character
/// </summary>
public class AbilityManager : NetworkBehaviour
{
    [Header("Abilities")]
    [SerializeField] private BaseAbility[] abilities = new BaseAbility[4];
    
    private BaseCharacter character;
    
    public void Initialize(BaseCharacter ownerChar)
    {
        character = ownerChar;

        for (var i = 0; i < abilities.Length; i++)
        {
            if (abilities[i] == null) continue;
            abilities[i] = Instantiate(abilities[i]);
            abilities[i].Initialize(character);
        }
    }

    
    /// <summary>
    /// Use an ability by index (0-3)
    /// </summary>
    public void UseAbility(int index, Vector3 targetPosition)
    {
        if (index < 0 || index >= abilities.Length) return;
        if (abilities[index] == null) return;
        
        if (abilities[index].CanUse())
        {
            UseAbilityServerRpc(index, targetPosition);
        }
    }
    
    [ServerRpc]
    private void UseAbilityServerRpc(int index, Vector3 targetPosition)
    {
        if (index < 0 || index >= abilities.Length) return;
        if (abilities[index] == null) return;
        
        abilities[index].Use(targetPosition);
        
        // Notify clients to play effects
        UseAbilityClientRpc(index, targetPosition);
    }
    
    [ClientRpc]
    private void UseAbilityClientRpc(int index, Vector3 targetPosition)
    {
        // Play visual effects on all clients
        if (IsServer) return; // Server already executed
        
        if (index >= 0 && index < abilities.Length && abilities[index] != null)
        {
            // Play animation or effects (without actual ability logic)
            PlayAbilityEffects(index, targetPosition);
        }
    }
    
    private void PlayAbilityEffects(int index, Vector3 targetPosition)
    {
        // Override or extend this to play visual/audio effects
        // This runs on clients only to sync visuals
    }
    
    /// <summary>
    /// Get ability by index
    /// </summary>
    public BaseAbility GetAbility(int index)
    {
        if (index < 0 || index >= abilities.Length) return null;
        return abilities[index];
    }
    
    /// <summary>
    /// Set ability at index
    /// </summary>
    public void SetAbility(int index, BaseAbility ability)
    {
        if (index < 0 || index >= abilities.Length) return;
        abilities[index] = ability;
        if (ability != null)
        {
            abilities[index].Initialize(character);
        }
    }
}