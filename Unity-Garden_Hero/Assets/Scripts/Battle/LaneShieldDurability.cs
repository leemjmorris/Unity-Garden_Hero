using UnityEngine;
using MoreMountains.Tools;

public class LaneShieldDurability : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private MMProgressBar durabilityBar;
    [SerializeField] private ShieldDurabilitySystem durabilitySystem;
    
    void Update()
    {
        if (durabilitySystem == null || durabilityBar == null) return;
        
        // LMJ: All lanes show same durability
        float durability = durabilitySystem.GetShieldDurabilityPercent();
        durabilityBar.UpdateBar01(durability);
    }
}