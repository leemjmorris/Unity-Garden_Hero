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

        Debug.Log("=== Creating New Pattern Set ===");

        foreach (int patternIndex in shuffledIndices)
        {
            RhythmPattern pattern = allPatterns[patternIndex];
            
            // LMJ: Create notes for this pattern at calculated start time
            CreateNotesForPatternAtTime(pattern, currentPatternStartTime);
            
            // LMJ: Calculate when next pattern should start (seamlessly)
            float patternDuration = GetPatternDuration(pattern);
            Debug.Log($"Pattern '{pattern.patternName}' placed at time {currentPatternStartTime:F2}, duration: {patternDuration:F2}");
            
            // LMJ: No gap - seamless connection
            currentPatternStartTime += patternDuration;
        }

        // LMJ: Record when this entire set will end
        currentSetEndTime = currentPatternStartTime;
        Debug.Log($"Pattern set ends at time: {currentSetEndTime:F2}");
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
            float spawnDistance = lookAheadTime * gameSystem.noteSpeed;

            GameObject noteObj = gameSystem.CreateNoteObject(noteData.direction, spawnDistance);
            if (noteObj != null)
            {
                RhythmNote note = noteObj.GetComponent<RhythmNote>();
                if (note == null)
                {
                    note = noteObj.AddComponent<RhythmNote>();
                }

                // LMJ: Calculate absolute hit time (pattern start + note time)
                float actualHitTime = patternStartTime + noteData.time;
                note.Initialize(noteData.direction, actualHitTime, noteData.type, gameSystem.noteSpeed, noteData.duration);

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
        while (gameStarted)
        {
            yield return new WaitForSeconds(1f);

            float currentGameTime = NoteTimeManager.Instance.GetNoteTime();
            float timeUntilSetEnd = currentSetEndTime - currentGameTime;

            // LMJ: When approaching the end of current set, generate new set seamlessly
            if (timeUntilSetEnd <= lookAheadTime * 2f && timeUntilSetEnd > 0f)
            {
                Debug.Log($"Generating next pattern set. Time until current set ends: {timeUntilSetEnd:F2}");
                
                List<RhythmNote> previousNotes = new List<RhythmNote>(allNotes);
                int previousCount = allNotes.Count;

                // LMJ: Set new start time to continue seamlessly from current set end
                currentSetStartTime = currentSetEndTime;
                CreateFullPatternSet();

                List<RhythmNote> newNotes = allNotes.GetRange(previousCount, allNotes.Count - previousCount);
                gameSystem.AddNotes(newNotes);

                Debug.Log($"Added {newNotes.Count} new notes. Next set will end at: {currentSetEndTime:F2}");

                // LMJ: Wait longer to prevent multiple generations
                yield return new WaitForSeconds(lookAheadTime);
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

    // LMJ: New method for DealingTime recovery - start from current time immediately
    public void AddNextPatternSetFromCurrentTime()
    {
        List<RhythmNote> previousNotes = new List<RhythmNote>(allNotes);
        int previousCount = allNotes.Count;

        // LMJ: Start immediately from current game time (no delay)
        float currentGameTime = NoteTimeManager.Instance.GetNoteTime() - gameSystem.GameStartTime;
        currentSetStartTime = currentGameTime + 1f; // LMJ: Small buffer for smooth transition
        
        CreateFullPatternSet();

        // LMJ: Update the set end time tracking
        List<RhythmNote> newNotes = allNotes.GetRange(previousCount, allNotes.Count - previousCount);
        gameSystem.AddNotes(newNotes);

        Debug.Log($"DealingTime Recovery: Started new pattern set from time {currentSetStartTime:F2}");
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        gameStarted = false;
        allNotes.Clear();
        allPatterns.Clear();
    }
}