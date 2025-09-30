using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Audio Settings")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    // [SerializeField] private AudioManager audioManager; // To be connected later when AudioManager is created

    [Header("Note Speed Settings")]
    [SerializeField] private Button noteSpeedDecreaseButton;
    [SerializeField] private Button noteSpeedIncreaseButton;
    [SerializeField] private TextMeshProUGUI noteSpeedText;
    [SerializeField] private RhythmGameSystem rhythmGameSystem;

    [Header("Game Manager")]
    [SerializeField] private GameManager gameManager;

    [Header("Input Systems")]
    [SerializeField] private DodgeSystem dodgeSystem;
    [SerializeField] private TouchInputManager touchInputManager;

    private bool isPaused = false;
    private float currentSpeedMultiplier = 4.0f; // Default: 4x (noteSpeed = 800)
    private const float MIN_SPEED = 1.0f;
    private const float MAX_SPEED = 8.0f;
    private const float SPEED_STEP = 0.5f;
    private const float SPEED_TO_NOTE_SPEED_RATIO = 200f; // noteSpeed = multiplier * 200

    void Start()
    {
        // Find GameManager if not assigned
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        // Find input systems if not assigned
        if (dodgeSystem == null)
        {
            dodgeSystem = FindFirstObjectByType<DodgeSystem>();
        }

        if (touchInputManager == null)
        {
            touchInputManager = FindFirstObjectByType<TouchInputManager>();
        }

        // Initialize pause panel as inactive
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Setup button listeners using PointerUp for safer input handling
        SetupButtonWithPointerUp(pauseButton, OpenPauseMenu);
        SetupButtonWithPointerUp(closeButton, ClosePauseMenu);
        SetupButtonWithPointerUp(restartButton, RestartBattle);
        SetupButtonWithPointerUp(quitButton, QuitGame);

        if (noteSpeedDecreaseButton != null)
            noteSpeedDecreaseButton.onClick.AddListener(DecreaseNoteSpeed);

        if (noteSpeedIncreaseButton != null)
            noteSpeedIncreaseButton.onClick.AddListener(IncreaseNoteSpeed);

        // Setup slider listeners
        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // Initialize note speed display
        UpdateNoteSpeedDisplay();

        // Initialize current note speed from RhythmGameSystem if available
        if (rhythmGameSystem != null)
        {
            currentSpeedMultiplier = rhythmGameSystem.noteSpeed / SPEED_TO_NOTE_SPEED_RATIO;
            UpdateNoteSpeedDisplay();
        }
    }

    void Update()
    {
        // Optional: ESC key to toggle pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ClosePauseMenu();
            else
                OpenPauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        // Reset all input states to prevent ghost inputs
        ResetInputStates();

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // Set GameState to Paused
        if (gameManager != null)
        {
            gameManager.SetGameState(GameState.Paused);
        }

        Time.timeScale = 0f;
        isPaused = true;

        Debug.Log("[PauseManager] Game paused");
    }

    public void ClosePauseMenu()
    {
        // Reset all input states to prevent ghost inputs
        ResetInputStates();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Resume GameState to Playing
        if (gameManager != null)
        {
            gameManager.SetGameState(GameState.Playing);
        }

        Time.timeScale = 1f;
        isPaused = false;

        Debug.Log("[PauseManager] Game resumed");
    }

    public void RestartBattle()
    {
        Debug.Log("[PauseManager] Restarting battle...");

        // Resume time before reloading scene
        Time.timeScale = 1f;

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Debug.Log("[PauseManager] Quitting game...");

        // Resume time before quitting
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void DecreaseNoteSpeed()
    {
        currentSpeedMultiplier -= SPEED_STEP;

        // Clamp to minimum
        if (currentSpeedMultiplier < MIN_SPEED)
        {
            currentSpeedMultiplier = MIN_SPEED;
        }

        ApplyNoteSpeed();
        UpdateNoteSpeedDisplay();

        Debug.Log($"[PauseManager] Note speed decreased to {currentSpeedMultiplier}x");
    }

    private void IncreaseNoteSpeed()
    {
        currentSpeedMultiplier += SPEED_STEP;

        // Clamp to maximum
        if (currentSpeedMultiplier > MAX_SPEED)
        {
            currentSpeedMultiplier = MAX_SPEED;
        }

        ApplyNoteSpeed();
        UpdateNoteSpeedDisplay();

        Debug.Log($"[PauseManager] Note speed increased to {currentSpeedMultiplier}x");
    }

    private void ApplyNoteSpeed()
    {
        if (rhythmGameSystem != null)
        {
            float newNoteSpeed = currentSpeedMultiplier * SPEED_TO_NOTE_SPEED_RATIO;
            rhythmGameSystem.noteSpeed = newNoteSpeed;

            Debug.Log($"[PauseManager] Applied noteSpeed: {newNoteSpeed}");
        }
        else
        {
            Debug.LogWarning("[PauseManager] RhythmGameSystem reference is null!");
        }
    }

    private void UpdateNoteSpeedDisplay()
    {
        if (noteSpeedText != null)
        {
            noteSpeedText.text = $"{currentSpeedMultiplier:F1}x";
        }
    }

    private void OnBGMVolumeChanged(float value)
    {
        // TODO: Connect to AudioManager when available
        // if (audioManager != null)
        // {
        //     audioManager.SetBGMVolume(value);
        // }
        Debug.Log($"[PauseManager] BGM volume changed to {value} (AudioManager not connected yet)");
    }

    private void OnSFXVolumeChanged(float value)
    {
        // TODO: Connect to AudioManager when available
        // if (audioManager != null)
        // {
        //     audioManager.SetSFXVolume(value);
        // }
        Debug.Log($"[PauseManager] SFX volume changed to {value} (AudioManager not connected yet)");
    }

    private void ResetInputStates()
    {
        // Reset DodgeSystem swipe state to prevent ghost swipes
        if (dodgeSystem != null)
        {
            dodgeSystem.ResetSwipeBlock();
            Debug.Log("[PauseManager] Reset DodgeSystem swipe state");
        }

        // Reset TouchInputManager button states (if needed in future)
        // TouchInputManager doesn't have a public reset method yet, but DodgeSystem reset should be sufficient
    }

    // LMJ: Setup button to trigger on PointerUp instead of onClick (safer for pause menu)
    private void SetupButtonWithPointerUp(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;

        // Remove default onClick listener
        button.onClick.RemoveAllListeners();

        // Add EventTrigger for PointerUp
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // Clear existing triggers for this button
        trigger.triggers.Clear();

        // Add PointerUp event
        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => { action.Invoke(); });
        trigger.triggers.Add(pointerUpEntry);
    }
}