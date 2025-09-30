using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ShieldDurabilitySystem : MonoBehaviour
{
    [Header("Shield References")]
    [SerializeField] private GameObject leftShield;
    [SerializeField] private GameObject rightShield;
    [SerializeField] private GameObject frontShield;
    
    [Header("Durability Settings")]
    [SerializeField] private float maxDurability = 100f;
    [SerializeField] private float currentDurability = 100f; // LMJ: Single durability value
    
    [Header("Damage Settings")]
    [SerializeField] private float perfectHitDamage = 5f;
    [SerializeField] private float goodHitDamage = 3f;
    [SerializeField] private float missDamage = 0f;
    
    [Header("Visual Settings")]
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private Color fullDurabilityColor = new Color(0.5f, 0.5f, 1f, 1f);
    [SerializeField] private Color halfDurabilityColor = new Color(1f, 1f, 0f, 1f);
    [SerializeField] private Color lowDurabilityColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color brokenColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    
    [Header("Disable Settings")]
    [SerializeField] private float disableDuration = 3f;
    
    private Renderer leftShieldRenderer;
    private Renderer rightShieldRenderer;
    private Renderer frontShieldRenderer;
    
    private bool isSystemDisabled = false; // LMJ: Single disable state
    private float disableStartTime = 0f;
    
    void Start()
    {
        InitializeShields();
    }
    
    void InitializeShields()
    {
        if (leftShield != null) leftShieldRenderer = leftShield.GetComponent<Renderer>();
        if (rightShield != null) rightShieldRenderer = rightShield.GetComponent<Renderer>();
        if (frontShield != null) frontShieldRenderer = frontShield.GetComponent<Renderer>();

        currentDurability = maxDurability;
        isSystemDisabled = false;
        
        UpdateShieldVisuals();
    }
    
    public void ProcessNoteResult(string direction, JudgmentResult result)
    {
        if (result == JudgmentResult.Miss)
        {
            return;
        }
        else 
        {
            if (!isSystemDisabled)
            {
                float damage = GetDamageAmount(result);
                DamageShield(damage);
            }
        }
    }
    
    float GetDamageAmount(JudgmentResult result)
    {
        switch (result)
        {
            case JudgmentResult.Perfect:
                return perfectHitDamage;
            case JudgmentResult.Good:
                return goodHitDamage;
            case JudgmentResult.Miss:
                return missDamage;
            default:
                return 0f;
        }
    }
    
    void DamageShield(float damage)
    {
        if (isSystemDisabled || currentDurability <= 0) return;
        
        currentDurability = Mathf.Max(0, currentDurability - damage);
        
        // LMJ: Flash all shields when damaged
        StartCoroutine(DamageFlashAll());
        
        UpdateShieldVisuals();
        
        if (currentDurability <= 0)
        {
            DisableAllShields();
        }
    }
    
    void DisableAllShields()
    {
        isSystemDisabled = true;
        disableStartTime = Time.time;
        
        
        ShieldController shieldController = FindFirstObjectByType<ShieldController>();
        if (shieldController != null)
        {
            // LMJ: Disable all shields visually
            shieldController.SetShieldParticleEffect("Left", true);
            shieldController.SetShieldParticleEffect("Right", true);
            shieldController.SetShieldParticleEffect("Center", true);
        }
        
        StartCoroutine(ShieldBreakEffectAll());
    }
    
    public void RestoreAllShields()
    {
        isSystemDisabled = false;
        currentDurability = maxDurability;
        
        
        ShieldController shieldController = FindFirstObjectByType<ShieldController>();
        if (shieldController != null)
        {
            // LMJ: Restore all shields visually
            shieldController.SetShieldParticleEffect("Left", false);
            shieldController.SetShieldParticleEffect("Right", false);
            shieldController.SetShieldParticleEffect("Center", false);
        }
        
        UpdateShieldVisuals();
    }
    
    public bool IsShieldDisabled(string direction)
    {
        // LMJ: All shields share same disabled state
        if (!isSystemDisabled) return false;
        
        float timeSinceDisabled = Time.time - disableStartTime;
        if (timeSinceDisabled >= disableDuration)
        {
            RestoreAllShields();
            return false;
        }
        return true;
    }
    
    void UpdateShieldVisuals()
    {
        Color targetColor = GetDurabilityColor();
        
        if (leftShieldRenderer != null && !isSystemDisabled) leftShieldRenderer.material.color = targetColor;
        if (rightShieldRenderer != null && !isSystemDisabled) rightShieldRenderer.material.color = targetColor;
        if (frontShieldRenderer != null && !isSystemDisabled) frontShieldRenderer.material.color = targetColor;
    }
    
    Color GetDurabilityColor()
    {
        float durabilityPercent = currentDurability / maxDurability;
        
        if (durabilityPercent <= 0)
        {
            return brokenColor;
        }
        else if (durabilityPercent > 0.7f)
        {
            return Color.Lerp(halfDurabilityColor, fullDurabilityColor, (durabilityPercent - 0.7f) / 0.3f);
        }
        else if (durabilityPercent > 0.3f)
        {
            return Color.Lerp(lowDurabilityColor, halfDurabilityColor, (durabilityPercent - 0.3f) / 0.4f);
        }
        else
        {
            return Color.Lerp(brokenColor, lowDurabilityColor, durabilityPercent / 0.3f);
        }
    }
    
    IEnumerator DamageFlashAll()
    {
        Color originalColor = GetDurabilityColor();
        
        if (leftShieldRenderer != null) leftShieldRenderer.material.color = Color.white;
        if (rightShieldRenderer != null) rightShieldRenderer.material.color = Color.white;
        if (frontShieldRenderer != null) frontShieldRenderer.material.color = Color.white;

        yield return new WaitForSeconds(damageFlashDuration);

        if (leftShieldRenderer != null) leftShieldRenderer.material.color = originalColor;
        if (rightShieldRenderer != null) rightShieldRenderer.material.color = originalColor;
        if (frontShieldRenderer != null) frontShieldRenderer.material.color = originalColor;
    }
    
    IEnumerator ShieldBreakEffectAll()
    {
        // LMJ: Break effect for all shields
        CreateBreakEffect(leftShield);
        CreateBreakEffect(rightShield);
        CreateBreakEffect(frontShield);
        
        yield return null;
    }
    
    void CreateBreakEffect(GameObject shield)
    {
        if (shield == null) return;
        
        for (int i = 0; i < 5; i++)
        {
            GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fragment.transform.position = shield.transform.position;
            fragment.transform.localScale = Vector3.one * 0.2f;
            
            Renderer fragRenderer = fragment.GetComponent<Renderer>();
            fragRenderer.material.color = fullDurabilityColor;
            
            Rigidbody rb = fragment.AddComponent<Rigidbody>();
            rb.AddExplosionForce(300f, shield.transform.position, 2f);
            
            Destroy(fragment, 2f);
        }
    }
    
    void Update()
    {
        // LMJ: Check for restore
        if (isSystemDisabled)
        {
            float timeSinceDisabled = Time.time - disableStartTime;
            if (timeSinceDisabled >= disableDuration)
            {
                RestoreAllShields();
            }
        }
    }
    
    public float GetShieldDurabilityPercent(string direction = "")
    {
        // LMJ: Return same value for all directions
        return currentDurability / maxDurability;
    }
    
    public void RepairShield(string direction, float amount)
    {
        // LMJ: Repair affects shared durability
        currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
        UpdateShieldVisuals();
    }
    
    public void RepairAllShields()
    {
        currentDurability = maxDurability;
        UpdateShieldVisuals();
    }
}