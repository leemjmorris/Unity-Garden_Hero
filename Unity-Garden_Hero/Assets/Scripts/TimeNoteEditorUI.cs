using UnityEngine;
using UnityEngine.UI;

public class TimeNoteEditorUI : MonoBehaviour
{
    [Header("Main Panels")]
    public RectTransform topPanel;
    public RectTransform leftPanel;
    public RectTransform mainPanel;
    public RectTransform bottomPanel;
    
    [Header("Colors")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    public Color panelColor = new Color(0.165f, 0.165f, 0.165f, 1f);
    public Color borderColor = new Color(0.267f, 0.267f, 0.267f, 1f);
    
    void Start()
    {
        SetupCanvas();
    }
    
    void SetupCanvas()
    {
        // LMJ: Set camera background
        Camera.main.backgroundColor = backgroundColor;
        
        // LMJ: Canvas setup
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }
        
        CreatePanelStructure();
    }
    
    void CreatePanelStructure()
    {
        // LMJ: Create main container
        GameObject container = new GameObject("Container");
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.SetParent(transform);
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        // LMJ: Add vertical layout
        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 0;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        
        // LMJ: Create Top Panel
        CreateTopPanel(containerRect);
        
        // LMJ: Create Middle Section
        CreateMiddleSection(containerRect);
        
        // LMJ: Create Bottom Panel
        CreateBottomPanel(containerRect);
    }
    
    void CreateTopPanel(Transform parent)
    {
        GameObject panel = new GameObject("TopPanel");
        topPanel = panel.AddComponent<RectTransform>();
        topPanel.SetParent(parent);
        
        LayoutElement le = panel.AddComponent<LayoutElement>();
        le.preferredHeight = 50;
        le.minHeight = 50;
        
        Image bg = panel.AddComponent<Image>();
        bg.color = panelColor;
        
        // LMJ: Add horizontal layout for controls
        HorizontalLayoutGroup hlg = panel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.padding = new RectOffset(20, 20, 10, 10);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        
        // LMJ: Add bottom border
        CreateBorder(panel, "BottomBorder", new Vector2(0, 0), new Vector2(1, 0), 1);
    }
    
    void CreateMiddleSection(Transform parent)
    {
        GameObject middle = new GameObject("MiddleSection");
        RectTransform middleRect = middle.AddComponent<RectTransform>();
        middleRect.SetParent(parent);
        
        LayoutElement le = middle.AddComponent<LayoutElement>();
        le.flexibleHeight = 1;
        
        // LMJ: Add horizontal layout
        HorizontalLayoutGroup hlg = middle.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        
        // LMJ: Create Left Panel
        CreateLeftPanel(middleRect);
        
        // LMJ: Create Main Panel
        CreateMainPanel(middleRect);
    }
    
    void CreateLeftPanel(Transform parent)
    {
        GameObject panel = new GameObject("LeftPanel");
        leftPanel = panel.AddComponent<RectTransform>();
        leftPanel.SetParent(parent);
        
        LayoutElement le = panel.AddComponent<LayoutElement>();
        le.preferredWidth = 200;
        le.minWidth = 200;
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.145f, 0.145f, 0.145f, 1f);
        
        // LMJ: Add right border
        CreateBorder(panel, "RightBorder", new Vector2(1, 0), new Vector2(1, 1), 1);
        
        // LMJ: Add vertical layout for controls
        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childForceExpandWidth = true;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
    }
    
    void CreateMainPanel(Transform parent)
    {
        GameObject panel = new GameObject("MainPanel");
        mainPanel = panel.AddComponent<RectTransform>();
        mainPanel.SetParent(parent);
        
        LayoutElement le = panel.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        
        Image bg = panel.AddComponent<Image>();
        bg.color = backgroundColor;
        
        // LMJ: Create timeline container structure
        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 0;
        vlg.childForceExpandWidth = true;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        
        // LMJ: Create ruler
        GameObject ruler = new GameObject("TimelineRuler");
        RectTransform rulerRect = ruler.AddComponent<RectTransform>();
        rulerRect.SetParent(mainPanel);
        
        LayoutElement rulerLE = ruler.AddComponent<LayoutElement>();
        rulerLE.preferredHeight = 30;
        rulerLE.minHeight = 30;
        
        Image rulerBg = ruler.AddComponent<Image>();
        rulerBg.color = panelColor;
        
        CreateBorder(ruler, "BottomBorder", new Vector2(0, 0), new Vector2(1, 0), 1);
        
        // LMJ: Create timeline container
        GameObject timeline = new GameObject("TimelineContainer");
        RectTransform timelineRect = timeline.AddComponent<RectTransform>();
        timelineRect.SetParent(mainPanel);
        
        LayoutElement timelineLE = timeline.AddComponent<LayoutElement>();
        timelineLE.flexibleHeight = 1;
        
        Image timelineBg = timeline.AddComponent<Image>();
        timelineBg.color = backgroundColor;
    }
    
    void CreateBottomPanel(Transform parent)
    {
        GameObject panel = new GameObject("BottomPanel");
        bottomPanel = panel.AddComponent<RectTransform>();
        bottomPanel.SetParent(parent);
        
        LayoutElement le = panel.AddComponent<LayoutElement>();
        le.preferredHeight = 100;
        le.minHeight = 100;
        
        Image bg = panel.AddComponent<Image>();
        bg.color = panelColor;
        
        // LMJ: Add top border
        CreateBorder(panel, "TopBorder", new Vector2(0, 1), new Vector2(1, 1), 1);
        
        // LMJ: Add vertical layout
        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(20, 20, 10, 10);
        vlg.childForceExpandWidth = true;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
    }
    
    void CreateBorder(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, float thickness)
    {
        GameObject border = new GameObject(name);
        RectTransform borderRect = border.AddComponent<RectTransform>();
        borderRect.SetParent(parent.transform);
        borderRect.anchorMin = anchorMin;
        borderRect.anchorMax = anchorMax;
        
        if (anchorMin.y == anchorMax.y)
        {
            borderRect.sizeDelta = new Vector2(0, thickness);
            borderRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            borderRect.sizeDelta = new Vector2(thickness, 0);
            borderRect.anchoredPosition = Vector2.zero;
        }
        
        borderRect.offsetMin = new Vector2(borderRect.offsetMin.x, 0);
        borderRect.offsetMax = new Vector2(borderRect.offsetMax.x, 0);
        
        Image borderImage = border.AddComponent<Image>();
        borderImage.color = borderColor;
    }
}