using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PhaseHeartUI : MonoBehaviour
{
    [Header("Heart Container Settings")]
    [SerializeField] private List<GameObject> heartContainers = new List<GameObject>(8);
    //[SerializeField] private string activeHeartImageName = "mini-heart_0";
    //[SerializeField] private string inactiveHeartImageName = "mini-heart_1";

    [Header("Heart Sprites")]
    [SerializeField] private Sprite activeHeartSprite;   // mini-heart_0 (drag from HealthHeartSystem/Graphics)
    [SerializeField] private Sprite inactiveHeartSprite; // mini-heart_1 (drag from HealthHeartSystem/Graphics)

    [Header("Phase Status")]
    [SerializeField] private int totalPhases;
    [SerializeField] private int currentPhase;
    [SerializeField] private int remainingPhases;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private string statusMessage;

    private MonsterManager monsterManager;

    void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    System.Collections.IEnumerator InitializeWhenReady()
    {
        // Wait for MonsterManager to be ready
        while (monsterManager == null)
        {
            monsterManager = FindFirstObjectByType<MonsterManager>();
            yield return null;
        }

        // Wait one more frame to ensure MonsterManager has loaded CSV data
        yield return null;

        if (activeHeartSprite == null || inactiveHeartSprite == null)
        {
            Debug.LogWarning("[PhaseHeartUI] Heart sprites not assigned in Inspector! Please assign mini-heart_0 and mini-heart_1 sprites from HealthHeartSystem/Graphics folder.");
        }

        InitializeHearts();

        if (monsterManager.OnPhaseChanged != null)
        {
            monsterManager.OnPhaseChanged.AddListener(OnPhaseChangedEvent);
        }

        if (monsterManager.OnMonsterDead != null)
        {
            monsterManager.OnMonsterDead.AddListener(OnMonsterDead);
        }

        if (debugMode)
        {
            Debug.Log($"[PhaseHeartUI] Initialized with {heartContainers.Count} heart containers");
        }
    }


    void InitializeHearts()
    {
        if (monsterManager == null) return;

        totalPhases = monsterManager.GetTotalPhases();
        currentPhase = monsterManager.GetPhase();
        remainingPhases = totalPhases - currentPhase + 1;

        Debug.Log($"[PhaseHeartUI] MonsterManager data: Total={totalPhases}, Current={currentPhase}, Remaining={remainingPhases}");

        for (int i = 0; i < heartContainers.Count; i++)
        {
            if (heartContainers[i] == null) continue;

            if (i < totalPhases)
            {
                heartContainers[i].SetActive(true);
                Debug.Log($"[PhaseHeartUI] Heart {i} activated (within totalPhases)");

                // Hearts are deactivated from right to left
                // So we check from the end
                int phasesCompleted = currentPhase - 1;
                if (i >= totalPhases - phasesCompleted)
                {
                    // This heart represents a completed phase
                    SetHeartActive(heartContainers[i], false);
                    Debug.Log($"[PhaseHeartUI] Heart {i} set to INACTIVE (completed phase)");
                }
                else
                {
                    // This heart represents an active or future phase
                    SetHeartActive(heartContainers[i], true);
                    Debug.Log($"[PhaseHeartUI] Heart {i} set to ACTIVE (active/future phase)");
                }
            }
            else
            {
                heartContainers[i].SetActive(false);
                Debug.Log($"[PhaseHeartUI] Heart {i} deactivated (beyond totalPhases)");
            }
        }

        statusMessage = $"Initialized - Total: {totalPhases}, Current: {currentPhase}, Remaining: {remainingPhases}";

        Debug.Log($"[PhaseHeartUI] {statusMessage}");

        if (debugMode)
        {
            Debug.Log($"[PhaseHeartUI] Hearts initialized: Total={totalPhases}, Current={currentPhase}");
        }
    }

    void OnPhaseChangedEvent(int newPhase)
    {
        OnPhaseChanged(currentPhase, newPhase);
    }

    void OnMonsterDead()
    {
        Debug.Log("[PhaseHeartUI] Monster died - deactivating all hearts");

        // Deactivate all hearts when monster dies
        for (int i = 0; i < heartContainers.Count; i++)
        {
            if (heartContainers[i] == null) continue;

            if (i < totalPhases)
            {
                SetHeartActive(heartContainers[i], false);
            }
        }

        statusMessage = "Monster Defeated - All Hearts Inactive";
    }

    void OnPhaseChanged(int oldPhase, int newPhase)
    {
        currentPhase = newPhase;
        remainingPhases = totalPhases - currentPhase + 1;

        UpdateHeartDisplay();

        statusMessage = $"Phase changed from {oldPhase} to {newPhase} - Remaining: {remainingPhases}";

        if (debugMode)
        {
            Debug.Log($"[PhaseHeartUI] {statusMessage}");
        }
    }

    void UpdateHeartDisplay()
    {
        for (int i = 0; i < totalPhases && i < heartContainers.Count; i++)
        {
            if (heartContainers[i] == null) continue;

            // Hearts are deactivated from right to left
            // So we check from the end
            int phasesCompleted = currentPhase - 1;
            if (i >= totalPhases - phasesCompleted)
            {
                // This heart represents a completed phase
                SetHeartActive(heartContainers[i], false);
            }
            else
            {
                // This heart represents an active or future phase
                SetHeartActive(heartContainers[i], true);
            }
        }
    }

    void SetHeartActive(GameObject heartContainer, bool isActive)
    {
        if (heartContainer == null) return;

        Image containerImage = heartContainer.GetComponent<Image>();
        if (containerImage != null)
        {
            if (inactiveHeartSprite != null)
            {
                containerImage.sprite = isActive ? null : inactiveHeartSprite;
                containerImage.enabled = !isActive;
            }
        }

        Transform heartFill = heartContainer.transform.Find("HeartFill");
        if (heartFill != null)
        {
            Image fillImage = heartFill.GetComponent<Image>();
            if (fillImage != null && activeHeartSprite != null)
            {
                fillImage.sprite = activeHeartSprite;
            }
            heartFill.gameObject.SetActive(isActive);
        }

        if (debugMode)
        {
            Debug.Log($"[PhaseHeartUI] Set heart {heartContainer.name} to {(isActive ? "Active" : "Inactive")}");
        }
    }

    public void RefreshDisplay()
    {
        InitializeHearts();
    }

    [ContextMenu("Test Phase 1")]
    void TestPhase1()
    {
        if (Application.isPlaying)
        {
            OnPhaseChanged(currentPhase, 1);
        }
    }

    [ContextMenu("Test Phase 2")]
    void TestPhase2()
    {
        if (Application.isPlaying)
        {
            OnPhaseChanged(currentPhase, 2);
        }
    }

    [ContextMenu("Test Phase 3")]
    void TestPhase3()
    {
        if (Application.isPlaying)
        {
            OnPhaseChanged(currentPhase, 3);
        }
    }

    [ContextMenu("Force Refresh")]
    void ForceRefresh()
    {
        if (Application.isPlaying)
        {
            RefreshDisplay();
        }
    }

    void OnValidate()
    {
        if (heartContainers != null)
        {
            for (int i = 0; i < heartContainers.Count; i++)
            {
                if (heartContainers[i] == null) continue;
                heartContainers[i].name = $"HeartContainer_{i + 1}";
            }
        }
    }

    void OnDestroy()
    {
        if (monsterManager != null)
        {
            if (monsterManager.OnPhaseChanged != null)
            {
                monsterManager.OnPhaseChanged.RemoveListener(OnPhaseChangedEvent);
            }
            if (monsterManager.OnMonsterDead != null)
            {
                monsterManager.OnMonsterDead.RemoveListener(OnMonsterDead);
            }
        }
    }
}