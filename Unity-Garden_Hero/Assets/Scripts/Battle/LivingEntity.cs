using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct DamageInfo
{
    public int baseDamage;
    public string noteType;     // Normal, Long, Special, Dodge
    public float multiplier;
    public bool ignoreDefense;
    
    public DamageInfo(int damage, string type = "Normal", float mult = 1.0f, bool ignoreDef = false)
    {
        baseDamage = damage;
        noteType = type;
        multiplier = mult;
        ignoreDefense = ignoreDef;
    }
}

public abstract class LivingEntity : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] protected int level = 1;
    [SerializeField] protected int attackPower = 10;
    [SerializeField] protected int defense = 0;
    [SerializeField] public int maxHealth = 100;
    [SerializeField] public int currentHealth = 100;
    [SerializeField] protected bool isDead = false;
    //[SerializeField] public Animator animator;
    
    [Header("Events")]
    public UnityEvent<DamageInfo, int> OnDamageReceived;
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent<int> OnHealed;
    [System.NonSerialized] public UnityEvent OnDied = new UnityEvent();
    
    public virtual void Initialize(int entityLevel, int attack, int def, int health)
    {
        level = entityLevel;
        attackPower = attack;
        defense = def;
        maxHealth = health;
        currentHealth = maxHealth;
        
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    public virtual void OnDamage(DamageInfo damageInfo)
    {
        if (!IsAlive()) return;
        
        int finalDamage = CalculateDamage(damageInfo);
        ApplyDamage(finalDamage);
        
        OnDamageReceived?.Invoke(damageInfo, finalDamage);
    }

    public virtual void OnDamage(int simpleDamage)
    {
        DamageInfo info = new DamageInfo(simpleDamage, "Normal", 1.0f, false);
        OnDamage(info);
        //animator.SetTrigger("GetHit");
    }
    
    protected virtual int CalculateDamage(DamageInfo damageInfo)
    {
        float damage = damageInfo.baseDamage * damageInfo.multiplier;
        
        if (!damageInfo.ignoreDefense)
        {
            damage = Mathf.Max(1, damage - defense);
        }
        
        return Mathf.RoundToInt(damage);
    }
    
    protected virtual void ApplyDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth);

        // Only call OnDeath once when entity dies
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            OnDeath();
        }
    }
    
    public virtual void OnHeal(int healAmount)
    {
        if (!IsAlive()) return;
        
        int actualHeal = Mathf.Min(healAmount, maxHealth - currentHealth);
        currentHealth += actualHeal;
        
        OnHealthChanged?.Invoke(currentHealth);
        OnHealed?.Invoke(actualHeal);
    }
    
    protected virtual void OnDeath()
    {
        OnDied?.Invoke();
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0 && !isDead;
    }
    
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }
    
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public int GetLevel() => level;
    public int GetAttackPower() => attackPower;
    public int GetDefense() => defense;
}