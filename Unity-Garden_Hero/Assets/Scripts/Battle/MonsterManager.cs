using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class MonsterManager : LivingEntity
{
    [Header("Monster CSV Settings")]
    [SerializeField] private int phaseId = 80000001; // 늑대 전사 페이즈 ID
    [SerializeField] private int currentPhaseIndex = 1; // 현재 페이즈 인덱스 (0부터 시작)
    
    [Header("Monster Info (Auto Loaded)")]
    [SerializeField] private string monsterName = "늑대 전사";
    [SerializeField] private int currentBossId;
    [SerializeField] private int totalPhases;

    [Header("STUN System (Auto Loaded)")]
    [SerializeField] private float maxStun = 100;
    [SerializeField] private float currentStun = 100;
    [SerializeField] private int stunRecovery;
    [SerializeField] private int stunDefense; // STUN_DEF

    [Header("STUN Visual")]
    [SerializeField] private GameObject stunObject;

    [Header("STUN Events")]
    public UnityEvent OnStunBroken;
    public UnityEvent<float> OnStunChanged;

    [Header("Attack Multipliers (Auto Loaded)")]
    [SerializeField] private float normalAttMultiplier = 1.0f;
    [SerializeField] private float normalDefAttMultiplier = 1.0f;
    [SerializeField] private float longAttMultiplier = 1.0f;
    [SerializeField] private float longDefAttMultiplier = 1.0f;
    [SerializeField] private float specialAttMultiplier = 1.0f;
    [SerializeField] private float specialDefAttMultiplier = 1.0f;

    [Header("Debug Info")]
    [SerializeField] private string csvStatus;

    // CSV 데이터 캐시
    private PhaseData currentPhaseData;
    private BossData currentBossData;
    private bool csvDataLoaded = false;

    void Start()
    {
        LoadCSVData();
        
        if (csvDataLoaded)
        {
            InitializeFromCSV();
        }
        else
        {
            InitializeFallback();
        }
    }

    void LoadCSVData()
    {
        if (CSVManager.Instance == null)
        {
            csvStatus = "CSVManager.Instance is null";
            Debug.LogError("[MonsterManager] CSVManager.Instance is null!");
            return;
        }

        var csvDataAsset = CSVManager.Instance.GetCSVDataAsset();
        if (csvDataAsset?.phaseDataList == null || csvDataAsset?.bossDataList == null)
        {
            csvStatus = "CSV data asset or lists are null";
            Debug.LogError("[MonsterManager] CSV data not available!");
            return;
        }

        // PHASE 데이터 로드
        currentPhaseData = GetPhaseData(phaseId);
        if (currentPhaseData == null)
        {
            csvStatus = $"Phase data not found for ID: {phaseId}";
            Debug.LogError($"[MonsterManager] Phase data not found for ID: {phaseId}");
            return;
        }

        // 총 페이즈 수 계산
        CountTotalPhases();

        // 현재 보스 데이터 로드
        LoadCurrentBossData();

        csvDataLoaded = true;
        csvStatus = $"CSV loaded successfully - Phase ID: {phaseId}";
        Debug.Log($"[MonsterManager] CSV data loaded successfully for Phase ID: {phaseId}");
    }

    PhaseData GetPhaseData(int phaseId)
    {
        var phaseDataList = CSVManager.Instance.GetCSVDataAsset().phaseDataList;
        
        foreach (var phase in phaseDataList)
        {
            if (phase.PHASE_ID == phaseId)
            {
                return phase;
            }
        }
        
        return null;
    }

    void CountTotalPhases()
    {
        totalPhases = 0;
        int[] bossIds = { currentPhaseData.BOSS_ID_1, currentPhaseData.BOSS_ID_2, 
                         currentPhaseData.BOSS_ID_3, currentPhaseData.BOSS_ID_4, 
                         currentPhaseData.BOSS_ID_5 };

        foreach (int bossId in bossIds)
        {
            if (bossId > 0) // NULL이 아닌 경우 (CSV에서는 0 또는 음수)
            {
                totalPhases++;
            }
        }

        Debug.Log($"[MonsterManager] Total phases: {totalPhases}");
    }

    int GetBossIdForPhaseIndex(int phaseIndex)
    {
        int[] bossIds = { currentPhaseData.BOSS_ID_1, currentPhaseData.BOSS_ID_2, 
                         currentPhaseData.BOSS_ID_3, currentPhaseData.BOSS_ID_4, 
                         currentPhaseData.BOSS_ID_5 };

        if (phaseIndex >= 0 && phaseIndex < bossIds.Length && phaseIndex < totalPhases)
        {
            return bossIds[phaseIndex];
        }

        return -1;
    }

    void LoadCurrentBossData()
    {
        currentBossId = GetBossIdForPhaseIndex(currentPhaseIndex);
        
        if (currentBossId <= 0)
        {
            csvStatus = $"Invalid boss ID for phase index: {currentPhaseIndex}";
            Debug.LogError($"[MonsterManager] Invalid boss ID for phase index: {currentPhaseIndex}");
            return;
        }

        currentBossData = GetBossData(currentBossId);
        if (currentBossData == null)
        {
            csvStatus = $"Boss data not found for ID: {currentBossId}";
            Debug.LogError($"[MonsterManager] Boss data not found for ID: {currentBossId}");
            return;
        }

        Debug.Log($"[MonsterManager] Loaded boss data for ID: {currentBossId} ({currentBossData.BOSS_NAME})");
    }

    BossData GetBossData(int bossId)
    {
        var bossDataList = CSVManager.Instance.GetCSVDataAsset().bossDataList;
        
        foreach (var boss in bossDataList)
        {
            if (boss.BOSS_ID == bossId)
            {
                return boss;
            }
        }
        
        return null;
    }

    void InitializeFromCSV()
    {
        if (currentBossData == null) return;

        // 기본 정보 설정
        monsterName = currentBossData.BOSS_NAME;
        
        // LivingEntity 초기화
        int bossLevel = currentBossData.PHASE;
        int bossAttack = Mathf.RoundToInt(currentBossData.NORMAL_ATT);
        int bossDefense = currentBossData.DEF;
        int bossHealth = Mathf.RoundToInt(currentBossData.HP);
        
        Initialize(bossLevel, bossAttack, bossDefense, bossHealth);

        // STUN 시스템 초기화
        maxStun = currentBossData.STUN;
        currentStun = maxStun;
        stunRecovery = currentBossData.STUN_RECOVERY;
        stunDefense = currentBossData.STUN_DEF;

        // 공격 배율 설정
        normalAttMultiplier = currentBossData.NORMAL_ATT;
        normalDefAttMultiplier = currentBossData.NORMAL_DEF_ATT;
        longAttMultiplier = currentBossData.LONG_ATT;
        longDefAttMultiplier = currentBossData.LONG_DEF_ATT;
        specialAttMultiplier = currentBossData.SPECIAL_ATT;
        specialDefAttMultiplier = currentBossData.SPECIAL_DEF_ATT;

        OnStunChanged?.Invoke(currentStun);
        UpdateStunVisual();

        csvStatus = $"Initialized {monsterName} Phase {currentBossData.PHASE} - HP:{bossHealth}, STUN:{maxStun}";
        Debug.Log($"[MonsterManager] {csvStatus}");
    }

    void InitializeFallback()
    {
        // CSV 로드 실패시 기본값
        Initialize(1, 10, 2, 50);
        maxStun = 30;
        currentStun = maxStun;
        stunRecovery = 7;
        totalPhases = 2;
        
        OnStunChanged?.Invoke(currentStun);
        UpdateStunVisual();
        
        csvStatus = "Using fallback values - CSV data not available";
        Debug.LogWarning("[MonsterManager] " + csvStatus);
    }

    public void TakeNoteHit(int playerAttack, string noteType, JudgmentResult judgment)
    {
        if (judgment == JudgmentResult.Miss) return;

        // 노트 타입별 데미지 배율 적용
        float damageMultiplier = GetNoteTypeDamageMultiplier(noteType);
        float judgmentMultiplier = GetJudgmentMultiplier(judgment);
        
        int finalDamage = Mathf.RoundToInt(playerAttack * damageMultiplier * judgmentMultiplier);

        if (currentStun > 0)
        {
            // STUN에 데미지 적용 (STUN_DEF 고려)
            float stunDamage = Mathf.Max(1, finalDamage - stunDefense);
            currentStun = Mathf.Max(0, currentStun - stunDamage);

            OnStunChanged?.Invoke(currentStun);

            if (currentStun <= 0)
            {
                Debug.Log($"{monsterName}'s stun is broken! Entering DealingTime...");
                UpdateStunVisual();
                OnStunBroken?.Invoke();
            }
            else
            {
                UpdateStunVisual();
            }

            Debug.Log($"[MonsterManager] STUN hit: -{stunDamage}, remaining: {currentStun}");
        }
        else
        {
            // 체력에 직접 데미지 (DealingTime 중)
            OnDamage(finalDamage);
            Debug.Log($"[MonsterManager] HP hit: -{finalDamage}, remaining: {currentHealth}");
        }
    }

    float GetNoteTypeDamageMultiplier(string noteType)
    {
        if (!csvDataLoaded || currentBossData == null) return 1.0f;

        return noteType.ToLower() switch
        {
            "normal" => normalDefAttMultiplier,
            "long" or "long_head" or "long_tail" or "long_hold" => longDefAttMultiplier,
            "special" => specialDefAttMultiplier,
            _ => 1.0f
        };
    }

    float GetJudgmentMultiplier(JudgmentResult judgment)
    {
        return judgment switch
        {
            JudgmentResult.Perfect => 1.0f,
            JudgmentResult.Good => 0.7f,
            JudgmentResult.Miss => 0.0f,
            _ => 1.0f
        };
    }

    public void TakeDealingTimeDamage(int damage)
    {
        if (currentStun > 0)
        {
            Debug.LogWarning("Cannot deal direct damage while stun is active!");
            return;
        }

        OnDamage(damage);
        Debug.Log($"{monsterName} takes {damage} direct damage! Health: {currentHealth}/{maxHealth}");
    }

    public void ResetStun()
    {
        currentStun = maxStun;
        OnStunChanged?.Invoke(currentStun);
        UpdateStunVisual();

        Debug.Log($"{monsterName}'s stun restored to {currentStun}!");
    }

    protected override void OnDeath()
    {
        Debug.Log($"{monsterName} Phase {currentPhaseIndex + 1} has been defeated!");

        if (CanAdvancePhase())
        {
            AdvancePhase();
        }
        else
        {
            HandleMonsterDeath();
        }

        base.OnDeath();
    }

    bool CanAdvancePhase()
    {
        return currentPhaseIndex + 1 < totalPhases;
    }

    void AdvancePhase()
    {
        currentPhaseIndex++;
        LoadCurrentBossData();
        
        if (csvDataLoaded && currentBossData != null)
        {
            InitializeFromCSV();
            Debug.Log($"{monsterName} advanced to Phase {currentPhaseIndex + 1}!");
        }
        else
        {
            // Fallback phase advancement
            Initialize(level + 1, attackPower + 5, defense + 1, maxHealth + 20);
            ResetStun();
        }
    }

    void UpdateStunVisual()
    {
        if (stunObject == null) return;
        stunObject.SetActive(currentStun > 0);
    }

    void HandleMonsterDeath()
    {
        Debug.Log($"{monsterName} completely defeated! Victory!");
        gameObject.SetActive(false);
    }

    // Getter methods
    public float GetCurrentStun() => currentStun;
    public float GetMaxStun() => maxStun;
    public float GetStunPercentage() => maxStun > 0 ? currentStun / maxStun : 0f;
    public bool HasStun() => currentStun > 0;
    public string GetMonsterName() => monsterName;
    public int GetPhase() => currentPhaseIndex + 1;
    public int GetCurrentBossId() => currentBossId;
    public int GetTotalPhases() => totalPhases;

    // Context Menu methods
    [ContextMenu("🔄 Reload CSV Data")]
    public void ReloadCSVData()
    {
        LoadCSVData();
        if (csvDataLoaded)
        {
            InitializeFromCSV();
        }
    }

    [ContextMenu("⚡ Force Next Phase")]
    public void ForceNextPhase()
    {
        if (CanAdvancePhase())
        {
            AdvancePhase();
        }
        else
        {
            Debug.Log("Cannot advance - already at final phase");
        }
    }

    [ContextMenu("🛡️ Reset STUN")]
    public void ForceResetStun()
    {
        ResetStun();
    }

    [ContextMenu("📊 Log Monster Status")]
    public void LogMonsterStatus()
    {
        Debug.Log($"[MonsterManager Status]\n" +
                 $"Name: {monsterName}\n" +
                 $"Phase: {currentPhaseIndex + 1}/{totalPhases}\n" +
                 $"Boss ID: {currentBossId}\n" +
                 $"HP: {currentHealth}/{maxHealth}\n" +
                 $"STUN: {currentStun}/{maxStun}\n" +
                 $"ATK: {attackPower}, DEF: {defense}\n" +
                 $"CSV Status: {csvStatus}");
    }
}