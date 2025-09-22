using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class DirectionalShield
{
    public float currentDurability;
    public float maxDurability;
    public bool isActive;
    public bool isDisabled;
    public float disableStartTime;

    public DirectionalShield(float maxDur)
    {
        maxDurability = maxDur;
        currentDurability = maxDur;
        isActive = false;
        isDisabled = false;
        disableStartTime = 0f;
    }

    public float GetDurabilityPercent()
    {
        return currentDurability / maxDurability;
    }

    public void TakeDamage(float damage)
    {
        if (isDisabled) return;
        currentDurability = Mathf.Max(0, currentDurability - damage);
    }

    public void Restore(float amount)
    {
        currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
        if (currentDurability > 0)
        {
            isDisabled = false;
        }
    }

    public void FullRestore()
    {
        currentDurability = maxDurability;
        isDisabled = false;
    }

    public void Disable(float time)
    {
        isDisabled = true;
        disableStartTime = time;
        currentDurability = 0f;
    }
}

[System.Serializable]
public class DirectionInfo
{
    public string displayName;
    public string visualDirection;

    public DirectionInfo(string display, string visual)
    {
        displayName = display;
        visualDirection = visual;
    }
}

public class DirectionalShieldSystem : MonoBehaviour
{
    [Header("Shield Settings")]
    [SerializeField] private float maxDurability = 100f;
    [SerializeField] private float restoreRate = 10f;
    [SerializeField] private float disableDuration = 3f;

    [Header("Damage Settings")]
    [SerializeField] private float perfectHitDamage = 5f;
    [SerializeField] private float goodHitDamage = 3f;
    [SerializeField] private float missDamage = 0f;

    [Header("Visual Settings")]
    [SerializeField] private GameObject[] shieldVisuals = new GameObject[3];
    [SerializeField] private Color fullDurabilityColor = new Color(0.5f, 0.5f, 1f, 1f);
    [SerializeField] private Color halfDurabilityColor = new Color(1f, 1f, 0f, 1f);
    [SerializeField] private Color lowDurabilityColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color brokenColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    [Header("System References")]
    [SerializeField] private DodgeSystem dodgeSystem;
    [SerializeField] private ShieldController shieldController;

    private DirectionalShield[] shields = new DirectionalShield[4];
    private int currentDirection = 0;
    private Renderer[] shieldRenderers = new Renderer[3];

    private static readonly DirectionInfo[] directionInfos = new DirectionInfo[]
    {
        new DirectionInfo("A(0Â°)", "Up"),
        new DirectionInfo("B(90Â°)", "Left"),
        new DirectionInfo("C(180Â°)", "Up"),
        new DirectionInfo("D(270Â°)", "Right")
    };

    void Start()
    {
        InitializeShields();

        if (dodgeSystem == null)
            dodgeSystem = FindFirstObjectByType<DodgeSystem>();

        if (shieldController == null)
            shieldController = FindFirstObjectByType<ShieldController>();
    }

    void InitializeShields()
    {
        // LMJ: Ensure arrays are properly sized
        if (shieldVisuals.Length != 3)
        {
            Debug.LogError("shieldVisuals array must have exactly 3 elements");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            shields[i] = new DirectionalShield(maxDurability);
        }

        // LMJ: Initialize shield visuals (only 3 shields)
        for (int i = 0; i < 3; i++)
        {
            if (shieldVisuals[i] != null)
            {
                shieldRenderers[i] = shieldVisuals[i].GetComponent<Renderer>();
                shieldVisuals[i].SetActive(true);
            }
            else
            {
                Debug.LogWarning($"Shield visual {i} is null!");
            }
        }

        // LMJ: Ensure currentDirection is valid
        currentDirection = Mathf.Clamp(currentDirection, 0, 3);
        shields[currentDirection].isActive = true;
        UpdateAllShieldVisuals();
    }

    void Update()
    {
        UpdateCurrentDirection();
        UpdateShieldRegeneration();
        UpdateDisabledShields();
        UpdateAllShieldVisuals();
        // UpdateParticleEffects(); // 제거 - SetShieldBrokenState가 대신 처리
    }

    private void UpdateParticleEffects()
    {
        if (shieldController == null) return;

        bool shouldShowParticles = shields[currentDirection].currentDurability <= 0;

        // 방패 GameObject는 건드리지 않고 파티클만 제어
        shieldController.SetShieldParticleEffect("Left", shouldShowParticles);
        shieldController.SetShieldParticleEffect("Right", shouldShowParticles);
        shieldController.SetShieldParticleEffect("Up", shouldShowParticles);
    }

    void UpdateCurrentDirection()
    {
        if (dodgeSystem == null) return;

        float rotation = dodgeSystem.GetCurrentRotation();
        int newDirection = GetDirectionFromRotation(rotation);

        if (newDirection != currentDirection)
        {
            shields[currentDirection].isActive = false;
            currentDirection = newDirection;
            shields[currentDirection].isActive = true;
        }
    }

    int GetDirectionFromRotation(float rotation)
    {
        rotation = rotation % 360f;
        if (rotation < 0) rotation += 360f;

        if (rotation >= 315f || rotation < 45f) return 0;
        else if (rotation >= 45f && rotation < 135f) return 1;
        else if (rotation >= 135f && rotation < 225f) return 2;
        else return 3;
    }

