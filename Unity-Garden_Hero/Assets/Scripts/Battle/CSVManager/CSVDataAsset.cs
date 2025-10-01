using System.Collections.Generic;
using UnityEngine;
using System.IO;

[CreateAssetMenu(fileName = "CSVDataAsset", menuName = "CSV Data/CSV Data Asset")]
public class CSVDataAsset : ScriptableObject
{
    [Header("Boss Data")]
    public List<BossData> bossDataList = new List<BossData>();

    [Header("Boss Attack Data")]
    public List<BossAttData> bossAttDataList = new List<BossAttData>();

    [Header("Consumable Data")]
    public List<ConsumableData> consumableDataList = new List<ConsumableData>();

    [Header("Gear Data")]
    public List<GearData> gearDataList = new List<GearData>();

    [Header("Gear Set Data")]
    public List<GearSetData> gearSetDataList = new List<GearSetData>();

    [Header("Gear Effects Data")]
    public List<GearEffectData> gearEffectDataList = new List<GearEffectData>();

    [Header("Link Data")]
    public List<LinkData> linkDataList = new List<LinkData>();

    [Header("Phase Data")]
    public List<PhaseData> phaseDataList = new List<PhaseData>();

    [Header("Pattern Data")]
    public List<PatternData> patternDataList = new List<PatternData>();

    [Header("Stage Data")]
    public List<StageData> stageDataList = new List<StageData>();

    [Header("Stat Data")]
    public List<StatData> statDataList = new List<StatData>();

    // LMJ: Dictionary for fast lookup (built at runtime)
    private Dictionary<string, BossData> bossDataDict;
    private Dictionary<int, BossAttData> bossAttDataDict;
    private Dictionary<string, ConsumableData> consumableDataDict;
    private Dictionary<string, GearData> gearDataDict;
    private Dictionary<string, GearSetData> gearSetDataDict;
    private Dictionary<string, GearEffectData> gearEffectDataDict;
    private Dictionary<string, LinkData> linkDataDict;
    private Dictionary<string, PhaseData> phaseDataDict;
    private Dictionary<string, PatternData> patternDataDict;
    private Dictionary<string, StageData> stageDataDict;
    private Dictionary<string, StatData> statDataDict;

    void OnEnable()
    {
        BuildDictionaries();
    }

    public void BuildDictionaries()
    {
        bossDataDict = new Dictionary<string, BossData>();
        foreach (var data in bossDataList)
        {
            bossDataDict[data.BOSS_ID.ToString()] = data;
        }

        bossAttDataDict = new Dictionary<int, BossAttData>();
        foreach (var data in bossAttDataList)
        {
            bossAttDataDict[data.BOSS_ATT_ID] = data;
        }

        consumableDataDict = new Dictionary<string, ConsumableData>();
        foreach (var data in consumableDataList)
        {
            consumableDataDict[data.ITME_ID.ToString()] = data;
        }

        gearDataDict = new Dictionary<string, GearData>();
        foreach (var data in gearDataList)
        {
            gearDataDict[data.GEAR_ID.ToString()] = data;
        }

        gearSetDataDict = new Dictionary<string, GearSetData>();
        foreach (var data in gearSetDataList)
        {
            gearSetDataDict[data.SET_ID.ToString()] = data;
        }

        gearEffectDataDict = new Dictionary<string, GearEffectData>();
        foreach (var data in gearEffectDataList)
        {
            gearEffectDataDict[data.GR_EFF_ID.ToString()] = data;
        }

        linkDataDict = new Dictionary<string, LinkData>();
        foreach (var data in linkDataList)
        {
            linkDataDict[data.GROUP] = data;
        }

        phaseDataDict = new Dictionary<string, PhaseData>();
        foreach (var data in phaseDataList)
        {
            phaseDataDict[data.PHASE_ID.ToString()] = data;
        }

        patternDataDict = new Dictionary<string, PatternData>();
        foreach (var data in patternDataList)
        {
            patternDataDict[data.PT_ID.ToString()] = data;
        }

        stageDataDict = new Dictionary<string, StageData>();
        foreach (var data in stageDataList)
        {
            stageDataDict[data.STAGE_ID.ToString()] = data;
        }

        statDataDict = new Dictionary<string, StatData>();
        foreach (var data in statDataList)
        {
            string key = $"{data.STAT_ID}_{data.STAT_LV}";
            statDataDict[key] = data;
        }

    }

