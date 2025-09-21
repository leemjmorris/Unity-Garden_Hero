using UnityEngine;
using MoreMountains.Tools;

public class LaneShieldDurability : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private MMProgressBar durabilityBar;
    [SerializeField] private DirectionalShieldSystem directionalShieldSystem;
    
    void Update()
    {
        if (directionalShieldSystem == null || durabilityBar == null) return;
        
        // LMJ: Show current direction shield durability
        float durability = directionalShieldSystem.GetShieldDurabilityPercent();
        durabilityBar.UpdateBar01(durability);
    }
}