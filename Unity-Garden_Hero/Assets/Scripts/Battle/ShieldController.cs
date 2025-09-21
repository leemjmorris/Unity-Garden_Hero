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
    [SerializeField] private DirectionalShieldSystem directionalShieldSystem;

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

        // LMJ: Check if current direction shield is disabled
        if (directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled("Left"))
            return;

        leftShield.SetActive(show);
    }

    public void ShowRightShield(bool show)
    {
        if (rightShield == null) return;

        // LMJ: Check if current direction shield is disabled
        if (directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled("Right"))
            return;

        rightShield.SetActive(show);
    }

    public void ShowFrontShield(bool show)
    {
        if (frontShield == null) return;

        // LMJ: Check if current direction shield is disabled
        if (directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled("Up"))
            return;

        frontShield.SetActive(show);
    }

    // LMJ: Visual feedback for shield states
    public void SetShieldDisabledVisual(string direction, bool disabled)
    {
        GameObject shield = GetShieldByDirection(direction);
        ParticleSystem brokenEffect = GetBrokenEffectByDirection(direction);

        if (shield == null) return;

        if (disabled)
        {
            // LMJ: Disabled state: semi-transparent + particle effects
            shield.SetActive(true);
            SetShieldOpacity(shield, 0.3f);

            if (brokenEffect != null)
            {
                brokenEffect.Play();
            }
        }
        else
        {
            // LMJ: Restored state: full opacity, hidden + stop particles
            SetShieldOpacity(shield, 1.0f);
            shield.SetActive(false);

            if (brokenEffect != null)
            {
                brokenEffect.Stop();
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