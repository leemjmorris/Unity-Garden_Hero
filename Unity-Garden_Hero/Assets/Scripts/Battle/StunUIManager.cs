using UnityEngine;
using MoreMountains.Tools;

public class StunUIManager : MonoBehaviour
{
    [Header("MM Progress Bar")]
    [SerializeField] private MMProgressBar stunProgressBar;
    
    [Header("Monster Reference")]
    [SerializeField] private MonsterManager monsterManager;
    
    void Start()
    {
        // LMJ: Auto-find MonsterManager if not assigned
        if (monsterManager == null)
        {
            monsterManager = FindFirstObjectByType<MonsterManager>();
        }

        if (monsterManager != null)
        {
            InitializeStunBar();
            SubscribeToStunEvents();
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
        Debug.Log($"[StunUIManager] OnStunChanged called with newStun: {newStun}");
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