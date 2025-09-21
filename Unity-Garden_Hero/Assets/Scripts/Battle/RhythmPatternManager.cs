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
                }
            }
        }
    }

    void GenerateInitialPatternSet()
    {
        if (allPatterns.Count == 0)
        {
            return;
        }

        nextPatternStartTime = startDelay;
        CreateRandomPatternSet();
    }

    void CreateRandomPatternSet()
    {
        List<int> shuffledIndices = GetShuffledPatternIndices();

        foreach (int patternIndex in shuffledIndices)
        {
            RhythmPattern pattern = allPatterns[patternIndex];
            CreateNotesForPattern(pattern);

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

        return maxTime + 1f;
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
            yield return new WaitForSeconds(2f);

            // LMJ: Use custom note time instead of Time.time
            float currentGameTime = NoteTimeManager.Instance.GetNoteTime();
            float timeUntilNextSet = nextPatternStartTime - currentGameTime;

            if (timeUntilNextSet <= 8f && timeUntilNextSet > 0f)
            {
                List<RhythmNote> previousNotes = new List<RhythmNote>(allNotes);
                int previousCount = allNotes.Count;

                CreateRandomPatternSet();

                List<RhythmNote> newNotes = allNotes.GetRange(previousCount, allNotes.Count - previousCount);
                gameSystem.AddNotes(newNotes);

                yield return new WaitForSeconds(5f);
            }
        }
    }

    [ContextMenu("Add Next Pattern Set Now")]
    public void AddNextPatternSetNow()
    {
        List<RhythmNote> previousNotes = new List<RhythmNote>(allNotes);
        int previousCount = allNotes.Count;

        CreateRandomPatternSet();

        List<RhythmNote> newNotes = allNotes.GetRange(previousCount, allNotes.Count - previousCount);
        gameSystem.AddNotes(newNotes);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        gameStarted = false;
        allNotes.Clear();
        allPatterns.Clear();
    }
}