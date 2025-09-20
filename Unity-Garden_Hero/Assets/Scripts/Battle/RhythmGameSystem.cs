using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// HoldEffectAnimation 클래스 추가
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

        // 확대 및 페이드 아웃
        float scale = Mathf.Lerp(1f, 2f, t);
        rectTransform.localScale = Vector3.one * scale;

        Color color = image.color;
        color.a = Mathf.Lerp(1f, 0f, t);
        image.color = color;

        // 회전
        rectTransform.Rotate(0f, 0f, 180f * Time.deltaTime);
    }
}

// PulseAnimation 클래스
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

// RhythmGameSystem 메인 클래스
public partial class RhythmGameSystem : MonoBehaviour
{
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
    [SerializeField] private ShieldDurabilitySystem shieldDurabilitySystem; // LMJ: 필드명 변경

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
        Debug.Log($"Added {newNotes.Count} notes. Total notes: {allNotes.Count}");
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
        float currentGameTime = Time.time - GameStartTime;

        foreach (var note in allNotes)
        {
            if (note == null) continue;

            float timeUntilHit = note.hitTime - currentGameTime;
            float distanceFromJudgment = timeUntilHit * noteSpeed;

            Vector2 targetPos = GetSpawnPosition(note.direction, distanceFromJudgment);
            note.GetComponent<RectTransform>().anchoredPosition = targetPos;

            if (note.direction == "Up" && note.IsLongNote())
            {
                Debug.Log($"Up Note Position - TimeUntilHit: {timeUntilHit:F2}, Distance: {distanceFromJudgment:F1}, Pos: {targetPos}");
            }
        }
    }

    public void CheckInputs()
    {
        if (Input.GetKeyDown(KeyCode.A)) CheckHitWithLongNote("Left");
        if (Input.GetKeyDown(KeyCode.D)) CheckHitWithLongNote("Right");
        if (Input.GetKeyDown(KeyCode.W)) CheckHitWithLongNote("Up");

        if (Input.GetKeyUp(KeyCode.A)) ReleaseLongNote("Left");
        if (Input.GetKeyUp(KeyCode.D)) ReleaseLongNote("Right");
        if (Input.GetKeyUp(KeyCode.W)) ReleaseLongNote("Up");
    }

    public void CheckHit(string direction)
    {
        CheckHitWithLongNote(direction);
    }

    public void ReleaseHold(string direction)
    {
        ReleaseLongNote(direction);
    }

    // 롱노트 처리가 추가된 CheckHit
    public void CheckHitWithLongNote(string direction)
    {
        // LMJ: 쉴드 비활성화 상태면 입력 무시
        if (shieldDurabilitySystem != null && shieldDurabilitySystem.IsShieldDisabled(direction))
        {
            CreateHitEffect(direction); // 시각적 피드백은 주되
            ShowJudgment(direction, JudgmentResult.Miss); // Miss 표시
            return; // 노트 처리는 안함
        }
        
        float currentGameTime = Time.time - GameStartTime;
        RhythmNote closestNote = null;
        float minTimeDiff = float.MaxValue;

        foreach (var note in allNotes)
        {
            if (note.direction != direction || note.IsHolding()) continue;

            float timeDiff = Mathf.Abs(currentGameTime - note.hitTime);

            // LMJ: Use same logic for all lanes
            if (timeDiff <= missTolerance && timeDiff < minTimeDiff)
            {
                minTimeDiff = timeDiff;
                closestNote = note;
            }
        }

        CreateHitEffect(direction);

        if (closestNote != null)
        {
            if (closestNote.IsLongNote() && !closestNote.HasStartedHold())
            {
                ProcessLongNoteStart(closestNote, minTimeDiff, direction);
            }
            else if (!closestNote.IsLongNote())
            {
                JudgmentResult result = GetJudgment(minTimeDiff);
                ProcessNoteDamage(closestNote, result);
                allNotes.Remove(closestNote);
                Destroy(closestNote.gameObject);
                ShowJudgment(direction, result);
            }
        }
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

        // LMJ: Mark as processed to prevent double judgment
        longNote.SetHit();

        float currentGameTime = Time.time - GameStartTime;
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
        float currentGameTime = Time.time - GameStartTime;

        foreach (var kvp in new Dictionary<string, RhythmNote>(heldNotes))
        {
            string direction = kvp.Key;
            RhythmNote note = kvp.Value;

            if (note == null) continue;

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
            int damage = Mathf.RoundToInt(playerManager.GetAttackPower() * longNoteHeadMultiplier);
            monsterManager.TakeNoteHit(damage, "Long_Head", result);
            
            // LMJ: 롱노트 헤드도 쉴드 데미지 적용
            if (shieldDurabilitySystem != null)
            {
                shieldDurabilitySystem.ProcessNoteResult(note.direction, result);
            }
        }
    }

    void ProcessLongNoteTailDamage(RhythmNote note, JudgmentResult result, float holdProgress)
    {
        if (monsterManager != null && playerManager != null)
        {
            float baseDamage = playerManager.GetAttackPower() * longNoteTailMultiplier;
            float holdBonus = baseDamage * holdProgress * longNoteHoldBonusMultiplier;
            int totalDamage = Mathf.RoundToInt(baseDamage + holdBonus);

            monsterManager.TakeNoteHit(totalDamage, "Long_Tail", result);

            Debug.Log($"Long Note Tail - Base: {baseDamage}, Bonus: {holdBonus}, Total: {totalDamage}");
            
            // LMJ: 롱노트 테일도 쉴드 데미지 적용
            if (shieldDurabilitySystem != null)
            {
                shieldDurabilitySystem.ProcessNoteResult(note.direction, result);
            }
        }
    }

    void AddHoldBonus(RhythmNote note)
    {
        if (monsterManager != null && playerManager != null)
        {
            int bonusDamage = Mathf.RoundToInt(playerManager.GetAttackPower() * 0.05f);
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
        float currentGameTime = Time.time - GameStartTime;

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

            // LMJ: 몬스터 데미지 (히트 성공시)
            if (result != JudgmentResult.Miss && noteType != "Dodge")
            {
                monsterManager.TakeNoteHit(playerManager.GetAttackPower(), noteType, result);
            }

            // LMJ: 플레이어 데미지 (Miss시 또는 Dodge 히트시)
            playerManager.ProcessNoteResult(monsterManager.GetAttackPower(), noteType, result);
            
            // LMJ: 쉴드 데미지 (히트 성공시만, 비활성화 상태가 아닐 때만)
            if (shieldDurabilitySystem != null)
            {
                shieldDurabilitySystem.ProcessNoteResult(note.direction, result);
            }
        }
    }

    void CheckMissedNotes()
    {
        float currentGameTime = Time.time - GameStartTime;

        for (int i = allNotes.Count - 1; i >= 0; i--)
        {
            RhythmNote note = allNotes[i];
            if (note == null || note.IsHit()) continue;  // LMJ: Skip already processed notes

            if (currentGameTime > note.hitTime + missTolerance && !note.IsHolding())
            {
                ProcessNoteDamage(note, JudgmentResult.Miss);
                ShowJudgment(note.direction, JudgmentResult.Miss);
                allNotes.RemoveAt(i);
                Destroy(note.gameObject);
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
        return Time.time - GameStartTime;
    }
}