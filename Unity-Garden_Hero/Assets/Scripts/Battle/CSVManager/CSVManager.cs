using UnityEngine;

public class CSVManager : MonoBehaviour
{
    private static CSVManager instance;
    public static CSVManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CSVManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CSVManager");
                    instance = go.AddComponent<CSVManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("CSV Data Asset")]
    [SerializeField] private CSVDataAsset csvDataAsset;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCSVData();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void InitializeCSVData()
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned to CSVManager!");
            return;
        }

        csvDataAsset.BuildDictionaries();
        Debug.Log("CSV data initialized successfully!");
    }

    // LMJ: Wrapper methods for easy access
    public BossData GetBossData(string bossId)
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return null;
        }
        return csvDataAsset.GetBossData(bossId);
    }

    public ConsumableData GetConsumableData(string consId)
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return null;
        }
        return csvDataAsset.GetConsumableData(consId);
    }

    public GearData GetGearData(string gearId)
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return null;
        }
        return csvDataAsset.GetGearData(gearId);
    }

    public GearSetData GetGearSetData(string setId)
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return null;
        }
        return csvDataAsset.GetGearSetData(setId);
    }

    public GearEffectData GetGearEffectData(string effectId)
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return null;
        }
        return csvDataAsset.GetGearEffectData(effectId);
    }

    public LinkData GetLinkData(string group)
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return null;
        }
        return csvDataAsset.GetLinkData(group);
    }

    public PhaseData GetPhaseData(string phaseId)
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return null;
        }
        return csvDataAsset.GetPhaseData(phaseId);
    }

    public PatternData GetPatternData(string patternId)
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return null;
        }
        return csvDataAsset.GetPatternData(patternId);
    }

    public StageData GetStageData(string stageId)
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return null;
        }
        return csvDataAsset.GetStageData(stageId);
    }

    public StatData GetStatData(string statId, int level)
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return null;
        }
        return csvDataAsset.GetStatData(statId, level);
    }

    public StatData GetStatData(string statId, string level)
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return null;
        }
        return csvDataAsset.GetStatData(statId, level);
    }

    // LMJ: Helper methods for converting string values to int
    public int GetIntValue(string value, int defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        if (int.TryParse(value, out int result)) return result;
        return defaultValue;
    }

    public float GetFloatValue(string value, float defaultValue = 0f)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        if (float.TryParse(value, out float result)) return result;
        return defaultValue;
    }

    // LMJ: Check if CSV data is loaded
    public bool IsCSVDataLoaded()
    {
        return csvDataAsset != null;
    }

    // LMJ: Public getter for CSVDataAsset (for CSVHelper)
    public CSVDataAsset GetCSVDataAsset()
    {
        return csvDataAsset;
    }

    // LMJ: Get data counts for debugging
    public void LogDataCounts()
    {
        if (csvDataAsset == null)
        {
            Debug.LogError("CSVDataAsset is not assigned!");
            return;
        }

        Debug.Log($"=== CSV Data Counts ===");
        Debug.Log($"Boss Data: {csvDataAsset.bossDataList.Count}");
        Debug.Log($"Consumable Data: {csvDataAsset.consumableDataList.Count}");
        Debug.Log($"Gear Data: {csvDataAsset.gearDataList.Count}");
        Debug.Log($"Gear Set Data: {csvDataAsset.gearSetDataList.Count}");
        Debug.Log($"Gear Effect Data: {csvDataAsset.gearEffectDataList.Count}");
        Debug.Log($"Link Data: {csvDataAsset.linkDataList.Count}");
        Debug.Log($"Phase Data: {csvDataAsset.phaseDataList.Count}");
        Debug.Log($"Pattern Data: {csvDataAsset.patternDataList.Count}");
        Debug.Log($"Stage Data: {csvDataAsset.stageDataList.Count}");
        Debug.Log($"Stat Data: {csvDataAsset.statDataList.Count}");
    }
}