using UnityEngine;

public class NoteController : MonoBehaviour
{
    private Note noteData;
    private float approachTime;
    private Vector3 startPosition;
    private Vector3 targetPosition = Vector3.zero;
    private float spawnTime;
    private Renderer noteRenderer;
    private NoteSpawner spawner;

    // LMJ: Long note specific variables
    private bool isLongNote;
    private bool longNoteStarted = false;
    private bool longNoteCompleted = false;
    private float longNoteEndTime;

    public void SetNoteData(Note data, float approach, NoteSpawner noteSpawner)
    {
        noteData = data;
        approachTime = approach;
        spawner = noteSpawner;
        startPosition = transform.position;
        spawnTime = GameTimeManager.Instance.gameTime;
        noteRenderer = GetComponent<Renderer>();

        // LMJ: Set target based on direction and shield positions
        SetTargetBasedOnDirection();

        isLongNote = noteData.type == "Long" && noteData.duration > 0;
        if (isLongNote)
        {
            longNoteEndTime = noteData.hitTime + noteData.duration;
        }

        SetNoteAppearance();
    }

    void SetTargetBasedOnDirection()
    {
        switch (noteData.direction)
        {
            case "Left":
                targetPosition = new Vector3(-1.7f, 2f, 0f); // LMJ: Near left shield
                break;
            case "Right":
                targetPosition = new Vector3(1.7f, 2f, 0f); // LMJ: Near right shield
                break;
            case "Up":
                targetPosition = new Vector3(0f, 2f, 1.7f); // LMJ: Near front shield
                break;
            default:
                targetPosition = Vector3.zero;
                break;
        }
    }

    void Update()
    {
        MoveTowardsTarget();
        CheckForMiss();
        UpdateLongNote();
    }

    void MoveTowardsTarget()
    {
        float currentTime = GameTimeManager.Instance.gameTime;
        float elapsedTime = currentTime - spawnTime;
        float progress = elapsedTime / approachTime;

        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
    }

    void UpdateLongNote()
    {
        if (!isLongNote || longNoteCompleted) return;

        float currentTime = GameTimeManager.Instance.gameTime;

        // LMJ: Check if long note duration has passed
        if (longNoteStarted && currentTime > longNoteEndTime + spawner.goodTolerance)
        {
            HandleLongNoteMiss();
        }
        else if (!longNoteStarted && currentTime > noteData.hitTime + spawner.goodTolerance)
        {
            HandleMiss();
        }
    }

    void SetNoteAppearance()
    {
        if (noteRenderer == null) return;

        switch (noteData.type)
        {
            case "Normal":
                noteRenderer.material.color = Color.white;
                break;
            case "Long":
                noteRenderer.material.color = Color.yellow;
                // LMJ: Scale long note to show duration
                Vector3 scale = transform.localScale;
                scale.z *= (1 + noteData.duration * 0.5f);
                transform.localScale = scale;
                break;
            case "Defense":
                noteRenderer.material.color = Color.blue;
                break;
            case "Special":
                noteRenderer.material.color = Color.red;
                break;
        }
    }

    void CheckForMiss()
    {
        if (isLongNote && longNoteStarted) return;

        float currentTime = GameTimeManager.Instance.gameTime;

        // LMJ: Give more time after hit time for player to react
        float missThreshold = noteData.hitTime + spawner.goodTolerance + 1.0f; // 추가 1초 여유

        if (currentTime > missThreshold)
        {
            HandleMiss();
        }
    }

    void HandleMiss()
    {
        Debug.Log($"Miss: {noteData.type} {noteData.direction} at {noteData.hitTime}");
        Destroy(gameObject);
    }

    void HandleLongNoteMiss()
    {
        Debug.Log($"Long Note Miss: Released too early - {noteData.direction}");
        longNoteCompleted = true;
        Destroy(gameObject);
    }

    public JudgmentResult CheckHitStart(float inputTime)
    {
        float timeDiff = Mathf.Abs(inputTime - noteData.hitTime);

        if (timeDiff <= spawner.perfectTolerance)
        {
            if (isLongNote)
            {
                longNoteStarted = true;
                SetLongNoteActiveAppearance();
            }
            return JudgmentResult.Perfect;
        }
        else if (timeDiff <= spawner.goodTolerance)
        {
            if (isLongNote)
            {
                longNoteStarted = true;
                SetLongNoteActiveAppearance();
            }
            return JudgmentResult.Good;
        }
        else
        {
            return JudgmentResult.Miss;
        }
    }

    public JudgmentResult CheckHitEnd(float inputTime)
    {
        if (!isLongNote || !longNoteStarted) return JudgmentResult.Miss;

        float timeDiff = Mathf.Abs(inputTime - longNoteEndTime);

        longNoteCompleted = true;

        if (timeDiff <= spawner.perfectTolerance)
            return JudgmentResult.Perfect;
        else if (timeDiff <= spawner.goodTolerance)
            return JudgmentResult.Good;
        else
            return JudgmentResult.Miss;
    }

    void SetLongNoteActiveAppearance()
    {
        if (noteRenderer != null)
        {
            noteRenderer.material.color = Color.green; // LMJ: Show note is being held
        }
    }

    public Note GetNoteData()
    {
        return noteData;
    }

    public bool IsLongNote() => isLongNote;
    public bool IsLongNoteActive() => isLongNote && longNoteStarted && !longNoteCompleted;
    public float GetLongNoteEndTime() => longNoteEndTime;
}