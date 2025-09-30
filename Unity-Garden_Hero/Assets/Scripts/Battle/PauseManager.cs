using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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

    private bool isPaused = false;
    private float currentSpeedMultiplier = 4.0f; // Default: 4x (noteSpeed = 800)
    private const float MIN_SPEED = 1.0f;
    private const float MAX_SPEED = 8.0f;
    private const float SPEED_STEP = 0.5f;
    private const float SPEED_TO_NOTE_SPEED_RATIO = 200f; // noteSpeed = multiplier * 200

    void Start()
    {
        // Initialize pause panel as inactive
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Setup button listeners
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OpenPauseMenu);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePauseMenu);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartBattle);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

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
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        Time.timeScale = 0f;
        isPaused = true;

        Debug.Log("[PauseManager] Game paused");
    }

    public void ClosePauseMenu()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
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
}