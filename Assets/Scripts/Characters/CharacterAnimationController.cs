using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls character animations and syncs them across network
/// Links movement/attack speed to animation speed
/// Handles looking at cursor for players with priority over movement direction
/// </summary>
public class CharacterAnimationController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private BaseCharacter character;
    [SerializeField] private Transform modelTransform; // The visual model (not root)
    
    [Header("Animation Settings")]
    [SerializeField] private float movementAnimationMultiplier = 1f;
    [SerializeField] private float attackAnimationSpeedMultiplier = 1f;
    [SerializeField] private bool useDirectionalAnimations = false; // Enable for 8-direction anims
    
    [Header("Look At Settings")]
    [SerializeField] private bool lookAtCursor = true;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private bool lockRotationWhileMoving = false; // Set to true to fix issue #1
    
    // Animation parameter hashes (for performance)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    
    // For directional animations
    private static readonly int DirectionXHash = Animator.StringToHash("DirectionX");
    private static readonly int DirectionYHash = Animator.StringToHash("DirectionY");
    
    // Network variables for syncing animations
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    
    private NetworkVariable<bool> networkIsDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<Vector2> networkDirection = new NetworkVariable<Vector2>(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    
    private Camera mainCamera;
    private Vector3 lastPosition;
    private Vector3 currentMovementDirection;
    
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        if (character == null)
        {
            character = GetComponent<BaseCharacter>();
        }
        
        // Auto-find model transform if not assigned
        if (modelTransform == null && animator != null)
        {
            modelTransform = animator.transform;
        }
        
        lastPosition = transform.position;
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            mainCamera = Camera.main;
        }
        
        // Subscribe to network variable changes
        networkSpeed.OnValueChanged += OnSpeedChanged;
        networkIsDead.OnValueChanged += OnDeathChanged;
        networkDirection.OnValueChanged += OnDirectionChanged;
    }
    
    public override void OnNetworkDespawn()
    {
        networkSpeed.OnValueChanged -= OnSpeedChanged;
        networkIsDead.OnValueChanged -= OnDeathChanged;
        networkDirection.OnValueChanged -= OnDirectionChanged;
        base.OnNetworkDespawn();
    }
    
    private void Update()
    {
        if (animator == null || character == null) return;
        
        UpdateMovementAnimation();
        
        if (IsOwner && lookAtCursor && !character.IsDead())
        {
            LookAtCursor();
        }
    }
    
    private void UpdateMovementAnimation()
    {
        // Calculate current movement speed
        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        float currentSpeed = velocity.magnitude;
        
        // Store movement direction
        if (currentSpeed > 0.1f)
        {
            currentMovementDirection = velocity.normalized;
        }
        
        lastPosition = transform.position;
        
        // Normalize to 0-1 range based on character's max speed
        float maxSpeed = character.GetMoveSpeed();
        float normalizedSpeed = maxSpeed > 0 ? Mathf.Clamp01(currentSpeed / maxSpeed) : 0f;
        
        if (IsOwner)
        {
            // Update network variable
            networkSpeed.Value = normalizedSpeed;
            
            // Update directional blend if using directional animations
            if (useDirectionalAnimations && currentSpeed > 0.1f)
            {
                UpdateDirectionalAnimation();
            }
        }
        
        // Update animator
        animator.SetFloat(SpeedHash, IsOwner ? normalizedSpeed : networkSpeed.Value);
        
        // Adjust animation playback speed based on movement speed
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Movement") || stateInfo.IsName("Walk") || stateInfo.IsName("Run"))
        {
            float speedMultiplier = maxSpeed / 5f; // 5f is base move speed
            animator.speed = Mathf.Clamp(speedMultiplier * movementAnimationMultiplier, 0.5f, 3f);
        }
        else if (!stateInfo.IsName("Attack"))
        {
            animator.speed = 1f;
        }
    }
    
    private void UpdateDirectionalAnimation()
    {
        if (modelTransform == null) return;
        
        // Get movement direction relative to model's facing direction
        Vector3 localDirection = modelTransform.InverseTransformDirection(currentMovementDirection);
        
        // Set blend tree parameters (X = strafe, Y = forward/back)
        Vector2 direction = new Vector2(localDirection.x, localDirection.z);
        networkDirection.Value = direction;
        
        animator.SetFloat(DirectionXHash, direction.x);
        animator.SetFloat(DirectionYHash, direction.y);
    }
    
    private void LookAtCursor()
    {
        if (mainCamera == null) return;
        
        // Get cursor world position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 direction = (targetPoint - transform.position).normalized;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                
                // FIX #1: Always rotate towards cursor, ignore movement direction
                if (lockRotationWhileMoving)
                {
                    // Rotate root transform (for gameplay)
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                else
                {
                    // Rotate root transform (for gameplay)
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }
    }
    
    /// <summary>
    /// Trigger attack animation (synced across network)
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (animator == null) return;
        
        if (IsServer)
        {
            PlayAttackClientRpc();
        }
        else if (IsOwner)
        {
            PlayAttackServerRpc();
        }
    }
    
    [ServerRpc]
    private void PlayAttackServerRpc()
    {
        PlayAttackClientRpc();
    }
    
    [ClientRpc]
    private void PlayAttackClientRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
            
            // Adjust attack animation speed based on attack speed
            StartCoroutine(AdjustAttackAnimationSpeed());
        }
    }
    
    private System.Collections.IEnumerator AdjustAttackAnimationSpeed()
    {
        yield return null; // Wait one frame for animation to start
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Attack"))
        {
            float attackSpeedMultiplier = character.GetAttackSpeed();
            animator.speed = Mathf.Clamp(attackSpeedMultiplier * attackAnimationSpeedMultiplier, 0.5f, 3f);
            
            // Reset speed after attack animation
            float animLength = stateInfo.length / animator.speed;
            yield return new WaitForSeconds(animLength);
            animator.speed = 1f;
        }
    }
    
    /// <summary>
    /// Play death animation (server authoritative)
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (IsServer)
        {
            networkIsDead.Value = true;
            PlayDeathClientRpc();
        }
    }
    
    [ClientRpc]
    private void PlayDeathClientRpc()
    {
        if (animator != null)
        {
            animator.SetBool(IsDeadHash, true);
        }
    }
    
    /// <summary>
    /// Manually set movement direction for animation (when not looking at cursor)
    /// Used for enemies
    /// </summary>
    public void SetMovementDirection(Vector3 direction)
    {
        if (!lookAtCursor && direction != Vector3.zero)
        {
            direction.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            // FIX #2: Only rotate root, not model
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    // Network callbacks
    private void OnSpeedChanged(float oldValue, float newValue)
    {
        if (animator != null && !IsOwner)
        {
            animator.SetFloat(SpeedHash, newValue);
        }
    }
    
    private void OnDeathChanged(bool oldValue, bool newValue)
    {
        if (animator != null && newValue)
        {
            animator.SetBool(IsDeadHash, true);
        }
    }
    
    private void OnDirectionChanged(Vector2 oldValue, Vector2 newValue)
    {
        if (animator != null && !IsOwner && useDirectionalAnimations)
        {
            animator.SetFloat(DirectionXHash, newValue.x);
            animator.SetFloat(DirectionYHash, newValue.y);
        }
    }
}