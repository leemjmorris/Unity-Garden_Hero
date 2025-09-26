using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas gameOverCanvas;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Manager References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private MonsterManager monsterManager;

    void Start()
    {
        CreateGameOverUI();
        SetupEventListeners();
    }

    void CreateGameOverUI()
    {
        if (gameOverCanvas == null)
        {
            GameObject canvasObj = new GameObject("GameOverCanvas");
            gameOverCanvas = canvasObj.AddComponent<Canvas>();
            gameOverCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameOverCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (gameOverPanel == null)
        {
            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(gameOverCanvas.transform, false);

            RectTransform panelRect = gameOverPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = gameOverPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.8f);
        }

        CreateGameOverText();
        CreateRestartButton();

        gameOverPanel.SetActive(false);
    }

    void CreateGameOverText()
    {
        GameObject textObj = new GameObject("GameOverText");
        textObj.transform.SetParent(gameOverPanel.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0, 100);
        textRect.sizeDelta = new Vector2(800, 100);

        gameOverText = textObj.AddComponent<TextMeshProUGUI>();
        gameOverText.text = "GAME OVER";
        gameOverText.fontSize = 80;
        gameOverText.color = Color.red;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.fontStyle = FontStyles.Bold;
    }

    void CreateRestartButton()
    {
        GameObject buttonObj = new GameObject("RestartButton");
        buttonObj.transform.SetParent(gameOverPanel.transform, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0, -50);
        buttonRect.sizeDelta = new Vector2(300, 80);

        restartButton = buttonObj.AddComponent<Button>();

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);

        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = buttonTextObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "RESTART";
        buttonText.fontSize = 40;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;

        restartButton.onClick.AddListener(RestartGame);
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
                    }
                }
                catch (System.Exception)
                {
                }
            }
        }
    }

    void OnPlayerDeath()
    {
        ShowGameOver("GAME OVER");
    }

    void OnMonsterDeath()
    {
        ShowGameOver("VICTORY!");
    }

    void ShowGameOver(string message)
    {
        gameOverText.text = message;

        if (message == "VICTORY!")
        {
            gameOverText.color = Color.green;
        }
        else
        {
            gameOverText.color = Color.red;
        }

        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        StopAllCoroutines();

        var allManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var manager in allManagers)
        {
            manager.StopAllCoroutines();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        StopAllCoroutines();

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
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
    void Update()
    {
        // LMJ: Fallback check for player death
        if (playerManager != null && !playerManager.IsAlive() && gameOverPanel != null && !gameOverPanel.activeInHierarchy)
        {
            OnPlayerDeath();
        }
    }
}