using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class NoteEditorManager : MonoBehaviour
{
    [Header("References")]
    public AudioManager audioManager;
    public Transform noteContainer;
    public GameObject normalNotePrefab;
    public GameObject longNotePrefab;
    
    [Header("UI Controls")]
    public Button normalNoteButton;
    public Button longNoteButton;
    public Button upDirectionButton;
    public Button leftDirectionButton;
    public Button rightDirectionButton;
    public Button testPlayButton;
    public Button saveButton;
    public Button loadButton;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI noteCountText;
    
    [Header("Timeline")]
    public RectTransform timelineContent;
    public ScrollRect timelineScrollRect;
    public float timelineScale = 100f; // pixels per second
    
    [Header("Grid Settings")]
    public float beatSnapValue = 0.25f; // 1/4 beat snapping
    public bool snapToGrid = true;
    
    private List<NoteData> notes = new List<NoteData>();
    private List<GameObject> noteObjects = new List<GameObject>();
    private NoteData.NoteType currentNoteType = NoteData.NoteType.Normal;
    private NoteData.NoteDirection currentDirection = NoteData.NoteDirection.TopToBottom;
    private bool isPlacingLongNote = false;
    private float longNoteStartTime = 0f;
    
    // Undo system
    private List<List<NoteData>> undoHistory = new List<List<NoteData>>();
    private int maxUndoSteps = 50;
    
    void Start()
    {
        SetupUI();
        UpdateInstructions();
    }
    
    void Update()
    {
        HandleInput();
        UpdateNoteCount();
    }
    
    void SetupUI()
    {
        normalNoteButton.onClick.AddListener(() => SetNoteType(NoteData.NoteType.Normal));
        longNoteButton.onClick.AddListener(() => SetNoteType(NoteData.NoteType.Long));
        
        upDirectionButton.onClick.AddListener(() => SetDirection(NoteData.NoteDirection.TopToBottom));
        leftDirectionButton.onClick.AddListener(() => SetDirection(NoteData.NoteDirection.LeftToRight));
        rightDirectionButton.onClick.AddListener(() => SetDirection(NoteData.NoteDirection.RightToLeft));
        
        testPlayButton.onClick.AddListener(TestPlay);
        saveButton.onClick.AddListener(SaveNotes);
        loadButton.onClick.AddListener(LoadNotes);
        
        // 초기 선택 상태
        SetNoteType(NoteData.NoteType.Normal);
        SetDirection(NoteData.NoteDirection.TopToBottom);
    }
    
    void HandleInput()
    {
        // 스페이스바로 노트 배치
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlaceNote();
        }
        
        // 방향키로 방향 변경
        if (Input.GetKeyDown(KeyCode.UpArrow))
            SetDirection(NoteData.NoteDirection.TopToBottom);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            SetDirection(NoteData.NoteDirection.LeftToRight);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            SetDirection(NoteData.NoteDirection.RightToLeft);
        
        // Ctrl+Z로 실행취소
        if (Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.LeftControl))
        {
            Undo();
        }
        
        // 마우스 우클릭으로 노트 삭제
        if (Input.GetMouseButtonDown(1))
        {
            DeleteNoteAtMouse();
        }
    }
    
    void PlaceNote()
    {
        if (!audioManager.audioSource.clip) return;
        
        float currentTime = audioManager.CurrentTime;
        
        if (snapToGrid)
        {
            currentTime = SnapToGrid(currentTime);
        }
        
        if (currentNoteType == NoteData.NoteType.Normal)
        {
            AddNote(currentTime, currentDirection, NoteData.NoteType.Normal);
        }
        else if (currentNoteType == NoteData.NoteType.Long)
        {
            if (!isPlacingLongNote)
            {
                // 롱노트 시작
                isPlacingLongNote = true;
                longNoteStartTime = currentTime;
                instructionText.text = "롱노트 끝점을 설정하세요 (스페이스바)";
            }
            else
            {
                // 롱노트 완료
                float duration = currentTime - longNoteStartTime;
                if (duration > 0.1f) // 최소 지속시간
                {
                    AddNote(longNoteStartTime, currentDirection, NoteData.NoteType.Long, duration);
                }
                isPlacingLongNote = false;
                UpdateInstructions();
            }
        }
    }
    
    void AddNote(float timing, NoteData.NoteDirection direction, NoteData.NoteType type, float duration = 0f)
    {
        SaveToUndoHistory();
        
        NoteData newNote = new NoteData(timing, direction, type, duration);
        notes.Add(newNote);
        
        CreateNoteObject(newNote);
        SortNotesByTiming();
    }
    
    void CreateNoteObject(NoteData noteData)
    {
        GameObject prefab = noteData.type == NoteData.NoteType.Normal ? normalNotePrefab : longNotePrefab;
        GameObject noteObj = Instantiate(prefab, noteContainer);
        
        // 타임라인에서의 위치 설정
        float xPos = noteData.timing * timelineScale;
        float yPos = GetYPositionForDirection(noteData.direction);
        
        RectTransform rectTransform = noteObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(xPos, yPos);
        
        if (noteData.type == NoteData.NoteType.Long)
        {
            // 롱노트의 경우 길이 설정
            rectTransform.sizeDelta = new Vector2(noteData.duration * timelineScale, rectTransform.sizeDelta.y);
        }
        
        // 클릭 이벤트 추가
        Button noteButton = noteObj.GetComponent<Button>();
        if (noteButton)
        {
            noteButton.onClick.AddListener(() => DeleteNote(noteData));
        }
        
        noteObjects.Add(noteObj);
    }
    
    float GetYPositionForDirection(NoteData.NoteDirection direction)
    {
        switch (direction)
        {
            case NoteData.NoteDirection.TopToBottom: return 100f;
            case NoteData.NoteDirection.LeftToRight: return 0f;
            case NoteData.NoteDirection.RightToLeft: return -100f;
            default: return 0f;
        }
    }
    
    void DeleteNoteAtMouse()
    {
        Vector2 mousePos = Input.mousePosition;
        
        for (int i = noteObjects.Count - 1; i >= 0; i--)
        {
            RectTransform rectTransform = noteObjects[i].GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos))
            {
                DeleteNote(notes[i]);
                break;
            }
        }
    }
    
    void DeleteNote(NoteData noteToDelete)
    {
        SaveToUndoHistory();
        
        int index = notes.IndexOf(noteToDelete);
        if (index >= 0)
        {
            notes.RemoveAt(index);
            DestroyImmediate(noteObjects[index]);
            noteObjects.RemoveAt(index);
        }
    }
    
    void SetNoteType(NoteData.NoteType type)
    {
        currentNoteType = type;
        isPlacingLongNote = false;
        
        // UI 업데이트
        normalNoteButton.GetComponent<Image>().color = type == NoteData.NoteType.Normal ? Color.green : Color.white;
        longNoteButton.GetComponent<Image>().color = type == NoteData.NoteType.Long ? Color.green : Color.white;
        
        UpdateInstructions();
    }
    
    void SetDirection(NoteData.NoteDirection direction)
    {
        currentDirection = direction;
        
        // UI 업데이트
        upDirectionButton.GetComponent<Image>().color = direction == NoteData.NoteDirection.TopToBottom ? Color.green : Color.white;
        leftDirectionButton.GetComponent<Image>().color = direction == NoteData.NoteDirection.LeftToRight ? Color.green : Color.white;
        rightDirectionButton.GetComponent<Image>().color = direction == NoteData.NoteDirection.RightToLeft ? Color.green : Color.white;
        
        UpdateInstructions();
    }
    
    void UpdateInstructions()
    {
        if (isPlacingLongNote)
        {
            instructionText.text = "롱노트 끝점을 설정하세요 (스페이스바)";
        }
        else
        {
            string noteTypeText = currentNoteType == NoteData.NoteType.Normal ? "일반노트" : "롱노트";
            string directionText = "";
            
            switch (currentDirection)
            {
                case NoteData.NoteDirection.TopToBottom: directionText = "위→아래"; break;
                case NoteData.NoteDirection.LeftToRight: directionText = "왼쪽→오른쪽"; break;
                case NoteData.NoteDirection.RightToLeft: directionText = "오른쪽→왼쪽"; break;
            }
            
            instructionText.text = $"{noteTypeText} ({directionText}) - 스페이스바로 배치, 우클릭으로 삭제";
        }
    }
    
    void UpdateNoteCount()
    {
        noteCountText.text = $"노트 개수: {notes.Count}";
    }
    
    float SnapToGrid(float time)
    {
        // BPM 기반 그리드 스냅 (추후 BPM 설정 기능과 연동)
        float bpm = 120f; // 기본값
        float beatDuration = 60f / bpm;
        float snapInterval = beatDuration * beatSnapValue;
        
        return Mathf.Round(time / snapInterval) * snapInterval;
    }
    
    void SortNotesByTiming()
    {
        notes = notes.OrderBy(n => n.timing).ToList();
        RefreshNoteObjects();
    }
    
    void RefreshNoteObjects()
    {
        // 모든 노트 오브젝트 삭제 후 재생성
        foreach (GameObject obj in noteObjects)
        {
            DestroyImmediate(obj);
        }
        noteObjects.Clear();
        
        foreach (NoteData note in notes)
        {
            CreateNoteObject(note);
        }
    }
    
    void SaveToUndoHistory()
    {
        List<NoteData> currentState = new List<NoteData>();
        foreach (NoteData note in notes)
        {
            currentState.Add(new NoteData(note.timing, note.direction, note.type, note.duration));
        }
        
        undoHistory.Add(currentState);
        
        if (undoHistory.Count > maxUndoSteps)
        {
            undoHistory.RemoveAt(0);
        }
    }
    
    void Undo()
    {
        if (undoHistory.Count > 0)
        {
            notes = undoHistory[undoHistory.Count - 1];
            undoHistory.RemoveAt(undoHistory.Count - 1);
            RefreshNoteObjects();
        }
    }
    
    void TestPlay()
    {
        // 테스트 플레이 기능 (간단한 시각적 피드백)
        audioManager.Stop();
        audioManager.Play();
        
        StartCoroutine(TestPlayCoroutine());
    }
    
    System.Collections.IEnumerator TestPlayCoroutine()
    {
        foreach (NoteData note in notes.OrderBy(n => n.timing))
        {
            yield return new WaitForSeconds(note.timing);
            
            // 간단한 시각적 피드백
            Debug.Log($"노트 재생: {note.direction} - {note.type}");
        }
    }
    
    void SaveNotes()
    {
        SongData songData = new SongData();
        songData.songName = "My Song";
        songData.audioFileName = audioManager.audioSource.clip ? audioManager.audioSource.clip.name : "";
        songData.bpm = 120f;
        songData.notes = notes.ToArray();
        
        string json = JsonUtility.ToJson(songData, true);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/song_data.json", json);
        
        Debug.Log("노트 데이터 저장 완료: " + Application.persistentDataPath + "/song_data.json");
    }
    
    void LoadNotes()
    {
        string filePath = Application.persistentDataPath + "/song_data.json";
        if (System.IO.File.Exists(filePath))
        {
            string json = System.IO.File.ReadAllText(filePath);
            SongData songData = JsonUtility.FromJson<SongData>(json);
            
            notes = songData.notes.ToList();
            RefreshNoteObjects();
            
            Debug.Log("노트 데이터 로드 완료");
        }
        else
        {
            Debug.Log("저장된 파일이 없습니다.");
        }
    }
}