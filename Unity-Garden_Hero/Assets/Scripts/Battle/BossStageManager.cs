using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class BossStageManager : MonoBehaviour
{
    [Header("Stage Settings")]
    [SerializeField] private int currentStageId = 90000001; // 시작 스테이지 ID

    [Header("Boss Prefab References")]
    [SerializeField] private Transform bossPrefabsParent; // BossPrefabs 부모 오브젝트

    [Header("Animator Settings")]
    [SerializeField] private RuntimeAnimatorController baseAnimatorController; // 기본 Animator Controller
    [SerializeField] private List<string> bossesWithCustomAnimator = new List<string>(); // Override를 사용하지 않는 보스 목록

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
        Debug.Log("[BossStageManager] Awake() called");

        // BossPrefabs 부모가 설정되지 않았으면 자동으로 찾기
        if (bossPrefabsParent == null)
        {
            bossPrefabsParent = transform;
            Debug.Log("[BossStageManager] bossPrefabsParent not assigned, using transform");
        }

        // 모든 Boss Prefab을 Dictionary에 저장
        CacheBossPrefabs();
    }

    void Start()
    {
        Debug.Log($"[BossStageManager] Start() called with initial currentStageId: {currentStageId}");

        // Check if we're returning from a stage completion (PlayerPrefs)
        if (PlayerPrefs.HasKey("NextStageId"))
        {
            currentStageId = PlayerPrefs.GetInt("NextStageId");
            PlayerPrefs.DeleteKey("NextStageId"); // Clear after reading
            Debug.Log($"[BossStageManager] Loaded next stage ID from PlayerPrefs: {currentStageId}");
        }

        // CSV 데이터 로드 및 첫 스테이지 설정
        Debug.Log($"[BossStageManager] About to load stage data for ID: {currentStageId}");
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
        Debug.Log($"[BossStageManager] Cached {bossPrefabMap.Count} boss prefabs: {string.Join(", ", bossPrefabMap.Keys)}");
    }

    void LoadStageData(int stageId)
    {
        if (CSVManager.Instance == null)
        {
            csvStatus = "CSVManager.Instance is null";
            Debug.LogError("[BossStageManager] CSVManager.Instance is null!");
            return;
        }

        // STAGE 데이터 로드
        currentStageData = CSVManager.Instance.GetStageData(stageId.ToString());
        if (currentStageData == null)
        {
            csvStatus = $"Stage data not found for ID: {stageId}";
            Debug.LogError($"[BossStageManager] Stage data not found for ID: {stageId}. Make sure to load CSV data in CSVDataAsset (Right-click → Load All CSV Data)");
            return;
        }

        currentPhaseId = currentStageData.PHASE_ID;

        // PHASE 데이터 로드
        currentPhaseData = CSVManager.Instance.GetPhaseData(currentPhaseId.ToString());
        if (currentPhaseData == null)
        {
            csvStatus = $"Phase data not found for ID: {currentPhaseId}";
            Debug.LogError($"[BossStageManager] Phase data not found for ID: {currentPhaseId}");
            return;
        }

        // 첫 번째 BOSS 데이터 로드 (BOSS_ID_1)
        int firstBossId = currentPhaseData.BOSS_ID_1;
        if (firstBossId <= 0)
        {
            csvStatus = $"No valid boss found in phase {currentPhaseId}";
            Debug.LogError($"[BossStageManager] No valid boss found in phase {currentPhaseId}. BOSS_ID_1: {firstBossId}");
            return;
        }

        firstBossData = CSVManager.Instance.GetBossData(firstBossId.ToString());
        if (firstBossData == null)
        {
            csvStatus = $"Boss data not found for ID: {firstBossId}";
            Debug.LogError($"[BossStageManager] Boss data not found for ID: {firstBossId}");
            return;
        }

        currentBossPrefabName = firstBossData.BOSS_PREFABS;
        csvStatus = $"Stage {stageId} loaded - Phase {currentPhaseId} - Boss: {currentBossPrefabName}";

        Debug.Log($"[BossStageManager] Stage {stageId} loaded successfully:\n" +
                  $"  Phase ID: {currentPhaseId}\n" +
                  $"  Boss ID: {firstBossId}\n" +
                  $"  Boss Name: {firstBossData.BOSS_NAME}\n" +
                  $"  Boss Prefab: {currentBossPrefabName}");

        // Invoke stage changed event
        OnStageChanged?.Invoke(stageId);
    }

    void ActivateCurrentStageBoss()
    {
        if (string.IsNullOrEmpty(currentBossPrefabName))
        {
            csvStatus = "No boss prefab name to activate";
            Debug.LogError("[BossStageManager] No boss prefab name to activate!");
            return;
        }

        // 현재 활성화된 Boss가 있으면 비활성화
        if (currentActiveBoss != null)
        {
            currentActiveBoss.SetActive(false);
            Debug.Log($"[BossStageManager] Deactivated previous boss: {currentActiveBoss.name}");
        }

        // 새로운 Boss 활성화
        if (bossPrefabMap.TryGetValue(currentBossPrefabName, out GameObject bossPrefab))
        {
            bossPrefab.SetActive(true);
            currentActiveBoss = bossPrefab;

            // Animator Override 설정
            SetupBossAnimator(bossPrefab, currentBossPrefabName);

            csvStatus = $"Activated boss: {currentBossPrefabName}";
            Debug.Log($"[BossStageManager] ✓ Activated boss: {currentBossPrefabName}");
        }
        else
        {
            csvStatus = $"Boss prefab not found: {currentBossPrefabName}";
            Debug.LogError($"[BossStageManager] Boss prefab not found in scene: {currentBossPrefabName}\n" +
                          $"Available prefabs in cache: {string.Join(", ", bossPrefabMap.Keys)}");
        }
    }

    void SetupBossAnimator(GameObject bossObject, string bossType)
    {
        // Animator 컴포넌트 찾기 (자식 오브젝트 포함)
        Animator animator = bossObject.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[BossStageManager] No Animator found on boss: {bossType}");
            return;
        }

        // 커스텀 Animator를 사용하는 보스인지 확인
        if (bossesWithCustomAnimator.Contains(bossType))
        {
            Debug.Log($"[BossStageManager] Boss '{bossType}' uses custom animator. Skipping override.");
            return;
        }

        // Base Animator Controller가 설정되어 있지 않으면 현재 컨트롤러를 사용
        RuntimeAnimatorController controllerToUse = baseAnimatorController != null ? baseAnimatorController : animator.runtimeAnimatorController;

        if (controllerToUse == null)
        {
            Debug.LogWarning($"[BossStageManager] No base animator controller set for boss: {bossType}");
            return;
        }

        // Override Animator Controller 생성
        AnimatorOverrideController overrideController = new AnimatorOverrideController(controllerToUse);

        // 해당 보스 타입의 애니메이션 클립들을 로드
        AnimationClip[] clips = LoadBossAnimationClips(bossType);

        if (clips.Length == 0)
        {
            Debug.LogWarning($"[BossStageManager] No animation clips found for boss: {bossType}");
            return;
        }

        // 클립 이름으로 매칭해서 자동 교체
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        int overrideCount = 0;

        // AnimatorOverrideController에서 현재 오버라이드 목록 가져오기
        var currentOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(currentOverrides);

        foreach (var clipPair in currentOverrides)
        {
            if (clipPair.Key == null) continue;

            // 원본 클립 이름과 같은 이름의 클립을 찾아서 교체
            AnimationClip newClip = System.Array.Find(clips, c => c.name == clipPair.Key.name);
            if (newClip != null)
            {
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(clipPair.Key, newClip));
                overrideCount++;
                Debug.Log($"[BossStageManager] Override: {clipPair.Key.name} → {newClip.name}");
            }
            else
            {
                // 매칭되는 클립이 없으면 원본 유지
                overrides.Add(clipPair);
            }
        }

        if (overrideCount > 0)
        {
            overrideController.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = overrideController;
            Debug.Log($"[BossStageManager] ✓ Applied {overrideCount} animation overrides for {bossType}");
        }
        else
        {
            Debug.LogWarning($"[BossStageManager] No matching animation clips found to override for {bossType}");
        }
    }

    AnimationClip[] LoadBossAnimationClips(string bossType)
    {
        // 에셋 경로에서 직접 로드 (RPGMonsterBundlePolyart 구조)
        string[] possiblePaths = new string[]
        {
            $"RPGMonsterBundlePolyart/RPGMonsterWave02Polyart/Animations/{bossType}",
            $"RPGMonsterBundlePolyart/RPGMonsterWave01Polyart/Animations/{bossType}",
            $"RPGMonsterBundlePolyart/RPGMonsterWave03Polyart/Animations/{bossType}",
        };

        foreach (string path in possiblePaths)
        {
            string fullPath = $"Assets/{path}";

#if UNITY_EDITOR
            // 에디터에서는 AssetDatabase 사용
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AnimationClip", new[] { fullPath });
            if (guids.Length > 0)
            {
                AnimationClip[] clips = new AnimationClip[guids.Length];
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                    clips[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                }
                Debug.Log($"[BossStageManager] Loaded {clips.Length} animation clips from {fullPath}");
                return clips;
            }
#endif
        }

        // Resources 폴더에서도 시도 (백업)
        string resourcePath = $"Animations/{bossType}";
        AnimationClip[] resourceClips = Resources.LoadAll<AnimationClip>(resourcePath);
        if (resourceClips.Length > 0)
        {
            Debug.Log($"[BossStageManager] Loaded {resourceClips.Length} animation clips from Resources/{resourcePath}");
            return resourceClips;
        }

        Debug.LogWarning($"[BossStageManager] Could not find animation clips for boss: {bossType}");
        return new AnimationClip[0];
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
