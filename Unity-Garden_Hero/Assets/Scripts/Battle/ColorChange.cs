using UnityEngine;

public class ColorChange : MonoBehaviour
{
    [SerializeField] private Color cubeColor = Color.white;

    void Start()
    {
        GetComponent<Renderer>().material.color = cubeColor;
    }
}
