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

        if (direction == "Up")
        {
            rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
        }

        Image defaultImage = GetComponent<Image>();
        if (defaultImage != null)
        {
            defaultImage.enabled = false;
        }

        if (IsLongNote())
        {
            holdEndTime = hitTime + duration;
            CreateDJMaxLongNote();
        }
        else
        {
            CreateNormalNote();
        }

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

        if (direction == "Up")
        {
            normalRect.sizeDelta = new Vector2(100, 20);
        }
        else
        {
            normalRect.sizeDelta = new Vector2(20, 100);
        }

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

        if (direction == "Up")
        {
            headRect.sizeDelta = new Vector2(100, 20);
        }
        else
        {
            headRect.sizeDelta = new Vector2(20, 100);
        }

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

        if (direction == "Up")
        {
            bodyRect.sizeDelta = new Vector2(80, originalBodyLength);
        }
        else
        {
            bodyRect.sizeDelta = new Vector2(originalBodyLength, 80);
        }

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

        if (direction == "Up")
        {
            tailRect.sizeDelta = new Vector2(100, 20);
        }
        else
        {
            tailRect.sizeDelta = new Vector2(20, 100);
        }

        tailImage = tailCap.AddComponent<Image>();
        tailImage.color = tailColor;
    }

    void PositionLongNoteParts()
    {
        float bodyLength = duration * speed;
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        RectTransform headRect = headCap.GetComponent<RectTransform>();
        RectTransform tailRect = tailCap.GetComponent<RectTransform>();

        if (direction == "Right")
        {
            headRect.anchoredPosition = Vector2.zero;
            bodyRect.anchoredPosition = new Vector2(bodyLength / 2, 0);
            tailRect.anchoredPosition = new Vector2(bodyLength, 0);
        }
        else if (direction == "Left")
        {
            headRect.anchoredPosition = Vector2.zero;
            bodyRect.anchoredPosition = new Vector2(-bodyLength / 2, 0);
            tailRect.anchoredPosition = new Vector2(-bodyLength, 0);
        }
        else if (direction == "Up")
        {
            tailRect.anchoredPosition = Vector2.zero;
            bodyRect.anchoredPosition = new Vector2(0, -bodyLength / 2);
            headRect.anchoredPosition = new Vector2(0, -bodyLength);
        }

        originalBodyPosition = bodyRect.anchoredPosition;
    }

    void Update()
    {
        if (isHolding && bodyObject != null)
        {
            UpdateHoldingVisual();
        }
    }

    void UpdateHoldingVisual()
    {
        if (!isHolding || bodyObject == null) return;

        // LMJ: Use custom note time instead of Time.time
        float timeSinceHoldStart = NoteTimeManager.Instance.GetNoteTime() - holdStartTime;
        float remainingDuration = duration - timeSinceHoldStart;
        float progress = Mathf.Clamp01(timeSinceHoldStart / duration);
        float remainingLength = remainingDuration * speed;
        float consumedLength = timeSinceHoldStart * speed;

        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        RectTransform tailRect = tailCap != null ? tailCap.GetComponent<RectTransform>() : null;
        RectTransform headRect = headCap != null ? headCap.GetComponent<RectTransform>() : null;

        if (remainingLength <= 5f || progress >= 0.98f)
        {
            bodyObject.SetActive(false);
            if (tailCap != null && direction != "Up")
            {
                tailRect.anchoredPosition = Vector2.zero;
            }
            if (headRect != null && direction == "Up")
            {
                headRect.anchoredPosition = Vector2.zero;
            }
            return;
        }

        if (direction == "Up")
        {
            bodyRect.sizeDelta = new Vector2(80, remainingLength);
            bodyRect.anchoredPosition = new Vector2(0, -consumedLength - remainingLength / 2);

            if (headRect != null)
            {
                headRect.anchoredPosition = new Vector2(0, -consumedLength - remainingLength);
            }

            if (tailRect != null)
            {
                tailRect.anchoredPosition = Vector2.zero;
            }
        }
        else if (direction == "Right")
        {
            bodyRect.sizeDelta = new Vector2(remainingLength, 80);
            bodyRect.anchoredPosition = new Vector2(consumedLength + remainingLength / 2, 0);

            if (tailRect != null)
            {
                tailRect.anchoredPosition = new Vector2(consumedLength + remainingLength, 0);
            }
        }
        else
        {
            bodyRect.sizeDelta = new Vector2(remainingLength, 80);
            bodyRect.anchoredPosition = new Vector2(-consumedLength - remainingLength / 2, 0);

            if (tailRect != null)
            {
                tailRect.anchoredPosition = new Vector2(-consumedLength - remainingLength, 0);
            }
        }

        ApplyHoldGlowEffect();

        if (direction != "Up" && headCap != null && timeSinceHoldStart > 0.1f)
        {
            headCap.SetActive(false);
        }
        else if (direction == "Up" && tailCap != null && timeSinceHoldStart > 0.1f)
        {
            tailCap.SetActive(false);
        }
    }

    void ApplyHoldGlowEffect()
    {
        if (bodyImage != null)
        {
            float pulse = Mathf.Sin(Time.time * 5f) * 0.2f + 0.8f;
            Color glowColor = Color.Lerp(bodyColor, holdingGlowColor, pulse);
            bodyImage.color = glowColor;
        }

        if (tailImage != null)
        {
            float pulse = Mathf.Sin(Time.time * 3f) * 0.3f + 0.7f;
            Color tailGlow = tailColor;
            tailGlow.a = pulse;
            tailImage.color = tailGlow;
        }
    }

    public void StartHold()
    {
        if (!IsLongNote()) return;

        isHolding = true;
        holdStarted = true;
        holdStartTime = NoteTimeManager.Instance.GetNoteTime();

        if (direction == "Up" && tailCap != null)
        {
            StartCoroutine(HitEffect(tailImage));
        }
        else if (headCap != null)
        {
            StartCoroutine(HitEffect(headImage));
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