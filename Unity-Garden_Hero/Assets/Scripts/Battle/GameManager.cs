using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;

public enum GameState
{
    Playing,
    DealingTime,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Playing;
    
    [Header("DealingTime Settings")]
    [SerializeField] private float dealingTimeDuration = 10f;
    [SerializeField] private float touchDamageMultiplier = 1.5f;
    
    [Header("UI References")]
    [SerializeField] private CanvasGroup gameUICanvasGroup;
    [SerializeField] private GameObject dealingTimeUI;
    [SerializeField] private TextMeshProUGUI dealingTimeTimerText;
    [SerializeField] private TextMeshProUGUI dealingTimeInstructionText;
    [SerializeField] private HealthUIManager monsterHealthUIManager; // LMJ: Monster health UI for reset
    [SerializeField] private HealthUIManager playerHealthUIManager; // LMJ: Player health UI for reset
    [SerializeField] private StunUIManager stunUIManager; // LMJ: Stun UI for reset
    [SerializeField] private LaneShieldDurability shieldDurabilityUI; // LMJ: Shield durability UI for reset
    
    [Header("Camera Feedback Effects")]
    [SerializeField] private MMFeedbacks dealingTimeStartFeedback; // LMJ: Camera effects when entering dealing time
    [SerializeField] private MMFeedbacks dealingTimeEndFeedback; // LMJ: Camera effects when exiting dealing time
    
    [Header("System References")]
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private RhythmGameSystem rhythmGameSystem;
    [SerializeField] private RhythmPatternManager rhythmPatternManager;
    
    private bool isDealingTimeActive = false;
    private float dealingTimeTimer = 0f;

    void Start()
    {
        // LMJ: Subscribe to monster stun broken event
        if (monsterManager != null)
        {
            monsterManager.OnStunBroken.AddListener(StartDealingTime);
        }
        
        SetupUI();
    }

    void SetupUI()
    {
        if (dealingTimeUI != null)
        {
            dealingTimeUI.SetActive(false);
        }
        
        if (dealingTimeInstructionText != null)
        {
            dealingTimeInstructionText.text = "Touch the Boss to Deal Damage!";
        }
    }

    void Update()
    {
        HandleInput();
        UpdateDealingTime();
    }

    void HandleInput()
    {
        if (currentState == GameState.DealingTime && Input.GetMouseButtonDown(0))
        {
            HandleBossTouch();
        }
    }

    void HandleBossTouch()
    {
        if (!isDealingTimeActive || monsterManager == null || playerManager == null) return;

        // LMJ: Calculate touch damage based on player attack power
        int touchDamage = Mathf.RoundToInt(playerManager.GetAttackPower() * touchDamageMultiplier);
        
        // LMJ: Deal direct damage to boss real health
        monsterManager.TakeDealingTimeDamage(touchDamage);
    }

    public void StartDealingTime()
    {
        if (isDealingTimeActive) return;

        currentState = GameState.DealingTime;
        isDealingTimeActive = true;
        dealingTimeTimer = dealingTimeDuration;

        // LMJ: Play camera feedback effects for dealing time start
        if (dealingTimeStartFeedback != null)
        {
            dealingTimeStartFeedback.PlayFeedbacks();
        }

        // LMJ: Hide game UI
        if (gameUICanvasGroup != null)
        {
            gameUICanvasGroup.alpha = 0f;
            gameUICanvasGroup.interactable = false;
        }

        // LMJ: Show dealing time UI
        if (dealingTimeUI != null)
        {
            dealingTimeUI.SetActive(true);
        }

        // LMJ: Delete all existing notes
        PauseAllNotes();

        Debug.Log("DealingTime Started!");
    }

    void PauseAllNotes()
    {
        // LMJ: Find all active notes and immediately destroy them
        RhythmNote[] allNotes = FindObjectsByType<RhythmNote>(FindObjectsSortMode.None);
        
        foreach (RhythmNote note in allNotes)
        {
            if (note != null)
            {
                Destroy(note.gameObject);
            }
        }

        // LMJ: Clear the rhythm game system's note list and disable it
        if (rhythmGameSystem != null)
        {
            rhythmGameSystem.ClearAllNotes();
            rhythmGameSystem.enabled = false;
        }
    }

    void UpdateDealingTime()
    {
        if (!isDealingTimeActive) return;

        dealingTimeTimer -= Time.deltaTime;

        // LMJ: Update timer UI
        if (dealingTimeTimerText != null)
        {
            dealingTimeTimerText.text = $"Time: {dealingTimeTimer:F1}s";
        }

        // LMJ: Check if time is up
        if (dealingTimeTimer <= 0f)
        {
            EndDealingTime();
        }
    }

    void EndDealingTime()
    {
        if (!isDealingTimeActive) return;

        isDealingTimeActive = false;
        currentState = GameState.Playing;

        // LMJ: Show game UI
        if (gameUICanvasGroup != null)
        {
            gameUICanvasGroup.alpha = 1f;
            gameUICanvasGroup.interactable = true;
        }

        // LMJ: Hide dealing time UI
        if (dealingTimeUI != null)
        {
            dealingTimeUI.SetActive(false);
        }

        // LMJ: Resume rhythm game with new notes
        ResumeAllNotes();

        // LMJ: Reset boss stun
        if (monsterManager != null)
        {
            monsterManager.ResetStun();
        }

        Debug.Log("DealingTime Ended! Returning to rhythm game.");
    }

    void ResumeAllNotes()
{
    // LMJ: Resume rhythm game system
    if (rhythmGameSystem != null)
    {
        rhythmGameSystem.enabled = true;
    }

    // LMJ: Generate new notes immediately from current time
    if (rhythmPatternManager != null)
    {
        rhythmPatternManager.AddNextPatternSetFromCurrentTime(); // LMJ: 새로운 메서드 사용
    }
}

    public GameState GetCurrentState() => currentState;
    public bool IsDealingTimeActive() => isDealingTimeActive;
    
    public void SetGameState(GameState newState)
    {
        currentState = newState;
    }

    void OnDestroy()
    {
        // LMJ: Unsubscribe from events
        if (monsterManager != null)
        {
            monsterManager.OnStunBroken.RemoveListener(StartDealingTime);
        }
    }
}