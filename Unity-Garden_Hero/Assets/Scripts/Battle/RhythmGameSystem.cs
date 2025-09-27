using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HoldEffectAnimation : MonoBehaviour
{
    private float duration;
    private float elapsed;
    private Image image;
    private RectTransform rectTransform;

    public void Initialize(float dur)
    {
        duration = dur;
        elapsed = 0f;
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        float scale = Mathf.Lerp(1f, 2f, t);
        rectTransform.localScale = Vector3.one * scale;

        Color color = image.color;
        color.a = Mathf.Lerp(1f, 0f, t);
        image.color = color;

        rectTransform.Rotate(0f, 0f, 180f * Time.deltaTime);
    }
}

public class PulseAnimation : MonoBehaviour
{
    private float animationTime = 0.3f;
    private float currentTime = 0f;
    private Image image;
    private RectTransform rectTransform;
    private string direction;

    public void Initialize(string dir)
    {
        direction = dir;
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        currentTime += Time.deltaTime;
        float progress = currentTime / animationTime;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        float scale = 1f + progress * 2f;

        if (direction == "Up")
        {
            rectTransform.localScale = new Vector3(1f, scale, 1f);
        }
        else
        {
            rectTransform.localScale = new Vector3(scale, 1f, 1f);
        }

        Color col = image.color;
        col.a = 1f - progress;
        image.color = col;
    }
}



public partial class RhythmGameSystem : MonoBehaviour
{

    [System.Serializable]
    public class MissDamageSettings
    {
        [Header("Basic Notes")]
        public int normalMissDamage = 10;
        public int specialMissDamage = 20;
        public int dodgeMissDamage = 0;

        [Header("Long Notes")]
        public int longMissDamage = 15;
        public int longHeadMissDamage = 15;
        public int longTailMissDamage = 10;
        public int longHoldMissDamage = 5;

        [Header("Default")]
        public int defaultMissDamage = 10;
    }

    [Header("Miss Damage Settings")]
    [SerializeField] private MissDamageSettings missDamageSettings = new MissDamageSettings();

    [Header("UI References")]
    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private GameObject notePrefab;

    [Header("Lane References - Assign from Scene")]
    [SerializeField] private Transform leftLane;
    [SerializeField] private Transform rightLane;
    [SerializeField] private Transform upLane;
    [SerializeField] private Transform leftJudgmentLine;
    [SerializeField] private Transform rightJudgmentLine;
    [SerializeField] private Transform upJudgmentLine;

    [Header("Judgment Line Offsets")]
    [SerializeField] private float leftJudgmentOffset = 310f;
    [SerializeField] private float rightJudgmentOffset = -310f;
    [SerializeField] private float upJudgmentOffset = -310f;

    [Header("Timing Settings")]
    [SerializeField] private float perfectTolerance = 0.05f;
    [SerializeField] private float goodTolerance = 0.1f;
    [SerializeField] private float missTolerance = 0.15f;
    [SerializeField] public float noteSpeed = 300f;

    [Header("Manager References")]
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private DirectionalShieldSystem directionalShieldSystem;

    [Header("Long Note Settings")]
    [SerializeField] private float longNoteHeadMultiplier = 0.5f;
    [SerializeField] private float longNoteTailMultiplier = 0.5f;
    [SerializeField] private float longNoteHoldBonusMultiplier = 0.5f;

    private Dictionary<string, Transform> lanes = new Dictionary<string, Transform>();
    private Dictionary<string, Transform> judgmentLines = new Dictionary<string, Transform>();
    private List<RhythmNote> allNotes = new List<RhythmNote>();
    private Dictionary<string, RhythmNote> heldNotes = new Dictionary<string, RhythmNote>();

    public float GameStartTime { get; private set; }

    void Awake()
    {
        SetupLaneReferences();
    }

    void SetupLaneReferences()
    {
        if (leftLane != null) lanes["Left"] = leftLane;
        if (rightLane != null) lanes["Right"] = rightLane;
        if (upLane != null) lanes["Up"] = upLane;

        if (leftJudgmentLine != null) judgmentLines["Left"] = leftJudgmentLine;
        if (rightJudgmentLine != null) judgmentLines["Right"] = rightJudgmentLine;
        if (upJudgmentLine != null) judgmentLines["Up"] = upJudgmentLine;

        SetJudgmentLinePositions();
    }

