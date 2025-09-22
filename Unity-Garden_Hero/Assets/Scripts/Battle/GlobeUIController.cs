using UnityEngine;
using UnityEngine.UI;

public class GlobeUIController : MonoBehaviour
{
    [Header("Current Globe (Center - Monster Health)")]
    [SerializeField] private GameObject centerHealthGlobe;
    
    [Header("Shield Durability Globes")]
    [SerializeField] private GameObject leftShieldDurability;
    [SerializeField] private GameObject rightShieldDurability;
    [SerializeField] private GameObject topShieldDurability;
    [SerializeField] private GameObject bottomShieldDurability;
    
    [Header("Shield Globe Highlight Circles")]
    [SerializeField] private GameObject leftGlobeHighlightCircle;
    [SerializeField] private GameObject rightGlobeHighlightCircle;
    [SerializeField] private GameObject topGlobeHighlightCircle;
    [SerializeField] private GameObject bottomGlobeHighlightCircle;
    
    [Header("System References")]
    [SerializeField] private DirectionalShieldSystem directionalShieldSystem;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private HealthSystem mainHealthSystem;
    [SerializeField] private bool autoFindSystems = true;
    
    void Start()
    {
        if (autoFindSystems)
        {
            if (directionalShieldSystem == null)
                directionalShieldSystem = FindFirstObjectByType<DirectionalShieldSystem>();
                
            if (monsterManager == null)
                monsterManager = FindFirstObjectByType<MonsterManager>();
                
            if (mainHealthSystem == null)
                mainHealthSystem = GetComponent<HealthSystem>();
        }
        
        CheckConnections();
        SetupHealthSystem();
    }
    
    void CheckConnections()
    {
        Debug.Log($"Center Globe: {centerHealthGlobe != null}");
        Debug.Log($"Shield Globes - Left: {leftShieldDurability != null}, Right: {rightShieldDurability != null}, Top: {topShieldDurability != null}, Bottom: {bottomShieldDurability != null}");
        Debug.Log($"Highlight Circles - Top: {topGlobeHighlightCircle != null}, Bottom: {bottomGlobeHighlightCircle != null}, Left: {leftGlobeHighlightCircle != null}, Right: {rightGlobeHighlightCircle != null}");
        Debug.Log($"Systems - DirectionalShield: {directionalShieldSystem != null}, Monster: {monsterManager != null}, HealthSystem: {mainHealthSystem != null}");
    }
    
    void SetupHealthSystem()
    {
        if (mainHealthSystem != null)
        {
            mainHealthSystem.Regenerate = false;
            mainHealthSystem.GodMode = false;
            mainHealthSystem.hitPoint = 100f;
            mainHealthSystem.maxHitPoint = 100f;
        }
    }
    
    void Update()
    {
        UpdateGlobes();
        UpdateHighlight();
    }
    
    void UpdateGlobes()
    {
        //LMJ: Update center globe with monster health  
        if (monsterManager != null)
        {
            float healthPercent = monsterManager.GetHealthPercentage();
            UpdateCenterGlobeManually(healthPercent);
        }
        
        //LMJ: Update shield durability visuals
        if (directionalShieldSystem != null)
        {
            UpdateShieldGlobe(leftShieldDurability, 1, "Left");
            UpdateShieldGlobe(rightShieldDurability, 3, "Right");  
            UpdateShieldGlobe(topShieldDurability, 0, "Top");
            UpdateShieldGlobe(bottomShieldDurability, 2, "Bottom");
        }
    }
    
    void UpdateCenterGlobeManually(float healthPercent)
    {
        if (centerHealthGlobe != null)
        {
            //LMJ: Find all Image components and update filled ones
            Image[] allImages = centerHealthGlobe.GetComponentsInChildren<Image>();
            bool updated = false;
            
            foreach (Image img in allImages)
            {
                if (img.type == Image.Type.Filled)
                {
                    //LMJ: Set fill method to vertical for potion-like effect
                    img.fillMethod = Image.FillMethod.Vertical;
                    img.fillOrigin = 0; // Bottom to top
                    img.fillAmount = healthPercent;
                    Debug.Log($"Center Globe Updated Image: {img.name} fillAmount: {healthPercent * 100f}%");
                    updated = true;
                }
            }
            
            if (!updated)
            {
                Debug.LogWarning("No Filled Image found in Center Globe. Found Images:");
                foreach (Image img in allImages)
                {
                    Debug.LogWarning($"  - {img.name} (Type: {img.type})");
                }
            }
        }
    }
    
    void UpdateShieldGlobe(GameObject shieldGlobe, int directionIndex, string globeName)
    {
        if (shieldGlobe == null) return;
        
        DirectionalShield shield = directionalShieldSystem.GetShield(directionIndex);
        if (shield == null) return;
        
        float durabilityPercent = shield.GetDurabilityPercent();
        
        //LMJ: Find all Image components and try to update them
        Image[] allImages = shieldGlobe.GetComponentsInChildren<Image>();
        bool updated = false;
        
        foreach (Image img in allImages)
        {
            if (img.type == Image.Type.Filled)
            {
                //LMJ: Set fill method to vertical for potion-like effect
                img.fillMethod = Image.FillMethod.Vertical;
                img.fillOrigin = 0; // Bottom to top
                img.fillAmount = durabilityPercent;
                Debug.Log($"{globeName} Shield Updated Image: {img.name} fillAmount: {durabilityPercent * 100f}%");
                updated = true;
            }
        }
        
        if (!updated)
        {
            Debug.LogWarning($"No Filled Image found in {globeName} Shield Globe. Found Images:");
            foreach (Image img in allImages)
            {
                Debug.LogWarning($"  - {img.name} (Type: {img.type})");
            }
        }
    }
    
    void UpdateHighlight()
    {
        if (directionalShieldSystem == null) return;
        
        int currentDirection = directionalShieldSystem.GetCurrentDirection();
        
        //LMJ: Disable all highlights first
        SetHighlightCircle(topGlobeHighlightCircle, false);
        SetHighlightCircle(bottomGlobeHighlightCircle, false);
        SetHighlightCircle(leftGlobeHighlightCircle, false);
        SetHighlightCircle(rightGlobeHighlightCircle, false);
        
        //LMJ: Enable highlight for current direction
        switch (currentDirection)
        {
            case 0: // A(0°) - Top
                SetHighlightCircle(topGlobeHighlightCircle, true);
                break;
            case 1: // B(90°) - Left
                SetHighlightCircle(leftGlobeHighlightCircle, true);
                break;
            case 2: // C(180°) - Bottom
                SetHighlightCircle(bottomGlobeHighlightCircle, true);
                break;
            case 3: // D(270°) - Right
                SetHighlightCircle(rightGlobeHighlightCircle, true);
                break;
        }
    }
    
    void SetHighlightCircle(GameObject highlightCircle, bool enabled)
    {
        if (highlightCircle != null)
        {
            highlightCircle.SetActive(enabled);
        }
    }
}