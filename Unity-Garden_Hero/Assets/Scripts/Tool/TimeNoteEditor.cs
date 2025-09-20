using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
using Newtonsoft.Json;

public class TimeNoteEditor : MonoBehaviour
{
    [System.Serializable]
    public class Note
    {
        public string id;
        public float time;
        public NoteType type;
        public TrackDirection direction;
        public float duration;
        public float snapValue;
        public GameObject visualObject;
        public bool isHit;
        public JudgmentResult judgment;

        public Note(float time, NoteType type, TrackDirection direction, float duration = 0f, float snapValue = 1f)
        {
            this.id = System.Guid.NewGuid().ToString();
            this.time = time;
            this.type = type;
            this.direction = direction;
            this.duration = duration;
            this.snapValue = snapValue;
            this.isHit = false;
            this.judgment = JudgmentResult.None;
        }
    }

    public enum NoteType
    {
        Normal,
        Charged,
        Special,
        Defense
    }

    public enum TrackDirection
    {
        Left,
        Up,
        Right
    }

    public enum JudgmentResult
    {
        None,
        Perfect,
        Good,
        Miss
    }

    [Header("Timeline Settings")]
    [SerializeField] private float totalDuration = 60f;
    [SerializeField] private float pixelsPerSecond = 100f;
    [SerializeField] private float playbackSpeed = 1f;
    [SerializeField] private bool syncEndTimeInput = true;
    [SerializeField] private float playheadFixedTime = 2f;

    [Header("Judgment Settings")]
    [SerializeField] private float perfectTolerance = 0.2f;
    [SerializeField] private float goodTolerance = 0.4f;

    [Header("UI References")]
    [SerializeField] private RectTransform timelineContent;
    [SerializeField] private RectTransform[] tracks;
    [SerializeField] private RectTransform playhead;
    [SerializeField] private ScrollRect timelineScrollRect;
    [SerializeField] private TMP_InputField patternNameInput;
    [SerializeField] private TMP_InputField endTimeInput;
    [SerializeField] private TMP_InputField currentTimeInput;
    [SerializeField] private TMP_InputField perfectToleranceInput;
    [SerializeField] private TMP_InputField goodToleranceInput;
    [SerializeField] private TextMeshProUGUI noteCountText;
    [SerializeField] private TextMeshProUGUI currentTimeText;
    [SerializeField] private TextMeshProUGUI totalTimeText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI historyCountText;
    [SerializeField] private TextMeshProUGUI judgmentStatsText;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private Toggle snapToggle;
    [SerializeField] private TMP_Dropdown snapValueDropdown;
    [SerializeField] private Button[] noteTypeButtons;
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button exportButton;
    [SerializeField] private Button importButton;
    [SerializeField] private Button[] hitButtons;
    [SerializeField] private TMP_Dropdown fileSelectDropdown;
    [SerializeField] private Button refreshFilesButton;

