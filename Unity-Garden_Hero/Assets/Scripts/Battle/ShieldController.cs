using UnityEngine;

public class ShieldController : MonoBehaviour
{
    [SerializeField] private GameObject rightShield;
    [SerializeField] private GameObject leftShield;
    [SerializeField] private GameObject frontShield;

    [Header("Hit Lines")]
    public Material hitLineMaterial;
    public float lineOffset = 0.3f;
    public Vector3 lineScale = new Vector3(0.8f, 0.03f, 0.8f);

    private GameObject leftHitLine;
    private GameObject rightHitLine;
    private GameObject frontHitLine;

    void Start()
    {
        CreateHitLines();
    }

    void CreateHitLines()
    {
        // LMJ: Create hit lines near each shield
        if (leftShield != null)
        {
            Vector3 pos = leftShield.transform.position;
            pos.x += lineOffset; // Move towards center
            leftHitLine = CreateHitLine("LeftHitLine", pos, Color.red);
        }

        if (rightShield != null)
        {
            Vector3 pos = rightShield.transform.position;
            pos.x -= lineOffset; // Move towards center
            rightHitLine = CreateHitLine("RightHitLine", pos, Color.blue);
        }

        if (frontShield != null)
        {
            Vector3 pos = frontShield.transform.position;
            pos.z -= lineOffset; // Move towards center
            frontHitLine = CreateHitLine("FrontHitLine", pos, Color.green);
        }
    }

    GameObject CreateHitLine(string name, Vector3 position, Color color)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube); // LMJ: Cylinder -> Cube
        line.name = name;
        line.transform.position = position;

        // LMJ: Make it look like a thin line
        Vector3 scale = lineScale;
        if (name.Contains("Left") || name.Contains("Right"))
        {
            scale = new Vector3(0.05f, 2.0f, 0.05f); // Vertical line
        }
        else // Up direction
        {
            scale = new Vector3(2.0f, 0.05f, 0.05f); // Horizontal line
        }
        line.transform.localScale = scale;

        Renderer renderer = line.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Mode", 2);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        Color transparentColor = color;
        transparentColor.a = 0.8f; // LMJ: More visible
        mat.color = transparentColor;
        renderer.material = mat;

        Destroy(line.GetComponent<Collider>());
        return line;
    }

    void Update()
    {
        leftShield.SetActive(Input.GetKey(KeyCode.A));
        rightShield.SetActive(Input.GetKey(KeyCode.D));
        frontShield.SetActive(Input.GetKey(KeyCode.W));
    }
}