    // LMJ: Public getter methods
    public BossData GetBossData(string bossId)
    {
        if (bossDataDict == null) BuildDictionaries();
        return bossDataDict.ContainsKey(bossId) ? bossDataDict[bossId] : null;
    }

    public BossAttData GetBossAttData(int bossAttId)
    {
        if (bossAttDataDict == null) BuildDictionaries();
        return bossAttDataDict.ContainsKey(bossAttId) ? bossAttDataDict[bossAttId] : null;
    }

    public ConsumableData GetConsumableData(string consId)
    {
        if (consumableDataDict == null) BuildDictionaries();
        return consumableDataDict.ContainsKey(consId) ? consumableDataDict[consId] : null;
    }

    public GearData GetGearData(string gearId)
    {
        if (gearDataDict == null) BuildDictionaries();
        return gearDataDict.ContainsKey(gearId) ? gearDataDict[gearId] : null;
    }

    public GearSetData GetGearSetData(string setId)
    {
        if (gearSetDataDict == null) BuildDictionaries();
        return gearSetDataDict.ContainsKey(setId) ? gearSetDataDict[setId] : null;
    }

    public GearEffectData GetGearEffectData(string effectId)
    {
        if (gearEffectDataDict == null) BuildDictionaries();
        return gearEffectDataDict.ContainsKey(effectId) ? gearEffectDataDict[effectId] : null;
    }

    public LinkData GetLinkData(string group)
    {
        if (linkDataDict == null) BuildDictionaries();
        return linkDataDict.ContainsKey(group) ? linkDataDict[group] : null;
    }

    public PhaseData GetPhaseData(string phaseId)
    {
        if (phaseDataDict == null) BuildDictionaries();
        return phaseDataDict.ContainsKey(phaseId) ? phaseDataDict[phaseId] : null;
    }

    public PatternData GetPatternData(string patternId)
    {
        if (patternDataDict == null) BuildDictionaries();
        return patternDataDict.ContainsKey(patternId) ? patternDataDict[patternId] : null;
    }

    public StageData GetStageData(string stageId)
    {
        if (stageDataDict == null) BuildDictionaries();
        return stageDataDict.ContainsKey(stageId) ? stageDataDict[stageId] : null;
    }

    public StatData GetStatData(string statId, string level)
    {
        if (statDataDict == null) BuildDictionaries();
        string key = $"{statId}_{level}";
        return statDataDict.ContainsKey(key) ? statDataDict[key] : null;
    }