    void SetJudgmentLinePositions()
    {
        if (leftJudgmentLine != null)
        {
            RectTransform rect = leftJudgmentLine.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(leftJudgmentOffset, 0);
        }

        if (rightJudgmentLine != null)
        {
            RectTransform rect = rightJudgmentLine.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(rightJudgmentOffset, 0);
        }

        if (upJudgmentLine != null)
        {
            RectTransform rect = upJudgmentLine.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(upJudgmentOffset, 0);
        }
    }

    public GameObject CreateNoteObject(string direction, float spawnDistance)
    {
        if (!lanes.ContainsKey(direction)) return null;

        GameObject noteObj = Instantiate(notePrefab);
        noteObj.transform.SetParent(lanes[direction], false);

        Vector2 spawnPos = GetSpawnPosition(direction, spawnDistance);
        RectTransform noteRect = noteObj.GetComponent<RectTransform>();
        noteRect.anchoredPosition = spawnPos;

        return noteObj;
    }

    Vector2 GetSpawnPosition(string direction, float distance)
    {
        switch (direction)
        {
            case "Left":
                return new Vector2(leftJudgmentOffset - distance, 0);
            case "Right":
                return new Vector2(rightJudgmentOffset + distance, 0);
            case "Up":
                return new Vector2(upJudgmentOffset + distance, 0);
            default:
                return Vector2.zero;
        }
    }

    public void StartGame(float startTime, List<RhythmNote> notes)
    {
        GameStartTime = startTime;
        allNotes = notes;
    }

    public void AddNotes(List<RhythmNote> newNotes)
    {
        allNotes.AddRange(newNotes);
    }

    public void ClearAllNotes()
    {
        foreach (var note in allNotes)
        {
            if (note != null)
            {
                Destroy(note.gameObject);
            }
        }

        allNotes.Clear();
        heldNotes.Clear();
    }

    void Update()
    {
        UpdateNotePositions();
        UpdateHeldNotes();
        CheckInputs();
        CheckMissedNotes();
        CheckMissedLongNotes();
    }

    void UpdateNotePositions()
    {
        float currentGameTime = NoteTimeManager.Instance.GetNoteTime() - GameStartTime;

        foreach (var note in allNotes)
        {
            if (note == null) continue;

            float timeUntilHit = note.hitTime - currentGameTime;
            float distanceFromJudgment = timeUntilHit * noteSpeed;

            Vector2 targetPos = GetSpawnPosition(note.direction, distanceFromJudgment);
            note.GetComponent<RectTransform>().anchoredPosition = targetPos;
        }
    }

    public void CheckInputs()
    {
        // LMJ: Keyboard input is now handled by TouchInputManager
        // This prevents duplicate input processing
    }

    public void CheckHit(string direction)
    {
        CheckHitWithLongNote(direction);
    }

    public void ReleaseHold(string direction)
    {
        ReleaseLongNote(direction);
    }

    public void CheckHitWithLongNote(string direction)
    {
        if (directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled(direction))
        {
            return;
        }

        float currentGameTime = NoteTimeManager.Instance.GetNoteTime() - GameStartTime;
        RhythmNote closestNote = FindClosestNote(direction, currentGameTime);

        CreateHitEffect(direction);

        if (closestNote != null)
        {
            // LMJ: Defense Notes cannot be removed by key/touch input
            if (closestNote.noteType == "Defense")
            {
                Debug.Log($"[RhythmGameSystem] Defense Note cannot be removed by key/touch input - Direction: {direction}");
                return;
            }

            float timeDiff = Mathf.Abs(currentGameTime - closestNote.hitTime);

            if (closestNote.IsLongNote() && !closestNote.HasStartedHold())
            {
                ProcessLongNoteStart(closestNote, timeDiff, direction);
            }
            else if (!closestNote.IsLongNote())
            {
                JudgmentResult result = GetJudgment(timeDiff);
                ProcessNoteDamage(closestNote, result);
                allNotes.Remove(closestNote);
                Destroy(closestNote.gameObject);
                ShowJudgment(direction, result);
            }
        }
    }

    RhythmNote FindClosestNote(string direction, float currentGameTime)
    {
        RhythmNote closestNote = null;
        float minTimeDiff = missTolerance;

        foreach (var note in allNotes)
        {
            if (note.direction != direction || note.IsHolding()) continue;

            float timeDiff = Mathf.Abs(currentGameTime - note.hitTime);
            if (timeDiff < minTimeDiff)
            {
                minTimeDiff = timeDiff;
                closestNote = note;
            }
        }

        return closestNote;
    }

