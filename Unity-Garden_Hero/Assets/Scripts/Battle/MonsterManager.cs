using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class MonsterManager : LivingEntity
{
    [Header("Monster Settings")]
    [SerializeField] private string monsterName = "Wolf";
    [SerializeField] private int phase = 1;

    [Header("STUN System")]
    [SerializeField] private int maxStun = 100;
    [SerializeField] private int currentStun = 100;
    [SerializeField] private int[] phaseStunValues = { 100, 120, 150 }; // LMJ: STUN values per phase
    
    [Header("STUN Visual")]
    [SerializeField] private GameObject stunObject; // LMJ: Visual stun GameObject
    
    [Header("STUN Events")]
    public UnityEvent OnStunBroken;
    public UnityEvent<int> OnStunChanged;

    [Header("Damage Multipliers")]
    [SerializeField]
    private Dictionary<string, float> noteTypeMultipliers = new Dictionary<string, float>
    {
        {"Normal", 1.0f},
        {"Long", 1.2f},
        {"Special", 1.5f},
        {"Defense", 0.8f}
    };

    [SerializeField]
    private Dictionary<JudgmentResult, float> judgmentMultipliers = new Dictionary<JudgmentResult, float>
    {
        {JudgmentResult.Perfect, 1.0f},
        {JudgmentResult.Good, 0.7f},
        {JudgmentResult.Miss, 0.0f}
    };

    void Start()
    {
        // LMJ: Only initialize if values haven't been set in inspector
        if (maxHealth == 0)
        {
            InitializeFromTable();
        }
        
        // LMJ: Initialize stun based on current phase
        InitializeStun();
    }

    void InitializeFromTable()
    {
        // LMJ: Initialize with sample boss data from table
        Initialize(1, 10, 2, 50);
    }

    void InitializeStun()
    {
        if (phaseStunValues.Length > 0 && phase > 0 && phase <= phaseStunValues.Length)
        {
            maxStun = phaseStunValues[phase - 1];
        }
        
        currentStun = maxStun;
        OnStunChanged?.Invoke(currentStun);
        
        // LMJ: Show stun visual
        UpdateStunVisual();
    }

    public void TakeNoteHit(int playerAttack, string noteType, JudgmentResult judgment)
    {
        if (judgment == JudgmentResult.Miss) return;

        DamageInfo damageInfo = new DamageInfo
        {
            baseDamage = playerAttack,
            noteType = noteType,
            multiplier = GetDamageMultiplier(noteType, judgment),
            ignoreDefense = false
        };

        // LMJ: Apply damage to stun first, then health
        ApplyDamageToStunAndHealth(damageInfo);
    }

    void ApplyDamageToStunAndHealth(DamageInfo damageInfo)
    {
        int calculatedDamage = CalculateDamage(damageInfo);

        if (currentStun > 0)
        {
            // LMJ: Damage stun first
            int stunDamage = Mathf.Min(currentStun, calculatedDamage);
            currentStun = Mathf.Max(0, currentStun - stunDamage);
            
            OnStunChanged?.Invoke(currentStun);
            // LMJ: Don't trigger OnDamageReceived when only STUN is damaged
            // OnDamageReceived?.Invoke(damageInfo, stunDamage);

            // LMJ: Check if stun is broken
            if (currentStun <= 0)
            {
                Debug.Log($"{monsterName}'s stun is broken! Entering DealingTime...");
                UpdateStunVisual();
                OnStunBroken?.Invoke();
            }
            else
            {
                UpdateStunVisual();
            }
        }
        // LMJ: If stun is broken, damage goes to health (during DealingTime)
        else
        {
            ApplyDamage(calculatedDamage);
        }
    }

    // LMJ: Direct damage to health during DealingTime
    public void TakeDealingTimeDamage(int damage)
    {
        if (currentStun > 0)
        {
            Debug.LogWarning("Cannot deal direct damage while stun is active!");
            return;
        }

        ApplyDamage(damage);
        Debug.Log($"{monsterName} takes {damage} direct damage! Health: {currentHealth}/{maxHealth}");
    }

    protected override int CalculateDamage(DamageInfo damageInfo)
    {
        // LMJ: Calculate base damage with multipliers
        float totalDamage = damageInfo.baseDamage * damageInfo.multiplier;

        // LMJ: Apply defense reduction
        if (!damageInfo.ignoreDefense)
        {
            totalDamage = Mathf.Max(1, totalDamage - defense);
        }

        return Mathf.RoundToInt(totalDamage);
    }

    float GetDamageMultiplier(string noteType, JudgmentResult judgment)
    {
        float noteMultiplier = noteTypeMultipliers.ContainsKey(noteType) ?
            noteTypeMultipliers[noteType] : 1.0f;

        float judgmentMultiplier = judgmentMultipliers.ContainsKey(judgment) ?
            judgmentMultipliers[judgment] : 1.0f;

        return noteMultiplier * judgmentMultiplier;
    }

    public void ResetStun()
    {
        // LMJ: Reset stun to maximum value for current phase
        if (phaseStunValues.Length > 0 && phase > 0 && phase <= phaseStunValues.Length)
        {
            maxStun = phaseStunValues[phase - 1];
        }
        
        currentStun = maxStun;
        OnStunChanged?.Invoke(currentStun);
        
        // LMJ: Show stun visual
        UpdateStunVisual();
        
        Debug.Log($"{monsterName}'s stun restored to {currentStun}!");
    }

    protected override void OnDeath()
    {
        Debug.Log($"{monsterName} has been defeated!");

        // LMJ: Handle phase transition or death
        if (CanAdvancePhase())
        {
            AdvancePhase();
        }
        else
        {
            HandleMonsterDeath();
        }

        base.OnDeath();
    }

    bool CanAdvancePhase()
    {
        // LMJ: Check if there are more phases available
        return phase < phaseStunValues.Length;
    }

    void AdvancePhase()
    {
        phase++;

        // LMJ: Increase stats for next phase
        int newAttack = attackPower + 5;
        int newDefense = defense + 1;
        int newHealth = maxHealth + 20;

        Initialize(level, newAttack, newDefense, newHealth);
        
        // LMJ: Reset stun for new phase
        ResetStun();

        Debug.Log($"{monsterName} advanced to Phase {phase}!");
    }

    void UpdateStunVisual()
    {
        if (stunObject == null) return;

        // LMJ: Show/hide stun based on current stun value
        bool shouldShowStun = currentStun > 0;
        stunObject.SetActive(shouldShowStun);
    }

    void HandleMonsterDeath()
    {
        // LMJ: Reward experience, items, etc.
        Debug.Log($"{monsterName} completely defeated! Victory!");

        // LMJ: Disable monster or trigger victory screen
        gameObject.SetActive(false);
    }

    // LMJ: Getter methods for stun system
    public int GetCurrentStun() => currentStun;
    public int GetMaxStun() => maxStun;
    public float GetStunPercentage() => maxStun > 0 ? (float)currentStun / maxStun : 0f;
    public bool HasStun() => currentStun > 0;

    public string GetMonsterName() => monsterName;
    public int GetPhase() => phase;
}