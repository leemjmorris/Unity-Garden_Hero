using UnityEngine;

public class NoteTimeManager : MonoBehaviour
{
    private static NoteTimeManager instance;
    public static NoteTimeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<NoteTimeManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("NoteTimeManager");
                    instance = go.AddComponent<NoteTimeManager>();
                }
            }
            return instance;
        }
    }

    private bool isPaused = false;
    private float gameTime = 0f;
    private float pauseStartTime = 0f;
    private float totalPausedTime = 0f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!isPaused)
        {
            gameTime += Time.deltaTime;
        }
    }

    public void PauseNoteTime()
    {
        if (!isPaused)
        {
            isPaused = true;
            pauseStartTime = Time.time;
        }
    }

    public void ResumeNoteTime()
    {
        if (isPaused)
        {
            isPaused = false;
            totalPausedTime += Time.time - pauseStartTime;
        }
    }

    public float GetNoteTime()
    {
        return gameTime;
    }

    public float GetNoteDeltaTime()
    {
        return isPaused ? 0f : Time.deltaTime;
    }

    public bool IsNotePaused()
    {
        return isPaused;
    }

    // LMJ: Convert real time to note time
    public float RealTimeToNoteTime(float realTime)
    {
        return realTime - totalPausedTime;
    }

    // LMJ: Reset all time-related states for game restart
    public void ResetForRestart()
    {
        isPaused = false;
        gameTime = 0f;
        pauseStartTime = 0f;
        totalPausedTime = 0f;
    }
}