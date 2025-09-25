using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class CSVHelper
{
    public static int ToInt(string value, int defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        if (int.TryParse(value, out int result)) return result;
        return defaultValue;
    }

    public static float ToFloat(string value, float defaultValue = 0f)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        if (float.TryParse(value, out float result)) return result;
        return defaultValue;
    }

    public static bool ToBool(string value, bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        if (bool.TryParse(value, out bool result)) return result;

        string lower = value.ToLower();
        if (lower == "1" || lower == "true" || lower == "yes" || lower == "on") return true;
        if (lower == "0" || lower == "false" || lower == "no" || lower == "off") return false;

        return defaultValue;
    }

    // LMJ: Updated gear helper methods - no more string conversion needed
    public static int GetGearAttackBonus(string gearId)
    {
        if (CSVManager.Instance == null) return 0;

        GearData gear = CSVManager.Instance.GetGearData(gearId);
        if (gear == null) return 0;

        GearEffectData effect = CSVManager.Instance.GetGearEffectData(gear.GR_EFF_ID.ToString());
        if (effect == null) return 0;

        return effect.ATT_EFF; // LMJ: Direct int access, no conversion needed
    }

    public static int GetGearDefenseBonus(string gearId)
    {
        if (CSVManager.Instance == null) return 0;

        GearData gear = CSVManager.Instance.GetGearData(gearId);
        if (gear == null) return 0;

        GearEffectData effect = CSVManager.Instance.GetGearEffectData(gear.GR_EFF_ID.ToString());
        if (effect == null) return 0;

        return effect.DEF_ATT_EFF; // LMJ: Direct int access
    }

    public static int GetGearHPBonus(string gearId)
    {
        if (CSVManager.Instance == null) return 0;

        GearData gear = CSVManager.Instance.GetGearData(gearId);
        if (gear == null) return 0;

        GearEffectData effect = CSVManager.Instance.GetGearEffectData(gear.GR_EFF_ID.ToString());
        if (effect == null) return 0;

        return effect.HP_EFF; // LMJ: Direct int access
    }

    // LMJ: Updated stat helper methods
    public static int GetPlayerAttack(string statId, int level)
    {
        if (CSVManager.Instance == null) return 10;

        StatData stat = CSVManager.Instance.GetStatData(statId, level);
        if (stat == null) return 10;

        return stat.ATT; // LMJ: Direct int access
    }

    public static int GetPlayerDefense(string statId, int level)
    {
        if (CSVManager.Instance == null) return 1;

        StatData stat = CSVManager.Instance.GetStatData(statId, level);
        if (stat == null) return 1;

        return (int)stat.DEF; // LMJ: Cast float to int
    }

    public static int GetPlayerHP(string statId, int level)
    {
        if (CSVManager.Instance == null) return 100;

        StatData stat = CSVManager.Instance.GetStatData(statId, level);
        if (stat == null) return 100;

        return stat.HP; // LMJ: Direct int access
    }

    // LMJ: Updated pattern helper methods
    public static string GetPatternValue(string patternId, int index)
    {
        if (CSVManager.Instance == null) return "NULL";

        PatternData pattern = CSVManager.Instance.GetPatternData(patternId);
        if (pattern == null) return "NULL";

        switch (index)
        {
            case 0: return pattern.PT_1;
            case 1: return pattern.PT_2;
            case 2: return pattern.PT_3;
            case 3: return pattern.PT_4;
            case 4: return pattern.PT_5;
            case 5: return pattern.PT_6;
            case 6: return pattern.PT_7;
            case 7: return pattern.PT_8;
            case 8: return pattern.PT_9;
            case 9: return pattern.PT_10;
            default: return "NULL";
        }
    }

    public static string[] GetAllPatternValues(string patternId)
    {
        if (CSVManager.Instance == null) return new string[10];

        PatternData pattern = CSVManager.Instance.GetPatternData(patternId);
        if (pattern == null) return new string[10];

        return new string[]
        {
            pattern.PT_1, pattern.PT_2, pattern.PT_3, pattern.PT_4, pattern.PT_5,
            pattern.PT_6, pattern.PT_7, pattern.PT_8, pattern.PT_9, pattern.PT_10
        };
    }

    // LMJ: Updated filtering methods
    public static List<GearData> GetGearByType(int gearType)
    {
        if (CSVManager.Instance?.GetCSVDataAsset() == null) return new List<GearData>();

        return CSVManager.Instance.GetCSVDataAsset().gearDataList
            .Where(gear => gear.GEAR_TYPE == gearType)
            .ToList();
    }

    public static List<GearData> GetGearByRarity(int rarity)
    {
        if (CSVManager.Instance?.GetCSVDataAsset() == null) return new List<GearData>();

        return CSVManager.Instance.GetCSVDataAsset().gearDataList
            .Where(gear => gear.GEAR_RARE == rarity)
            .ToList();
    }

    public static List<StatData> GetStatsByLevel(int level)
    {
        if (CSVManager.Instance?.GetCSVDataAsset() == null) return new List<StatData>();

        return CSVManager.Instance.GetCSVDataAsset().statDataList
            .Where(stat => stat.STAT_LV == level)
            .ToList();
    }

    // LMJ: Updated debug methods
    public static void LogGearInfo(string gearId)
    {
        GearData gear = CSVManager.Instance?.GetGearData(gearId);
        if (gear == null)
        {
            Debug.LogWarning($"Gear not found: {gearId}");
            return;
        }

        GearEffectData effect = CSVManager.Instance.GetGearEffectData(gear.GR_EFF_ID.ToString());

        Debug.Log($"=== Gear Info: {gearId} ===");
        Debug.Log($"Name: {gear.GEAR_NAME}");
        Debug.Log($"Type: {gear.GEAR_TYPE}");
        Debug.Log($"Rarity: {gear.GEAR_RARE}");
        Debug.Log($"Effect ID: {gear.GR_EFF_ID}");

        if (effect != null)
        {
            Debug.Log($"Effect: {effect.EFF_NAME}");
            Debug.Log($"ATT: +{effect.ATT_EFF}, DEF: +{effect.DEF_ATT_EFF}, HP: +{effect.HP_EFF}");
        }
    }

    public static void LogStatInfo(string statId, int level)
    {
        StatData stat = CSVManager.Instance?.GetStatData(statId, level);
        if (stat == null)
        {
            Debug.LogWarning($"Stat not found: {statId} Level {level}");
            return;
        }

        Debug.Log($"=== Stat Info: {statId} Level {level} ===");
        Debug.Log($"Type: {stat.STAT}");
        Debug.Log($"ATT: {stat.ATT}, DEF: {stat.DEF}, HP: {stat.HP}");
        Debug.Log($"DEF_ATT: {stat.DEF_ATT}");
    }

    // LMJ: Player stat helper methods
    public static StatData GetPlayerStatData(StatType statType, int level)
    {
        if (CSVManager.Instance == null) return null;

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

    public static int GetMaxLevelForStatType(StatType statType)
    {
        if (CSVManager.Instance == null) return 1;

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
}