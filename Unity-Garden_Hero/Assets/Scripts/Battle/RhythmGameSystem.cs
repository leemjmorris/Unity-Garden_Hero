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

        // Unified scale animation for all directions (Center style)
        rectTransform.localScale = new Vector3(1f, scale, 1f);

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

    [Header("Note Prefabs")]
    [SerializeField] private GameObject normalNotePrefab;
    [SerializeField] private GameObject longNotePrefab;
    [SerializeField] private GameObject dodgeNotePrefab;
    [SerializeField] private GameObject specialNotePrefab;

    [Header("Lane System - Left | Center | Right")]
    [SerializeField] private Transform leftLane;
    [SerializeField] private Transform centerLane;
    [SerializeField] private Transform rightLane;
    [SerializeField] private Transform leftJudgmentLine;
    [SerializeField] private Transform centerJudgmentLine;
    [SerializeField] private Transform rightJudgmentLine;

    [Header("Note Movement Settings")]
    [SerializeField] private float noteDelay = 2.0f; // Time before note reaches judgment line

    [Header("Timing Settings")]
    [SerializeField] private float perfectTolerance = 0.05f;
    [SerializeField] private float goodTolerance = 0.1f;
    [SerializeField] private float missTolerance = 0.15f;
    [SerializeField] public float noteSpeed = 300f;

    [Header("Manager References")]
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private JudgmentTextManager judgmentTextManager;
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
    private Dictionary<string, float> holdTickTimers = new Dictionary<string, float>();

    public float GameStartTime { get; private set; }

    void Awake()
    {
        SetupLaneReferences();
    }

    void SetupLaneReferences()
    {
        // Setup lane references for Left | Center | Right
        if (leftLane != null) lanes["Left"] = leftLane;
        if (centerLane != null) lanes["Center"] = centerLane;
        if (rightLane != null) lanes["Right"] = rightLane;

        if (leftJudgmentLine != null) judgmentLines["Left"] = leftJudgmentLine;
        if (centerJudgmentLine != null) judgmentLines["Center"] = centerJudgmentLine;
        if (rightJudgmentLine != null) judgmentLines["Right"] = rightJudgmentLine;

        // Initialize hold tick timers
        holdTickTimers["Left"] = 0f;
        holdTickTimers["Center"] = 0f;
        holdTickTimers["Right"] = 0f;
    }

    // Convert JSON direction to Unity lane direction
    string ConvertDirection(string jsonDirection)
    {
        return jsonDirection.ToLower() switch
        {
            "up" => "Center",
            "left" => "Left",
            "right" => "Right",
            _ => jsonDirection // fallback
        };
    }

    // Convert JSON note type to Unity note type
    string ConvertNoteType(string jsonType)
    {
        return jsonType.ToLower() switch
        {
            "charged" => "Long",
            "defence" => "Dodge",
            "defense" => "Dodge", // alternate spelling
            _ => jsonType // Normal, Special, etc.
        };
    }

    // Get appropriate note prefab based on type
    GameObject GetNotePrefab(string noteType)
    {
        GameObject result = noteType switch
        {
            "Normal" => normalNotePrefab,
            "Long" => longNotePrefab,
            "Dodge" => dodgeNotePrefab,
            "Special" => specialNotePrefab,
            _ => normalNotePrefab // fallback
        };

        return result;
    }

    public GameObject CreateNoteObject(string jsonDirection, string jsonNoteType, float hitTime, float duration = 0f)
    {
        // Convert JSON data to Unity format
        string direction = ConvertDirection(jsonDirection);
        string noteType = ConvertNoteType(jsonNoteType);


        if (!lanes.ContainsKey(direction))
        {
            return null;
        }

        // Get appropriate prefab
        GameObject prefab = GetNotePrefab(noteType);
        if (prefab == null)
        {
            return null;
        }


        // Create note object
        GameObject noteObj = Instantiate(prefab);
        noteObj.transform.SetParent(lanes[direction], false);

        // Calculate spawn position (spawn above judgment line)
        float spawnDistance = noteSpeed * noteDelay;
        Vector2 spawnPos = GetSpawnPosition(direction, spawnDistance);
        RectTransform noteRect = noteObj.GetComponent<RectTransform>();
        noteRect.anchoredPosition = spawnPos;

        // Setup note component
        RhythmNote noteComponent = noteObj.GetComponent<RhythmNote>();
        if (noteComponent != null)
        {
            noteComponent.Initialize(direction, hitTime, noteType, noteSpeed, duration);

            // For Long notes, adjust height based on duration
            if (noteType == "Long" && duration > 0)
            {
                // Set pivot to bottom center for proper Long Note scaling
                noteRect.pivot = new Vector2(0.5f, 0f);

                float longNoteHeight = duration * noteSpeed;
                noteRect.sizeDelta = new Vector2(noteRect.sizeDelta.x, longNoteHeight);
            }
        }

        return noteObj;
    }

    Vector2 GetSpawnPosition(string direction, float distance)
    {
        // Get judgment line position and spawn above it
        if (judgmentLines.ContainsKey(direction) && judgmentLines[direction] != null)
        {
            RectTransform judgmentRect = judgmentLines[direction].GetComponent<RectTransform>();
            if (judgmentRect != null)
            {
                Vector2 judgmentPos = judgmentRect.anchoredPosition;
                return new Vector2(judgmentPos.x, judgmentPos.y + distance);
            }
        }

        // Fallback: spawn in center of respective lane
        return direction switch
        {
            "Left" => new Vector2(-200f, distance), // Left lane center
            "Center" => new Vector2(0f, distance),  // Center lane
            "Right" => new Vector2(200f, distance), // Right lane center
            _ => new Vector2(0f, distance)
        };
    }

    // Public method to change note speed during gameplay
    public void SetNoteSpeed(float newSpeed)
    {
        noteSpeed = Mathf.Max(50f, newSpeed); // Minimum speed limit
    }

    // Public method to get current note speed
    public float GetNoteSpeed()
    {
        return noteSpeed;
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
        // Stop all note updates when game is over or monster is dead
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            GameState currentState = gameManager.GetCurrentState();

            // Block updates when game is over or paused
            if (currentState == GameState.GameOver || currentState == GameState.Paused)
            {
                return;
            }
        }

        // Also stop if monster is dead (final phase defeated)
        if (monsterManager != null && !monsterManager.IsAlive())
        {
            // Check if this is the final phase
            if (monsterManager.GetPhase() >= monsterManager.GetTotalPhases())
            {
                return;
            }
        }

        UpdateNotePositions();
        UpdateHeldNotes();
        UpdateLongNoteHoldTicks();
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

            // Skip moving notes that are being held - they should stay at judgment line
            if (note.IsHolding())
            {
                // Keep the note at judgment line (distance = 0)
                Vector2 judgmentPos = GetSpawnPosition(note.direction, 0);
                note.GetComponent<RectTransform>().anchoredPosition = judgmentPos;
                continue;
            }

            float timeUntilHit = note.hitTime - currentGameTime;
            float distanceFromJudgment = timeUntilHit * noteSpeed;

            Vector2 targetPos = GetSpawnPosition(note.direction, distanceFromJudgment);
            note.GetComponent<RectTransform>().anchoredPosition = targetPos;
        }
    }

    // DJ MAX style Long Note hold tick system
    void UpdateLongNoteHoldTicks()
    {
        float tickInterval = 0.1f; // 100ms tick interval

        foreach (var kvp in heldNotes)
        {
            string direction = kvp.Key;
            RhythmNote note = kvp.Value;

            if (note == null || !note.IsHolding()) continue;

            // Update tick timer
            holdTickTimers[direction] += Time.deltaTime;

            // Process tick every interval
            if (holdTickTimers[direction] >= tickInterval)
            {
                holdTickTimers[direction] = 0f;
                ProcessLongNoteHoldTick(note, direction);
            }
        }
    }

    void ProcessLongNoteHoldTick(RhythmNote note, string direction)
    {
        // Give small bonus score for holding
        if (monsterManager != null && playerManager != null)
        {
            int tickDamage = Mathf.RoundToInt(playerManager.GetStunAttackPower() * 0.1f);
            monsterManager.TakeNoteHit(tickDamage, "Long_Hold_Tick", JudgmentResult.Perfect);
        }

        // Visual feedback for successful hold tick
        CreateHoldTickEffect(direction);
    }

    void CreateHoldTickEffect(string direction)
    {
        if (!judgmentLines.ContainsKey(direction)) return;

        GameObject tickEffect = new GameObject("HoldTickEffect");
        tickEffect.transform.SetParent(judgmentLines[direction], false);

        UnityEngine.UI.Image effectImg = tickEffect.AddComponent<UnityEngine.UI.Image>();
        effectImg.color = new Color(1f, 1f, 0.5f, 0.6f);

        RectTransform rect = tickEffect.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 10);
        rect.anchoredPosition = Vector2.zero;

        // Simple fade out effect
        StartCoroutine(FadeOutEffect(effectImg, 0.3f));
    }

    System.Collections.IEnumerator FadeOutEffect(UnityEngine.UI.Image image, float duration)
    {
        float elapsed = 0f;
        Color startColor = image.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            image.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        if (image != null && image.gameObject != null)
            Destroy(image.gameObject);
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
                return;
            }

            // LMJ: Dodge Notes can only be processed by dodge action (Q/E keys), not normal inputs
            if (closestNote.noteType == "Dodge")
            {
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

            // Skip Dodge Notes - they can only be handled by dodge action
            if (note.noteType == "Dodge") continue;

            // Skip Defense Notes - they cannot be removed by normal input
            if (note.noteType == "Defense") continue;

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
        Debug.Log($"[RhythmGameSystem] ShowJudgment called: direction={direction}, result={result}");

        // LMJ: Use JudgmentTextManager if available, otherwise fallback to old system
        if (judgmentTextManager != null)
        {
            Debug.Log("[RhythmGameSystem] Using JudgmentTextManager");
            Debug.Log($"[RhythmGameSystem] Calling ShowJudgment with direction: {direction}, result: {result}");
            judgmentTextManager.ShowJudgment(direction, result);
            Debug.Log("[RhythmGameSystem] ShowJudgment call completed");
            return;
        }
        else
        {
            Debug.LogWarning("[RhythmGameSystem] JudgmentTextManager is null, using fallback system");
        }

        // LMJ: Fallback to old judgment display system
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

        // Unified pulse effect size for all directions (Center style)
        pulseRect.sizeDelta = new Vector2(150, 10);

        pulseRect.anchoredPosition = Vector2.zero;

        PulseAnimation anim = pulse.AddComponent<PulseAnimation>();
        anim.Initialize(direction);
    }

    Color GetDirectionColor(string direction)
    {
        // 모든 방향 통일 - 완전 노란색
        return new Color(1f, 1f, 0f, 0.8f); // Yellow
    }

    public float GetCurrentGameTime()
    {
        return NoteTimeManager.Instance.GetNoteTime() - GameStartTime;
    }

    // LMJ: Method to clear Dodge Notes when dodging - gives Perfect judgment without damage
    public void ClearDodgeNotesOnScreen()
    {
        List<RhythmNote> notesToRemove = new List<RhythmNote>();
        int dodgeCount = 0;

        foreach (RhythmNote note in allNotes)
        {
            if (note != null && note.noteType == "Dodge" && IsNoteOnScreen(note))
            {
                notesToRemove.Add(note);
                dodgeCount++;
            }
        }

        // Remove all Dodge Notes and show Perfect judgment for each
        foreach (RhythmNote note in notesToRemove)
        {
            // Show Perfect judgment without damage (Dodge Notes don't deal damage)
            ShowJudgment(note.direction, JudgmentResult.Perfect);

            // Create visual effect for successful dodge
            CreateDodgeSuccessEffect(note.direction);

            allNotes.Remove(note);
            Destroy(note.gameObject);
        }

        if (dodgeCount > 0)
        {
        }
    }

    // Visual effect for successful dodge
    void CreateDodgeSuccessEffect(string direction)
    {
        // You can add visual effects here if needed
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