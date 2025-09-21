using UnityEngine;
using UnityEngine.UI;

public class DodgeSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button leftDodgeButton;
    [SerializeField] private Button rightDodgeButton;

    [Header("Rotation Settings")]
    [SerializeField] private Transform mapTransform;
    [SerializeField] private float rotationSpeed = 2f;

    [Header("Shield System")]
    [SerializeField] private DirectionalShieldSystem directionalShieldSystem;

    private bool isRotating = false;
    private bool isDodgeActive = false;
    private float targetRotation = 0f;
    private float lastRotationEndTime = 0f;

    void Start()
    {
        SetupButtons();
    }

    void SetupButtons()
    {
        if (leftDodgeButton != null)
            leftDodgeButton.onClick.AddListener(DodgeLeft);

        if (rightDodgeButton != null)
            rightDodgeButton.onClick.AddListener(DodgeRight);
    }

    public void DodgeLeft()
    {
        if (isDodgeActive && isRotating) return;

        targetRotation -= 90f;
        StartDodge();
    }

    public void DodgeRight()
    {
        if (isDodgeActive && isRotating) return;

        targetRotation += 90f;
        StartDodge();
    }

    void StartDodge()
    {
        isDodgeActive = true;
        isRotating = true;

        // LMJ: Disable buttons during dodge
        if (leftDodgeButton != null) leftDodgeButton.interactable = false;
        if (rightDodgeButton != null) rightDodgeButton.interactable = false;

        // LMJ: Pause note time system
        NoteTimeManager.Instance.PauseNoteTime();
    }

    void Update()
    {
        if (isRotating && mapTransform != null)
        {
            float currentY = mapTransform.eulerAngles.y;
            float newY = Mathf.LerpAngle(currentY, targetRotation, rotationSpeed * Time.deltaTime);

            mapTransform.rotation = Quaternion.Euler(0, newY, 0);

            if (Mathf.Abs(Mathf.DeltaAngle(newY, targetRotation)) < 0.1f)
            {
                mapTransform.rotation = Quaternion.Euler(0, targetRotation, 0);
                CompleteRotation();
            }
        }

        // LMJ: Force complete dodge if no rotation is happening but dodge is still active
        if (isDodgeActive && !isRotating)
        {
            float timeSinceRotationEnd = Time.time - lastRotationEndTime;
            if (timeSinceRotationEnd > 0.5f) // 0.5초 후 강제 완료
            {
                CompleteDodge();
            }
        }
    }

    void CompleteRotation()
    {
        isRotating = false;
        lastRotationEndTime = Time.time;

        // LMJ: Start checking for dodge completion
        Invoke("CheckDodgeCompletion", 0.1f);
    }

    void CheckDodgeCompletion()
    {
        if (!isRotating)
        {
            CompleteDodge();
        }
    }

    void CompleteDodge()
    {
        if (!isDodgeActive) return;

        isDodgeActive = false;

        if (leftDodgeButton != null) leftDodgeButton.interactable = true;
        if (rightDodgeButton != null) rightDodgeButton.interactable = true;

        NoteTimeManager.Instance.ResumeNoteTime();
    }

    public bool IsRotating()
    {
        return isRotating;
    }

    public bool IsDodgeActive()
    {
        return isDodgeActive;
    }

    public float GetCurrentRotation()
    {
        return targetRotation;
    }
}