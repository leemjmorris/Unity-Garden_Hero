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

    [Header("Playback Settings")]
    [SerializeField] private bool randomOrder = true;

    private List<RhythmPattern> allPatterns = new List<RhythmPattern>();
    private List<RhythmNote> allNotes = new List<RhythmNote>();
    private float nextPatternStartTime = 0f;
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
                    Debug.Log($"Loaded pattern: {pattern.patternName}");
                }
            }
        }

        Debug.Log($"Total patterns loaded: {allPatterns.Count}");
    }

    void GenerateInitialPatternSet()
    {
        if (allPatterns.Count == 0)
        {
            Debug.LogWarning("No patterns loaded!");
            return;
        }

        // LMJ: First pattern set starts with delay
        nextPatternStartTime = startDelay;

        CreateRandomPatternSet();
        Debug.Log($"Initial pattern set created. {allNotes.Count} notes generated. Next set will start at: {nextPatternStartTime}");
    }

    void CreateRandomPatternSet()
    {
        List<int> shuffledIndices = GetShuffledPatternIndices();

        Debug.Log($"Creating new pattern set. Order: {string.Join(", ", shuffledIndices)}");

        // LMJ: Create notes for each pattern in shuffled order
        foreach (int patternIndex in shuffledIndices)
        {
            RhythmPattern pattern = allPatterns[patternIndex];
            CreateNotesForPattern(pattern);

            // LMJ: Move start time to end of this pattern
            float patternDuration = GetPatternDuration(pattern);
            nextPatternStartTime += patternDuration;
        }
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
            // LMJ: Fisher-Yates shuffle
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

    void CreateNotesForPattern(RhythmPattern pattern)
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

                // LMJ: Hit time is based on current pattern start time
                float actualHitTime = nextPatternStartTime + noteData.time;
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

        // LMJ: Add buffer time between patterns
        return maxTime + 1f;
    }

    void StartGame()
    {
        float gameStartTime = Time.time;
        gameSystem.StartGame(gameStartTime, allNotes);
        gameStarted = true;
        Debug.Log($"Game started with {allNotes.Count} notes");
    }

    // LMJ: Monitor when to add next pattern set
    IEnumerator MonitorForNextPatternSet()
    {
        while (gameStarted)
        {
            yield return new WaitForSeconds(2f); // Check every 2 seconds

            float currentGameTime = Time.time;
            float timeUntilNextSet = nextPatternStartTime - currentGameTime;

            // LMJ: When 8 seconds left, generate and append next pattern set
            if (timeUntilNextSet <= 8f && timeUntilNextSet > 0f)
            {
                Debug.Log($"Adding next pattern set. Time until current set ends: {timeUntilNextSet:F1}s");

                List<RhythmNote> previousNotes = new List<RhythmNote>(allNotes);
                int previousCount = allNotes.Count;

                // LMJ: Generate next pattern set (appends to allNotes)
                CreateRandomPatternSet();

                // LMJ: Get only the new notes that were added
                List<RhythmNote> newNotes = allNotes.GetRange(previousCount, allNotes.Count - previousCount);

                // LMJ: Add new notes to game system without resetting
                gameSystem.AddNotes(newNotes);

                Debug.Log($"Next pattern set added. New notes: {newNotes.Count}, Total: {allNotes.Count}. Next set at: {nextPatternStartTime}");

                // LMJ: Wait to avoid multiple generations
                yield return new WaitForSeconds(5f);
            }
        }
    }

    // LMJ: Manual controls for testing
    [ContextMenu("Add Next Pattern Set Now")]
    public void AddNextPatternSetNow()
    {
        List<RhythmNote> previousNotes = new List<RhythmNote>(allNotes);
        int previousCount = allNotes.Count;

        CreateRandomPatternSet();

        List<RhythmNote> newNotes = allNotes.GetRange(previousCount, allNotes.Count - previousCount);
        gameSystem.AddNotes(newNotes);

        Debug.Log($"Manually added pattern set. New notes: {newNotes.Count}");
    }

    void OnDestroy()
    {
        // LMJ: Stop all coroutines and clean up
        StopAllCoroutines();
        gameStarted = false;
        allNotes.Clear();
        allPatterns.Clear();
    }
}