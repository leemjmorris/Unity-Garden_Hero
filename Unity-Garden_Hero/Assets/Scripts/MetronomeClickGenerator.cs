using UnityEngine;

public class MetronomeClickGenerator : MonoBehaviour
{
    void Awake()
    {
        // LMJ: Generate metronome click sound
        AudioManager audioManager = GetComponent<AudioManager>();
        if (audioManager != null && audioManager.metronomeClickSound == null)
        {
            audioManager.metronomeClickSound = CreateClickSound();
            Debug.Log("Metronome click sound generated");
        }
    }
    
    AudioClip CreateClickSound()
    {
        int sampleRate = 44100;
        float frequency = 800f;
        float duration = 0.05f;
        
        int samples = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("MetronomeClick", samples, 1, sampleRate, false);
        
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t);
            // LMJ: Apply envelope
            data[i] *= Mathf.Exp(-t * 20f);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
}