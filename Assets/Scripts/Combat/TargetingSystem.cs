using UnityEngine;

/// <summary>
/// Displays a visual indicator around the targeted enemy
/// </summary>
public class TargetingSystem : MonoBehaviour
{
    [Header("Target Indicator")]
    [SerializeField] private GameObject targetIndicatorPrefab;
    [SerializeField] private Vector3 indicatorOffset = new Vector3(0, 0.1f, 0);
    [SerializeField] private float indicatorScale = 2f;
    
    [Header("Colors")]
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private Color attackableColor = Color.yellow;
    [SerializeField] private Color allyColor = Color.green;
    
    private GameObject currentIndicator;
    private BaseCharacter currentTarget;
    private Renderer indicatorRenderer;
    
    private void Update()
    {
        UpdateIndicatorPosition();
    }
    
    /// <summary>
    /// Set a new target and show indicator
    /// </summary>
    public void SetTarget(BaseCharacter target)
    {
        if (target == null) return;
        
        // Clear previous target
        ClearTarget();
        
        currentTarget = target;
        
        // Create or reuse indicator
        if (currentIndicator == null)
        {
            if (targetIndicatorPrefab != null)
            {
                currentIndicator = Instantiate(targetIndicatorPrefab);
            }
            else
            {
                // Create default indicator (circle)
                CreateDefaultIndicator();
            }
        }
        
        // Get renderer for color changes
        if (indicatorRenderer == null && currentIndicator != null)
        {
            indicatorRenderer = currentIndicator.GetComponentInChildren<Renderer>();
        }
        
        // Position indicator
        UpdateIndicatorPosition();
        
        // Set color based on target type
        SetIndicatorColor();
        
        // Show indicator
        if (currentIndicator != null)
        {
            currentIndicator.SetActive(true);
        }
    }
    
    /// <summary>
    /// Clear current target and hide indicator
    /// </summary>
    public void ClearTarget()
    {
        currentTarget = null;
        
        if (currentIndicator != null)
        {
            currentIndicator.SetActive(false);
        }
    }
    
    private void UpdateIndicatorPosition()
    {
        if (currentTarget == null || currentIndicator == null || !currentIndicator.activeSelf)
            return;
        
        // Position indicator at target's feet
        currentIndicator.transform.position = currentTarget.transform.position + indicatorOffset;
        
        // Optional: Rotate indicator
        currentIndicator.transform.Rotate(Vector3.up, 50f * Time.deltaTime);
    }
    
    private void SetIndicatorColor()
    {
        if (indicatorRenderer == null || currentTarget == null) return;
        
        // Determine color based on target layer
        int targetLayer = currentTarget.gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int playerLayer = LayerMask.NameToLayer("Player");
        
        Color color;
        
        if (targetLayer == enemyLayer)
        {
            color = targetColor; // Red for enemies
        }
        else if (targetLayer == playerLayer)
        {
            color = allyColor; // Green for allies
        }
        else
        {
            color = attackableColor; // Yellow for other
        }
        
        Debug.Log($"Target {currentTarget.gameObject.name} on layer {LayerMask.LayerToName(targetLayer)} - Color: {color}");
        
        // Apply color
        if (indicatorRenderer.material != null)
        {
            indicatorRenderer.material.color = color;
            
            // Make it glow if material supports emission
            if (indicatorRenderer.material.HasProperty("_EmissionColor"))
            {
                indicatorRenderer.material.SetColor("_EmissionColor", color * 0.5f);
                indicatorRenderer.material.EnableKeyword("_EMISSION");
            }
        }
    }
    
    private void CreateDefaultIndicator()
    {
        // Create a simple circle indicator
        currentIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        currentIndicator.name = "TargetIndicator";
        
        // Scale it to be flat and wide
        currentIndicator.transform.localScale = new Vector3(indicatorScale, 0.1f, indicatorScale);
        
        // Remove collider
        Destroy(currentIndicator.GetComponent<Collider>());
        
        // Create material
        Renderer renderer = currentIndicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = targetColor;
        mat.SetFloat("_Metallic", 0.5f);
        mat.SetFloat("_Glossiness", 0.8f);
        renderer.material = mat;
        
        indicatorRenderer = renderer;
    }
    
    /// <summary>
    /// Get current target
    /// </summary>
    public BaseCharacter GetCurrentTarget()
    {
        return currentTarget;
    }
    
    /// <summary>
    /// Check if there's an active target
    /// </summary>
    public bool HasTarget()
    {
        return currentTarget != null;
    }
    
    private void OnDestroy()
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
        }
    }
}