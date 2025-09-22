using UnityEngine;

public class ShieldController : MonoBehaviour
{
    [Header("Shield References")]
    [SerializeField] private GameObject rightShield;
    [SerializeField] public GameObject leftShield;
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
        leftShield.SetActive(show);
    }

    public void ShowRightShield(bool show)
    {
        if (rightShield == null) return;
        rightShield.SetActive(show);
    }

    public void ShowFrontShield(bool show)
    {
        if (frontShield == null) return;
        frontShield.SetActive(show);
    }

    public void SetShieldParticleEffect(string direction, bool showParticles)
    {
        ParticleSystem brokenEffect = GetBrokenEffectByDirection(direction);

        if (brokenEffect != null)
        {
            if (showParticles)
            {
                brokenEffect.Play();
            }
            else
            {
                brokenEffect.Stop();
            }
        }
    }

    public void SetShieldBrokenState(string direction, bool isBroken)
    {
        Debug.Log($"SetShieldBrokenState called: {direction}, isBroken: {isBroken}");

        GameObject shield = GetShieldByDirection(direction);
        ParticleSystem brokenEffect = GetBrokenEffectByDirection(direction);

        Debug.Log($"Shield GameObject found: {shield != null}");
        Debug.Log($"Particle System found: {brokenEffect != null}");

        if (shield == null)
        {
            Debug.LogError($"Shield GameObject for {direction} is null!");
            return;
        }

        if (isBroken)
        {
            Debug.Log($"Setting {direction} shield to broken state");
            shield.SetActive(true);
            SetShieldOpacity(shield, 0.3f);

            if (brokenEffect != null)
            {
                brokenEffect.Play();
                Debug.Log($"Particle effect started for {direction}");
            }
            else
            {
                Debug.LogError($"Particle effect for {direction} is null!");
            }
        }
        else
        {
            Debug.Log($"Restoring {direction} shield");
            SetShieldOpacity(shield, 1.0f);

            if (brokenEffect != null)
            {
                brokenEffect.Stop();
                Debug.Log($"Particle effect stopped for {direction}");
            }

            shield.SetActive(false);
        }
    }

    public bool IsShieldBroken(string direction)
    {
        GameObject shield = GetShieldByDirection(direction);
        if (shield == null) return false;

        Renderer renderer = shield.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.material.color.a < 1.0f;
        }
        return false;
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
}