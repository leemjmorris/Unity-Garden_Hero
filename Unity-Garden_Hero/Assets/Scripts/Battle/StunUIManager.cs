using UnityEngine;
using MoreMountains.Tools;

public class StunUIManager : MonoBehaviour
{
    [Header("MM Progress Bar")]
    [SerializeField] private MMProgressBar stunProgressBar;
    
    [Header("Monster Reference")]
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private Transform bossPrefabsParent; // BossPrefabs 부모 오브젝트

    void Start()
    {
        // Auto-find active monster from BossPrefabs parent
        if (monsterManager == null && bossPrefabsParent != null)
        {
            FindActiveMonster();
        }

        // Fallback: Find any MonsterManager in scene
        if (monsterManager == null)
        {
            monsterManager = FindFirstObjectByType<MonsterManager>();
        }

        if (monsterManager != null)
        {
            InitializeStunBar();
            SubscribeToStunEvents();
        }
        else
        {
            Debug.LogWarning("[StunUIManager] Monster target not found. Will retry in Update.");
        }
    }

    void Update()
    {
        // 타겟이 없으면 계속 찾기
        if (monsterManager == null && bossPrefabsParent != null)
        {
            FindActiveMonster();
        }
    }

    void FindActiveMonster()
    {
        if (bossPrefabsParent == null) return;

        // BossPrefabs의 자식 중 활성화된 몬스터 찾기
        foreach (Transform child in bossPrefabsParent)
        {
            if (child.gameObject.activeSelf)
            {
                MonsterManager monster = child.GetComponent<MonsterManager>();
                if (monster != null)
                {
                    SetMonsterManager(monster);
                    Debug.Log($"[StunUIManager] Found active monster: {child.name}");
                    return;
                }
            }
        }
    }
    
    void InitializeStunBar()
    {
        if (stunProgressBar == null) return;
        
        // LMJ: Set initial values
        float initialStun = monsterManager.GetStunPercentage();
        stunProgressBar.SetBar01(initialStun);
    }
    
    void SubscribeToStunEvents()
    {
        monsterManager.OnStunChanged.AddListener(OnStunChanged);
        monsterManager.OnStunBroken.AddListener(OnStunBroken);
        monsterManager.OnStunRestored.AddListener(OnStunRestored);
    }
    
    void OnStunChanged(float newStun)
    {
        if (monsterManager != null)
        {
            Debug.Log($"[StunUIManager] OnStunChanged - Monster:{monsterManager.gameObject.name}, NewStun:{newStun}, Percentage:{monsterManager.GetStunPercentage():F2}");
        }
        else
        {
            Debug.LogWarning($"[StunUIManager] OnStunChanged called but monsterManager is NULL! newStun:{newStun}");
        }

        UpdateStunBar();

        // LMJ: Trigger bump effect when stun decreases
        if (stunProgressBar != null)
        {
            stunProgressBar.Bump();
        }
    }
    
    void OnStunBroken()
    {
        if (stunProgressBar != null)
        {
            stunProgressBar.SetBar01(0f);
        }
    }

    void OnStunRestored()
    {
        if (stunProgressBar == null || monsterManager == null) return;

        float stunPercentage = monsterManager.GetStunPercentage();
        Debug.Log($"[StunUIManager] OnStunRestored - immediate update to: {stunPercentage:F2}");

        // LMJ: Immediate update for stun restoration (no animation)
        stunProgressBar.SetBar01(stunPercentage);
    }
    
    void UpdateStunBar()
    {
        if (stunProgressBar == null || monsterManager == null) return;

        float stunPercentage = monsterManager.GetStunPercentage();
        Debug.Log($"[StunUIManager] UpdateStunBar - stunPercentage: {stunPercentage:F2}");

        // LMJ: Use UpdateBar01 for animated progress (normal stun damage)
        stunProgressBar.UpdateBar01(stunPercentage);
    }
    
    public void SetMonsterManager(MonsterManager manager)
    {
        // LMJ: Unsubscribe from old monster
        if (monsterManager != null)
        {
            UnsubscribeFromEvents();
        }

        monsterManager = manager;

        if (monsterManager != null)
        {
            InitializeStunBar();
            SubscribeToStunEvents();
            UpdateStunBar();
        }
    }
    
    void UnsubscribeFromEvents()
    {
        if (monsterManager != null)
        {
            monsterManager.OnStunChanged.RemoveListener(OnStunChanged);
            monsterManager.OnStunBroken.RemoveListener(OnStunBroken);
            monsterManager.OnStunRestored.RemoveListener(OnStunRestored);
        }
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}