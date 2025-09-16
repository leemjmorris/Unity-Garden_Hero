using UnityEngine;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode upKey = KeyCode.W;

    [Header("Hit Detection")]
    public float hitRadius = 1.5f;
    public LayerMask noteLayer = 1;

    // LMJ: Track held keys and active long notes
    private Dictionary<string, bool> keysHeld = new Dictionary<string, bool>();
    private Dictionary<string, NoteController> activeLongNotes = new Dictionary<string, NoteController>();

    void Start()
    {
        keysHeld["Left"] = false;
        keysHeld["Right"] = false;
        keysHeld["Up"] = false;
    }

    void Update()
    {
        CheckInputs();
        CheckLongNoteReleases();
    }

    void CheckInputs()
    {
        // LMJ: Left key
        if (Input.GetKeyDown(leftKey))
            HandleInputStart("Left");
        if (Input.GetKeyUp(leftKey))
            HandleInputEnd("Left");

        // LMJ: Right key
        if (Input.GetKeyDown(rightKey))
            HandleInputStart("Right");
        if (Input.GetKeyUp(rightKey))
            HandleInputEnd("Right");

        // LMJ: Up key
        if (Input.GetKeyDown(upKey))
            HandleInputStart("Up");
        if (Input.GetKeyUp(upKey))
            HandleInputEnd("Up");
    }

    void HandleInputStart(string direction)
    {
        float inputTime = GameTimeManager.Instance.gameTime;
        keysHeld[direction] = true;

        Vector3 detectionCenter = GetDetectionCenter(direction);
        Collider[] hitColliders = Physics.OverlapSphere(detectionCenter, hitRadius, noteLayer);

        NoteController bestNote = null;
        float bestTimeDiff = float.MaxValue;

        foreach (var collider in hitColliders)
        {
            NoteController noteController = collider.GetComponent<NoteController>();
            if (noteController != null && noteController.GetNoteData().direction == direction)
            {
                float timeDiff = Mathf.Abs(inputTime - noteController.GetNoteData().hitTime);
                if (timeDiff < bestTimeDiff)
                {
                    bestTimeDiff = timeDiff;
                    bestNote = noteController;
                }
            }
        }

        if (bestNote != null)
        {
            ProcessHitStart(bestNote, inputTime, direction);
        }
        else
        {
            Debug.Log($"No {direction} note in range at {inputTime:F2}");
        }
    }

    Vector3 GetDetectionCenter(string direction)
    {
        switch (direction)
        {
            case "Left": return new Vector3(-1.7f, 2f, 0f);
            case "Right": return new Vector3(1.7f, 2f, 0f);
            case "Up": return new Vector3(0f, 2f, 1.7f);
            default: return Vector3.zero;
        }
    }

    void HandleInputEnd(string direction)
    {
        float inputTime = GameTimeManager.Instance.gameTime;
        keysHeld[direction] = false;

        if (activeLongNotes.ContainsKey(direction))
        {
            NoteController longNote = activeLongNotes[direction];
            if (longNote != null)
            {
                ProcessHitEnd(longNote, inputTime, direction);
            }
            activeLongNotes.Remove(direction);
        }
    }

    void CheckLongNoteReleases()
    {
        List<string> toRemove = new List<string>();

        foreach (var kvp in activeLongNotes)
        {
            string direction = kvp.Key;
            NoteController longNote = kvp.Value;

            if (longNote == null || !longNote.IsLongNoteActive())
            {
                toRemove.Add(direction);
                continue;
            }

            // LMJ: Auto-complete long note if held past end time
            float currentTime = GameTimeManager.Instance.gameTime;
            if (currentTime >= longNote.GetLongNoteEndTime())
            {
                ProcessHitEnd(longNote, currentTime, direction);
                toRemove.Add(direction);
            }
        }

        foreach (string direction in toRemove)
        {
            activeLongNotes.Remove(direction);
        }
    }

    void ProcessHitStart(NoteController noteController, float inputTime, string direction)
    {
        JudgmentResult result = noteController.CheckHitStart(inputTime);

        switch (result)
        {
            case JudgmentResult.Perfect:
                Debug.Log($"Perfect Start! {direction} (Timing: {inputTime:F2})");
                break;
            case JudgmentResult.Good:
                Debug.Log($"Good Start! {direction} (Timing: {inputTime:F2})");
                break;
            case JudgmentResult.Miss:
                Debug.Log($"Miss Start! {direction} (Timing: {inputTime:F2})");
                Destroy(noteController.gameObject);
                return;
        }

        if (noteController.IsLongNote())
        {
            activeLongNotes[direction] = noteController;
        }
        else
        {
            Destroy(noteController.gameObject);
        }
    }

    void ProcessHitEnd(NoteController noteController, float inputTime, string direction)
    {
        JudgmentResult result = noteController.CheckHitEnd(inputTime);

        switch (result)
        {
            case JudgmentResult.Perfect:
                Debug.Log($"Perfect End! {direction} (Timing: {inputTime:F2})");
                break;
            case JudgmentResult.Good:
                Debug.Log($"Good End! {direction} (Timing: {inputTime:F2})");
                break;
            case JudgmentResult.Miss:
                Debug.Log($"Miss End! {direction} (Timing: {inputTime:F2})");
                break;
        }

        Destroy(noteController.gameObject);
    }
}