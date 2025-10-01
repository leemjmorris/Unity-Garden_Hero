using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Tools;

public class HealthUIManager : MonoBehaviour
{
    [Header("MM Progress Bar")]
    [SerializeField] private MMProgressBar healthProgressBar;
    
    [Header("Entity Reference")]
    [SerializeField] private LivingEntity targetEntity;
    [SerializeField] private Transform bossPrefabsParent; // BossPrefabs 부모 오브젝트 (Monster UI용)

    [Header("UI Type")]
    [SerializeField] private bool isPlayerUI = true;
    
    [Header("Color Settings")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private Color healFlashColor = Color.green;
    
    private Image foregroundImage;
    
    void Start()
    {
        // Auto-find target entity for monster UI
        if (!isPlayerUI && targetEntity == null && bossPrefabsParent != null)
        {
            FindActiveMonster();
        }

        if (targetEntity != null)
        {
            InitializeHealthBar();
            SubscribeToEntityEvents();
        }
        else if (!isPlayerUI)
        {
            // Monster UI가 타겟을 찾지 못했으면 Update에서 계속 찾기
            Debug.LogWarning("[HealthUIManager] Monster target not found. Will retry in Update.");
        }
    }

    void Update()
    {
        // Monster UI가 타겟이 없으면 계속 찾기
        if (!isPlayerUI && targetEntity == null && bossPrefabsParent != null)
        {
            FindActiveMonster();
        }
    }

    void FindActiveMonster()
    {
        if (bossPrefabsParent == null) return;

        // BossPrefabs의 자식 중 활성화된 몬스터 찾기
        foreach (Transform child in bossPrefabsParent)
        {
            if (child.gameObject.activeSelf)
            {
                MonsterManager monster = child.GetComponent<MonsterManager>();
                if (monster != null)
                {
                    SetTargetEntity(monster);
                    Debug.Log($"[HealthUIManager] Found active monster: {child.name}");
                    return;
                }
            }
        }
    }
    
    void InitializeHealthBar()
    {
        if (healthProgressBar == null) return;
        
        // LMJ: Get the foreground bar image
        if (healthProgressBar.ForegroundBar != null)
        {
            foregroundImage = healthProgressBar.ForegroundBar.GetComponent<Image>();
        }
        
        // LMJ: Set initial values
        float initialHealth = targetEntity.GetHealthPercentage();
        healthProgressBar.SetBar01(initialHealth);
        
        // LMJ: Set initial color
        UpdateHealthBarColor(initialHealth);
    }
    
    void SubscribeToEntityEvents()
    {
        targetEntity.OnHealthChanged.AddListener(OnHealthChanged);
        targetEntity.OnDamageReceived.AddListener(OnDamageReceived);
        targetEntity.OnHealed.AddListener(OnHealed);
        targetEntity.OnDied.AddListener(OnEntityDied);
    }
    
    void OnHealthChanged(int newHealth)
    {
        UpdateHealthBar();
    }
    
    void OnDamageReceived(DamageInfo damageInfo, int finalDamage)
    {
        // LMJ: Trigger bump effect
        if (healthProgressBar != null)
        {
            healthProgressBar.Bump();
        }
        
        TriggerDamageEffect();
    }
    
    void OnHealed(int healAmount)
    {
        TriggerHealEffect();
    }
    
    void OnEntityDied()
    {
        TriggerDeathEffect();
    }
    
    void UpdateHealthBar()
    {
        if (healthProgressBar == null || targetEntity == null) return;
        
        float healthPercentage = targetEntity.GetHealthPercentage();
        
        // LMJ: Update progress bar using MM Progress Bar API
        healthProgressBar.UpdateBar01(healthPercentage);
        
        // LMJ: Change color based on health percentage
        UpdateHealthBarColor(healthPercentage);
    }
    
    void UpdateHealthBarColor(float healthPercentage)
    {
        if (foregroundImage == null) return;
        
        Color targetColor;
        if (isPlayerUI)
        {
            // LMJ: Player colors change based on health
            if (healthPercentage > 0.6f)
                targetColor = healthyColor;
            else if (healthPercentage > 0.3f)
                targetColor = warningColor;
            else
                targetColor = dangerColor;
        }
        else
        {
            // LMJ: Monster health bar stays red
            targetColor = dangerColor;
        }
        
        foregroundImage.color = targetColor;
    }
    
    void TriggerDamageEffect()
    {
        if (foregroundImage != null)
        {
            //StartCoroutine(FlashEffect(damageFlashColor, 0.2f));
        }
    }
    
    void TriggerHealEffect()
    {
        if (foregroundImage != null)
        {
            StartCoroutine(FlashEffect(healFlashColor, 0.3f));
        }
    }
    
    void TriggerDeathEffect()
    {
        if (healthProgressBar != null)
        {
            healthProgressBar.SetBar01(0f);
            StartCoroutine(FlashEffect(Color.black, 1f));
        }
    }
    
    System.Collections.IEnumerator FlashEffect(Color flashColor, float duration)
    {
        if (foregroundImage == null) yield break;
        
        Color originalColor = foregroundImage.color;
        float elapsed = 0f;
        
        // LMJ: Flash to target color
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            foregroundImage.color = Color.Lerp(originalColor, flashColor, t);
            yield return null;
        }
        
        elapsed = 0f;
        
        // LMJ: Flash back to original color
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            foregroundImage.color = Color.Lerp(flashColor, originalColor, t);
            yield return null;
        }
        
        foregroundImage.color = originalColor;
    }
    
    public void SetTargetEntity(LivingEntity entity)
    {
        // LMJ: Unsubscribe from old entity
        if (targetEntity != null)
        {
            UnsubscribeFromEvents();
        }

        targetEntity = entity;

        if (targetEntity != null)
        {
            InitializeHealthBar();
            SubscribeToEntityEvents();
            UpdateHealthBar();
        }
    }
    
    void UnsubscribeFromEvents()
    {
        if (targetEntity != null)
        {
            targetEntity.OnHealthChanged.RemoveListener(OnHealthChanged);
            targetEntity.OnDamageReceived.RemoveListener(OnDamageReceived);
            targetEntity.OnHealed.RemoveListener(OnHealed);
            targetEntity.OnDied.RemoveListener(OnEntityDied);
        }
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}