    public StatData GetStatData(string statId, int level)
    {
        return GetStatData(statId, level.ToString());
    }

#if UNITY_EDITOR
    [ContextMenu("Load All CSV Data")]
    public void LoadAllCSVData()
    {
        LoadBossData();
        LoadBossAttData();
        LoadConsumableData();
        LoadGearData();
        LoadGearSetData();
        LoadGearEffectData();
        LoadLinkData();
        LoadPhaseData();
        LoadPatternData();
        LoadStageData();
        LoadStatData();

        BuildDictionaries();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    void LoadBossData()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CSV/BOSS.csv");
        if (!File.Exists(path)) return;

        bossDataList.Clear();
        string[] lines = File.ReadAllLines(path);

        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length >= 11)
                {
                    BossData data = new BossData
                    {
                        BOSS_ID = CSVHelper.ToInt(values[0]),
                        BOSS_NAME = values[1].Trim(),
                        PHASE = CSVHelper.ToInt(values[2]),
                        STUN = CSVHelper.ToFloat(values[3]),
                        STUN_RECOVERY = CSVHelper.ToInt(values[4]),
                        DEF = CSVHelper.ToInt(values[5]),
                        STUN_DEF = CSVHelper.ToInt(values[6]),
                        HP = CSVHelper.ToFloat(values[7]),
                        PT_ID = CSVHelper.ToInt(values[8]),
                        BOSS_PREFABS = values[9].Trim(),
                        BOSS_ATT_ID = CSVHelper.ToInt(values[10])
                    };
                    bossDataList.Add(data);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    void LoadBossAttData()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CSV/BS_ATT.csv");
        if (!File.Exists(path)) return;

        bossAttDataList.Clear();
        string[] lines = File.ReadAllLines(path);

        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length >= 12)
                {
                    BossAttData data = new BossAttData
                    {
                        BOSS_ATT_ID = CSVHelper.ToInt(values[0]),
                        BOSS_NAME = values[1].Trim(),
                        PHASE = CSVHelper.ToInt(values[2]),
                        NORMAL_ATT = CSVHelper.ToInt(values[3]),
                        NORMAL_DEF_ATT = CSVHelper.ToInt(values[4]),
                        LONG_ATT = CSVHelper.ToInt(values[5]),
                        LONG_DEF_ATT = CSVHelper.ToInt(values[11]), // 마지막 LONG_DEF_ATT 사용
                        SPECIAL_ATT = CSVHelper.ToInt(values[10]),
                        SPECIAL_DEF_ATT = CSVHelper.ToInt(values[11])
                    };
                    bossAttDataList.Add(data);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    void LoadConsumableData()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CSV/CONS.csv");
        if (!File.Exists(path)) return;

        consumableDataList.Clear();
        string[] lines = File.ReadAllLines(path);

        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length >= 13)
                {
                    ConsumableData data = new ConsumableData
                    {
                        ITME_ID = CSVHelper.ToInt(values[0]),
                        ITEM_NAME = values[1].Trim(),
                        ITEM_RARE = CSVHelper.ToInt(values[2]),
                        ITEM_TYPE = CSVHelper.ToInt(values[3]),
                        OPTION_TYPE = CSVHelper.ToInt(values[4]),
                        EFF_INT_VALUE = CSVHelper.ToInt(values[5]),
                        EFF_FLOAT_VALUE = CSVHelper.ToFloat(values[6]),
                        BT_MAX_STACK = CSVHelper.ToInt(values[7]),
                        MAX_STACK = CSVHelper.ToInt(values[8]),
                        DURATION = CSVHelper.ToInt(values[9]),
                        COOLTIME = CSVHelper.ToFloat(values[10]),
                        MAX_DROP_VALUE = CSVHelper.ToInt(values[11]),
                        ITEM_IMAGE = values[12].Trim()
                    };
                    consumableDataList.Add(data);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    void LoadGearSetData()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CSV/GEAR_SET.csv");
        if (!File.Exists(path)) return;

        gearSetDataList.Clear();
        string[] lines = File.ReadAllLines(path);

        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length >= 9)
                {
                    GearSetData data = new GearSetData
                    {
                        SET_ID = CSVHelper.ToInt(values[0]),
                        SET_EFF = values[1].Trim(),
                        SET_EFF_3 = CSVHelper.ToFloat(values[2]),
                        SET_EFF_5 = CSVHelper.ToFloat(values[3]),
                        GEAR_ID_1 = CSVHelper.ToInt(values[4]),
                        GEAR_ID_2 = CSVHelper.ToInt(values[5]),
                        GEAR_ID_3 = CSVHelper.ToInt(values[6]),
                        GEAR_ID_4 = CSVHelper.ToInt(values[7]),
                        GEAR_ID_5 = CSVHelper.ToInt(values[8])
                    };
                    gearSetDataList.Add(data);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    void LoadPhaseData()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CSV/PHASE.csv");
        if (!File.Exists(path)) return;

        phaseDataList.Clear();
        string[] lines = File.ReadAllLines(path);

        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length >= 6)
                {
                    PhaseData data = new PhaseData
                    {
                        PHASE_ID = CSVHelper.ToInt(values[0]),
                        BOSS_ID_1 = CSVHelper.ToInt(values[1]),
                        BOSS_ID_2 = CSVHelper.ToInt(values[2]),
                        BOSS_ID_3 = CSVHelper.ToInt(values[3]),
                        BOSS_ID_4 = CSVHelper.ToInt(values[4]),
                        BOSS_ID_5 = CSVHelper.ToInt(values[5])
                    };
                    phaseDataList.Add(data);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    void LoadGearData()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CSV/GEAR.csv");
        if (!File.Exists(path)) return;

        gearDataList.Clear();
        string[] lines = File.ReadAllLines(path);

        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length >= 6)
                {
                    GearData data = new GearData
                    {
                        GEAR_ID = CSVHelper.ToInt(values[0]),
                        GEAR_NAME = values[1].Trim(),
                        GEAR_RARE = CSVHelper.ToInt(values[2]),
                        GEAR_TYPE = CSVHelper.ToInt(values[3]),
                        GR_EFF_ID = CSVHelper.ToInt(values[4]),
                        ITEM_IMAGE = values[5].Trim()
                    };
                    gearDataList.Add(data);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    void LoadGearEffectData()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CSV/GR_EFF.csv");
        if (!File.Exists(path)) return;

        gearEffectDataList.Clear();
        string[] lines = File.ReadAllLines(path);

        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length >= 7)
                {
                    GearEffectData data = new GearEffectData
                    {
                        GR_EFF_ID = CSVHelper.ToInt(values[0]),
                        EFF_NAME = values[1].Trim(),
                        ATT_EFF = CSVHelper.ToInt(values[2]),
                        DEF_ATT_EFF = CSVHelper.ToInt(values[3]),
                        DEE_EFF = CSVHelper.ToFloat(values[4]),
                        HP_EFF = CSVHelper.ToInt(values[5]),
                        DURABILITY = CSVHelper.ToInt(values[6])
                    };
                    gearEffectDataList.Add(data);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    void LoadLinkData()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CSV/LINK.csv");
        if (!File.Exists(path)) return;

        linkDataList.Clear();
        string[] lines = File.ReadAllLines(path);

        for (int i = 2; i < lines.Length; i++) // LMJ: Skip header and type rows
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length >= 2)
                {
                    LinkData data = new LinkData
                    {
                        GROUP = values[0].Trim(),
                        LINK = values[1].Trim()
                    };
                    linkDataList.Add(data);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    void LoadPatternData()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CSV/PT.csv");
        if (!File.Exists(path)) return;

        patternDataList.Clear();
        string[] lines = File.ReadAllLines(path);

        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length >= 11)
                {
                    PatternData data = new PatternData
                    {
                        PT_ID = CSVHelper.ToInt(values[0]),
                        PT_1 = values[1].Trim(),
                        PT_2 = values[2].Trim(),
                        PT_3 = values[3].Trim(),
                        PT_4 = values[4].Trim(),
                        PT_5 = values[5].Trim(),
                        PT_6 = values[6].Trim(),
                        PT_7 = values[7].Trim(),
                        PT_8 = values[8].Trim(),
                        PT_9 = values[9].Trim(),
                        PT_10 = values[10].Trim()
                    };
                    patternDataList.Add(data);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    void LoadStageData()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CSV/STAGE.csv");
        if (!File.Exists(path)) return;

        stageDataList.Clear();
        string[] lines = File.ReadAllLines(path);

        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length >= 2)
                {
                    StageData data = new StageData
                    {
                        STAGE_ID = CSVHelper.ToInt(values[0]),
                        PHASE_ID = CSVHelper.ToInt(values[1])
                    };
                    stageDataList.Add(data);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    void LoadStatData()
    {
        string path = Path.Combine(Application.dataPath, "Resources/CSV/STAT.csv");
        if (!File.Exists(path)) return;

        statDataList.Clear();
        string[] lines = File.ReadAllLines(path);

        for (int i = 2; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                string[] values = SplitCSVLine(line);
                if (values.Length >= 7)
                {
                    StatData data = new StatData
                    {
                        STAT_ID = CSVHelper.ToInt(values[0]),
                        STAT_LV = CSVHelper.ToInt(values[1]),
                        STAT = CSVHelper.ToInt(values[2]),
                        ATT = CSVHelper.ToInt(values[3]),
                        DEF_ATT = CSVHelper.ToInt(values[4]),
                        HP = CSVHelper.ToInt(values[5]),
                        DEF = CSVHelper.ToFloat(values[6])
                    };
                    statDataList.Add(data);
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    string[] SplitCSVLine(string line)
    {
        // LMJ: Handle quoted CSV fields and multiple separators
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if ((c == ',' || c == ';') && !inQuotes)
            {
                result.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField.Trim());
        return result.ToArray();
    }
#endif
}