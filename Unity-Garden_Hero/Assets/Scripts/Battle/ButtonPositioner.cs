using UnityEngine;

public class ButtonPositioner : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private RectTransform leftButton;
    [SerializeField] private RectTransform centerButton;
    [SerializeField] private RectTransform rightButton;
    
    [Header("Position Settings")]
    [SerializeField] private float bottomMargin = 100f;
    [SerializeField] private float sideMargin = 100f;
    [SerializeField] private float buttonSpacing = 120f;
    
    [Header("Button Size")]
    [SerializeField] private Vector2 buttonSize = new Vector2(200f, 100f);
    
    void Start()
    {
        SetupButtons();
        PositionButtons();
    }
    
    void SetupButtons()
    {
        SetupButton(leftButton);
        SetupButton(centerButton);
        SetupButton(rightButton);
    }
    
    void SetupButton(RectTransform button)
    {
        if (button == null) return;
        
        // LMJ: Set anchor to bottom-left
        button.anchorMin = Vector2.zero;
        button.anchorMax = Vector2.zero;
        button.pivot = new Vector2(0.5f, 0.5f);
        button.sizeDelta = buttonSize;
    }
    
    void PositionButtons()
    {
        RectTransform panelRect = GetComponent<RectTransform>();
        Vector2 panelSize = panelRect.rect.size;
        
        // LMJ: Calculate positions within panel
        float buttonY = bottomMargin;
        float leftX = sideMargin;
        float rightX = panelSize.x - sideMargin;
        float centerX = rightX - buttonSpacing;
        
        // LMJ: Apply positions
        if (leftButton != null)
            leftButton.anchoredPosition = new Vector2(leftX, buttonY);
            
        if (centerButton != null)
            centerButton.anchoredPosition = new Vector2(centerX, buttonY);
            
        if (rightButton != null)
            rightButton.anchoredPosition = new Vector2(rightX, buttonY);
    }
    
    // LMJ: Call this if you need to update positions at runtime
    public void UpdatePositions()
    {
        PositionButtons();
    }
}