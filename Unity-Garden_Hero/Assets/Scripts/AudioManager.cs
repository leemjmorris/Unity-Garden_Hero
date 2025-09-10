using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Components")]
    public AudioSource audioSource;
    public AudioSource metronomeSource;
    
    [Header("UI Controls")]
    public Button playButton;
    public Button pauseButton;
    public Button stopButton;
    public Slider timeSlider;
    public Slider speedSlider;
    public TextMeshProUGUI currentTimeText;
    public TextMeshProUGUI totalTimeText;
    public TextMeshProUGUI speedText;
    
    [Header("Loop Controls")]
    public Button setLoopStartButton;
    public Button setLoopEndButton;
    public Button toggleLoopButton;
    public TextMeshProUGUI loopInfoText;
    
    [Header("Metronome Controls")]
    public Toggle toggleMetronomeButton;
    public Slider bpmSlider;
    public TextMeshProUGUI bpmText;
    public AudioClip metronomeClickSound;
    public Toggle visualMetronomeToggle;
    public Image metronomeVisualIndicator;
    
    private float loopStartTime = 0f;
    private float loopEndTime = 0f;
    private bool isLooping = false;
    private bool isDraggingSlider = false;
    
    // Metronome variables
    private bool isMetronomeEnabled = false;
    private bool showVisualMetronome = true;
    private float bpm = 120f;
    private float nextBeatTime = 0f;
    private float beatInterval = 0.5f; // seconds per beat
    private int beatCount = 0;
    
    public float CurrentTime => audioSource.time;
    public float TotalTime => audioSource.clip ? audioSource.clip.length : 0f;
    public bool IsPlaying => audioSource.isPlaying;
    public float BPM => bpm;
    public bool IsMetronomeEnabled => isMetronomeEnabled;
    
    void Start()
    {
        SetupUI();
    }
    
    void Update()
    {
        UpdateUI();
        HandleLooping();
        HandleMetronome();
    }
    
    void SetupUI()
    {
        playButton.onClick.AddListener(Play);
        pauseButton.onClick.AddListener(Pause);
        stopButton.onClick.AddListener(Stop);
        
        setLoopStartButton.onClick.AddListener(SetLoopStart);
        setLoopEndButton.onClick.AddListener(SetLoopEnd);
        toggleLoopButton.onClick.AddListener(ToggleLoop);
        
        // Metronome setup
        toggleMetronomeButton.onValueChanged.AddListener(OnMetronomeToggleChanged);
        bpmSlider.onValueChanged.AddListener(OnBPMChanged);
        visualMetronomeToggle.onValueChanged.AddListener(OnVisualMetronomeToggled);
        
        timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
        speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
        
        speedSlider.minValue = 0.25f;
        speedSlider.maxValue = 2f;
        speedSlider.value = 1f;
        
        // BPM slider setup
        bpmSlider.minValue = 60f;
        bpmSlider.maxValue = 200f;
        bpmSlider.value = bpm;
        
        // Setup metronome audio source
        if (metronomeSource == null)
        {
            metronomeSource = gameObject.AddComponent<AudioSource>();
        }
        metronomeSource.clip = metronomeClickSound;
        metronomeSource.volume = 0.5f;
        
        UpdateBPMDisplay();
        UpdateMetronomeUI();
    }
    
    public void LoadAudioClip(AudioClip clip)
    {
        audioSource.clip = clip;
        timeSlider.maxValue = clip.length;
        loopEndTime = clip.length;
        UpdateLoopInfo();
    }
    
    public void Play()
    {
        audioSource.Play();
        if (isMetronomeEnabled)
        {
            ResetMetronome();
        }
    }
    
    public void Pause()
    {
        audioSource.Pause();
    }
    
    public void Stop()
    {
        audioSource.Stop();
        audioSource.time = 0f;
        if (isMetronomeEnabled)
        {
            ResetMetronome();
        }
    }
    
    public void SetLoopStart()
    {
        loopStartTime = audioSource.time;
        UpdateLoopInfo();
    }
    
    public void SetLoopEnd()
    {
        loopEndTime = audioSource.time;
        UpdateLoopInfo();
    }
    
    public void ToggleLoop()
    {
        isLooping = !isLooping;
        UpdateLoopInfo();
    }
    
    // Metronome functions
    public void OnMetronomeToggleChanged(bool value)
    {
        isMetronomeEnabled = value;
        UpdateMetronomeUI();

        if (isMetronomeEnabled && IsPlaying)
        {
            ResetMetronome();
        }
    }

    public void ToggleMetronome()
    {
        isMetronomeEnabled = !isMetronomeEnabled;
        UpdateMetronomeUI();
        
        if (isMetronomeEnabled && IsPlaying)
        {
            ResetMetronome();
        }
    }
    
    public void OnBPMChanged(float value)
    {
        bpm = value;
        beatInterval = 60f / bpm;
        UpdateBPMDisplay();
        
        if (isMetronomeEnabled)
        {
            ResetMetronome();
        }
    }
    
    public void OnVisualMetronomeToggled(bool value)
    {
        showVisualMetronome = value;
    }
    
    void ResetMetronome()
    {
        nextBeatTime = Time.time;
        beatCount = 0;
    }
    
    void HandleMetronome()
    {
        if (isMetronomeEnabled && IsPlaying && Time.time >= nextBeatTime)
        {
            PlayMetronomeBeat();
            nextBeatTime += beatInterval;
            beatCount++;
        }
        
        // Visual metronome fade out
        if (showVisualMetronome && metronomeVisualIndicator != null)
        {
            float timeSinceLastBeat = (Time.time - (nextBeatTime - beatInterval));
            float alpha = Mathf.Clamp01(1f - (timeSinceLastBeat / (beatInterval * 0.3f)));
            Color color = metronomeVisualIndicator.color;
            color.a = alpha;
            metronomeVisualIndicator.color = color;
        }
    }
    
    void PlayMetronomeBeat()
    {
        if (metronomeSource && metronomeClickSound)
        {
            // 4/4박자에서 첫 박은 더 강하게
            metronomeSource.volume = (beatCount % 4 == 0) ? 0.8f : 0.5f;
            metronomeSource.pitch = (beatCount % 4 == 0) ? 1.2f : 1.0f;
            metronomeSource.PlayOneShot(metronomeClickSound);
        }
        
        // Visual feedback
        if (showVisualMetronome && metronomeVisualIndicator != null)
        {
            Color color = (beatCount % 4 == 0) ? Color.red : Color.yellow;
            color.a = 1f;
            metronomeVisualIndicator.color = color;
        }
    }
    
    void UpdateMetronomeUI()
    {
        Color buttonColor = isMetronomeEnabled ? Color.green : Color.white;
        toggleMetronomeButton.GetComponent<Image>().color = buttonColor;
        
        Text buttonText = toggleMetronomeButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = isMetronomeEnabled ? "메트로놈 OFF" : "메트로놈 ON";
        }
    }
    
    void UpdateBPMDisplay()
    {
        if (bpmText != null)
        {
            bpmText.text = $"BPM: {bpm:F0}";
        }
    }
    
    void OnTimeSliderChanged(float value)
    {
        if (isDraggingSlider)
        {
            audioSource.time = value;
        }
    }
    
    void OnSpeedSliderChanged(float value)
    {
        audioSource.pitch = value;
        speedText.text = $"속도: {value:F2}x";
    }
    
    void UpdateUI()
    {
        if (!isDraggingSlider && audioSource.clip)
        {
            timeSlider.value = audioSource.time;
        }
        
        currentTimeText.text = FormatTime(audioSource.time);
        totalTimeText.text = FormatTime(TotalTime);
    }
    
    void HandleLooping()
    {
        if (isLooping && audioSource.isPlaying && audioSource.time >= loopEndTime)
        {
            audioSource.time = loopStartTime;
        }
    }
    
    void UpdateLoopInfo()
    {
        string loopStatus = isLooping ? "ON" : "OFF";
        loopInfoText.text = $"구간반복: {loopStatus} ({FormatTime(loopStartTime)} - {FormatTime(loopEndTime)})";
    }
    
    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        int milliseconds = (int)((time % 1) * 100);
        return $"{minutes:D2}:{seconds:D2}:{milliseconds:D2}";
    }
    
    public void OnSliderPointerDown()
    {
        isDraggingSlider = true;
    }
    
    public void OnSliderPointerUp()
    {
        isDraggingSlider = false;
    }
}