using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class JudgmentTextManager : MonoBehaviour
{
    [Header("Judgment Text Prefabs")]
    [SerializeField] private TextMeshProUGUI perfectTextPrefab;
    [SerializeField] private TextMeshProUGUI goodTextPrefab;
    [SerializeField] private TextMeshProUGUI missTextPrefab;

    [Header("Judgment Line References")]
    [SerializeField] private Transform leftJudgmentLine;
    [SerializeField] private Transform centerJudgmentLine;
    [SerializeField] private Transform rightJudgmentLine;

    [Header("Animation Settings")]
    [SerializeField] private float showDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private Vector2 textOffset = new Vector2(0, 100);
    [SerializeField] private float fountainUpSpeed = 200f;
    [SerializeField] private float gravity = 300f;
    [SerializeField] private float horizontalSpread = 50f;
    [SerializeField] private float scaleUpTime = 0.15f;
    [SerializeField] private float maxScale = 1.3f;

    private Dictionary<string, Transform> judgmentLines = new Dictionary<string, Transform>();

    void Start()
    {
        Debug.Log("[JudgmentTextManager] Start called - Initializing judgment lines");

        // Setup judgment line references
        if (leftJudgmentLine != null) judgmentLines["Left"] = leftJudgmentLine;
        if (centerJudgmentLine != null) judgmentLines["Center"] = centerJudgmentLine;
        if (rightJudgmentLine != null) judgmentLines["Right"] = rightJudgmentLine;

        // Validate prefabs
        if (perfectTextPrefab == null) Debug.LogWarning("[JudgmentTextManager] Perfect text prefab is not assigned!");
        if (goodTextPrefab == null) Debug.LogWarning("[JudgmentTextManager] Good text prefab is not assigned!");
        if (missTextPrefab == null) Debug.LogWarning("[JudgmentTextManager] Miss text prefab is not assigned!");

        Debug.Log($"[JudgmentTextManager] Initialized with {judgmentLines.Count} judgment lines");
    }

    public void ShowJudgment(string direction, JudgmentResult judgment)
    {
        Debug.Log($"[JudgmentTextManager] ShowJudgment called - direction: {direction}, judgment: {judgment}");

        if (!judgmentLines.ContainsKey(direction))
        {
            Debug.LogWarning($"[JudgmentTextManager] Judgment line not found for direction: {direction}");
            return;
        }

        TextMeshProUGUI prefab = GetJudgmentPrefab(judgment);
        if (prefab == null)
        {
            Debug.LogWarning($"[JudgmentTextManager] Prefab not found for judgment: {judgment}");
            return;
        }

        // Instantiate the text at the judgment line position
        Transform parent = judgmentLines[direction];
        TextMeshProUGUI textInstance = Instantiate(prefab, parent);
        textInstance.gameObject.SetActive(true);

        // Set initial position
        RectTransform rectTransform = textInstance.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = textOffset;
        rectTransform.localScale = Vector3.one;

        Debug.Log($"[JudgmentTextManager] Text instantiated at {direction} lane");

        // Animate and destroy after duration
        StartCoroutine(AnimateAndDestroyText(textInstance));
    }

    private TextMeshProUGUI GetJudgmentPrefab(JudgmentResult judgment)
    {
        switch (judgment)
        {
            case JudgmentResult.Perfect:
                return perfectTextPrefab;
            case JudgmentResult.Good:
                return goodTextPrefab;
            case JudgmentResult.Miss:
                return missTextPrefab;
            default:
                return null;
        }
    }

    private IEnumerator AnimateAndDestroyText(TextMeshProUGUI text)
    {
        RectTransform rectTransform = text.GetComponent<RectTransform>();
        Vector2 startPosition = rectTransform.anchoredPosition;
        Color originalColor = text.color;

        // Random horizontal offset for fountain spread
        float horizontalOffset = Random.Range(-horizontalSpread, horizontalSpread);

        // Initial velocity
        float verticalVelocity = fountainUpSpeed;
        float elapsed = 0f;

        // Start with small scale and grow
        float startScale = 0.5f;
        text.transform.localScale = Vector3.one * startScale;

        while (elapsed < showDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / showDuration;

            // Scale up animation (first scaleUpTime seconds)
            if (elapsed < scaleUpTime)
            {
                float scaleT = elapsed / scaleUpTime;
                float currentScale = Mathf.Lerp(startScale, maxScale, scaleT);
                text.transform.localScale = Vector3.one * currentScale;
            }

            // Apply gravity to vertical velocity
            verticalVelocity -= gravity * Time.deltaTime;

            // Update position (fountain physics)
            Vector2 currentPos = rectTransform.anchoredPosition;
            currentPos.y += verticalVelocity * Time.deltaTime;
            currentPos.x = startPosition.x + horizontalOffset * t;
            rectTransform.anchoredPosition = currentPos;

            // Fade out in the last fadeOutDuration seconds
            if (elapsed > showDuration - fadeOutDuration)
            {
                float fadeT = (elapsed - (showDuration - fadeOutDuration)) / fadeOutDuration;
                float alpha = Mathf.Lerp(1f, 0f, fadeT);
                Color currentColor = originalColor;
                currentColor.a = alpha;
                text.color = currentColor;
            }

            yield return null;
        }

        // Destroy the text instance
        Destroy(text.gameObject);
    }
}