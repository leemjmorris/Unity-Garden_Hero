using UnityEngine;
using UnityEngine.Events;

public class PlayerManager : MonoBehaviour
{
    private static PlayerManager instance;
    public static PlayerManager Instance => instance;

    [Header("🎮 God Mode")]
    [SerializeField] private bool isGodMode = false;
    public UnityEvent OnDied;

    [Header("🔧 Development Mode")]
    [SerializeField] private bool enableInspectorTesting = true;
    [Space(5)]

    [Header("🎯 Player Stat Levels - ADJUSTABLE")]
    [SerializeField, Range(1, 20)] private int strengthLevel = 1;
    [SerializeField, Range(1, 20)] private int dexterityLevel = 1; 
    [SerializeField, Range(1, 20)] private int constitutionLevel = 1;

    [Header("⚔️ Final Combat Stats (Auto Calculated)")]
    [SerializeField] private int dealingDamage;    // DealingTime 공격력
    [SerializeField] private int stunDamage;       // 리듬노트 STUN 데미지  

    [Header("💚 Final Survival Stats (Auto Calculated)")]
    [SerializeField] private int totalHP;          // 체력
    [SerializeField] private float totalDefense;   // 방어력
    [SerializeField] private int currentHealth;    // 현재 체력

    [Header("📊 Stat Breakdown Detail")]
    [Space(10)]
    [SerializeField] private StatBreakdown strengthStats;
    [SerializeField] private StatBreakdown dexterityStats;
    [SerializeField] private StatBreakdown constitutionStats;

    [Header("📈 Stat Summary")]
    [SerializeField] private string statSummary;

    // LMJ: Previous values to detect changes
    private int prevStrengthLevel;
    private int prevDexterityLevel;
    private int prevConstitutionLevel;

    [System.Serializable]
    public class StatBreakdown
    {
        [SerializeField] public string statName;
        [SerializeField] public int level;
        [SerializeField] public int att;
        [SerializeField] public int defAtt;
        [SerializeField] public int hp;
        [SerializeField] public float def;
        [SerializeField] public string contribution;
        
        public StatBreakdown(string name)
        {
            statName = name;
        }
        
        public void UpdateStats(int lv, StatData data)
        {
            level = lv;
            if (data != null)
            {
                att = data.ATT;
                defAtt = data.DEF_ATT;
                hp = data.HP;
                def = data.DEF;
                contribution = $"ATT:{att} | DEF_ATT:{defAtt} | HP:{hp} | DEF:{def:F2}";
            }
            else
            {
                att = defAtt = hp = 0;
                def = 0f;
                contribution = "No Data Found";
            }
        }
    }

    // LMJ: God Mode property
    public bool IsGodMode
    {
        get => isGodMode;
        set => isGodMode = value;
    }

