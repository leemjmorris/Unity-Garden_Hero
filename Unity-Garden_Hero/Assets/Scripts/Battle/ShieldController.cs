using UnityEngine;

public class ShieldController : MonoBehaviour
{
    [Header("Shield References")]
    [SerializeField] private GameObject rightShield;
    [SerializeField] private GameObject leftShield;
    [SerializeField] private GameObject frontShield;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem rightShieldBrokenEffect;
    [SerializeField] private ParticleSystem leftShieldBrokenEffect;
    [SerializeField] private ParticleSystem frontShieldBrokenEffect;

    [Header("Systems")]
    [SerializeField] private TouchInputManager touchInputManager;
    [SerializeField] private ShieldDurabilitySystem durabilitySystem;

    void Start()
    {
        if (leftShield != null) leftShield.SetActive(false);
        if (rightShield != null) rightShield.SetActive(false);
        if (frontShield != null) frontShield.SetActive(false);

        if (rightShieldBrokenEffect != null) rightShieldBrokenEffect.Stop();
        if (leftShieldBrokenEffect != null) leftShieldBrokenEffect.Stop();
        if (frontShieldBrokenEffect != null) frontShieldBrokenEffect.Stop();
    }

    public void ShowLeftShield(bool show)
    {
        if (leftShield == null) return;

        // LMJ: 비활성화 상태면 입력 무시 (이미 표시된 상태 유지)
        if (durabilitySystem != null && durabilitySystem.IsShieldDisabled("Left"))
            return;

        leftShield.SetActive(show);
    }

    public void ShowRightShield(bool show)
    {
        if (rightShield == null) return;

        // LMJ: 비활성화 상태면 입력 무시 (이미 표시된 상태 유지)
        if (durabilitySystem != null && durabilitySystem.IsShieldDisabled("Right"))
            return;

        rightShield.SetActive(show);
    }

    public void ShowFrontShield(bool show)
    {
        if (frontShield == null) return;

        // LMJ: 비활성화 상태면 입력 무시 (이미 표시된 상태 유지)
        if (durabilitySystem != null && durabilitySystem.IsShieldDisabled("Up"))
            return;

        frontShield.SetActive(show);
    }

    // LMJ: 비활성화 상태 시각화
    public void SetShieldDisabledVisual(string direction, bool disabled)
    {
        GameObject shield = GetShieldByDirection(direction);
        ParticleSystem brokenEffect = GetBrokenEffectByDirection(direction);

        if (shield == null) return;

        if (disabled)
        {
            // LMJ: 비활성화: 불투명하게 계속 표시 + 파티클 재생
            shield.SetActive(true);
            SetShieldOpacity(shield, 0.3f); // 불투명

            if (brokenEffect != null)
            {
                brokenEffect.Play(); // LMJ: 파티클 재생
            }
        }
        else
        {
            // LMJ: 복구: 투명도 원래대로, 숨김 상태로 + 파티클 정지
            SetShieldOpacity(shield, 1.0f);
            shield.SetActive(false);

            if (brokenEffect != null)
            {
                brokenEffect.Stop(); // LMJ: 파티클 정지
            }
        }
    }

    ParticleSystem GetBrokenEffectByDirection(string direction)
    {
        switch (direction)
        {
            case "Left": return leftShieldBrokenEffect;
            case "Right": return rightShieldBrokenEffect;
            case "Up": return frontShieldBrokenEffect;
            default: return null;
        }
    }

    GameObject GetShieldByDirection(string direction)
    {
        switch (direction)
        {
            case "Left": return leftShield;
            case "Right": return rightShield;
            case "Up": return frontShield;
            default: return null;
        }
    }

    void SetShieldOpacity(GameObject shield, float alpha)
    {
        Renderer renderer = shield.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color color = renderer.material.color;
            color.a = alpha;
            renderer.material.color = color;

            // LMJ: 투명도 설정을 위한 렌더링 모드 조정
            if (alpha < 1.0f)
            {
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.renderQueue = 3000;
            }
            else
            {
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                renderer.material.SetInt("_ZWrite", 1);
                renderer.material.DisableKeyword("_ALPHABLEND_ON");
                renderer.material.renderQueue = -1;
            }
        }
    }
}