    [Header("Note Prefabs")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject dragPreviewPrefab;
    [SerializeField] private GameObject hitMarkerPrefab;
    [SerializeField] private GameObject gridLinePrefab;

    [Header("Note Colors")]
    [SerializeField] private Color normalNoteColor = new Color(0, 1, 0, 0.7f);
    [SerializeField] private Color chargedNoteColor = new Color(0, 0.4f, 1, 0.7f);
    [SerializeField] private Color specialNoteColor = new Color(1, 1, 0, 0.7f);
    [SerializeField] private Color defenseNoteColor = new Color(1, 0, 1, 0.7f);

    [Header("Judgment Colors")]
    [SerializeField] private Color perfectColor = new Color(1, 1, 0, 0.8f);
    [SerializeField] private Color goodColor = new Color(0, 1, 0, 0.8f);
    [SerializeField] private Color missColor = new Color(1, 0, 0, 0.8f);

    private RectTransform ruler;
    private List<Note> notes = new List<Note>();
    private List<string> history = new List<string>();
    private const int MAX_HISTORY = 50;
    private bool isPlaying = false;
    private float currentTime = 0f;
    private NoteType selectedNoteType = NoteType.Normal;
    private bool isDragging = false;
    private float dragStartTime;
    private int currentTrackIndex = -1;
    private GameObject dragPreview;
    private HashSet<string> hitNotes = new HashSet<string>();
    private List<GameObject> hitMarkers = new List<GameObject>();

    private int perfectCount = 0;
    private int goodCount = 0;
    private int missCount = 0;

    // LMJ: Get MonsterPatterns directory path and create if not exists
    private string GetPatternsDirectory()
    {
        string path = Path.Combine(Application.dataPath, "MonsterPatterns");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    void Start()
    {
        InitializeUI();
        SetupRuler();
        SetupPlayhead();
        DrawGrid();
        UpdateHistoryUI();
        UpdateJudgmentStats();
        if (syncEndTimeInput)
        {
            UpdateEndTimeDisplay();
        }
        RefreshFileList();
    }

    void SetupRuler()
    {
        if (ruler != null && timelineContent != null)
        {
            ruler.SetParent(timelineContent);

            ruler.anchorMin = new Vector2(0, 1);
            ruler.anchorMax = new Vector2(0, 1);
            ruler.pivot = new Vector2(0, 1);
            ruler.sizeDelta = new Vector2(totalDuration * pixelsPerSecond, 30);
            ruler.anchoredPosition = new Vector2(0, 30);

            Image rulerImage = ruler.GetComponent<Image>();
            if (rulerImage == null)
            {
                rulerImage = ruler.gameObject.AddComponent<Image>();
            }
            rulerImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }
    }

    void SetupPlayhead()
    {
        if (playhead != null)
        {
            if (playhead.parent == timelineContent)
            {
                playhead.SetParent(timelineContent.parent);
            }

            playhead.anchorMin = new Vector2(0, 0);
            playhead.anchorMax = new Vector2(0, 1);
            playhead.pivot = new Vector2(0.5f, 0.5f);
            playhead.sizeDelta = new Vector2(2, 0);

            Image playheadImage = playhead.GetComponent<Image>();
            if (playheadImage != null)
            {
                playheadImage.color = new Color(1, 0, 0, 0.8f);
            }
        }
    }

    void Update()
    {
        if (isPlaying)
        {
            currentTime += Time.deltaTime * playbackSpeed;
            if (currentTime >= totalDuration)
            {
                Stop();
            }
            else
            {
                UpdatePlayhead();
                UpdateTimeDisplay();
            }
        }

        Transform labelsContainer = timelineContent.parent.Find("TimeLabelsContainer");
        if (labelsContainer != null)
        {
            RectTransform labelsRect = labelsContainer.GetComponent<RectTransform>();
            labelsRect.anchoredPosition = new Vector2(timelineContent.anchoredPosition.x, 240);
        }

        HandleKeyboardInput();
        HandleMouseInput();
    }

    void InitializeUI()
    {
        playButton.onClick.AddListener(Play);
        pauseButton.onClick.AddListener(Pause);
        stopButton.onClick.AddListener(Stop);
        resetButton.onClick.AddListener(ResetAllNotes);
        undoButton.onClick.AddListener(Undo);
        exportButton.onClick.AddListener(ExportJSON);
        importButton.onClick.AddListener(ImportSelectedJSON);

        speedSlider.onValueChanged.AddListener(OnSpeedChanged);

        if (syncEndTimeInput && endTimeInput != null)
        {
            endTimeInput.onEndEdit.AddListener(OnEndTimeChanged);
        }

        if (currentTimeInput != null)
        {
            currentTimeInput.onEndEdit.AddListener(OnCurrentTimeChanged);
        }

        if (perfectToleranceInput != null)
        {
            perfectToleranceInput.text = perfectTolerance.ToString("F2");
            perfectToleranceInput.onEndEdit.AddListener(OnPerfectToleranceChanged);
        }

        if (goodToleranceInput != null)
        {
            goodToleranceInput.text = goodTolerance.ToString("F2");
            goodToleranceInput.onEndEdit.AddListener(OnGoodToleranceChanged);
        }

        if (refreshFilesButton != null)
        {
            refreshFilesButton.onClick.AddListener(RefreshFileList);
        }

        for (int i = 0; i < noteTypeButtons.Length; i++)
        {
            int index = i;
            noteTypeButtons[i].onClick.AddListener(() => SelectNoteType((NoteType)index));
        }
        for (int i = 0; i < hitButtons.Length; i++)
        {
            int index = i;
            hitButtons[i].onClick.AddListener(() => CheckNotesAtPosition((TrackDirection)index));
        }
        SetHitButtonsInteractable(false);
        SelectNoteType(NoteType.Normal);
    }

    private void OnPerfectToleranceChanged(string value)
    {
        if (float.TryParse(value, out float newValue))
        {
            perfectTolerance = Mathf.Max(0.01f, newValue);
            perfectToleranceInput.text = perfectTolerance.ToString("F2");
            statusText.text = $"Perfect tolerance set to ±{perfectTolerance:F2}s";
        }
    }

    private void OnGoodToleranceChanged(string value)
    {
        if (float.TryParse(value, out float newValue))
        {
            goodTolerance = Mathf.Max(perfectTolerance + 0.01f, newValue);
            goodToleranceInput.text = goodTolerance.ToString("F2");
            statusText.text = $"Good tolerance set to ±{goodTolerance:F2}s";
        }
    }

    private void UpdateJudgmentStats()
    {
        if (judgmentStatsText != null)
        {
            int totalJudged = perfectCount + goodCount + missCount;
            judgmentStatsText.text = $"Perfect: {perfectCount} | Good: {goodCount} | Miss: {missCount} | Total: {totalJudged}";
        }
    }

    // LMJ: Updated to use MonsterPatterns directory
    private void RefreshFileList()
    {
        if (fileSelectDropdown == null) return;

        fileSelectDropdown.options.Clear();
        string patternsPath = GetPatternsDirectory();
        string[] jsonFiles = Directory.GetFiles(patternsPath, "*.json");
        
        foreach (string file in jsonFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            fileSelectDropdown.options.Add(new TMP_Dropdown.OptionData(fileName));
        }

        fileSelectDropdown.RefreshShownValue();
        statusText.text = $"Found {jsonFiles.Length} JSON files";
    }

    private void OnCurrentTimeChanged(string value)
    {
        float newTime = ParseTimeInput(value);
        newTime = Mathf.Clamp(newTime, 0, totalDuration);

        currentTime = newTime;
        UpdatePlayhead();
        UpdateTimeDisplay();

        statusText.text = $"Current time set to {currentTime:F1} seconds";
    }

    void UpdateCurrentTimeDisplay()
    {
        if (currentTimeInput != null)
        {
            currentTimeInput.text = currentTime.ToString("F1");
        }
    }

    float ParseTimeInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return totalDuration;
        input = input.Trim();

        if (input.Contains(":"))
        {
            string[] parts = input.Split(':');
            if (parts.Length == 2 && float.TryParse(parts[0], out float minutes) && float.TryParse(parts[1], out float seconds))
            {
                return minutes * 60f + seconds;
            }
        }
        else if (float.TryParse(input, out float seconds))
        {
            return Mathf.Max(1f, seconds);
        }

        return totalDuration;
    }

    void OnEndTimeChanged(string value)
    {
        if (!syncEndTimeInput) return;

        float newDuration = ParseTimeInput(value);

        if (Mathf.Abs(newDuration - totalDuration) > 0.01f)
        {
            totalDuration = newDuration;

            if (currentTime >= totalDuration)
            {
                Stop();
            }

            DrawGrid();
            statusText.text = $"Timeline duration updated to {totalDuration:F1} seconds";
        }
    }

    void UpdateEndTimeDisplay()
    {
        if (endTimeInput != null)
        {
            endTimeInput.text = totalDuration.ToString("F1");
        }
    }

    void OnValidate()
    {
        totalDuration = Mathf.Max(1f, totalDuration);

        if (Application.isPlaying && syncEndTimeInput && endTimeInput != null)
        {
            endTimeInput.text = totalDuration.ToString("F1");
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DelayedDrawGrid());
            }
        }
    }

    System.Collections.IEnumerator DelayedDrawGrid()
    {
        yield return null;
        DrawGrid();
    }

    void DrawGrid()
    {
        foreach (RectTransform track in tracks)
        {
            foreach (Transform child in track)
            {
                if (child.name.StartsWith("GridLine"))
                    Destroy(child.gameObject);
            }
        }

        Transform oldContainer = timelineContent.Find("TimeLabelsContainer");
        if (oldContainer != null) Destroy(oldContainer.gameObject);

        float totalWidth = totalDuration * pixelsPerSecond;
        timelineContent.sizeDelta = new Vector2(totalWidth, timelineContent.sizeDelta.y);

        for (int i = 0; i < tracks.Length; i++)
        {
            tracks[i].anchorMin = new Vector2(0, tracks[i].anchorMin.y);
            tracks[i].anchorMax = new Vector2(0, tracks[i].anchorMax.y);
            tracks[i].pivot = new Vector2(0, tracks[i].pivot.y);
            tracks[i].sizeDelta = new Vector2(totalWidth, tracks[i].sizeDelta.y);
            Vector2 currentPos = tracks[i].anchoredPosition;
            tracks[i].anchoredPosition = new Vector2(0, currentPos.y);
        }

        GameObject timeLabelsContainer = new GameObject("TimeLabelsContainer");
        RectTransform labelsRect = timeLabelsContainer.AddComponent<RectTransform>();

        timeLabelsContainer.transform.SetParent(timelineContent);
        labelsRect.anchorMin = new Vector2(0, 1);
        labelsRect.anchorMax = new Vector2(0, 1);
        labelsRect.pivot = new Vector2(0, 1);
        labelsRect.anchoredPosition = new Vector2(0, 0);
        labelsRect.sizeDelta = new Vector2(totalWidth, 25);
        labelsRect.localScale = Vector3.one;

        Image bgImage = timeLabelsContainer.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 1f);
        bgImage.raycastTarget = false;
        int maxGridCount = Mathf.FloorToInt(totalDuration);

        for (int i = 0; i <= maxGridCount; i++)
        {
            float x = i * pixelsPerSecond;
            for (int trackIdx = 0; trackIdx < tracks.Length; trackIdx++)
            {
                GameObject gridLine = new GameObject($"GridLine_{i}_{trackIdx}");
                gridLine.transform.SetParent(tracks[trackIdx]);

                Image gridImage = gridLine.AddComponent<Image>();
                gridImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                gridImage.raycastTarget = false;

                RectTransform gridRect = gridLine.GetComponent<RectTransform>();
                gridRect.anchorMin = new Vector2(0, 0);
                gridRect.anchorMax = new Vector2(0, 1);
                gridRect.pivot = new Vector2(0, 0.5f);
                gridRect.anchoredPosition = new Vector2(x, 0);
                gridRect.sizeDelta = new Vector2(2, 0);
            }

            GameObject label = new GameObject($"TimeLabel_{i}");
            label.transform.SetParent(timeLabelsContainer.transform);

            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.zero;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(x, 10);
            labelRect.sizeDelta = new Vector2(40, 20);
            labelRect.localScale = Vector3.one;

            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = $"{i}s";
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.raycastTarget = false;

            if (i < Mathf.FloorToInt(totalDuration))
            {
                for (int j = 1; j < 10; j++)
                {
                    float minorX = x + j * (pixelsPerSecond / 10);
                    for (int trackIdx = 0; trackIdx < tracks.Length; trackIdx++)
                    {
                        GameObject minorLine = new GameObject($"GridLine_Minor_{i}_{j}_{trackIdx}");
                        minorLine.transform.SetParent(tracks[trackIdx]);
                        Image minorImage = minorLine.AddComponent<Image>();
                        minorImage.color = new Color(0.4f, 0.4f, 0.4f, 0.15f);
                        minorImage.raycastTarget = false;
                        RectTransform minorRect = minorLine.GetComponent<RectTransform>();
                        minorRect.anchorMin = new Vector2(0, 0);
                        minorRect.anchorMax = new Vector2(0, 1);
                        minorRect.pivot = new Vector2(0, 0.5f);
                        minorRect.anchoredPosition = new Vector2(minorX, 0);
                        minorRect.sizeDelta = new Vector2(1, 0);
                    }
                }
            }
        }

        if (playhead != null) playhead.SetAsLastSibling();

        foreach (GameObject marker in hitMarkers)
        {
            if (marker != null) marker.transform.SetAsLastSibling();
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            for (int i = 0; i < tracks.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(tracks[i], mousePos))
                {
                    HandleTrackClick(i, mousePos);
                    break;
                }
            }
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateDragPreview();
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            FinishDrag();
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryDeleteNote();
        }
    }

    void HandleTrackClick(int trackIndex, Vector2 mousePos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            timelineContent, mousePos, null, out Vector2 localPoint);

        float clickTime = localPoint.x / pixelsPerSecond;
        float snapValue = GetSnapValue();

        if (snapToggle.isOn)
        {
            clickTime = Mathf.Round(clickTime / snapValue) * snapValue;
        }

        clickTime = Mathf.Clamp(clickTime, 0, totalDuration);

        TrackDirection direction = (TrackDirection)trackIndex;

        if (CheckNoteOverlap(direction, clickTime, clickTime + snapValue))
        {
            statusText.text = "Cannot place note - position already occupied";
            return;
        }

        currentTrackIndex = trackIndex;
        dragStartTime = clickTime;

        if (selectedNoteType == NoteType.Charged)
        {
            isDragging = true;
            dragPreview = Instantiate(dragPreviewPrefab, tracks[trackIndex]);
            RectTransform previewRect = dragPreview.GetComponent<RectTransform>();
            previewRect.anchoredPosition = new Vector2(clickTime * pixelsPerSecond, 0);
            previewRect.sizeDelta = new Vector2(0, 40);
        }
        else
        {
            CreateNote(clickTime, selectedNoteType, direction, 0, snapValue);
            statusText.text = $"Placed {selectedNoteType} note at {clickTime:F2}s on {direction} track";
        }
    }

    void UpdateDragPreview()
    {
        if (dragPreview == null || currentTrackIndex < 0) return;

        Vector2 mousePos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            timelineContent, mousePos, null, out Vector2 localPoint);

        float currentDragTime = localPoint.x / pixelsPerSecond;
        float snapValue = GetSnapValue();

        if (snapToggle.isOn)
        {
            currentDragTime = Mathf.Round(currentDragTime / snapValue) * snapValue;
        }

        float duration = Mathf.Abs(currentDragTime - dragStartTime);
        float startTime = Mathf.Min(dragStartTime, currentDragTime);

        RectTransform previewRect = dragPreview.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0, 0.5f);
        previewRect.anchorMax = new Vector2(0, 0.5f);
        previewRect.pivot = new Vector2(0, 0.5f);
        previewRect.anchoredPosition = new Vector2(startTime * pixelsPerSecond, 0);
        previewRect.sizeDelta = new Vector2(duration * pixelsPerSecond, 40);
    }

    void FinishDrag()
    {
        if (!isDragging || dragPreview == null) return;

        Vector2 mousePos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            timelineContent, mousePos, null, out Vector2 localPoint);

        float endTime = localPoint.x / pixelsPerSecond;
        float snapValue = GetSnapValue();

        if (snapToggle.isOn)
        {
            endTime = Mathf.Round(endTime / snapValue) * snapValue;
        }

        float duration = Mathf.Abs(endTime - dragStartTime);
        float startTime = Mathf.Min(dragStartTime, endTime);

        if (duration > 0.05f)
        {
            TrackDirection direction = (TrackDirection)currentTrackIndex;
            if (!CheckNoteOverlap(direction, startTime, startTime + duration))
            {
                CreateNote(startTime, NoteType.Charged, direction, duration, snapValue);
                statusText.text = $"Placed charged note at {startTime:F2}s - {startTime + duration:F2}s on {direction} track";
            }
            else
            {
                statusText.text = "Cannot place charged note - overlaps with existing note";
            }
        }

        Destroy(dragPreview);
        dragPreview = null;
        isDragging = false;
        currentTrackIndex = -1;
    }

    bool CheckNoteOverlap(TrackDirection direction, float startTime, float endTime, string excludeId = null)
    {
        return notes.Any(note =>
        {
            if (note.direction != direction || note.id == excludeId) return false;

            float noteStart = note.time;
            float noteEnd = note.type == NoteType.Charged ? note.time + note.duration : note.time;

            if (note.type != NoteType.Charged && endTime == startTime)
            {
                return noteStart == startTime;
            }

            return !(endTime <= noteStart || startTime >= noteEnd);
        });
    }

    void CreateNote(float time, NoteType type, TrackDirection direction, float duration = 0f, float snapValue = 1f, bool skipHistory = false)
    {
        if (!skipHistory) SaveToHistory();

        Note note = new Note(time, type, direction, duration, snapValue);
        notes.Add(note);
        RenderNote(note);
        UpdateNoteCount();
    }

    void RenderNote(Note note)
    {
        GameObject noteObj = Instantiate(notePrefab, tracks[(int)note.direction]);
        note.visualObject = noteObj;

        RectTransform noteRect = noteObj.GetComponent<RectTransform>();
        float noteWidth = pixelsPerSecond * note.snapValue;

        noteRect.anchoredPosition = new Vector2(note.time * pixelsPerSecond, 0);

        if (note.type == NoteType.Charged && note.duration > 0)
        {
            noteRect.sizeDelta = new Vector2(note.duration * pixelsPerSecond, 40);
        }
        else
        {
            noteRect.sizeDelta = new Vector2(noteWidth, 40);
        }

        Image noteImage = noteObj.GetComponent<Image>();
        noteImage.color = GetNoteColor(note.type);

        if (note.isHit)
        {
            ApplyJudgmentVisual(note);
        }

        TextMeshProUGUI noteText = noteObj.GetComponentInChildren<TextMeshProUGUI>();
        if (noteText != null)
        {
            noteText.text = GetNoteIcon(note.type);
        }
    }

    void ApplyJudgmentVisual(Note note)
    {
        if (note.visualObject == null) return;

        Image noteImage = note.visualObject.GetComponent<Image>();
        Color baseColor = GetNoteColor(note.type);

        switch (note.judgment)
        {
            case JudgmentResult.Perfect:
                noteImage.color = Color.Lerp(baseColor, perfectColor, 0.7f);
                break;
            case JudgmentResult.Good:
                noteImage.color = Color.Lerp(baseColor, goodColor, 0.7f);
                break;
            case JudgmentResult.Miss:
                noteImage.color = Color.Lerp(baseColor, missColor, 0.7f);
                break;
            default:
                Color hitColor = baseColor;
                hitColor.a = 0.3f;
                noteImage.color = hitColor;
                break;
        }
    }

    Color GetNoteColor(NoteType type)
    {
        switch (type)
        {
            case NoteType.Normal: return normalNoteColor;
            case NoteType.Charged: return chargedNoteColor;
            case NoteType.Special: return specialNoteColor;
            case NoteType.Defense: return defenseNoteColor;
            default: return Color.white;
        }
    }

    string GetNoteIcon(NoteType type)
    {
        switch (type)
        {
            case NoteType.Normal: return "N";
            case NoteType.Charged: return "C";
            case NoteType.Special: return "S";
            case NoteType.Defense: return "D";
            default: return "";
        }
    }

    void TryDeleteNote()
    {
        Vector2 mousePos = Input.mousePosition;

        foreach (Note note in notes)
        {
            if (note.visualObject != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    note.visualObject.GetComponent<RectTransform>(), mousePos))
            {
                DeleteNote(note.id);
                break;
            }
        }
    }

    void DeleteNote(string noteId)
    {
        SaveToHistory();

        Note noteToDelete = notes.Find(n => n.id == noteId);
        if (noteToDelete != null)
        {
            if (noteToDelete.visualObject != null)
            {
                Destroy(noteToDelete.visualObject);
            }
            notes.Remove(noteToDelete);
            hitNotes.Remove(noteId);
            UpdateNoteCount();
        }
    }

    void UpdateNoteCount()
    {
        noteCountText.text = notes.Count.ToString();
    }

    void SaveToHistory()
    {
        PatternData currentState = new PatternData
        {
            patternName = patternNameInput.text,
            totalDuration = totalDuration,
            perfectTolerance = perfectTolerance,
            goodTolerance = goodTolerance,
            notes = notes.Select(n => new NoteData
            {
                time = n.time,
                type = n.type.ToString(),
                direction = n.direction.ToString(),
                duration = n.duration,
                snapValue = n.snapValue
            }).ToList()
        };

        string json = JsonUtility.ToJson(currentState);

        if (history.Count >= MAX_HISTORY)
        {
            history.RemoveAt(0);
        }
        history.Add(json);

        UpdateHistoryUI();
    }

    void UpdateHistoryUI()
    {
        historyCountText.text = $"{history.Count}/{MAX_HISTORY}";
        undoButton.interactable = history.Count > 0;
    }

    void Undo()
    {
        if (history.Count == 0) return;

        string lastState = history[history.Count - 1];
        history.RemoveAt(history.Count - 1);

        PatternData data = JsonUtility.FromJson<PatternData>(lastState);

        ClearAllNotes();

        foreach (NoteData noteData in data.notes)
        {
            NoteType type = (NoteType)System.Enum.Parse(typeof(NoteType), noteData.type);
            TrackDirection direction = (TrackDirection)System.Enum.Parse(typeof(TrackDirection), noteData.direction);
            float snapValue = noteData.snapValue > 0 ? noteData.snapValue : 1f;
            CreateNote(noteData.time, type, direction, noteData.duration, snapValue, true);
        }

        UpdateHistoryUI();
        statusText.text = "Undo completed";
    }

    void ResetAllNotes()
    {
        if (notes.Count == 0)
        {
            statusText.text = "No notes to reset";
            return;
        }

        SaveToHistory();
        ClearAllNotes();
        ResetJudgmentStats();
        statusText.text = "All notes have been reset";
    }

    void ClearAllNotes()
    {
        foreach (Note note in notes)
        {
            if (note.visualObject != null)
            {
                Destroy(note.visualObject);
            }
        }
        notes.Clear();
        hitNotes.Clear();
        UpdateNoteCount();
    }

    void ResetJudgmentStats()
    {
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
        UpdateJudgmentStats();
    }

    void Play()
    {
        if (isPlaying) return;
        isPlaying = true;
        SetHitButtonsInteractable(true);
    }

    void Pause()
    {
        isPlaying = false;
        SetHitButtonsInteractable(false);
    }

    void Stop()
    {
        isPlaying = false;
        currentTime = 0f;
        
        ResetTimelinePosition();
        
        UpdatePlayhead();
        UpdateTimeDisplay();

        hitNotes.Clear();
        foreach (Note note in notes)
        {
            note.isHit = false;
            note.judgment = JudgmentResult.None;
            if (note.visualObject != null)
            {
                Image img = note.visualObject.GetComponent<Image>();
                Color color = GetNoteColor(note.type);
                img.color = color;
            }
        }

        foreach (GameObject marker in hitMarkers)
        {
            Destroy(marker);
        }
        hitMarkers.Clear();

        ResetJudgmentStats();
        SetHitButtonsInteractable(false);
    }

    void SetHitButtonsInteractable(bool interactable)
    {
        foreach (Button btn in hitButtons)
        {
            btn.interactable = interactable;
        }
    }

    void UpdatePlayhead()
    {
        if (playhead != null)
        {
            float fixedPlayheadX = playheadFixedTime * pixelsPerSecond;
            
            if (currentTime <= playheadFixedTime)
            {
                Vector2 currentPos = playhead.anchoredPosition;
                playhead.anchoredPosition = new Vector2(currentTime * pixelsPerSecond, currentPos.y);
                
                if (timelineContent != null)
                {
                    Vector2 contentPos = timelineContent.anchoredPosition;
                    timelineContent.anchoredPosition = new Vector2(0, contentPos.y);
                }
            }
            else
            {
                Vector2 currentPos = playhead.anchoredPosition;
                playhead.anchoredPosition = new Vector2(fixedPlayheadX, currentPos.y);
                
                if (timelineContent != null)
                {
                    float offset = (currentTime - playheadFixedTime) * pixelsPerSecond;
                    Vector2 contentPos = timelineContent.anchoredPosition;
                    timelineContent.anchoredPosition = new Vector2(-offset, contentPos.y);
                }
            }
        }
    }

    void ResetTimelinePosition()
    {
        if (timelineContent != null)
        {
            Vector2 contentPos = timelineContent.anchoredPosition;
            timelineContent.anchoredPosition = new Vector2(0, contentPos.y);
        }
    }

    void UpdateTimeDisplay()
    {
        int mins = Mathf.FloorToInt(currentTime / 60);
        float secs = currentTime % 60;
        currentTimeText.text = $"{mins:00}:{secs:00.0}";

        UpdateCurrentTimeDisplay();
    }

    void CheckNotesAtPosition(TrackDirection direction)
    {
        if (!isPlaying) return;

        AddHitMarker(direction.ToString());

        foreach (Note note in notes)
        {
            if (note.direction != direction || note.isHit) continue;

            float noteStart = note.time;
            float noteEnd = note.type == NoteType.Charged ? note.time + note.duration : note.time;

            JudgmentResult judgment = CalculateJudgment(note, currentTime);

            if (judgment != JudgmentResult.None)
            {
                hitNotes.Add(note.id);
                note.isHit = true;
                note.judgment = judgment;

                switch (judgment)
                {
                    case JudgmentResult.Perfect:
                        perfectCount++;
                        break;
                    case JudgmentResult.Good:
                        goodCount++;
                        break;
                    case JudgmentResult.Miss:
                        missCount++;
                        break;
                }

                ApplyJudgmentVisual(note);
                UpdateJudgmentStats();

                statusText.text = $"{judgment} - {direction} track at {currentTime:F2}s";
            }
        }
    }

    JudgmentResult CalculateJudgment(Note note, float hitTime)
    {
        float noteStart = note.time;
        float noteEnd = note.type == NoteType.Charged ? note.time + note.duration : note.time;

        float timeDifference;

        if (note.type == NoteType.Charged)
        {
            if (hitTime >= noteStart && hitTime <= noteEnd)
            {
                timeDifference = 0f;
            }
            else
            {
                timeDifference = Mathf.Min(Mathf.Abs(hitTime - noteStart), Mathf.Abs(hitTime - noteEnd));
            }
        }
        else
        {
            timeDifference = Mathf.Abs(hitTime - noteStart);
        }

        if (timeDifference <= perfectTolerance)
        {
            return JudgmentResult.Perfect;
        }
        else if (timeDifference <= goodTolerance)
        {
            return JudgmentResult.Good;
        }
        else if (timeDifference <= goodTolerance + 0.2f)
        {
            return JudgmentResult.Miss;
        }

        return JudgmentResult.None;
    }

    void AddHitMarker(string key)
    {
        for (int i = 0; i < tracks.Length; i++)
        {
            GameObject marker = new GameObject($"HitMarker_{currentTime:F2}_{key}_Track{i}");
            marker.transform.SetParent(tracks[i], false);

            Image markerImage = marker.AddComponent<Image>();
            markerImage.color = new Color(1, 1, 0, 0.8f);
            markerImage.raycastTarget = false;

            RectTransform markerRect = marker.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0, 0);
            markerRect.anchorMax = new Vector2(0, 1);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(2, 0);
            markerRect.anchoredPosition = new Vector2(currentTime * pixelsPerSecond, 0);
            markerRect.localScale = Vector3.one;

            hitMarkers.Add(marker);
        }
    }

    void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPlaying) Pause();
            else Play();
        }

        if (isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                CheckNotesAtPosition(TrackDirection.Left);
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                CheckNotesAtPosition(TrackDirection.Up);
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                CheckNotesAtPosition(TrackDirection.Right);
            }
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
        {
            Undo();
        }
    }

    void SelectNoteType(NoteType type)
    {
        selectedNoteType = type;

        if (timelineScrollRect != null)
        {
            timelineScrollRect.horizontal = (type != NoteType.Charged);
        }

        for (int i = 0; i < noteTypeButtons.Length; i++)
        {
            ColorBlock colors = noteTypeButtons[i].colors;
            colors.normalColor = (i == (int)type) ? new Color(0.4f, 0.6f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
            noteTypeButtons[i].colors = colors;
        }
    }

    void OnSpeedChanged(float value)
    {
        playbackSpeed = value;
        speedText.text = $"{value:F1}x";
    }

    float GetSnapValue()
    {
        string[] snapValues = { "0.1", "0.25", "0.5", "1.0" };
        return float.Parse(snapValues[snapValueDropdown.value]);
    }

    // LMJ: Updated to save to MonsterPatterns directory
    void ExportJSON()
    {
        string fileName = patternNameInput.text;
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "pattern";
        }

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        PatternData data = new PatternData
        {
            patternName = patternNameInput.text,
            totalDuration = totalDuration,
            perfectTolerance = perfectTolerance,
            goodTolerance = goodTolerance,
            notes = notes.Select(n => new NoteData
            {
                time = n.time,
                type = n.type.ToString(),
                direction = n.direction.ToString(),
                duration = n.duration,
                snapValue = n.snapValue
            }).ToList()
        };

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        string path = Path.Combine(GetPatternsDirectory(), fileName + ".json");
        File.WriteAllText(path, json);

        statusText.text = $"Exported to: Assets/MonsterPatterns/{fileName}.json";
        RefreshFileList(); // LMJ: Refresh file list after export
    }

    // LMJ: Updated to load from MonsterPatterns directory
    void ImportSelectedJSON()
    {
        if (fileSelectDropdown == null || fileSelectDropdown.options.Count == 0)
        {
            statusText.text = "No files available. Click Refresh Files.";
            return;
        }

        string selectedFileName = fileSelectDropdown.options[fileSelectDropdown.value].text + ".json";
        string path = Path.Combine(GetPatternsDirectory(), selectedFileName);

        ImportFromPath(path);
    }

    void ImportFromPath(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            PatternData data = JsonConvert.DeserializeObject<PatternData>(json);

            ClearAllNotes();
            ResetJudgmentStats();

            patternNameInput.text = data.patternName;
            totalDuration = data.totalDuration;

            if (data.perfectTolerance > 0)
            {
                perfectTolerance = data.perfectTolerance;
                if (perfectToleranceInput != null)
                    perfectToleranceInput.text = perfectTolerance.ToString("F2");
            }

            if (data.goodTolerance > 0)
            {
                goodTolerance = data.goodTolerance;
                if (goodToleranceInput != null)
                    goodToleranceInput.text = goodTolerance.ToString("F2");
            }

            if (syncEndTimeInput)
            {
                UpdateEndTimeDisplay();
            }

            foreach (NoteData noteData in data.notes)
            {
                NoteType type = (NoteType)Enum.Parse(typeof(NoteType), noteData.type);
                TrackDirection direction = (TrackDirection)Enum.Parse(typeof(TrackDirection), noteData.direction);
                float snapValue = noteData.snapValue > 0 ? noteData.snapValue : 1f;
                CreateNote(noteData.time, type, direction, noteData.duration, snapValue, true);
            }

            DrawGrid();
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            statusText.text = $"Successfully imported {fileName}: {notes.Count} notes";
        }
        catch (Exception e)
        {
            statusText.text = $"Import failed: {e.Message}";
        }
    }

    [System.Serializable]
    public class PatternData
    {
        public string patternName;
        public float totalDuration;
        public float perfectTolerance;
        public float goodTolerance;
        public List<NoteData> notes;
    }

    [System.Serializable]
    public class NoteData
    {
        public float time;
        public string type;
        public string direction;
        public float duration;
        public float snapValue;
    }
}