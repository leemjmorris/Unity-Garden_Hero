using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class BossStageManager : MonoBehaviour
{
    [Header("Stage Settings")]
    [SerializeField] private int currentStageId = 90000001; // 시작 스테이지 ID

    [Header("Boss Prefab References")]
    [SerializeField] private Transform bossPrefabsParent; // BossPrefabs 부모 오브젝트

    [Header("Debug Info")]
    [SerializeField] private string csvStatus;
    [SerializeField] private int currentPhaseId;
    [SerializeField] private string currentBossPrefabName;

    [Header("Stage Events")]
    public UnityEvent<int> OnStageChanged; // 스테이지가 변경될 때 호출

    // CSV 데이터
    private StageData currentStageData;
    private PhaseData currentPhaseData;
    private BossData firstBossData; // Phase의 첫 번째 Boss

    // Boss Prefab 캐시
    private Dictionary<string, GameObject> bossPrefabMap = new Dictionary<string, GameObject>();
    private GameObject currentActiveBoss;

    void Awake()
    {
        // BossPrefabs 부모가 설정되지 않았으면 자동으로 찾기
        if (bossPrefabsParent == null)
        {
            bossPrefabsParent = transform;
        }

        // 모든 Boss Prefab을 Dictionary에 저장
        CacheBossPrefabs();
    }

    void Start()
    {
        // Check if we're returning from a stage completion (PlayerPrefs)
        if (PlayerPrefs.HasKey("NextStageId"))
        {
            currentStageId = PlayerPrefs.GetInt("NextStageId");
            PlayerPrefs.DeleteKey("NextStageId"); // Clear after reading
            Debug.Log($"[BossStageManager] Loaded next stage ID from PlayerPrefs: {currentStageId}");
        }

        // CSV 데이터 로드 및 첫 스테이지 설정
        LoadStageData(currentStageId);
        ActivateCurrentStageBoss();
    }

    void CacheBossPrefabs()
    {
        bossPrefabMap.Clear();

        // BossPrefabs 부모의 모든 자식을 캐싱
        foreach (Transform child in bossPrefabsParent)
        {
            bossPrefabMap[child.name] = child.gameObject;
            child.gameObject.SetActive(false); // 시작 시 모두 비활성화
        }

        csvStatus = $"Cached {bossPrefabMap.Count} boss prefabs";
    }

    void LoadStageData(int stageId)
    {
        if (CSVManager.Instance == null)
        {
            csvStatus = "CSVManager.Instance is null";
            return;
        }

        // STAGE 데이터 로드
        currentStageData = CSVManager.Instance.GetStageData(stageId.ToString());
        if (currentStageData == null)
        {
            csvStatus = $"Stage data not found for ID: {stageId}";
            return;
        }

        currentPhaseId = currentStageData.PHASE_ID;

        // PHASE 데이터 로드
        currentPhaseData = CSVManager.Instance.GetPhaseData(currentPhaseId.ToString());
        if (currentPhaseData == null)
        {
            csvStatus = $"Phase data not found for ID: {currentPhaseId}";
            return;
        }

        // 첫 번째 BOSS 데이터 로드 (BOSS_ID_1)
        int firstBossId = currentPhaseData.BOSS_ID_1;
        if (firstBossId <= 0)
        {
            csvStatus = $"No valid boss found in phase {currentPhaseId}";
            return;
        }

        firstBossData = CSVManager.Instance.GetBossData(firstBossId.ToString());
        if (firstBossData == null)
        {
            csvStatus = $"Boss data not found for ID: {firstBossId}";
            return;
        }

        currentBossPrefabName = firstBossData.BOSS_PREFABS;
        csvStatus = $"Stage {stageId} loaded - Phase {currentPhaseId} - Boss: {currentBossPrefabName}";

        // Invoke stage changed event
        OnStageChanged?.Invoke(stageId);
    }

    void ActivateCurrentStageBoss()
    {
        if (string.IsNullOrEmpty(currentBossPrefabName))
        {
            csvStatus = "No boss prefab name to activate";
            return;
        }

        // 현재 활성화된 Boss가 있으면 비활성화
        if (currentActiveBoss != null)
        {
            currentActiveBoss.SetActive(false);
        }

        // 새로운 Boss 활성화
        if (bossPrefabMap.TryGetValue(currentBossPrefabName, out GameObject bossPrefab))
        {
            bossPrefab.SetActive(true);
            currentActiveBoss = bossPrefab;

            csvStatus = $"Activated boss: {currentBossPrefabName}";
        }
        else
        {
            csvStatus = $"Boss prefab not found: {currentBossPrefabName}";
        }
    }

    public void ActivateNextStage()
    {
        // 다음 스테이지 ID로 증가
        int nextStageId = currentStageId + 1;

        // 다음 스테이지 데이터 로드 시도
        StageData nextStageData = CSVManager.Instance.GetStageData(nextStageId.ToString());
        if (nextStageData == null)
        {
            csvStatus = $"No more stages available after {currentStageId}";
            Debug.LogWarning($"[BossStageManager] No more stages available after Stage {currentStageId}");
            return;
        }

        // 스테이지 전환
        currentStageId = nextStageId;
        LoadStageData(currentStageId);
        ActivateCurrentStageBoss();

        Debug.Log($"[BossStageManager] Advanced to Stage {currentStageId}");
    }

    // Getter methods
    public int GetCurrentStageId() => currentStageId;
    public int GetCurrentPhaseId() => currentPhaseId;
    public string GetCurrentBossPrefabName() => currentBossPrefabName;
    public GameObject GetCurrentActiveBoss() => currentActiveBoss;

    // Context Menu for debugging
    [ContextMenu("🔄 Reload Current Stage")]
    public void ReloadCurrentStage()
    {
        LoadStageData(currentStageId);
        ActivateCurrentStageBoss();
    }

    [ContextMenu("⏭️ Force Next Stage")]
    public void ForceNextStage()
    {
        ActivateNextStage();
    }

    [ContextMenu("📊 Log Stage Status")]
    public void LogStageStatus()
    {
        Debug.Log($"Current Stage ID: {currentStageId}\n" +
                 $"Current Phase ID: {currentPhaseId}\n" +
                 $"Current Boss Prefab: {currentBossPrefabName}\n" +
                 $"Active Boss: {(currentActiveBoss != null ? currentActiveBoss.name : "None")}\n" +
                 $"CSV Status: {csvStatus}");
    }
}
