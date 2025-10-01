using System;
using UnityEngine;

public enum StatType
{
    Strength,
    Dexterity,
    Constitution
}

[System.Serializable]
public class BossData
{
    public int BOSS_ID;           // INT
    public string BOSS_NAME;      // STRING
    public int PHASE;             // INT
    public float STUN;            // FLOAT
    public int STUN_RECOVERY;     // INT
    public int DEF;               // INT
    public int STUN_DEF;          // INT
    public float HP;              // FLOAT
    public int PT_ID;             // INT
    public string BOSS_PREFABS;   // STRING
    public int BOSS_ATT_ID;       // INT - BS_ATT.csv 참조용
}

[System.Serializable]
public class BossAttData
{
    public int BOSS_ATT_ID;       // INT
    public string BOSS_NAME;      // STRING
    public int PHASE;             // INT
    public int NORMAL_ATT;        // INT
    public int NORMAL_DEF_ATT;    // INT
    public int LONG_ATT;          // INT
    public int LONG_DEF_ATT;      // INT
    public int SPECIAL_ATT;       // INT
    public int SPECIAL_DEF_ATT;   // INT
}

[System.Serializable]
public class ConsumableData
{
    public int ITME_ID;           // INT
    public string ITEM_NAME;      // STRING
    public int ITEM_RARE;         // INT
    public int ITEM_TYPE;         // INT
    public int OPTION_TYPE;       // INT
    public int EFF_INT_VALUE;     // INT
    public float EFF_FLOAT_VALUE; // FLOAT
    public int BT_MAX_STACK;      // INT
    public int MAX_STACK;         // INT
    public int DURATION;          // INT
    public float COOLTIME;        // FLOAT
    public int MAX_DROP_VALUE;    // INT
    public string ITEM_IMAGE;     // STRING
}

[System.Serializable]
public class GearData
{
    public int GEAR_ID;           // INT
    public string GEAR_NAME;      // STRING
    public int GEAR_RARE;         // INT
    public int GEAR_TYPE;         // INT
    public int GR_EFF_ID;         // INT
    public string ITEM_IMAGE;     // STRING
}

[System.Serializable]
public class GearSetData
{
    public int SET_ID;            // INT
    public string SET_EFF;        // STRING
    public float SET_EFF_3;       // FLOAT (3_SET_EFF)
    public float SET_EFF_5;       // FLOAT (5_SET_EFF)
    public int GEAR_ID_1;         // INT
    public int GEAR_ID_2;         // INT
    public int GEAR_ID_3;         // INT
    public int GEAR_ID_4;         // INT
    public int GEAR_ID_5;         // INT
}

[System.Serializable]
public class GearEffectData
{
    public int GR_EFF_ID;         // INT
    public string EFF_NAME;       // STRING
    public int ATT_EFF;           // INT
    public int DEF_ATT_EFF;       // INT
    public float DEE_EFF;         // FLOAT
    public int HP_EFF;            // INT
    public int DURABILITY;        // INT
}

[System.Serializable]
public class LinkData
{
    public string GROUP;          // STRING
    public string LINK;           // STRING
}

[System.Serializable]
public class PhaseData
{
    public int PHASE_ID;          // INT
    public int BOSS_ID_1;         // INT
    public int BOSS_ID_2;         // INT
    public int BOSS_ID_3;         // INT
    public int BOSS_ID_4;         // INT
    public int BOSS_ID_5;         // INT
}

[System.Serializable]
public class PatternData
{
    public int PT_ID;             // INT
    public string PT_1;           // STRING
    public string PT_2;           // STRING
    public string PT_3;           // STRING
    public string PT_4;           // STRING
    public string PT_5;           // STRING
    public string PT_6;           // STRING
    public string PT_7;           // STRING
    public string PT_8;           // STRING
    public string PT_9;           // STRING
    public string PT_10;          // STRING
}

[System.Serializable]
public class StageData
{
    public int STAGE_ID;          // INT
    public int PHASE_ID;          // INT
}

[System.Serializable]
public class StatData
{
    public int STAT_ID;           // INT
    public int STAT_LV;           // INT
    public int STAT;              // ENUM (실제로는 int로 처리)
    public int ATT;               // INT
    public int DEF_ATT;           // INT
    public int HP;                // INT
    public float DEF;             // FLOAT
}