    void UpdateShieldRegeneration()
    {
        for (int i = 0; i < 4; i++)
        {
            if (!shields[i].isActive && !shields[i].isDisabled && shields[i].currentDurability < shields[i].maxDurability)
            {
                shields[i].Restore(restoreRate * Time.deltaTime);
            }
        }
    }

    void UpdateDisabledShields()
    {
        if (shields[0].isDisabled)
        {
            float timeSinceDisabled = Time.time - shields[0].disableStartTime;

            if (timeSinceDisabled >= disableDuration)
            {
                for (int i = 0; i < 4; i++)
                {
                    shields[i].FullRestore();
                }

                if (shieldController != null)
                {
                    shieldController.SetShieldBrokenState("Left", false);
                    shieldController.SetShieldBrokenState("Right", false);
                    shieldController.SetShieldBrokenState("Up", false);
                }
            }
        }
    }

    public void ProcessNoteResult(string direction, JudgmentResult result)
    {
        if (result == JudgmentResult.Miss) return;

        float damage = GetDamageAmount(result);

        float previousDurability = shields[currentDirection].currentDurability;

        for (int i = 0; i < 4; i++)
        {
            shields[i].TakeDamage(damage);
        }

        if (shields[currentDirection].currentDurability <= 0 && previousDurability > 0)
        {
            for (int i = 0; i < 4; i++)
            {
                shields[i].Disable(Time.time);
            }

            if (shieldController != null)
            {
                shieldController.SetShieldBrokenState("Left", true);
                shieldController.SetShieldBrokenState("Right", true);
                shieldController.SetShieldBrokenState("Up", true);
            }
        }
    }

    float GetDamageAmount(JudgmentResult result)
    {
        switch (result)
        {
            case JudgmentResult.Perfect: return perfectHitDamage;
            case JudgmentResult.Good: return goodHitDamage;
            case JudgmentResult.Miss: return missDamage;
            default: return 0f;
        }
    }

    public bool IsShieldDisabled(string direction)
    {
        int directionIndex = GetDirectionIndex(direction);
        if (directionIndex >= 0 && directionIndex < 4)
        {
            return shields[directionIndex].isDisabled;
        }
        return false;
    }

    int GetDirectionIndex(string direction)
    {
        switch (direction)
        {
            case "Left": return 1;
            case "Right": return 3;
            case "Up": return 0;
            default: return -1;
        }
    }

    public float GetShieldDurabilityPercent(string direction = "")
    {
        return shields[currentDirection].GetDurabilityPercent();
    }

    public void RestoreAllShields()
    {
        for (int i = 0; i < 4; i++)
        {
            shields[i].FullRestore();
        }
    }

    public void RestoreCurrentShield()
    {
        shields[currentDirection].FullRestore();
    }

    void UpdateAllShieldVisuals()
    {
        // LMJ: Map 4 directions to 3 visual shields
        for (int i = 0; i < 3; i++)
        {
            if (shieldRenderers[i] != null)
            {
                int shieldIndex = GetShieldIndexForVisual(i);
                Color targetColor = GetShieldColor(shields[shieldIndex]);

                if (shieldIndex == currentDirection)
                {
                    targetColor.a = 1f;
                }
                else
                {
                    targetColor.a = 0.7f;
                }

                shieldRenderers[i].material.color = targetColor;
            }
        }
    }

    // LMJ: Map visual index to shield direction
    int GetShieldIndexForVisual(int visualIndex)
    {
        switch (visualIndex)
        {
            case 0: return 1; // Left shield -> direction 1
            case 1: return 3; // Right shield -> direction 3
            case 2: return 0; // Front shield -> direction 0 (or 2)
            default: return 0;
        }
    }

    Color GetShieldColor(DirectionalShield shield)
    {
        if (shield.isDisabled)
        {
            return brokenColor;
        }

        float durabilityPercent = shield.GetDurabilityPercent();

        if (durabilityPercent > 0.7f)
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

    public void LogShieldStatus()
    {
        for (int i = 0; i < 4; i++)
        {
            float percent = shields[i].GetDurabilityPercent() * 100f;
            string status = shields[i].isActive ? "ACTIVE" : "INACTIVE";
            if (shields[i].isDisabled) status = "DISABLED";

            Debug.Log($"Shield {GetDirectionName(i)}: {percent:F1}% ({status})");
        }
    }

    string GetDirectionName(int index)
    {
        if (index >= 0 && index < directionInfos.Length)
            return directionInfos[index].displayName;
        return "Unknown";
    }

    string GetVisualDirection(int directionIndex)
    {
        if (directionIndex >= 0 && directionIndex < directionInfos.Length)
            return directionInfos[directionIndex].visualDirection;
        return "Up";
    }

    public int GetCurrentDirection()
    {
        return currentDirection;
    }

    public DirectionalShield GetCurrentShield()
    {
        return shields[currentDirection];
    }

    public DirectionalShield GetShield(int direction)
    {
        if (direction >= 0 && direction < 4)
            return shields[direction];
        return null;
    }
}