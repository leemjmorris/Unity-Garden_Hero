using UnityEngine;
using MoreMountains.Tools;

public class LaneShieldDurability : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private MMProgressBar durabilityBar;
    [SerializeField] private DirectionalShieldSystem directionalShieldSystem;
    
    void Start()
    {
        // LMJ: Auto-find DirectionalShieldSystem if not assigned
        if (directionalShieldSystem == null)
        {
            directionalShieldSystem = FindFirstObjectByType<DirectionalShieldSystem>();
        }
    }
    
    void Update()
    {
        if (directionalShieldSystem == null || durabilityBar == null) return;
        
        // LMJ: Show current direction shield durability
        float durability = directionalShieldSystem.GetShieldDurabilityPercent();
        durabilityBar.UpdateBar01(durability);
    }

    // LMJ: Reset shield durability UI animation
    public void RefreshShieldUI()
    {
        if (directionalShieldSystem == null || durabilityBar == null) return;
        
        // LMJ: Force refresh the shield durability bar
        float currentDurability = directionalShieldSystem.GetShieldDurabilityPercent();
        durabilityBar.SetBar01(currentDurability);
    }
}