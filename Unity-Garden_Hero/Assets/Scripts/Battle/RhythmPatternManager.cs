using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class RhythmNoteData
{
    public float time;
    public string direction;
    public string type = "Normal";
    public float duration = 0f;
}

[System.Serializable]
public class RhythmPattern
{
    public string patternName;
    public float bpm = 120f;
    public List<RhythmNoteData> notes;
}

public class RhythmPatternManager : MonoBehaviour
{
    [Header("Pattern Settings")]
    [SerializeField] private List<TextAsset> patternJsonList = new List<TextAsset>();
    [SerializeField] private RhythmGameSystem gameSystem;

    [Header("Timing Settings")]
    [SerializeField] private float startDelay = 3f;
    [SerializeField] private float lookAheadTime = 3f;

    [Header("Playbook Settings")]
    [SerializeField] private bool randomOrder = true;

    private List<RhythmPattern> allPatterns = new List<RhythmPattern>();
    private List<RhythmNote> allNotes = new List<RhythmNote>();
    private float currentSetStartTime = 0f;
    private float currentSetEndTime = 0f;
    private bool gameStarted = false;

    void Start()
    {
        // LMJ: Initialize random seed for true randomization
        Random.InitState(System.DateTime.Now.Millisecond + System.DateTime.Now.Second * 1000);

        if (gameSystem == null)
            gameSystem = GetComponent<RhythmGameSystem>();

        LoadAllPatterns();
        GenerateInitialPatternSet();
        StartGame();
        StartCoroutine(MonitorForNextPatternSet());
    }

    void LoadAllPatterns()
    {
        allPatterns.Clear();

        foreach (TextAsset patternJson in patternJsonList)
        {
            if (patternJson != null)
            {
                RhythmPattern pattern = JsonUtility.FromJson<RhythmPattern>(patternJson.text);
                if (pattern != null)
                {
                    allPatterns.Add(pattern);
                }
            }
        }
    }

    void GenerateInitialPatternSet()
    {
        if (allPatterns.Count == 0) return;

        currentSetStartTime = startDelay;
        CreateFullPatternSet();
    }

    // LMJ: Create all patterns as one seamless connected score
    void CreateFullPatternSet()
    {
        List<int> shuffledIndices = GetShuffledPatternIndices();
        float currentPatternStartTime = currentSetStartTime;


        foreach (int patternIndex in shuffledIndices)
        {
            RhythmPattern pattern = allPatterns[patternIndex];
            
            // LMJ: Create notes for this pattern at calculated start time
            CreateNotesForPatternAtTime(pattern, currentPatternStartTime);
            
            // LMJ: Calculate when next pattern should start (seamlessly)
            float patternDuration = GetPatternDuration(pattern);
            
            // LMJ: No gap - seamless connection
            currentPatternStartTime += patternDuration;
        }

        // LMJ: Record when this entire set will end
        currentSetEndTime = currentPatternStartTime;
    }

