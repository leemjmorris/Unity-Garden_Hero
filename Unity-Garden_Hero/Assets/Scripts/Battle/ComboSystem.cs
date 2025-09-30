using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ComboSystem : MonoBehaviour
{
    [Header("Combo Sprites (0~9)")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    [Header("UI Settings")]
    [SerializeField] private Transform comboContainer; // 숫자 Sprite들이 생성될 부모 Transform
    [SerializeField] private float digitSpacing = 50f; // 숫자 간 간격
    [SerializeField] private Vector2 digitSize = new Vector2(80f, 100f); // 각 숫자의 크기

    [Header("Animation Settings")]
    [SerializeField] private float popScale = 1.5f; // 튕기는 최대 크기
    [SerializeField] private float popDuration = 0.3f; // 애니메이션 지속 시간
    [SerializeField] private float popUpHeight = 20f; // 위로 튕기는 높이
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int currentCombo = 0;
    private int maxCombo = 0;
    private List<GameObject> digitObjects = new List<GameObject>();

    void Start()
    {
        UpdateComboDisplay();
    }

    void Update()
    {
        // 테스트용: Space 키로 콤보 증가, R 키로 콤보 리셋
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddCombo();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCombo();
        }
    }

    public void AddCombo()
    {
        currentCombo++;
        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }
        UpdateComboDisplay();
        PlayComboAnimation();
    }

    public void ResetCombo()
    {
        currentCombo = 0;
        UpdateComboDisplay();
    }

    void UpdateComboDisplay()
    {
        // 기존 숫자 오브젝트 제거
        foreach (var digit in digitObjects)
        {
            Destroy(digit);
        }
        digitObjects.Clear();

        // 숫자를 문자열로 변환
        string comboString = currentCombo.ToString();

        // 오른쪽에서 왼쪽으로 배치하기 위해 역순으로 생성
        for (int i = comboString.Length - 1; i >= 0; i--)
        {
            int digit = int.Parse(comboString[i].ToString());
            CreateDigitSprite(digit, comboString.Length - 1 - i);
        }
    }

    void CreateDigitSprite(int digit, int position)
    {
        if (digit < 0 || digit > 9 || numberSprites[digit] == null)
        {
            Debug.LogWarning($"Invalid digit or sprite not assigned: {digit}");
            return;
        }

        GameObject digitObj = new GameObject($"Digit_{digit}");
        digitObj.transform.SetParent(comboContainer, false);

        Image digitImage = digitObj.AddComponent<Image>();
        digitImage.sprite = numberSprites[digit];
        digitImage.SetNativeSize();

        RectTransform rectTransform = digitObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = digitSize;

        // 위치 설정: 오른쪽(1의 자리)을 기준으로 왼쪽으로 배치
        float xPos = -position * digitSpacing;
        rectTransform.anchoredPosition = new Vector2(xPos, 0);

        digitObjects.Add(digitObj);
    }

    void PlayComboAnimation()
    {
        // 모든 숫자에 펑 튀는 애니메이션 적용
        foreach (var digitObj in digitObjects)
        {
            StartCoroutine(PopAnimation(digitObj.transform));
        }
    }

    System.Collections.IEnumerator PopAnimation(Transform target)
    {
        if (target == null) yield break;

        RectTransform rectTransform = target.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        Vector3 originalScale = Vector3.one;
        Vector2 originalPos = rectTransform.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < popDuration)
        {
            if (target == null) yield break;

            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / popDuration;

            // 스케일 애니메이션: 1 → popScale → 1 (탄성 느낌)
            float scale = 1f + (popScale - 1f) * Mathf.Sin(progress * Mathf.PI);
            target.localScale = originalScale * scale;

            // 위로 튕기는 애니메이션
            float yOffset = popUpHeight * Mathf.Sin(progress * Mathf.PI);
            rectTransform.anchoredPosition = originalPos + new Vector2(0, yOffset);

            yield return null;
        }

        if (target != null)
        {
            target.localScale = originalScale;
            rectTransform.anchoredPosition = originalPos;
        }
    }

    public int GetCurrentCombo() => currentCombo;
    public int GetMaxCombo() => maxCombo;
}