using UnityEngine;
using UnityEngine.UI;

public class NeonNoteVisual : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private Image coreImage;
    [SerializeField] private Image glowImage;
    [SerializeField] private Image outlineImage;
    [SerializeField] private Image typeIconImage;
    
    [Header("Neon Colors")]
    [SerializeField] private Color normalColor = new Color(0f, 1f, 1f, 1f);     // LMJ: Cyan
    [SerializeField] private Color longColor = new Color(1f, 1f, 0f, 1f);       // LMJ: Yellow
    [SerializeField] private Color specialColor = new Color(1f, 0f, 1f, 1f);    // LMJ: Magenta
    [SerializeField] private Color dodgeColor = new Color(1f, 0.2f, 0.2f, 1f);  // LMJ: Red
    
    [Header("Animation Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float rotateSpeed = 90f;
    
    private string noteType;
    private float baseScale;
    private bool isApproaching = true;
    
    void Start()
    {
        baseScale = transform.localScale.x;
        CreateNoteStructure();
    }
    
    void CreateNoteStructure()
    {
        // LMJ: Create core if not assigned - DJMAX style block
        if (coreImage == null)
        {
            GameObject core = new GameObject("Core");
            core.transform.SetParent(transform, false);
            coreImage = core.AddComponent<Image>();
            coreImage.sprite = Resources.Load<Sprite>("UI/Skin/UISprite");
            
            RectTransform coreRect = core.GetComponent<RectTransform>();
            coreRect.sizeDelta = new Vector2(80, 40);  // LMJ: Wide block shape
        }
        
        // LMJ: Create glow effect
        if (glowImage == null)
        {
            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(transform, false);
            glow.transform.SetAsFirstSibling();
            glowImage = glow.AddComponent<Image>();
            glowImage.sprite = Resources.Load<Sprite>("UI/Skin/UISprite");
            
            RectTransform glowRect = glow.GetComponent<RectTransform>();
            glowRect.sizeDelta = new Vector2(100, 60);  // LMJ: Wider glow for block
        }
        
        // LMJ: Create outline
        if (outlineImage == null)
        {
            GameObject outline = new GameObject("Outline");
            outline.transform.SetParent(transform, false);
            outlineImage = outline.AddComponent<Image>();
            outlineImage.sprite = Resources.Load<Sprite>("UI/Skin/Background");
            
            RectTransform outlineRect = outline.GetComponent<RectTransform>();
            outlineRect.sizeDelta = new Vector2(84, 44);  // LMJ: Slightly larger than core
        }
    }
    
    public void SetNoteType(string type)
    {
        noteType = type;
        Color baseColor = GetNoteColor(type);
        
        // LMJ: Apply colors with neon effect
        if (coreImage != null)
        {
            coreImage.color = new Color(baseColor.r * 0.8f, baseColor.g * 0.8f, baseColor.b * 0.8f, 1f);
        }
        
        if (glowImage != null)
        {
            Color glowColor = baseColor;
            glowColor.a = 0.6f;
            glowImage.color = glowColor;
        }
        
        if (outlineImage != null)
        {
            outlineImage.color = new Color(1f, 1f, 1f, 0.8f);
        }
        
        // LMJ: Add type-specific icons
        SetTypeIcon(type);
    }
    
    void SetTypeIcon(string type)
    {
        if (typeIconImage == null) return;
        
        switch (type)
        {
            case "Long":
                // LMJ: Show hold icon
                typeIconImage.gameObject.SetActive(true);
                break;
            case "Special":
                // LMJ: Show star icon
                typeIconImage.gameObject.SetActive(true);
                break;
            case "Dodge":
                // LMJ: Show X icon
                typeIconImage.gameObject.SetActive(true);
                break;
            default:
                typeIconImage.gameObject.SetActive(false);
                break;
        }
    }
    
    void Update()
    {
        if (!isApproaching) return;
        
        // LMJ: Pulse effect
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.1f + 1f;
        transform.localScale = Vector3.one * baseScale * pulse;
        
        // LMJ: Rotation effect
        if (noteType == "Special")
        {
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
        }
        
        // LMJ: Glow pulse
        if (glowImage != null)
        {
            float glowPulse = Mathf.Sin(Time.time * pulseSpeed * 1.5f) * 0.3f + 0.7f;
            Color currentGlow = glowImage.color;
            currentGlow.a = glowPulse * 0.6f;
            glowImage.color = currentGlow;
            
            // LMJ: Scale glow
            glowImage.transform.localScale = Vector3.one * (1f + (1f - glowPulse) * 0.3f);
        }
    }
    
    Color GetNoteColor(string type)
    {
        switch (type)
        {
            case "Normal": return normalColor;
            case "Long": return longColor;
            case "Special": return specialColor;
            case "Dodge": return dodgeColor;
            default: return normalColor;
        }
    }
    
    public void OnHit()
    {
        isApproaching = false;
        
        // LMJ: Hit effect
        StartCoroutine(HitEffect());
    }
    
    System.Collections.IEnumerator HitEffect()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // LMJ: Expand and fade
            float scale = Mathf.Lerp(1f, 2f, t);
            transform.localScale = Vector3.one * baseScale * scale;
            
            // LMJ: Fade out
            if (coreImage != null)
            {
                Color color = coreImage.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                coreImage.color = color;
            }
            
            if (glowImage != null)
            {
                Color glowColor = glowImage.color;
                glowColor.a = Mathf.Lerp(0.6f, 0f, t);
                glowImage.color = glowColor;
            }
            
            yield return null;
        }
    }
}