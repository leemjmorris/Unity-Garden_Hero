using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class PlayerManager : LivingEntity
{
    [Header("Player Settings")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private int experience = 0;

    [Header("God Mode")]
    [SerializeField] public Button godModeButton;
    [SerializeField] private bool isGodMode = false;

    [Header("Damage Multipliers - Monster Attack")]
    [SerializeField]
    private Dictionary<string, float> noteTypeMultipliers = new Dictionary<string, float>
    {
        {"Normal", 1.0f},
        {"Long", 1.2f},
        {"Special", 1.5f},
        {"Defense", 0.8f},
        {"Dodge", 2.0f}  // LMJ: Highest damage for dodge notes
    };

    void Start()
    {
        InitializePlayer();
        SetupGodModeButton();
    }

    void InitializePlayer()
    {
        // LMJ: Initialize player with base stats
        Initialize(1, 15, 1, 100);
    }

    void SetupGodModeButton()
    {
        if (godModeButton != null)
        {
            // LMJ: Clear existing listeners before adding
            godModeButton.onClick.RemoveAllListeners();
            godModeButton.onClick.AddListener(ToggleGodMode);
            UpdateGodModeButtonText();
        }
    }

    void ToggleGodMode()
    {
        isGodMode = !isGodMode;
        UpdateGodModeButtonText();

        Debug.Log($"God Mode: {(isGodMode ? "ON" : "OFF")}");
    }

    void UpdateGodModeButtonText()
    {
        if (godModeButton != null)
        {
            var buttonText = godModeButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isGodMode ? "GOD MODE: ON" : "GOD MODE: OFF";
                buttonText.color = isGodMode ? Color.green : Color.white;
            }
        }
    }

    public override void OnDamage(DamageInfo damageInfo)
    {
        // LMJ: Check god mode before taking damage
        if (isGodMode)
        {
            Debug.Log("God Mode Active - No damage taken!");
            return;
        }

        base.OnDamage(damageInfo);
    }

    public void ProcessNoteResult(int monsterAttack, string noteType, JudgmentResult judgment)
    {
        bool shouldTakeDamage = ShouldPlayerTakeDamage(noteType, judgment);

        if (shouldTakeDamage)
        {
            TakeDamageFromMiss(monsterAttack, noteType);
        }
    }

    bool ShouldPlayerTakeDamage(string noteType, JudgmentResult judgment)
    {
        if (noteType == "Dodge")
        {
            // LMJ: Dodge note - take damage when hit (Perfect/Good)
            return judgment == JudgmentResult.Perfect || judgment == JudgmentResult.Good;
        }
        else
        {
            // LMJ: Normal notes - take damage when missed
            return judgment == JudgmentResult.Miss;
        }
    }

    void TakeDamageFromMiss(int monsterAttack, string noteType)
    {
        DamageInfo damageInfo = new DamageInfo
        {
            baseDamage = monsterAttack,
            noteType = noteType,
            multiplier = GetDamageMultiplier(noteType),
            ignoreDefense = false
        };

        OnDamage(damageInfo);

        // LMJ: Log damage reason
        if (noteType == "Dodge")
        {
            Debug.Log($"Player failed to dodge! Took {CalculateDamage(damageInfo)} damage.");
        }
        else
        {
            Debug.Log($"Player missed {noteType} note! Took {CalculateDamage(damageInfo)} damage.");
        }
    }

    float GetDamageMultiplier(string noteType)
    {
        return noteTypeMultipliers.ContainsKey(noteType) ?
            noteTypeMultipliers[noteType] : 1.0f;
    }

    protected override int CalculateDamage(DamageInfo damageInfo)
    {
        // LMJ: Calculate damage with note type multiplier
        float totalDamage = damageInfo.baseDamage * damageInfo.multiplier;

        // LMJ: Apply defense reduction
        if (!damageInfo.ignoreDefense)
        {
            totalDamage = Mathf.Max(1, totalDamage - defense);
        }

        return Mathf.RoundToInt(totalDamage);
    }

    protected override void OnDeath()
    {
        Debug.Log($"{playerName} has been defeated! Game Over!");

        // LMJ: Handle game over logic
        HandleGameOver();

        base.OnDeath();
    }

    void HandleGameOver()
    {
        // LMJ: Stop game, show game over screen, etc.
        Time.timeScale = 0f;
        // TODO: Show game over UI
    }

    public void GainExperience(int exp)
    {
        experience += exp;
        Debug.Log($"Player gained {exp} experience! Total: {experience}");

        // LMJ: Check for level up
        CheckLevelUp();
    }

    void CheckLevelUp()
    {
        int requiredExp = level * 100;
        if (experience >= requiredExp)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        level++;
        experience -= (level - 1) * 100;

        // LMJ: Increase stats on level up
        int newAttack = attackPower + 3;
        int newDefense = defense + 1;
        int newMaxHealth = maxHealth + 20;

        attackPower = newAttack;
        defense = newDefense;
        maxHealth = newMaxHealth;
        currentHealth = maxHealth; // LMJ: Full heal on level up

        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"Level Up! Player is now level {level}");
    }

    public void RestoreHealth(int healAmount)
    {
        OnHeal(healAmount);
    }

    public string GetPlayerName() => playerName;
    public int GetExperience() => experience;
    public bool GetGodMode() => isGodMode;

    void OnDestroy()
    {
        if (godModeButton != null)
        {
            godModeButton.onClick.RemoveAllListeners();
        }
    }
}