    void ProcessLongNoteStart(RhythmNote note, float timeDiff, string direction)
    {
        JudgmentResult result = GetLongNoteStartJudgment(timeDiff);

        if (result != JudgmentResult.Miss)
        {
            note.StartHold();
            heldNotes[direction] = note;
            ProcessLongNoteHeadDamage(note, result);
            ShowJudgment(direction, result);
            CreateHoldStartEffect(direction);
        }
        else
        {
            ProcessNoteDamage(note, result);
            ShowJudgment(direction, result);
        }
    }

    public void ReleaseLongNote(string direction)
    {
        if (!heldNotes.ContainsKey(direction)) return;

        RhythmNote longNote = heldNotes[direction];

        if (longNote == null)
        {
            heldNotes.Remove(direction);
            return;
        }

        longNote.SetHit();

        float currentGameTime = NoteTimeManager.Instance.GetNoteTime() - GameStartTime;
        float timeDiff = Mathf.Abs(currentGameTime - longNote.GetHoldEndTime());
        JudgmentResult result = GetLongNoteTailJudgment(timeDiff);

        longNote.EndHold();
        float holdProgress = longNote.GetHoldProgress();
        ProcessLongNoteTailDamage(longNote, result, holdProgress);
        ShowJudgment(direction, result);
        CreateHoldReleaseEffect(direction);

        heldNotes.Remove(direction);
        allNotes.Remove(longNote);
        Destroy(longNote.gameObject, 0.5f);
    }

    void UpdateHeldNotes()
    {
        float currentGameTime = NoteTimeManager.Instance.GetNoteTime() - GameStartTime;

        foreach (var kvp in new Dictionary<string, RhythmNote>(heldNotes))
        {
            string direction = kvp.Key;
            RhythmNote note = kvp.Value;

            if (note == null)
            {
                heldNotes.Remove(direction);
                continue;
            }

            if (currentGameTime >= note.GetHoldEndTime())
            {
                ReleaseLongNoteAuto(direction, note);
            }

            if (Time.frameCount % 10 == 0)
            {
                AddHoldBonus(note);
            }
        }
    }

    void ReleaseLongNoteAuto(string direction, RhythmNote note)
    {
        note.EndHold();

        JudgmentResult result = JudgmentResult.Perfect;
        float holdProgress = 1f;

        ProcessLongNoteTailDamage(note, result, holdProgress);
        ShowJudgment(direction, result);
        CreateHoldReleaseEffect(direction);

        heldNotes.Remove(direction);
        allNotes.Remove(note);
        Destroy(note.gameObject, 0.5f);
    }

    JudgmentResult GetLongNoteStartJudgment(float timeDiff)
    {
        if (timeDiff <= perfectTolerance * 0.7f) return JudgmentResult.Perfect;
        if (timeDiff <= goodTolerance * 0.8f) return JudgmentResult.Good;
        return JudgmentResult.Miss;
    }

    JudgmentResult GetLongNoteTailJudgment(float timeDiff)
    {
        if (timeDiff <= perfectTolerance) return JudgmentResult.Perfect;
        if (timeDiff <= goodTolerance * 1.2f) return JudgmentResult.Good;
        return JudgmentResult.Miss;
    }

    void ProcessLongNoteHeadDamage(RhythmNote note, JudgmentResult result)
    {
        if (monsterManager != null && playerManager != null)
        {
            int damage = Mathf.RoundToInt(playerManager.GetStunAttackPower() * longNoteHeadMultiplier);
            monsterManager.TakeNoteHit(damage, "Long_Head", result);

            if (directionalShieldSystem != null)
            {
                directionalShieldSystem.ProcessNoteResult(note.direction, result);
            }
        }
    }

    void ProcessLongNoteTailDamage(RhythmNote note, JudgmentResult result, float holdProgress)
    {
        if (monsterManager != null && playerManager != null)
        {
            float baseDamage = playerManager.GetStunAttackPower() * longNoteTailMultiplier;
            float holdBonus = baseDamage * holdProgress * longNoteHoldBonusMultiplier;
            int totalDamage = Mathf.RoundToInt(baseDamage + holdBonus);

            monsterManager.TakeNoteHit(totalDamage, "Long_Tail", result);

            if (directionalShieldSystem != null)
            {
                directionalShieldSystem.ProcessNoteResult(note.direction, result);
            }
        }
    }