    List<int> GetShuffledPatternIndices()
    {
        List<int> indices = new List<int>();

        for (int i = 0; i < allPatterns.Count; i++)
        {
            indices.Add(i);
        }

        if (randomOrder)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                int randomIndex = Random.Range(i, indices.Count);
                int temp = indices[i];
                indices[i] = indices[randomIndex];
                indices[randomIndex] = temp;
            }
        }

        return indices;
    }

    void CreateNotesForPatternAtTime(RhythmPattern pattern, float patternStartTime)
    {
        if (pattern == null || pattern.notes == null) return;

        foreach (var noteData in pattern.notes)
        {
            // LMJ: Calculate absolute hit time (pattern start + note time)
            float actualHitTime = patternStartTime + noteData.time;

            // Use new CreateNoteObject method with JSON data
            GameObject noteObj = gameSystem.CreateNoteObject(noteData.direction, noteData.type, actualHitTime, noteData.duration);
            if (noteObj != null)
            {
                RhythmNote note = noteObj.GetComponent<RhythmNote>();
                // Note: Initialize is now called inside CreateNoteObject, so we don't need to call it again

                allNotes.Add(note);
            }
        }
    }

    float GetPatternDuration(RhythmPattern pattern)
    {
        if (pattern == null || pattern.notes == null || pattern.notes.Count == 0)
            return 2f;

        float maxTime = 0f;
        foreach (var noteData in pattern.notes)
        {
            float noteEndTime = noteData.time + noteData.duration;
            if (noteEndTime > maxTime)
                maxTime = noteEndTime;
        }

        return maxTime;
    }

    void StartGame()
    {
        float gameStartTime = NoteTimeManager.Instance.GetNoteTime();
        gameSystem.StartGame(gameStartTime, allNotes);
        gameStarted = true;
    }

    IEnumerator MonitorForNextPatternSet()
    {
        bool isGeneratingNext = false;

        while (gameStarted)
        {
            yield return new WaitForSeconds(1f);

            // Skip if GameManager is not in Playing state
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null && gameManager.GetCurrentState() != GameState.Playing)
            {
                isGeneratingNext = false; // Reset flag when not playing
                continue;
            }

            float currentGameTime = NoteTimeManager.Instance.GetNoteTime();
            float timeUntilSetEnd = currentSetEndTime - currentGameTime;

            // LMJ: When approaching the end of current set, generate new set seamlessly
            if (timeUntilSetEnd <= lookAheadTime * 2f && timeUntilSetEnd > 0f && !isGeneratingNext)
            {
                isGeneratingNext = true; // Set flag to prevent duplicate generation

                List<RhythmNote> previousNotes = new List<RhythmNote>(allNotes);
                int previousCount = allNotes.Count;

                // LMJ: Set new start time to continue seamlessly from current set end
                currentSetStartTime = currentSetEndTime;
                CreateFullPatternSet();

                List<RhythmNote> newNotes = allNotes.GetRange(previousCount, allNotes.Count - previousCount);
                gameSystem.AddNotes(newNotes);


                // LMJ: Wait longer to prevent multiple generations
                yield return new WaitForSeconds(lookAheadTime);
                isGeneratingNext = false; // Reset flag after waiting
            }
        }
    }

    [ContextMenu("Add Next Pattern Set Now")]
    public void AddNextPatternSetNow()
    {
        List<RhythmNote> previousNotes = new List<RhythmNote>(allNotes);
        int previousCount = allNotes.Count;

        // LMJ: Set new start time to continue seamlessly from current set end
        currentSetStartTime = currentSetEndTime;
        CreateFullPatternSet();

        List<RhythmNote> newNotes = allNotes.GetRange(previousCount, allNotes.Count - previousCount);
        gameSystem.AddNotes(newNotes);
    }

    // LMJ: Stop the pattern generation system
    public void StopPatternGeneration()
    {
        gameStarted = false;
        StopAllCoroutines();
        Debug.Log("[RhythmPatternManager] Pattern generation stopped");
    }

    // LMJ: Restart the pattern generation system
    public void RestartPatternGeneration()
    {
        StopAllCoroutines(); // Stop any existing coroutines first
        gameStarted = true;
        StartCoroutine(MonitorForNextPatternSet());
        Debug.Log("[RhythmPatternManager] Pattern generation restarted");
    }

    // LMJ: New method for DealingTime recovery - start from current time immediately
    public void AddNextPatternSetFromCurrentTime()
    {
        // LMJ: Clear all existing notes first to prevent overlapping
        allNotes.Clear();

        // LMJ: Start immediately from current game time (no delay)
        float currentGameTime = NoteTimeManager.Instance.GetNoteTime() - gameSystem.GameStartTime;
        currentSetStartTime = currentGameTime + 1f; // LMJ: Small buffer for smooth transition

        CreateFullPatternSet();

        // LMJ: Add all newly created notes
        gameSystem.AddNotes(allNotes);

    }

    void OnDestroy()
    {
        StopAllCoroutines();
        gameStarted = false;
        allNotes.Clear();
        allPatterns.Clear();
    }
}