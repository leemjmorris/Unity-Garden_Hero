using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class MonsterManager : LivingEntity
{
    [Header("Monster CSV Settings")]
    [SerializeField] private bool autoDetectFromPrefabName = true; // Prefab 이름으로 자동 감지
    [SerializeField] private int phaseId = 0; // 수동 설정용 (autoDetectFromPrefabName = false일 때)
    [SerializeField] private int currentPhaseIndex = 0; // 현재 페이즈 인덱스 (0부터 시작)

    [Header("Monster Info (Auto Loaded)")]
    [SerializeField] private string monsterName = "";
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
    public UnityEvent OnStunRestored;

    [Header("Phase Events")]
    public UnityEvent<int> OnPhaseChanged;
    public UnityEvent OnMonsterDead;

    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private float minAttackInterval = 2f;
    [SerializeField] private float maxAttackInterval = 5f;
    private Coroutine attackAnimationCoroutine;

    [Header("Attack Multipliers (Auto Loaded)")]
    [SerializeField] private float normalAttMultiplier = 1.0f;
    [SerializeField] private float normalDefAttMultiplier = 1.0f;
    [SerializeField] private float longAttMultiplier = 1.0f;
    [SerializeField] private float longDefAttMultiplier = 1.0f;
    [SerializeField] private float specialAttMultiplier = 1.0f;
    [SerializeField] private float specialDefAttMultiplier = 1.0f;

    [Header("Debug Info")]
    [SerializeField] private string csvStatus;

    [Header("Manager References")]
    [SerializeField] private RhythmGameSystem rhythmGameSystem;

    // CSV 데이터 캐시
    private PhaseData currentPhaseData;
    private BossData currentBossData;
    private BossAttData currentBossAttData;
    private bool csvDataLoaded = false;

    // GameManager 참조
    private GameManager gameManager;

    // Animator 파라미터 캐시 (성능 최적화 및 존재 여부 확인)
    private bool hasRandomAttParam = false;
    private bool hasIsDealingTimeParam = false;
    private bool hasIsVictoryParam = false;
    private bool hasGetHitTrigger = false;
    private bool hasOnDeathTrigger = false;

    void Start()
    {
        // Get references
        gameManager = FindFirstObjectByType<GameManager>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Auto-assign default animator controller if missing
        AssignDefaultAnimatorControllerIfNeeded();

        // Check animator parameters
        CheckAnimatorParameters();

        LoadCSVData();

        if (csvDataLoaded)
        {
            InitializeFromCSV();
        }
        else
        {
            InitializeFallback();
        }

        // Start attack animation coroutine
        if (animator != null && gameManager != null)
        {
            attackAnimationCoroutine = StartCoroutine(AutoAttackAnimation());
        }
    }

    void AssignDefaultAnimatorControllerIfNeeded()
    {
        // This function is kept for backward compatibility but does nothing now
        // Each boss should have its own Animator Controller with proper animation clips
        // MonsterManager will safely handle missing parameters through CheckAnimatorParameters()
    }

    void CheckAnimatorParameters()
    {
        if (animator == null)
        {
            Debug.LogWarning($"[MonsterManager] {gameObject.name} has no Animator component");
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[MonsterManager] {gameObject.name} has no Animator Controller");
            return;
        }

        // Check which parameters exist in the animator
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            switch (param.name)
            {
                case "randomAtt":
                    hasRandomAttParam = true;
                    break;
                case "isDealingTime":
                    hasIsDealingTimeParam = true;
                    break;
                case "isVictory":
                    hasIsVictoryParam = true;
                    break;
                case "GetHit":
                    hasGetHitTrigger = true;
                    break;
                case "OnDeath":
                    hasOnDeathTrigger = true;
                    break;
            }
        }

        Debug.Log($"[MonsterManager] {gameObject.name} Animator params - randomAtt:{hasRandomAttParam}, isDealingTime:{hasIsDealingTimeParam}, isVictory:{hasIsVictoryParam}, GetHit:{hasGetHitTrigger}, OnDeath:{hasOnDeathTrigger}");
    }

    void LoadCSVData()
    {
        if (CSVManager.Instance == null)
        {
            csvStatus = "CSVManager.Instance is null";
            return;
        }

        var csvDataAsset = CSVManager.Instance.GetCSVDataAsset();
        if (csvDataAsset?.phaseDataList == null || csvDataAsset?.bossDataList == null)
        {
            csvStatus = "CSV data asset or lists are null";
            return;
        }

        // Auto-detect Phase ID from prefab name if enabled
        if (autoDetectFromPrefabName)
        {
            string prefabName = gameObject.name.Replace("(Clone)", "").Trim();
            Debug.Log($"[MonsterManager] Attempting auto-detect for prefab: '{prefabName}'");

            int detectedPhaseId = FindPhaseIdByPrefabName(prefabName);

            if (detectedPhaseId > 0)
            {
                phaseId = detectedPhaseId;
                csvStatus = $"✓ Auto-detected Phase ID: {phaseId} from prefab: {prefabName}";
                Debug.Log($"[MonsterManager] {csvStatus}");
            }
            else
            {
                csvStatus = $"✗ Failed to auto-detect from '{prefabName}'. Manual phaseId: {phaseId}";
                Debug.LogWarning($"[MonsterManager] {csvStatus}");

                if (phaseId <= 0)
                {
                    Debug.LogError($"[MonsterManager] No valid Phase ID! Enable autoDetect or set phaseId manually.");
                    return;
                }
            }
        }
        else if (phaseId <= 0)
        {
            csvStatus = "✗ Auto-detect disabled and no manual phaseId set!";
            Debug.LogError($"[MonsterManager] {csvStatus}");
            return;
        }

        // PHASE 데이터 로드
        currentPhaseData = GetPhaseData(phaseId);
        if (currentPhaseData == null)
        {
            csvStatus = $"Phase data not found for ID: {phaseId}";
            return;
        }

        // 총 페이즈 수 계산
        CountTotalPhases();

        // 현재 보스 데이터 로드
        LoadCurrentBossData();

        csvDataLoaded = true;
        csvStatus = $"CSV loaded successfully - Phase ID: {phaseId}";

        // Invoke initial phase event
        OnPhaseChanged?.Invoke(currentPhaseIndex + 1);
    }

    int FindPhaseIdByPrefabName(string prefabName)
    {
        var csvDataAsset = CSVManager.Instance.GetCSVDataAsset();
        if (csvDataAsset?.bossDataList == null || csvDataAsset?.phaseDataList == null)
        {
            Debug.LogError("[MonsterManager] CSV data not loaded!");
            return -1;
        }

        Debug.Log($"[MonsterManager] Searching for prefab '{prefabName}' in {csvDataAsset.bossDataList.Count} boss entries...");

        // 1. Find matching BossData by BOSS_PREFABS field
        BossData matchingBoss = null;
        foreach (var boss in csvDataAsset.bossDataList)
        {
            Debug.Log($"[MonsterManager] Checking BOSS_ID {boss.BOSS_ID}: BOSS_PREFABS='{boss.BOSS_PREFABS}' vs '{prefabName}'");

            if (!string.IsNullOrEmpty(boss.BOSS_PREFABS) &&
                boss.BOSS_PREFABS.Equals(prefabName, System.StringComparison.OrdinalIgnoreCase))
            {
                matchingBoss = boss;
                Debug.Log($"[MonsterManager] ✓ Found match! BOSS_ID: {boss.BOSS_ID}, Name: {boss.BOSS_NAME}");
                break;
            }
        }

        if (matchingBoss == null)
        {
            Debug.LogWarning($"[MonsterManager] No boss found with BOSS_PREFABS = '{prefabName}'");
            return -1;
        }

        // 2. Find PhaseData that contains this BOSS_ID
        int bossId = matchingBoss.BOSS_ID;
        Debug.Log($"[MonsterManager] Looking for Phase containing BOSS_ID {bossId}...");

        foreach (var phase in csvDataAsset.phaseDataList)
        {
            if (phase.BOSS_ID_1 == bossId || phase.BOSS_ID_2 == bossId ||
                phase.BOSS_ID_3 == bossId || phase.BOSS_ID_4 == bossId ||
                phase.BOSS_ID_5 == bossId)
            {
                Debug.Log($"[MonsterManager] ✓ Found PHASE_ID: {phase.PHASE_ID}");
                return phase.PHASE_ID;
            }
        }

        Debug.LogWarning($"[MonsterManager] No Phase found containing BOSS_ID {bossId}");
        return -1;
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
            return;
        }

        currentBossData = GetBossData(currentBossId);
        if (currentBossData == null)
        {
            csvStatus = $"Boss data not found for ID: {currentBossId}";
            return;
        }

        // Load BossAttData using BOSS_ATT_ID
        currentBossAttData = GetBossAttData(currentBossData.BOSS_ATT_ID);
        if (currentBossAttData == null)
        {
            csvStatus = $"Boss attack data not found for ATT_ID: {currentBossData.BOSS_ATT_ID}";
            Debug.LogError($"[MonsterManager] BS_ATT data not found for BOSS_ATT_ID: {currentBossData.BOSS_ATT_ID}. Make sure to reload CSV data in CSVDataAsset (Right-click → Load All CSV Data)");
            return;
        }

        Debug.Log($"[MonsterManager] BS_ATT data loaded successfully for {currentBossData.BOSS_NAME} Phase {currentBossData.PHASE}:\n" +
                  $"  BOSS_ATT_ID: {currentBossAttData.BOSS_ATT_ID}\n" +
                  $"  NORMAL_ATT: {currentBossAttData.NORMAL_ATT}\n" +
                  $"  LONG_ATT: {currentBossAttData.LONG_ATT}\n" +
                  $"  SPECIAL_ATT: {currentBossAttData.SPECIAL_ATT}");
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

    BossAttData GetBossAttData(int bossAttId)
    {
        var csvDataAsset = CSVManager.Instance.GetCSVDataAsset();
        return csvDataAsset.GetBossAttData(bossAttId);
    }

    void InitializeFromCSV()
    {
        if (currentBossData == null || currentBossAttData == null) return;

        // 기본 정보 설정
        monsterName = currentBossData.BOSS_NAME;

        // LivingEntity 초기화
        int bossLevel = currentBossData.PHASE;
        int bossAttack = currentBossAttData.NORMAL_ATT; // BS_ATT의 NORMAL_ATT를 기본 공격력으로 사용
        int bossDefense = currentBossData.DEF;
        int bossHealth = Mathf.RoundToInt(currentBossData.HP);

        Initialize(bossLevel, bossAttack, bossDefense, bossHealth);

        // STUN 시스템 초기화
        maxStun = currentBossData.STUN;
        currentStun = maxStun;
        stunRecovery = currentBossData.STUN_RECOVERY;
        stunDefense = currentBossData.STUN_DEF;

        // 공격 배율 설정 (BS_ATT.csv에서 로드)
        normalAttMultiplier = currentBossAttData.NORMAL_ATT;
        normalDefAttMultiplier = currentBossAttData.NORMAL_DEF_ATT;
        longAttMultiplier = currentBossAttData.LONG_ATT;
        longDefAttMultiplier = currentBossAttData.LONG_DEF_ATT;
        specialAttMultiplier = currentBossAttData.SPECIAL_ATT;
        specialDefAttMultiplier = currentBossAttData.SPECIAL_DEF_ATT;

        OnStunChanged?.Invoke(currentStun);
        UpdateStunVisual();

        csvStatus = $"Initialized {monsterName} Phase {currentBossData.PHASE} - HP:{bossHealth}, STUN:{maxStun}";
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
    }

    public void TakeNoteHit(int playerAttack, string noteType, JudgmentResult judgment)
    {
        if (judgment == JudgmentResult.Miss) return;

        // 노트 타입별 데미지 배율 적용
        float damageMultiplier = GetNoteTypeDamageMultiplier(noteType);
        float judgmentMultiplier = GetJudgmentMultiplier(judgment);

        int finalDamage = Mathf.RoundToInt(playerAttack * damageMultiplier * judgmentMultiplier);

        Debug.Log($"[MonsterManager] TakeNoteHit - Phase:{currentPhaseIndex+1}, NoteType:{noteType}, Judgment:{judgment}, PlayerATK:{playerAttack}, Multiplier:{damageMultiplier}x{judgmentMultiplier}, FinalDmg:{finalDamage}, CurrentStun:{currentStun}/{maxStun}, StunDef:{stunDefense}");

        if (currentStun > 0)
        {
            // STUN에 데미지 적용 (STUN_DEF 고려)
            float stunDamage = Mathf.Max(1, finalDamage - stunDefense);
            float oldStun = currentStun;
            currentStun = Mathf.Max(0, currentStun - stunDamage);

            Debug.Log($"[MonsterManager] Stun Damage Applied: {stunDamage} (finalDmg:{finalDamage} - stunDef:{stunDefense}) | Stun: {oldStun} → {currentStun}");

            OnStunChanged?.Invoke(currentStun);

            // Combo System - Add combo when monster takes stun damage
            if (rhythmGameSystem != null)
            {
                rhythmGameSystem.OnMonsterStunDamage();
            }

            if (currentStun <= 0)
            {
                UpdateStunVisual();
                OnStunBroken?.Invoke();
                Debug.Log("[MonsterManager] STUN BROKEN! Invoking OnStunBroken event.");
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

            // Combo System - Add combo when monster takes HP damage
            if (rhythmGameSystem != null)
            {
                rhythmGameSystem.OnMonsterStunDamage();
            }
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
        // During DealingTime, we should always be able to deal damage
        // Remove the stun check since DealingTime means stun is broken


        // Force alive state if needed for DealingTime
        if (isDead && currentHealth > 0)
        {
            isDead = false;
        }

        OnDamage(damage);
    }

    public void ResetStun()
    {
        currentStun = maxStun;
        OnStunChanged?.Invoke(currentStun);
        OnStunRestored?.Invoke(); // New event for immediate UI update
        UpdateStunVisual();

        Debug.Log($"{monsterName}'s stun restored to {currentStun}!");
    }

    protected override void OnDeath()
    {
        // Stop attack animations
        if (attackAnimationCoroutine != null)
        {
            StopCoroutine(attackAnimationCoroutine);
            attackAnimationCoroutine = null;
        }

        if (CanAdvancePhase())
        {
            // Advance to next phase without death animation
            AdvancePhase();
        }
        else
        {
            // No more phases - play death animation
            StartCoroutine(DeathSequence());
        }
    }

    IEnumerator DeathSequence()
    {
        // LMJ: Trigger monster dead event immediately (for UI updates like hearts)
        OnMonsterDead?.Invoke();

        // LMJ: Stop game elements like note generation
        StopGameElements();

        // LMJ: End DealingTime immediately if monster dies during it
        if (gameManager != null)
        {
            if (gameManager.IsDealingTimeActive())
            {
                gameManager.EndDealingTime();
            }

            gameManager.SetGameState(GameState.GameOver);
        }

        // LMJ: Trigger player victory animation when monster dies on final phase
        PlayerManager player = FindFirstObjectByType<PlayerManager>();
        float playerVictoryAnimationLength = 0f;

        if (player != null && player.IsAlive())
        {
            player.PlayVictoryAnimation();

            // Disable dodge system to prevent input during victory
            DodgeSystem dodgeSystem = FindFirstObjectByType<DodgeSystem>();
            if (dodgeSystem != null)
            {
                dodgeSystem.SetDodgeSystemEnabled(false);
            }

            // Get player victory animation length
            if (player.GetComponent<Animator>() != null)
            {
                yield return null; // Wait one frame for animation to start

                AnimatorStateInfo playerStateInfo = player.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
                if (playerStateInfo.IsName("Victory") || playerStateInfo.IsName("Victory_SwordAndShield"))
                {
                    playerVictoryAnimationLength = playerStateInfo.length;
                }
                else
                {
                    playerVictoryAnimationLength = 2.5f; // Fallback duration
                }
            }
            else
            {
                playerVictoryAnimationLength = 2.5f; // Fallback duration
            }
        }

        // LMJ: Trigger death animation
        if (animator != null && hasOnDeathTrigger)
        {
            animator.SetTrigger("OnDeath");

            // LMJ: Wait for one frame to ensure animation has started
            yield return null;

            // LMJ: Wait for animation to complete
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            int maxWaitFrames = 300; // 5초 타임아웃 (60fps 기준)
            int frameCount = 0;

            while (!stateInfo.IsName("Die") && !stateInfo.IsName("Death") && !stateInfo.IsName("Died") &&
                   !stateInfo.IsName("die") && !stateInfo.IsName("death") && !stateInfo.IsName("died"))
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                frameCount++;

                if (frameCount >= maxWaitFrames)
                {
                    break;
                }
            }

            if (frameCount < maxWaitFrames)
            {
                // Wait for the death animation to finish
                yield return new WaitForSeconds(stateInfo.length);
            }
            else
            {
                // Fallback: just wait a short time
                yield return new WaitForSeconds(1f);
            }
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        // LMJ: Wait for player victory animation to complete before showing UI
        if (playerVictoryAnimationLength > 0f)
        {
            yield return new WaitForSeconds(playerVictoryAnimationLength + 0.5f); // Small buffer
        }

        // LMJ: Trigger GameOver through event (will show Victory UI)
        if (OnDied != null)
        {
            OnDied.Invoke();
        }

        // LMJ: Call base death after everything is done
        base.OnDeath();

        // LMJ: Optional - disable after GameOver is shown
        // gameObject.SetActive(false);
    }

    bool CanAdvancePhase()
    {
        return currentPhaseIndex + 1 < totalPhases;
    }

    void AdvancePhase()
    {
        // LMJ: Check if we're currently in DealingTime BEFORE ending it
        bool wasDealingTime = gameManager != null && gameManager.IsDealingTimeActive();

        // LMJ: End DealingTime immediately when advancing to next phase
        if (wasDealingTime)
        {
            gameManager.EndDealingTime();
        }

        currentPhaseIndex++;
        LoadCurrentBossData();

        // Invoke phase changed event
        OnPhaseChanged?.Invoke(currentPhaseIndex + 1);

        // LMJ: Restore all shields when entering new phase (especially phase 2)
        DirectionalShieldSystem shieldSystem = FindFirstObjectByType<DirectionalShieldSystem>();
        if (shieldSystem != null)
        {
            shieldSystem.RestoreAllShields();
        }

        ShieldDurabilitySystem durabilitySystem = FindFirstObjectByType<ShieldDurabilitySystem>();
        if (durabilitySystem != null)
        {
            durabilitySystem.RestoreAllShields();
        }

        // IMPORTANT: Reset dead flag to allow the new phase to take damage
        isDead = false;

        // LMJ: CRITICAL - Clear any existing notes when transitioning phases
        RhythmGameSystem rhythmSystem = FindFirstObjectByType<RhythmGameSystem>();
        if (rhythmSystem != null)
        {
            rhythmSystem.ClearAllNotes();
            Debug.Log($"[MonsterManager] Cleared all notes for phase transition to Phase {currentPhaseIndex + 1}");
        }

        // LMJ: Stop pattern manager before clearing
        RhythmPatternManager patternMgr = FindFirstObjectByType<RhythmPatternManager>();
        if (patternMgr != null)
        {
            patternMgr.StopAllCoroutines();
            Debug.Log("[MonsterManager] Stopped pattern generation for phase transition");
        }

        // LMJ: Reset DodgeSystem swipe block state when entering new phase
        DodgeSystem dodgeSystem = FindFirstObjectByType<DodgeSystem>();
        if (dodgeSystem != null)
        {
            dodgeSystem.ResetSwipeBlock();
        }

        if (csvDataLoaded && currentBossData != null)
        {
            // Save old max values before initializing
            float oldMaxHP = maxHealth;

            InitializeFromCSV();

            // HP is fully restored by Initialize, currentHealth = maxHealth
        }
        else
        {
            // Fallback phase advancement
            Initialize(level + 1, attackPower + 5, defense + 1, maxHealth + 20);
            ResetStun();
        }

        // LMJ: Update StunUIManager to re-subscribe to the new phase's stun events FIRST
        StunUIManager stunUI = FindFirstObjectByType<StunUIManager>();
        if (stunUI != null)
        {
            stunUI.SetMonsterManager(this);
        }

        // If we WERE in DealingTime, reset stun to max for the new phase
        // The phase transition means dealing time is over, so stun should be restored
        if (wasDealingTime)
        {
            // Phase transition from dealing time - restore stun to max
            currentStun = maxStun;
            OnStunChanged?.Invoke(currentStun);
            UpdateStunVisual();

            // Trigger restored event for immediate UI update
            OnStunRestored?.Invoke();

            // Stop dizzy animation for new phase
            if (animator != null && hasIsDealingTimeParam)
            {
                animator.SetBool("isDealingTime", false);
            }
        }
        else
        {
            // Normal phase transition - stun is already at max from InitializeFromCSV
            OnStunChanged?.Invoke(currentStun);
            UpdateStunVisual();
            OnStunRestored?.Invoke();
        }

        // Restart attack animations for new phase
        if (animator != null && gameManager != null && attackAnimationCoroutine == null)
        {
            attackAnimationCoroutine = StartCoroutine(AutoAttackAnimation());
        }

        // LMJ: Ensure notes restart properly after phase transition
        if (gameManager != null && gameManager.GetCurrentState() == GameState.Playing)
        {
            RhythmGameSystem gameRhythmSystem = FindFirstObjectByType<RhythmGameSystem>();
            if (gameRhythmSystem != null && !gameRhythmSystem.enabled)
            {
                gameRhythmSystem.enabled = true;
            }

            RhythmPatternManager patternManager = FindFirstObjectByType<RhythmPatternManager>();
            if (patternManager != null)
            {
                // Small delay to ensure everything is properly initialized
                StartCoroutine(DelayedRestartNotes(patternManager));
            }
        }
    }

    IEnumerator DelayedRestartNotes(RhythmPatternManager patternManager)
    {
        // Wait a small amount to ensure phase transition is complete
        yield return new WaitForSeconds(0.2f);

        if (patternManager != null)
        {
            patternManager.AddNextPatternSetFromCurrentTime();
            patternManager.RestartPatternGeneration();
            Debug.Log($"[MonsterManager] Restarted pattern generation for Phase {currentPhaseIndex + 1}");
        }
    }

    void UpdateStunVisual()
    {
        if (stunObject == null) return;
        stunObject.SetActive(currentStun > 0);
    }

    void StopGameElements()
    {
        // LMJ: Stop rhythm pattern generation
        RhythmPatternManager patternManager = FindFirstObjectByType<RhythmPatternManager>();
        if (patternManager != null)
        {
            patternManager.StopAllCoroutines();
        }

        // LMJ: Stop rhythm game system
        RhythmGameSystem gameSystem = FindFirstObjectByType<RhythmGameSystem>();
        if (gameSystem != null)
        {
            gameSystem.ClearAllNotes();
        }
    }

    IEnumerator AutoAttackAnimation()
    {
        while (true)
        {
            // Check if GameState is Playing
            if (gameManager != null && gameManager.GetCurrentState() == GameState.Playing)
            {
                // Play random attack animation
                PlayRandomAttackAnimation();

                // Wait for random interval before next attack
                float waitTime = Random.Range(minAttackInterval, maxAttackInterval);
                yield return new WaitForSeconds(waitTime);
            }
            else
            {
                // If not playing, check again after a short delay
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    void PlayRandomAttackAnimation()
    {
        if (animator == null || !hasRandomAttParam) return;

        // Set random attack parameter (0, 1, or 2 for Attack01, Attack02, Attack03)
        int randomAttack = Random.Range(0, 3);
        animator.SetInteger("randomAtt", randomAttack);

        // Reset the parameter after a short delay to allow transition back to idle
        StartCoroutine(ResetAttackParameter());
    }

    IEnumerator ResetAttackParameter()
    {
        // Wait for a short time to ensure the animation transition has started
        yield return new WaitForSeconds(0.1f);

        if (animator != null && hasRandomAttParam)
        {
            animator.SetInteger("randomAtt", -1);
        }
    }

    public void StartDizzyAnimation()
    {
        // Set stun to 0 when entering DealingTime (Dizzy state)
        currentStun = 0;
        UpdateStunVisual();

        if (animator != null && hasIsDealingTimeParam)
        {
            animator.SetBool("isDealingTime", true);
        }
    }

    public void StopDizzyAnimation()
    {
        if (animator != null && hasIsDealingTimeParam)
        {
            animator.SetBool("isDealingTime", false);
        }
    }

    public void PlayGetHitAnimation()
    {
        if (animator != null && hasGetHitTrigger)
        {
            // Reset the trigger first to ensure it can be triggered again immediately
            animator.ResetTrigger("GetHit");
            // Set the trigger to play GetHit animation
            animator.SetTrigger("GetHit");
        }
    }

    public void PlayVictoryAnimation()
    {
        if (animator != null && hasIsVictoryParam)
        {
            // Stop any ongoing attack animations
            if (attackAnimationCoroutine != null)
            {
                StopCoroutine(attackAnimationCoroutine);
                attackAnimationCoroutine = null;
            }

            animator.SetBool("isVictory", true);

            // Ensure victory animation keeps looping
            StartCoroutine(EnsureVictoryLoop());
        }
    }

    private System.Collections.IEnumerator EnsureVictoryLoop()
    {
        while (animator != null && hasIsVictoryParam && animator.GetBool("isVictory"))
        {
            // Wait a bit and check if we're still in victory state
            yield return new WaitForSeconds(0.5f);

            // If animator somehow left victory state, force it back
            if (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName("Victory") &&
                !animator.GetCurrentAnimatorStateInfo(0).IsName("Win") &&
                !animator.GetCurrentAnimatorStateInfo(0).IsName("Celebration"))
            {
                // Re-trigger victory if we somehow left the victory state
                if (hasIsVictoryParam && animator.GetBool("isVictory"))
                {
                    animator.SetBool("isVictory", false);
                    yield return null; // Wait one frame
                    animator.SetBool("isVictory", true);
                }
            }
        }
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
    public int GetStunRecovery() => stunRecovery; // LMJ: Get STUN_RECOVERY for DealingTime duration

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
        Debug.Log($"Name: {monsterName}\n" +
                 $"Phase: {currentPhaseIndex + 1}/{totalPhases}\n" +
                 $"Boss ID: {currentBossId}\n" +
                 $"HP: {currentHealth}/{maxHealth}\n" +
                 $"STUN: {currentStun}/{maxStun}\n" +
                 $"ATK: {attackPower}, DEF: {defense}\n" +
                 $"CSV Status: {csvStatus}");
    }
}