    // LMJ: Health management
    public int CurrentHealth
    {
        get => currentHealth;
        set
        {
            if (isGodMode && value < currentHealth) return; // God mode prevents damage
            
            int previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(value, 0, totalHP);
            
            if (currentHealth <= 0 && previousHealth > 0)
            {
                OnDied?.Invoke();
            }
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
            
            // LMJ: Initialize breakdown display
            strengthStats = new StatBreakdown("💪 Strength");
            dexterityStats = new StatBreakdown("⚡ Dexterity");
            constitutionStats = new StatBreakdown("🛡️ Constitution");
            
            // LMJ: Store initial values
            prevStrengthLevel = strengthLevel;
            prevDexterityLevel = dexterityLevel;
            prevConstitutionLevel = constitutionLevel;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CalculateStats();
        UpdateRangeValues();
    }

    // LMJ: Called when inspector values change
    void OnValidate()
    {
        if (enableInspectorTesting && Application.isPlaying)
        {
            if (prevStrengthLevel != strengthLevel || 
                prevDexterityLevel != dexterityLevel || 
                prevConstitutionLevel != constitutionLevel)
            {
                prevStrengthLevel = strengthLevel;
                prevDexterityLevel = dexterityLevel;
                prevConstitutionLevel = constitutionLevel;
                
                CalculateStats();
                Debug.Log($"[PlayerManager] Inspector values changed - Recalculating stats");
            }
        }
        
        UpdateRangeValues();
    }

    private void UpdateRangeValues()
    {
        if (!Application.isPlaying) return;
        
        int maxStr = GetMaxLevelForStatType(StatType.Strength);
        int maxDex = GetMaxLevelForStatType(StatType.Dexterity);
        int maxCon = GetMaxLevelForStatType(StatType.Constitution);
        
        strengthLevel = Mathf.Clamp(strengthLevel, 1, maxStr);
        dexterityLevel = Mathf.Clamp(dexterityLevel, 1, maxDex);
        constitutionLevel = Mathf.Clamp(constitutionLevel, 1, maxCon);
    }

    public void CalculateStats()
    {
        dealingDamage = 0;
        stunDamage = 0; 
        totalHP = 0;
        totalDefense = 0f;

        AddStatsFromLevel(StatType.Strength, strengthLevel);
        AddStatsFromLevel(StatType.Dexterity, dexterityLevel);
        AddStatsFromLevel(StatType.Constitution, constitutionLevel);

        // LMJ: Initialize current health if it's 0
        if (currentHealth == 0)
        {
            currentHealth = totalHP;
        }

        UpdateStatSummary();

        Debug.Log($"[PlayerManager] Stats Updated - ATT: {dealingDamage}, DEF_ATT: {stunDamage}, HP: {totalHP}, DEF: {totalDefense}");
    }

    private void UpdateStatSummary()
    {
        statSummary = $"Total Levels: {strengthLevel + dexterityLevel + constitutionLevel}\n" +
                     $"Combat Power: ATT={dealingDamage} | STUN={stunDamage}\n" +
                     $"Survival: HP={totalHP} | DEF={totalDefense:F2} | Current HP={currentHealth}\n" +
                     $"Distribution: STR({strengthLevel}) DEX({dexterityLevel}) CON({constitutionLevel})\n" +
                     $"God Mode: {(isGodMode ? "ON" : "OFF")}";
    }

    private void AddStatsFromLevel(StatType statType, int level)
    {
        StatData statData = GetStatData(statType, level);
        
        if (statData != null)
        {
            dealingDamage += statData.ATT;
            stunDamage += statData.DEF_ATT;
            totalHP += statData.HP;
            totalDefense += statData.DEF;
            
            if (enableInspectorTesting)
            {
                Debug.Log($"[PlayerManager] {statType} Lv.{level}: ATT+{statData.ATT}, DEF_ATT+{statData.DEF_ATT}, HP+{statData.HP}, DEF+{statData.DEF}");
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerManager] StatData not found for {statType} level {level}");
        }

        UpdateStatBreakdown(statType, level, statData);
    }

    private void UpdateStatBreakdown(StatType statType, int level, StatData statData)
    {
        switch (statType)
        {
            case StatType.Strength:
                strengthStats.UpdateStats(level, statData);
                break;
            case StatType.Dexterity:
                dexterityStats.UpdateStats(level, statData);
                break;
            case StatType.Constitution:
                constitutionStats.UpdateStats(level, statData);
                break;
        }
    }

    private StatData GetStatData(StatType statType, int level)
    {
        if (CSVManager.Instance == null)
        {
            Debug.LogError("[PlayerManager] CSVManager.Instance is null!");
            return null;
        }

        var statList = CSVManager.Instance.GetCSVDataAsset().statDataList;
        
        foreach (var stat in statList)
        {
            if (stat.STAT == (int)statType && stat.STAT_LV == level)
            {
                return stat;
            }
        }

        return null;
    }

    // LMJ: Compatibility method for RhythmGameSystem
    public void ProcessNoteResult(string noteType, bool isSuccess)
    {
        Debug.Log($"[PlayerManager] Note {noteType} result: {(isSuccess ? "Success" : "Fail")}");
        // 필요시 추가 로직 구현
    }

    // LMJ: Damage handling
    public void TakeDamage(int damage)
    {
        //if (isGodMode) return;
        
        CurrentHealth -= damage;
        Debug.Log($"[PlayerManager] Took {damage} damage. Health: {currentHealth}/{totalHP}");
    }

    // LMJ: Stat level management
    public void LevelUpStat(StatType statType)
    {
        switch (statType)
        {
            case StatType.Strength:
                strengthLevel++;
                break;
            case StatType.Dexterity:
                dexterityLevel++;
                break;
            case StatType.Constitution:
                constitutionLevel++;
                break;
        }
        
        CalculateStats();
    }

    // LMJ: Public getters for other systems
    public int GetAttackPower() => stunDamage;
    public int GetDealingAttackPower() => dealingDamage;
    
    public int DealingDamage => dealingDamage;
    public int StunDamage => stunDamage;
    public int TotalHP => totalHP;
    public float TotalDefense => totalDefense;

    public int StrengthLevel => strengthLevel;
    public int DexterityLevel => dexterityLevel;
    public int ConstitutionLevel => constitutionLevel;

    public int GetMaxLevelForStatType(StatType statType)
    {
        if (CSVManager.Instance == null) return 20;
        
        var statList = CSVManager.Instance.GetCSVDataAsset().statDataList;
        int maxLevel = 1;
        
        foreach (var stat in statList)
        {
            if (stat.STAT == (int)statType && stat.STAT_LV > maxLevel)
            {
                maxLevel = stat.STAT_LV;
            }
        }
        
        return maxLevel;
    }

    // LMJ: Context Menu methods
    [ContextMenu("🔄 Recalculate Stats")]
    public void RecalculateStats() => CalculateStats();

    [ContextMenu("🎲 Random Stats")]
    public void RandomizeStatsForTesting()
    {
        if (!enableInspectorTesting) return;
        
        strengthLevel = Random.Range(1, GetMaxLevelForStatType(StatType.Strength) + 1);
        dexterityLevel = Random.Range(1, GetMaxLevelForStatType(StatType.Dexterity) + 1);
        constitutionLevel = Random.Range(1, GetMaxLevelForStatType(StatType.Constitution) + 1);
        
        CalculateStats();
    }

    [ContextMenu("🔧 Reset to Level 1")]
    public void ResetToLevel1()
    {
        strengthLevel = dexterityLevel = constitutionLevel = 1;
        CalculateStats();
    }

    [ContextMenu("⚡ Toggle God Mode")]
    public void ToggleGodMode()
    {
        isGodMode = !isGodMode;
        Debug.Log($"[PlayerManager] God Mode: {(isGodMode ? "ON" : "OFF")}");
        UpdateStatSummary();
    }

    // LMJ: Testing keys
    void Update()
    {
        if (!enableInspectorTesting) return;
        
        if (Input.GetKeyDown(KeyCode.Alpha1)) LevelUpStat(StatType.Strength);
        if (Input.GetKeyDown(KeyCode.Alpha2)) LevelUpStat(StatType.Dexterity);
        if (Input.GetKeyDown(KeyCode.Alpha3)) LevelUpStat(StatType.Constitution);
        if (Input.GetKeyDown(KeyCode.G)) ToggleGodMode();
        if (Input.GetKeyDown(KeyCode.R)) RandomizeStatsForTesting();
        if (Input.GetKeyDown(KeyCode.Backspace)) ResetToLevel1();
        if (Input.GetKeyDown(KeyCode.H)) TakeDamage(10); // Test damage
    }
}