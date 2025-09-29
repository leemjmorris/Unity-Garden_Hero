using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RhythmNote : MonoBehaviour
{
    [Header("Note Properties")]
    public string direction;
    public float hitTime;
    public string noteType;
    public float speed;
    public float duration;

    [Header("Long Note Components")]
    private GameObject headCap;
    private GameObject bodyObject;
    private GameObject tailCap;
    private Image headImage;
    private Image bodyImage;
    private Image tailImage;

    [Header("System Reference")]
    private RhythmGameSystem gameSystem;

    [Header("State")]
    private bool isHit = false;
    private bool isHolding = false;
    private bool holdStarted = false;
    private float holdEndTime;
    private float holdStartTime;

    [Header("Visual")]
    private RectTransform rectTransform;
    private float originalBodyLength;
    private Vector2 originalBodyPosition;
    private Vector2 initialPosition;
    private Vector2 initialSizeDelta;

    private readonly Color headColor = new Color(1f, 0.9f, 0.2f, 1f);
    private readonly Color bodyColor = new Color(0.9f, 0.8f, 0.1f, 0.7f);
    private readonly Color tailColor = new Color(1f, 0.85f, 0.15f, 1f);
    private readonly Color holdingGlowColor = new Color(1f, 1f, 0.5f, 1f);

    public void Initialize(string dir, float time, string type, float spd, float dur = 0f)
    {
        direction = dir;
        hitTime = time;
        noteType = type;
        speed = spd;
        duration = dur;

        rectTransform = GetComponent<RectTransform>();

        // Note: Keep Image components enabled for prefab-based notes

        if (IsLongNote())
        {
            holdEndTime = hitTime + duration;
            // Note: Long note visuals are now handled by prefab, not generated dynamically
        }
        // Note: Normal note visuals are now handled by prefab, not generated dynamically

        gameSystem = FindFirstObjectByType<RhythmGameSystem>();
    }

    public Vector3 GetHeadWorldPosition()
    {
        if (headCap != null)
        {
            return headCap.transform.position;
        }
        return transform.position;
    }

    void CreateNormalNote()
    {
        GameObject normalNote = new GameObject("NormalNote");
        normalNote.transform.SetParent(transform, false);

        RectTransform normalRect = normalNote.AddComponent<RectTransform>();
        normalRect.anchorMin = normalRect.anchorMax = new Vector2(0.5f, 0.5f);
        normalRect.pivot = new Vector2(0.5f, 0.5f);
        normalRect.anchoredPosition = Vector2.zero;

        // Unified note size for all directions
        normalRect.sizeDelta = new Vector2(60, 60);

        Image noteImage = normalNote.AddComponent<Image>();
        noteImage.color = GetNoteColor(noteType);
    }

    void CreateDJMaxLongNote()
    {
        CreateHead();
        CreateBody();
        CreateTail();
        PositionLongNoteParts();
    }

    void CreateHead()
    {
        headCap = new GameObject("Head");
        headCap.transform.SetParent(transform, false);

        RectTransform headRect = headCap.AddComponent<RectTransform>();
        headRect.anchorMin = headRect.anchorMax = new Vector2(0.5f, 0.5f);
        headRect.pivot = new Vector2(0.5f, 0.5f);

        // Unified head size for all directions
        headRect.sizeDelta = new Vector2(60, 60);

        headImage = headCap.AddComponent<Image>();
        headImage.color = headColor;
        headCap.transform.SetAsLastSibling();
    }

    void CreateBody()
    {
        bodyObject = new GameObject("Body");
        bodyObject.transform.SetParent(transform, false);
        bodyObject.transform.SetAsFirstSibling();

        RectTransform bodyRect = bodyObject.AddComponent<RectTransform>();
        bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);

        originalBodyLength = duration * speed;

        // All lanes use vertical body orientation (top-to-bottom)
        bodyRect.sizeDelta = new Vector2(50, originalBodyLength);

        bodyImage = bodyObject.AddComponent<Image>();
        bodyImage.color = bodyColor;
    }

    void CreateTail()
    {
        tailCap = new GameObject("Tail");
        tailCap.transform.SetParent(transform, false);

        RectTransform tailRect = tailCap.AddComponent<RectTransform>();
        tailRect.anchorMin = tailRect.anchorMax = new Vector2(0.5f, 0.5f);
        tailRect.pivot = new Vector2(0.5f, 0.5f);

        // Unified tail size for all directions
        tailRect.sizeDelta = new Vector2(60, 60);

        tailImage = tailCap.AddComponent<Image>();
        tailImage.color = tailColor;
    }

    void PositionLongNoteParts()
    {
        float bodyLength = duration * speed;
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        RectTransform headRect = headCap.GetComponent<RectTransform>();
        RectTransform tailRect = tailCap.GetComponent<RectTransform>();

        // All lanes move from top to bottom
        headRect.anchoredPosition = Vector2.zero;
        bodyRect.anchoredPosition = new Vector2(0, bodyLength / 2);
        tailRect.anchoredPosition = new Vector2(0, bodyLength);

        originalBodyPosition = bodyRect.anchoredPosition;
    }

    void Update()
    {
        if (isHolding)
        {
            UpdateHoldingVisual();
        }
    }

    void UpdateHoldingVisual()
    {
        if (!isHolding || !IsLongNote())
        {
            return;
        }

        // Calculate remaining duration
        float timeSinceHoldStart = NoteTimeManager.Instance.GetNoteTime() - holdStartTime;
        float remainingDuration = duration - timeSinceHoldStart;


        // If hold is finished, hide the note
        if (remainingDuration <= 0.05f)
        {
            gameObject.SetActive(false);
            return;
        }

        // DJMAX Style: Note stays at judgment line and is consumed from bottom
        // Calculate remaining height based on remaining duration
        float remainingHeight = remainingDuration * speed;

        // Minimum height to maintain visual clarity
        float minHeight = 35f;
        remainingHeight = Mathf.Max(remainingHeight, minHeight);

        // Update the height (pivot at bottom, so it shrinks from top)
        rectTransform.sizeDelta = new Vector2(initialSizeDelta.x, remainingHeight);

        // Since the note is fixed at judgment line, no position adjustment needed
        // The visual effect is that the note is being "consumed" at the judgment line


        // Optional: Add glow effect during hold
        ApplyHoldGlowEffect();
    }

    void ApplyHoldGlowEffect()
    {
        // Simple glow effect for the entire prefab during hold
        UnityEngine.UI.Image[] images = GetComponentsInChildren<UnityEngine.UI.Image>();

        foreach (var image in images)
        {
            if (image != null)
            {
                float pulse = Mathf.Sin(Time.time * 5f) * 0.3f + 0.7f;
                Color originalColor = image.color;
                originalColor.a = pulse;
                image.color = originalColor;
            }
        }
    }

    public void StartHold()
    {
        if (!IsLongNote())
        {
            return;
        }


        isHolding = true;
        holdStarted = true;
        holdStartTime = NoteTimeManager.Instance.GetNoteTime();

        // Reset scale to normal in case it was modified
        transform.localScale = Vector3.one;

        // Store initial state WITHOUT changing pivot
        initialPosition = rectTransform.anchoredPosition;
        initialSizeDelta = rectTransform.sizeDelta;



        // Simple hit effect for prefab-based Long Note
        StartCoroutine(HitEffectForPrefab());
    }

    IEnumerator HitEffectForPrefab()
    {
        UnityEngine.UI.Image[] images = GetComponentsInChildren<UnityEngine.UI.Image>();
        Color[] originalColors = new Color[images.Length];

        // Store original colors
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
                originalColors[i] = images[i].color;
        }

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                {
                    images[i].color = Color.Lerp(originalColors[i], Color.white, 1f - t);
                    images[i].transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.2f, 1f - t);
                }
            }

            yield return null;
        }

        // Restore original colors and scale
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
            {
                images[i].color = originalColors[i];
                images[i].transform.localScale = Vector3.one;
            }
        }
    }

    public void EndHold()
    {
        if (!IsLongNote()) return;

        isHolding = false;

        if (tailCap != null)
        {
            StartCoroutine(HitEffect(tailImage));
        }
    }

    IEnumerator HitEffect(Image targetImage)
    {
        if (targetImage == null) yield break;

        Color originalColor = targetImage.color;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            targetImage.color = Color.Lerp(originalColor, Color.white, 1f - t);
            targetImage.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, 1f - t);

            yield return null;
        }

        targetImage.color = originalColor;
        targetImage.transform.localScale = Vector3.one;
    }

    Color GetNoteColor(string type)
    {
        switch (type)
        {
            case "Normal": return new Color(0.5f, 0.8f, 1f, 1f);
            case "Long": return new Color(1f, 0.9f, 0.2f, 1f);
            case "Special": return new Color(1f, 0.3f, 0.8f, 1f);
            case "Dodge": return new Color(1f, 0.2f, 0.2f, 1f);
            default: return Color.white;
        }
    }

    public bool IsLongNote() => noteType == "Long" || noteType == "Charged";
    public bool IsHolding() => isHolding;
    public bool HasStartedHold() => holdStarted;
    public float GetHoldEndTime() => holdEndTime;
    public bool IsHit() => isHit;
    public void SetHit() => isHit = true;

    public float GetHoldProgress()
    {
        if (!isHolding) return 0f;
        float elapsed = NoteTimeManager.Instance.GetNoteTime() - holdStartTime;
        return Mathf.Clamp01(elapsed / duration);
    }
}