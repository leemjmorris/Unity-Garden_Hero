using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Note
{
    public float time;
    public string type;
    public string direction;
    public float duration;

    // LMJ: Runtime calculated fields
    [System.NonSerialized] public float spawnTime;
    [System.NonSerialized] public float hitTime;
    [System.NonSerialized] public float actualApproachTime; // 추가
    [System.NonSerialized] public bool hasSpawned = false;
    [System.NonSerialized] public GameObject spawnedNote;
}

[System.Serializable]
public class NotePattern
{
    public string patternName;
    public float totalDuration;
    public float perfectTolerance;
    public float goodTolerance;
    public List<Note> notes;
}

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private TextAsset jsonFile;
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform leftSpawner;
    [SerializeField] private Transform rightSpawner;
    [SerializeField] private Transform upSpawner;

    [Header("Approach Times")]
    public float normalApproachTime = 4.0f;   // 3.0 -> 4.0
    public float chargedApproachTime = 4.5f;  // 3.5 -> 4.5
    public float defenseApproachTime = 3.0f;  // 2.0 -> 3.0
    public float specialApproachTime = 4.0f;  // 3.0 -> 4.0

    [Header("Tolerances")]
    public float perfectTolerance = 0.2f;
    public float goodTolerance = 0.4f;

    private NotePattern currentPattern;

    void Start()
    {
        LoadPattern();
        GameTimeManager.Instance.StartGame();
    }

    void Update()
    {
        CheckForNoteSpawns();
    }

    public void LoadPattern()
    {
        if (jsonFile != null)
        {
            string jsonString = jsonFile.text;
            currentPattern = JsonUtility.FromJson<NotePattern>(jsonString);

            if (currentPattern.perfectTolerance > 0) perfectTolerance = currentPattern.perfectTolerance;
            if (currentPattern.goodTolerance > 0) goodTolerance = currentPattern.goodTolerance;

            currentPattern.notes.Sort((a, b) => a.time.CompareTo(b.time));

            foreach (var note in currentPattern.notes)
            {
                CalculateNoteTimings(note);
            }

            Debug.Log($"Loaded pattern: {currentPattern.patternName} with {currentPattern.notes.Count} notes");
        }
    }

    void CalculateNoteTimings(Note note)
    {
        float baseApproachTime = GetApproachTime(note.type);
        float calculatedSpawnTime = note.time - baseApproachTime;

        if (calculatedSpawnTime < 0)
        {
            note.spawnTime = 0f;
            note.actualApproachTime = note.time; // LMJ: Store actual approach time
            Debug.Log($"Note {note.direction} adjusted: original approach={baseApproachTime:F1}s, actual={note.actualApproachTime:F1}s");
        }
        else
        {
            note.spawnTime = calculatedSpawnTime;
            note.actualApproachTime = baseApproachTime; // LMJ: Use original approach time
        }

        note.hitTime = note.time;
    }

    // LMJ: Dynamic approach time based on available time
    float GetDynamicApproachTime(Note note)
    {
        float baseApproachTime = GetApproachTime(note.type);
        float availableTime = note.time; // Time from game start to hit

        // LMJ: Use minimum of base time or available time
        return Mathf.Min(baseApproachTime, availableTime);
    }

    Vector3 GetTargetPosition(string direction)
    {
        switch (direction)
        {
            case "Left": return new Vector3(-1.7f, 2f, 0f);
            case "Right": return new Vector3(1.7f, 2f, 0f);
            case "Up": return new Vector3(0f, 2f, 1.7f);
            default: return Vector3.zero;
        }
    }

    void CheckForNoteSpawns()
    {
        if (!GameTimeManager.Instance.isPlaying || currentPattern == null) return;

        float currentTime = GameTimeManager.Instance.gameTime;

        foreach (var note in currentPattern.notes)
        {
            if (!note.hasSpawned && currentTime >= note.spawnTime)
            {
                SpawnNote(note);
                note.hasSpawned = true;
            }
        }
    }

    private void SpawnNote(Note note)
    {
        if (notePrefab != null)
        {
            Transform spawner = GetSpawnerByDirection(note.direction);
            if (spawner != null)
            {
                GameObject spawnedNote = Instantiate(notePrefab, spawner.position, spawner.rotation);
                spawnedNote.layer = LayerMask.NameToLayer("Notes");

                NoteController noteController = spawnedNote.GetComponent<NoteController>();
                if (noteController != null)
                {
                    noteController.SetNoteData(note, note.actualApproachTime, this); // LMJ: Use actual approach time
                }

                note.spawnedNote = spawnedNote;
            }
        }
    }

    private Transform GetSpawnerByDirection(string direction)
    {
        switch (direction)
        {
            case "Left":
                return leftSpawner;
            case "Right":
                return rightSpawner;
            case "Up":
                return upSpawner;
            default:
                return leftSpawner;
        }
    }

    float GetApproachTime(string noteType)
    {
        switch (noteType)
        {
            case "Normal": return normalApproachTime;
            case "Long": return chargedApproachTime; // LMJ: Renamed from Charged to Long
            case "Defense": return defenseApproachTime;
            case "Special": return specialApproachTime;
            default: return normalApproachTime;
        }
    }
}