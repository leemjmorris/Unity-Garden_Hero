using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaPanel : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private Rect lastSafeArea;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        UpdateSafeArea();
    }
    
    void Update()
    {
        if (Screen.safeArea != lastSafeArea)
        {
            lastSafeArea = Screen.safeArea;
            UpdateSafeArea();
        }
    }
    
    void UpdateSafeArea()
    {
        if (canvas == null) return;
        
        var safeArea = Screen.safeArea;
        
        // LMJ: Convert to normalized coordinates
        var anchorMin = safeArea.position;
        var anchorMax = safeArea.position + safeArea.size;
        
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        
        // LMJ: Apply safe area constraints
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}