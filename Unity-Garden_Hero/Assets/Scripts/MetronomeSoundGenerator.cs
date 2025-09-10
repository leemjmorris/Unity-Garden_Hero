using UnityEngine;

public class MetronomeSoundGenerator : MonoBehaviour
{
    [Header("Metronome Sound Settings")]
    public int sampleRate = 44100;
    public float frequency = 800f;        // 일반 박자 주파수
    public float accentFrequency = 1000f; // 강박 주파수
    public float duration = 0.1f;         // 클릭 지속시간
    
    private AudioClip clickSound;
    private AudioClip accentSound;
    
    void Start()
    {
        GenerateMetronomeClicks();
    }
    
    void GenerateMetronomeClicks()
    {
        // 일반 클릭 사운드 생성
        clickSound = GenerateClickSound(frequency);
        clickSound.name = "MetronomeClick";
        
        // 강박 클릭 사운드 생성
        accentSound = GenerateClickSound(accentFrequency);
        accentSound.name = "MetronomeAccent";
        
        // AudioManager에 자동으로 할당
        GameObject audioManagerObj = GameObject.FindWithTag("AudioManager");
        if (audioManagerObj != null)
        {
            AudioManager audioManager = audioManagerObj.GetComponent<AudioManager>();
            if (audioManager != null && audioManager.metronomeClickSound == null)
            {
                audioManager.metronomeClickSound = clickSound;
            }
        }
    }
    
    AudioClip GenerateClickSound(float freq)
    {
        int samples = Mathf.RoundToInt(sampleRate * duration);
        AudioClip clip = AudioClip.Create("MetronomeClick", samples, 1, sampleRate, false);
        
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float amplitude = Mathf.Exp(-t * 10f); // 감쇠 효과
            data[i] = amplitude * Mathf.Sin(2 * Mathf.PI * freq * t);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    public AudioClip GetClickSound(bool isAccent = false)
    {
        return isAccent ? accentSound : clickSound;
    }
}