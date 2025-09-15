using UnityEngine;

public class ShieldController : MonoBehaviour
{
    [SerializeField] private GameObject rightShield;
    [SerializeField] private GameObject leftShield;
    [SerializeField] private GameObject frontShield;
    
    void Update()
    {
        leftShield.SetActive(Input.GetKey(KeyCode.A));
        rightShield.SetActive(Input.GetKey(KeyCode.D));
        frontShield.SetActive(Input.GetKey(KeyCode.W));
    }
}