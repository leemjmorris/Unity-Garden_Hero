using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas victoryCanvas;
    [SerializeField] private Canvas defeatCanvas;
    [SerializeField] private Button victoryRestartButton;
    [SerializeField] private Button victoryNextButton;
    [SerializeField] private Button defeatRestartButton;
    [SerializeField] private Button defeatQuitButton;

    [Header("Judgment Stats UI (Victory)")]
    [SerializeField] private TextMeshProUGUI victoryPerfectText;
    [SerializeField] private TextMeshProUGUI victoryGoodText;
    [SerializeField] private TextMeshProUGUI victoryMissText;

    [Header("Judgment Stats UI (Defeat)")]
    [SerializeField] private TextMeshProUGUI defeatPerfectText;
    [SerializeField] private TextMeshProUGUI defeatGoodText;
    [SerializeField] private TextMeshProUGUI defeatMissText;

    [Header("Manager References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private Transform bossPrefabsParent; // BossPrefabs 부모 - 자동으로 활성화된 보스 찾기
    [SerializeField] private BossStageManager bossStageManager;

    [Header("Victory Animation Settings")]
    [SerializeField, Range(0f, 10f)] private float victoryAnimationDelay = 2.5f;

    [Header("Fade Settings")]
    [SerializeField, Range(0f, 5f)] private float fadeInDuration = 1f;

    void Start()
    {
        SetupButtonListeners();
        FindAndSetupMonster();
        SetupEventListeners();
        HideAllCanvas();
    }

    void Update()
    {
        // Continuously check for active monster if we don't have one
        if (monsterManager == null && bossPrefabsParent != null)
        {
            FindAndSetupMonster();
            SetupEventListeners(); // Re-setup listeners when new monster is found
        }

        // LMJ: Fallback check for player death (only trigger once)
        if (playerManager != null && !playerManager.IsAlive() && !playerDeathHandled)
        {
            bool bothCanvasHidden = (victoryCanvas == null || !victoryCanvas.gameObject.activeInHierarchy) &&
                                   (defeatCanvas == null || !defeatCanvas.gameObject.activeInHierarchy);

            if (bothCanvasHidden)
            {
                playerDeathHandled = true;
                OnPlayerDeath();
            }
        }
    }

    void FindAndSetupMonster()
    {
        MonsterManager newMonster = null;

        // First try: Use bossPrefabsParent to find active monster
        if (bossPrefabsParent != null)
        {
            foreach (Transform child in bossPrefabsParent)
            {
                if (child.gameObject.activeSelf)
                {
                    MonsterManager monster = child.GetComponent<MonsterManager>();
                    if (monster != null)
                    {
                        newMonster = monster;
                        break;
                    }
                }
            }
        }

        // Fallback: Find any MonsterManager in scene
        if (newMonster == null && monsterManager == null)
        {
            newMonster = FindFirstObjectByType<MonsterManager>();
        }

        // Setup new monster if found
        if (newMonster != null && newMonster != monsterManager)
        {
            monsterManager = newMonster;
            Debug.Log($"[GameOverManager] Connected to monster: {monsterManager.gameObject.name}");
        }
    }

    void SetupButtonListeners()
    {
        if (victoryRestartButton != null)
        {
            victoryRestartButton.onClick.AddListener(RestartGame);
        }

        if (victoryNextButton != null)
        {
            victoryNextButton.onClick.AddListener(OnNextStage);
        }

        if (defeatRestartButton != null)
        {
            defeatRestartButton.onClick.AddListener(RestartGame);
        }

        if (defeatQuitButton != null)
        {
            defeatQuitButton.onClick.AddListener(QuitGame);
        }
    }

    void HideAllCanvas()
    {
        if (victoryCanvas != null)
        {
            victoryCanvas.gameObject.SetActive(false);
        }

        if (defeatCanvas != null)
        {
            defeatCanvas.gameObject.SetActive(false);
        }
    }

    void SetupEventListeners()
    {
        // LMJ: Setup PlayerManager death listener
        if (playerManager != null)
        {
            playerManager.OnDied.RemoveAllListeners();
            playerManager.OnDied.AddListener(OnPlayerDeath);
        }
        else
        {
            // LMJ: Try to find PlayerManager if not assigned
            playerManager = PlayerManager.Instance;
            if (playerManager != null)
            {
                playerManager.OnDied.RemoveAllListeners();
                playerManager.OnDied.AddListener(OnPlayerDeath);
            }
        }

        // LMJ: Setup MonsterManager death listener (if MonsterManager has OnDied event)
        if (monsterManager != null)
        {
            // LMJ: Check if MonsterManager has OnDied event
            var onDiedField = monsterManager.GetType().GetField("OnDied");
            if (onDiedField != null)
            {
                // LMJ: MonsterManager has OnDied event
                try
                {
                    var onDiedEvent = onDiedField.GetValue(monsterManager) as UnityEngine.Events.UnityEvent;
                    if (onDiedEvent != null)
                    {
                        onDiedEvent.RemoveAllListeners();
                        onDiedEvent.AddListener(OnMonsterDeath);
                        Debug.Log($"[GameOverManager] Subscribed to {monsterManager.gameObject.name}.OnDied event");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[GameOverManager] Failed to subscribe to OnDied: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[GameOverManager] MonsterManager does not have OnDied event!");
            }
        }
        else
        {
            Debug.LogWarning("[GameOverManager] MonsterManager is null in SetupEventListeners!");
        }
    }

    void OnPlayerDeath()
    {
        // Don't show Defeat immediately, let death animation play first
        StartCoroutine(DelayedDefeat());
    }

    void OnMonsterDeath()
    {
        Debug.Log("[GameOverManager] OnMonsterDeath called! Showing Victory UI...");
        ShowVictory();
    }

    System.Collections.IEnumerator DelayedDefeat()
    {
        // Wait for death animation to play
        yield return new WaitForSeconds(victoryAnimationDelay);

        ShowDefeat();
    }

    void ShowVictory()
    {
        if (victoryCanvas != null)
        {
            UpdateJudgmentStats(true);
            StartCoroutine(FadeInCanvas(victoryCanvas));
        }
    }

    void ShowDefeat()
    {
        if (defeatCanvas != null)
        {
            UpdateJudgmentStats(false);
            StartCoroutine(FadeInCanvas(defeatCanvas));
        }
    }

    void UpdateJudgmentStats(bool isVictory)
    {
        if (JudgmentStatsManager.Instance == null) return;

        int perfectCount = JudgmentStatsManager.Instance.GetPerfectCount();
        int goodCount = JudgmentStatsManager.Instance.GetGoodCount();
        int missCount = JudgmentStatsManager.Instance.GetMissCount();

        if (isVictory)
        {
            if (victoryPerfectText != null)
                victoryPerfectText.text = $"Perfect : {perfectCount:D3}";
            if (victoryGoodText != null)
                victoryGoodText.text = $"Good : {goodCount:D3}";
            if (victoryMissText != null)
                victoryMissText.text = $"Miss : {missCount:D3}";
        }
        else
        {
            if (defeatPerfectText != null)
                defeatPerfectText.text = $"Perfect : {perfectCount:D3}";
            if (defeatGoodText != null)
                defeatGoodText.text = $"Good : {goodCount:D3}";
            if (defeatMissText != null)
                defeatMissText.text = $"Miss : {missCount:D3}";
        }
    }

    System.Collections.IEnumerator FadeInCanvas(Canvas canvas)
    {
        // Canvas 활성화
        canvas.gameObject.SetActive(true);

        // CanvasGroup 가져오거나 추가
        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        }

        // 초기 alpha 값 0으로 설정
        canvasGroup.alpha = 0f;

        // Fade In 진행
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // unscaledDeltaTime 사용 (Time.timeScale 영향 안받음)
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            yield return null;
        }

        // 최종 alpha 값 1로 설정
        canvasGroup.alpha = 1f;

        // Fade In 완료 후 게임 정지
        Time.timeScale = 0f;
    }

    void OnNextStage()
    {
        Debug.Log("[GameOverManager] Advancing to next stage - RESTARTING GAME...");

        // BossStageManager가 없으면 찾기
        if (bossStageManager == null)
        {
            bossStageManager = FindFirstObjectByType<BossStageManager>();
        }

        if (bossStageManager == null)
        {
            Debug.LogWarning("[GameOverManager] BossStageManager not found! Cannot advance to next stage.");
            return;
        }

        // 다음 스테이지 ID 저장
        int nextStageId = bossStageManager.GetCurrentStageId() + 1;
        PlayerPrefs.SetInt("NextStageId", nextStageId);
        PlayerPrefs.Save();

        Debug.Log($"[GameOverManager] Saved next stage ID: {nextStageId}. Restarting game...");

        // 게임 완전 재시작 (깔끔한 상태로 시작)
        RestartGame();
    }

    // NOTE: ResetGameForNextStage() removed - we now restart the entire game for clean state
    // Stage progression is handled via PlayerPrefs and BossStageManager

    void RestartGame()
    {
        Time.timeScale = 1f;
        StopAllCoroutines();

        // LMJ: Stop all coroutines on all MonoBehaviours
        var allManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var manager in allManagers)
        {
            manager.StopAllCoroutines();
        }

        // LMJ: Reset DontDestroyOnLoad singletons before scene restart
        ResetPersistentSingletons();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void ResetPersistentSingletons()
    {
        // LMJ: Reset NoteTimeManager state
        if (NoteTimeManager.Instance != null)
        {
            NoteTimeManager.Instance.ResetForRestart();
        }

        // LMJ: CSVManager data should persist, but ensure clean state
        // No reset needed for CSVManager as data should remain loaded
    }

    void OnDestroy()
    {
        StopAllCoroutines();

        if (victoryRestartButton != null)
        {
            victoryRestartButton.onClick.RemoveAllListeners();
        }

        if (victoryNextButton != null)
        {
            victoryNextButton.onClick.RemoveAllListeners();
        }

        if (defeatRestartButton != null)
        {
            defeatRestartButton.onClick.RemoveAllListeners();
        }

        if (defeatQuitButton != null)
        {
            defeatQuitButton.onClick.RemoveAllListeners();
        }

        if (playerManager != null && playerManager.OnDied != null)
        {
            playerManager.OnDied.RemoveAllListeners();
        }

        // LMJ: Clean up MonsterManager listener if exists
        if (monsterManager != null)
        {
            try
            {
                var onDiedField = monsterManager.GetType().GetField("OnDied");
                if (onDiedField != null)
                {
                    var onDiedEvent = onDiedField.GetValue(monsterManager) as UnityEngine.Events.UnityEvent;
                    onDiedEvent?.RemoveAllListeners();
                }
            }
            catch (System.Exception)
            {
                // Ignore cleanup errors
            }
        }
    }

    // LMJ: Alternative method to check for player death if event doesn't work
    private bool playerDeathHandled = false;
}