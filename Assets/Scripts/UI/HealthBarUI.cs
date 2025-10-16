using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays health bar above character's head
/// Follows character and faces camera
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
    [SerializeField] private bool alwaysVisible = true;
    [SerializeField] private bool smoothTransition = true;
    [SerializeField] private float transitionSpeed = 5f;
    
    private BaseCharacter character;
    private Camera mainCamera;
    private Canvas canvas;
    private float targetFillAmount = 1f;
    
    public void Initialize(BaseCharacter targetCharacter)
    {
        character = targetCharacter;
        mainCamera = Camera.main;
        canvas = GetComponent<Canvas>();
        
        // Find health fill image if not assigned
        if (healthFillImage == null)
        {
            Transform fillTransform = transform.Find("Background/HealthFill");
            if (fillTransform != null)
            {
                healthFillImage = fillTransform.GetComponent<Image>();
            }
        }
        
        UpdateHealthBar();
    }
    
    private void LateUpdate()
    {
        if (character == null || mainCamera == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // Position above character
        transform.position = character.transform.position + offset;
        
        // Face camera (billboard effect)
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        
        // Update health display
        UpdateHealthBar();
        
        // Hide if at full health (optional)
        if (!alwaysVisible && canvas != null)
        {
            canvas.enabled = character.GetCurrentHealth() < character.GetMaxHealth();
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthFillImage != null && character != null)
        {
            float healthPercent = Mathf.Clamp01(character.GetCurrentHealth() / character.GetMaxHealth());
            targetFillAmount = healthPercent;
            
            // Smooth or instant transition
            if (smoothTransition)
            {
                healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetFillAmount, Time.deltaTime * transitionSpeed);
            }
            else
            {
                healthFillImage.fillAmount = targetFillAmount;
            }
            
            // Color coding: Green -> Yellow -> Red
            if (healthPercent > 0.5f)
            {
                healthFillImage.color = Color.Lerp(Color.yellow, Color.green, (healthPercent - 0.5f) * 2f);
            }
            else
            {
                healthFillImage.color = Color.Lerp(Color.red, Color.yellow, healthPercent * 2f);
            }
        }
    }
}