    void AddHoldBonus(RhythmNote note)
    {
        if (monsterManager != null && playerManager != null)
        {
            int bonusDamage = Mathf.RoundToInt(playerManager.GetStunAttackPower() * 0.05f);
            monsterManager.TakeNoteHit(bonusDamage, "Long_Hold", JudgmentResult.Perfect);
        }
    }

    void CreateHoldStartEffect(string direction)
    {
        if (!judgmentLines.ContainsKey(direction)) return;

        GameObject effect = new GameObject("HoldStartEffect");
        effect.transform.SetParent(judgmentLines[direction], false);

        Image effectImg = effect.AddComponent<Image>();
        effectImg.color = new Color(1f, 1f, 0f, 0.8f);

        RectTransform rect = effect.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 100);
        rect.anchoredPosition = Vector2.zero;

        HoldEffectAnimation anim = effect.AddComponent<HoldEffectAnimation>();
        anim.Initialize(0.3f);
    }

    void CreateHoldReleaseEffect(string direction)
    {
        if (!judgmentLines.ContainsKey(direction)) return;

        GameObject effect = new GameObject("HoldReleaseEffect");
        effect.transform.SetParent(judgmentLines[direction], false);

        Image effectImg = effect.AddComponent<Image>();
        effectImg.color = new Color(1f, 0.8f, 0f, 1f);

        RectTransform rect = effect.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 120);
        rect.anchoredPosition = Vector2.zero;

        HoldEffectAnimation anim = effect.AddComponent<HoldEffectAnimation>();
        anim.Initialize(0.5f);
    }

    void CheckMissedLongNotes()
    {
        float currentGameTime = NoteTimeManager.Instance.GetNoteTime() - GameStartTime;

        for (int i = allNotes.Count - 1; i >= 0; i--)
        {
            RhythmNote note = allNotes[i];
            if (note == null || !note.IsLongNote() || note.HasStartedHold()) continue;

            if (currentGameTime > note.hitTime + missTolerance)
            {
                ProcessNoteDamage(note, JudgmentResult.Miss);
                ShowJudgment(note.direction, JudgmentResult.Miss);
                allNotes.RemoveAt(i);
                Destroy(note.gameObject);
            }
        }
    }

    void ProcessNoteDamage(RhythmNote note, JudgmentResult result)
    {
        if (monsterManager != null && playerManager != null)
        {
            string noteType = note.noteType;

            if (result != JudgmentResult.Miss && noteType != "Dodge")
            {
                monsterManager.TakeNoteHit(playerManager.GetStunAttackPower(), noteType, result);
            }
            else if (result == JudgmentResult.Miss)
            {
                int missDamage = GetMissDamage(noteType);
                playerManager.OnDamage(missDamage);
                Debug.Log($"[RhythmGameSystem] Player takes miss damage: {missDamage} from {noteType}");
            }

            playerManager.ProcessNoteResult(noteType, result != JudgmentResult.Miss);

            if (directionalShieldSystem != null)
            {
                directionalShieldSystem.ProcessNoteResult(note.direction, result);
            }
        }
    }

    int GetMissDamage(string noteType)
    {
        return noteType.ToLower() switch
        {
            "long_head" => missDamageSettings.longHeadMissDamage,
            "long_tail" => missDamageSettings.longTailMissDamage,
            "long_hold" => missDamageSettings.longHoldMissDamage,
            "long" => missDamageSettings.longMissDamage,
            "normal" => missDamageSettings.normalMissDamage,
            "special" => missDamageSettings.specialMissDamage,
            "dodge" => missDamageSettings.dodgeMissDamage,
            _ => missDamageSettings.defaultMissDamage
        };
    }

    void CheckMissedNotes()
    {
        if (allNotes == null || allNotes.Count == 0) return;

        float currentGameTime = NoteTimeManager.Instance.GetNoteTime() - GameStartTime;

        for (int i = allNotes.Count - 1; i >= 0; i--)
        {
            // Safety check: ensure index is still valid
            if (i >= allNotes.Count) continue;

            RhythmNote note = allNotes[i];
            if (note == null || note.IsHit())
            {
                // Remove null or hit notes safely
                if (i < allNotes.Count)
                {
                    allNotes.RemoveAt(i);
                }
                continue;
            }

            if (currentGameTime > note.hitTime + missTolerance && !note.IsHolding())
            {
                ProcessNoteDamage(note, JudgmentResult.Miss);
                ShowJudgment(note.direction, JudgmentResult.Miss);

                // Double-check index before removal
                if (i < allNotes.Count && allNotes[i] == note)
                {
                    allNotes.RemoveAt(i);
                }

                if (note != null)
                {
                    Destroy(note.gameObject);
                }
            }
        }
    }

    JudgmentResult GetJudgment(float timeDiff)
    {
        if (timeDiff <= perfectTolerance) return JudgmentResult.Perfect;
        if (timeDiff <= goodTolerance) return JudgmentResult.Good;
        return JudgmentResult.Miss;
    }

    void ShowJudgment(string direction, JudgmentResult result)
    {
        if (!judgmentLines.ContainsKey(direction)) return;

        GameObject judgmentText = new GameObject("JudgmentText");
        judgmentText.transform.SetParent(judgmentLines[direction], false);

        Text text = judgmentText.AddComponent<Text>();
        text.text = result.ToString();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 40;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = GetJudgmentColor(result);

        RectTransform textRect = judgmentText.GetComponent<RectTransform>();
        textRect.anchoredPosition = Vector2.up * 100;

        Destroy(judgmentText, 1f);
    }

    Color GetJudgmentColor(JudgmentResult result)
    {
        switch (result)
        {
            case JudgmentResult.Perfect: return Color.yellow;
            case JudgmentResult.Good: return Color.green;
            case JudgmentResult.Miss: return Color.red;
            default: return Color.white;
        }
    }

    void CreateHitEffect(string direction)
    {
        if (!judgmentLines.ContainsKey(direction)) return;

        GameObject pulse = new GameObject("PulseEffect");
        pulse.transform.SetParent(judgmentLines[direction], false);

        Image pulseImg = pulse.AddComponent<Image>();
        pulseImg.color = GetDirectionColor(direction);

        RectTransform pulseRect = pulse.GetComponent<RectTransform>();

        if (direction == "Up")
        {
            pulseRect.sizeDelta = new Vector2(150, 10);
        }
        else
        {
            pulseRect.sizeDelta = new Vector2(10, 150);
        }

        pulseRect.anchoredPosition = Vector2.zero;

        PulseAnimation anim = pulse.AddComponent<PulseAnimation>();
        anim.Initialize(direction);
    }

    Color GetDirectionColor(string direction)
    {
        switch (direction)
        {
            case "Left": return new Color(1f, 0.2f, 0.2f, 0.8f);
            case "Right": return new Color(0.2f, 0.2f, 1f, 0.8f);
            case "Up": return new Color(0.2f, 1f, 0.2f, 0.8f);
            default: return Color.white;
        }
    }

    public float GetCurrentGameTime()
    {
        return NoteTimeManager.Instance.GetNoteTime() - GameStartTime;
    }

    // LMJ: Method to clear Defense Notes when dodging
    public void ClearDefenseNotesOnScreen()
    {
        List<RhythmNote> notesToRemove = new List<RhythmNote>();

        foreach (RhythmNote note in allNotes)
        {
            if (note != null && note.noteType == "Defense" && IsNoteOnScreen(note))
            {
                notesToRemove.Add(note);
                Debug.Log($"[RhythmGameSystem] Removing Defense Note on screen - Direction: {note.direction}");
            }
        }

        foreach (RhythmNote note in notesToRemove)
        {
            allNotes.Remove(note);
            Destroy(note.gameObject);
        }

        Debug.Log($"[RhythmGameSystem] Cleared {notesToRemove.Count} Defense Notes from screen");
    }

    // LMJ: Check if note is currently visible on screen
    private bool IsNoteOnScreen(RhythmNote note)
    {
        if (note == null) return false;

        RectTransform noteRect = note.GetComponent<RectTransform>();
        if (noteRect == null) return false;

        // Get the note's position relative to its parent canvas
        Canvas canvas = noteRect.GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null) return false;

        // Convert note position to canvas space
        Vector2 notePosition = noteRect.anchoredPosition;

        // Get canvas bounds
        Rect canvasBounds = canvasRect.rect;

        // Check if note is within visible canvas area (with some margin for judgment line area)
        float margin = 200f; // Pixels margin to account for judgment line area
        bool isOnScreen = notePosition.x >= canvasBounds.xMin - margin &&
                         notePosition.x <= canvasBounds.xMax + margin &&
                         notePosition.y >= canvasBounds.yMin - margin &&
                         notePosition.y <= canvasBounds.yMax + margin;

        return isOnScreen;
    }
}