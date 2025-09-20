using UnityEngine;
using System.Collections.Generic;

public class MonsterManager : LivingEntity
{
    [Header("Monster Settings")]
    [SerializeField] private string monsterName = "Wolf";
    [SerializeField] private int phase = 1;

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
    }

    void InitializeFromTable()
    {
        // LMJ: Initialize with sample boss data from table
        Initialize(1, 10, 2, 50);
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

        OnDamage(damageInfo);
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
        // LMJ: Example: Wolf has max 3 phases
        return phase < 3;
    }

    void AdvancePhase()
    {
        phase++;

        // LMJ: Increase stats for next phase
        int newAttack = attackPower + 5;
        int newDefense = defense + 1;
        int newHealth = maxHealth + 20;

        Initialize(level, newAttack, newDefense, newHealth);

        Debug.Log($"{monsterName} advanced to Phase {phase}!");
    }

    void HandleMonsterDeath()
    {
        // LMJ: Reward experience, items, etc.
        Debug.Log($"{monsterName} completely defeated! Victory!");

        // LMJ: Disable monster or trigger victory screen
        gameObject.SetActive(false);
    }

    public string GetMonsterName() => monsterName;
    public int GetPhase() => phase;
}