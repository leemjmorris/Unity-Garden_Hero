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
}

[System.Serializable]
public class NotePattern
{
    public string patternName;
    public float totalDuration;
    public List<Note> notes;
}

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private TextAsset jsonFile;
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform leftSpawner;
    [SerializeField] private Transform rightSpawner;
    [SerializeField] private Transform upSpawner;
    
    private NotePattern currentPattern;
    private float gameTime;
    private int nextNoteIndex;
    private bool isPlaying;

    void Start()
    {
        LoadPattern();
        StartSpawning();
    }

    void Update()
    {
        if (isPlaying)
        {
            gameTime += Time.deltaTime;
            CheckForNoteSpawn();
        }
    }

    public void LoadPattern()
    {
        if (jsonFile != null)
        {
            string jsonString = jsonFile.text;
            currentPattern = JsonUtility.FromJson<NotePattern>(jsonString);
            
            // LMJ: Sort notes by time to ensure proper spawning order
            currentPattern.notes.Sort((a, b) => a.time.CompareTo(b.time));
            
            Debug.Log($"Loaded pattern: {currentPattern.patternName} with {currentPattern.notes.Count} notes");
        }
    }

    public void StartSpawning()
    {
        if (currentPattern != null)
        {
            gameTime = 0f;
            nextNoteIndex = 0;
            isPlaying = true;
        }
    }

    public void StopSpawning()
    {
        isPlaying = false;
    }

    private void CheckForNoteSpawn()
    {
        if (nextNoteIndex < currentPattern.notes.Count)
        {
            Note nextNote = currentPattern.notes[nextNoteIndex];
            
            if (gameTime >= nextNote.time)
            {
                SpawnNote(nextNote);
                nextNoteIndex++;
            }
        }
        else if (gameTime >= currentPattern.totalDuration)
        {
            StopSpawning();
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
                
                NoteController noteController = spawnedNote.GetComponent<NoteController>();
                if (noteController != null)
                {
                    noteController.SetNoteData(note);
                }
            }
        }
    }

    // LMJ: Get appropriate spawner based on note direction
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
}