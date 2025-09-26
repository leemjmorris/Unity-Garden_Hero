using UnityEngine;
using UnityEngine.Events;

public class PlayerManager : LivingEntity
{
    private static PlayerManager instance;
    public static PlayerManager Instance => instance;

    [Header("🎮 God Mode")]
    [SerializeField] private bool isGodMode = false;

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

    void Awake()
    {
        if (instance == null)
        {
            instance = this;

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

        // LMJ: Initialize LivingEntity with calculated stats
        InitializeFromCSV();
    }

    void InitializeFromCSV()
    {
        // LMJ: Use calculated stats to initialize LivingEntity
        int playerLevel = (strengthLevel + dexterityLevel + constitutionLevel) / 3;
        int playerAttack = dealingDamage; // Primary attack stat
        float playerDefense = totalDefense;
        int playerHealth = totalHP;

        Initialize(playerLevel, playerAttack, Mathf.RoundToInt(playerDefense), playerHealth);

    }

    // LMJ: Override OnDamage to add God Mode protection
    public override void OnDamage(DamageInfo damageInfo)
    {
        Debug.Log($"[PlayerManager] OnDamage(DamageInfo) called - Damage: {damageInfo.baseDamage}, Type: {damageInfo.noteType}, God Mode: {isGodMode}");

        if (isGodMode)
        {
            Debug.Log("[PlayerManager] God Mode - Damage ignored");
            return;
        }

        Debug.Log($"[PlayerManager] Before damage - Current HP: {currentHealth}/{maxHealth}");
        base.OnDamage(damageInfo);
        Debug.Log($"[PlayerManager] After damage - Current HP: {currentHealth}/{maxHealth}");
    }

    public override void OnDamage(int simpleDamage)
    {
        Debug.Log($"[PlayerManager] OnDamage(int) called - Damage: {simpleDamage}, God Mode: {isGodMode}");

        if (isGodMode)
        {
            Debug.Log("[PlayerManager] God Mode - Damage ignored");
            return;
        }

        Debug.Log($"[PlayerManager] Before simple damage - Current HP: {currentHealth}/{maxHealth}");
        base.OnDamage(simpleDamage);
        Debug.Log($"[PlayerManager] After simple damage - Current HP: {currentHealth}/{maxHealth}");
    }


    // LMJ: Override OnDeath for player-specific death handling
    protected override void OnDeath()
    {
        base.OnDeath(); // This will invoke OnDied event
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

        // LMJ: Update LivingEntity stats when CSV stats change
        if (Application.isPlaying)
        {
            UpdateLivingEntityStats();
        }

        UpdateStatSummary();

    }

    void UpdateLivingEntityStats()
    {
        // LMJ: Update the inherited LivingEntity properties
        int newMaxHealth = totalHP;
        float newDefense = totalDefense;
        int newAttack = dealingDamage;

        // LMJ: Preserve health ratio when changing max health
        float healthRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;

        maxHealth = newMaxHealth;
        defense = Mathf.RoundToInt(newDefense);
        attackPower = newAttack;

        // LMJ: Restore health based on ratio
        currentHealth = Mathf.RoundToInt(maxHealth * healthRatio);
        currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth); // Keep alive

        OnHealthChanged?.Invoke(currentHealth); // Notify health change
    }

    private void UpdateStatSummary()
    {
        string livingEntityInfo = $"LivingEntity - HP:{currentHealth}/{maxHealth}, ATK:{attackPower}, DEF:{defense}";

        statSummary = $"Total Levels: {strengthLevel + dexterityLevel + constitutionLevel}\n" +
                     $"Combat Power: ATT={dealingDamage} | STUN={stunDamage}\n" +
                     $"Survival: HP={totalHP} | DEF={totalDefense:F2}\n" +
                     $"Distribution: STR({strengthLevel}) DEX({dexterityLevel}) CON({constitutionLevel})\n" +
                     $"God Mode: {(isGodMode ? "ON" : "OFF")}\n" +
                     $"{livingEntityInfo}";
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
            }
        }
        else
        {
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
    }

    // LMJ: Public damage method for external systems
    public void TakeDamage(int damage)
    {
        Debug.Log($"[PlayerManager] TakeDamage wrapper called - Damage: {damage}");
        OnDamage(damage);
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
    public new int GetAttackPower() => dealingDamage; // Main attack for dealing time and rhythm notes
    public int GetStunAttackPower() => stunDamage; // Stun damage for rhythm game
    public int GetDealingAttackPower() => dealingDamage; // Compatibility method

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
        UpdateStatSummary();
    }

    [ContextMenu("💀 Test Damage")]
    public void TestDamage()
    {
        OnDamage(10);
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
        if (Input.GetKeyDown(KeyCode.H)) OnDamage(10); // Test damage with LivingEntity
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log($"God Mode Inspector: {isGodMode}");
            UpdateStatSummary();
        }
    }
}