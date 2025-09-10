using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GridVisualizer : MonoBehaviour
{
    [Header("Grid Settings")]
    public RectTransform gridContainer;
    public GameObject gridLinePrefab;
    public Color gridLineColor = new Color(1, 1, 1, 0.3f);
    public AudioManager audioManager;
    
    private List<GameObject> gridLines = new List<GameObject>();
    private float lastSongLength = 0f;
    private float lastBPM = 0f;
    
    void Update()
    {
        if (audioManager != null && audioManager.audioSource.clip != null)
        {
            float currentLength = audioManager.TotalTime;
            float currentBPM = audioManager.BPM;
            
            // 곡 길이나 BPM이 변경되면 그리드 재생성
            if (currentLength != lastSongLength || currentBPM != lastBPM)
            {
                GenerateGrid(currentLength, currentBPM);
                lastSongLength = currentLength;
                lastBPM = currentBPM;
            }
        }
    }
    
    void GenerateGrid(float songLength, float bpm)
    {
        ClearGrid();
        
        if (gridContainer == null || gridLinePrefab == null) return;
        
        float beatDuration = 60f / bpm;
        float timelineScale = 100f; // NoteEditorManager와 동일하게
        
        // 세로 그리드 라인 (박자 기준)
        for (float time = 0; time <= songLength; time += beatDuration)
        {
            GameObject gridLine = Instantiate(gridLinePrefab, gridContainer);
            RectTransform rect = gridLine.GetComponent<RectTransform>();
            
            // 위치 설정
            rect.anchoredPosition = new Vector2(time * timelineScale, 0);
            rect.sizeDelta = new Vector2(2, gridContainer.sizeDelta.y);
            
            // 색상 설정 (4박자마다 더 진하게)
            Image lineImage = gridLine.GetComponent<Image>();
            if (lineImage != null)
            {
                float intensity = (Mathf.RoundToInt(time / beatDuration) % 4 == 0) ? 0.5f : 0.3f;
                lineImage.color = new Color(gridLineColor.r, gridLineColor.g, gridLineColor.b, intensity);
            }
            
            gridLines.Add(gridLine);
        }
        
        // 가로 그리드 라인 (노트 레인 구분)
        float[] trackPositions = { 100f, 0f, -100f }; // TopToBottom, LeftToRight, RightToLeft
        
        foreach (float yPos in trackPositions)
        {
            GameObject gridLine = Instantiate(gridLinePrefab, gridContainer);
            RectTransform rect = gridLine.GetComponent<RectTransform>();
            
            rect.anchoredPosition = new Vector2(0, yPos);
            rect.sizeDelta = new Vector2(songLength * timelineScale, 2);
            
            Image lineImage = gridLine.GetComponent<Image>();
            if (lineImage != null)
            {
                lineImage.color = gridLineColor;
            }
            
            gridLines.Add(gridLine);
        }
    }
    
    void ClearGrid()
    {
        foreach (GameObject line in gridLines)
        {
            if (line != null)
                DestroyImmediate(line);
        }
        gridLines.Clear();
    }
    
    void OnDestroy()
    {
        ClearGrid();
    }
}