using UnityEngine;

public class DodgeSystem : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private bool enableSwipeInput = true;
    [SerializeField] private bool enableKeyboardInput = true;
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private float maxSwipeTime = 1f;

    [Header("Rotation Settings")]
    [SerializeField] private Transform mapTransform;
    [SerializeField] private float rotationSpeed = 2f;

    [Header("Shield System")]
    [SerializeField] private DirectionalShieldSystem directionalShieldSystem;

    [Header("Debug")]
    [SerializeField] private bool showInputDebug = false;

    private bool isRotating = false;
    private bool isDodgeActive = false;
    private float targetRotation = 0f;
    private float lastRotationEndTime = 0f;

    // Swipe input variables
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private float startTime;
    private bool isDragging = false;

    void Start()
    {
        // No button setup needed anymore
    }

    void Update()
    {
        HandleInput();
        HandleRotation();
    }

    void HandleInput()
    {
        if (enableSwipeInput)
            HandleSwipeInput();
            
        if (enableKeyboardInput)
            HandleKeyboardInput();
    }

    void HandleSwipeInput()
    {
        //LMJ: Handle mouse input for PC testing
        if (Input.GetMouseButtonDown(0))
        {
            StartSwipe(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateSwipe(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndSwipe(Input.mousePosition);
        }
        
        //LMJ: Handle mobile touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartSwipe(touch.position);
                    break;
                case TouchPhase.Moved:
                    if (isDragging)
                        UpdateSwipe(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isDragging)
                        EndSwipe(touch.position);
                    break;
            }
        }
    }

    void HandleKeyboardInput()
    {
        //LMJ: Q key for left dodge
        if (Input.GetKeyDown(KeyCode.Q))
        {
            DodgeLeft();
        }
        
        //LMJ: E key for right dodge
        if (Input.GetKeyDown(KeyCode.E))
        {
            DodgeRight();
        }
    }

    void StartSwipe(Vector2 position)
    {
        startTouchPosition = position;
        startTime = Time.time;
        isDragging = true;
        
        if (showInputDebug)
            Debug.Log($"Swipe started at: {position}");
    }
    
    void UpdateSwipe(Vector2 position)
    {
        endTouchPosition = position;
    }
    
    void EndSwipe(Vector2 position)
    {
        if (!isDragging) return;
        
        endTouchPosition = position;
        float swipeTime = Time.time - startTime;
        Vector2 swipeDirection = endTouchPosition - startTouchPosition;
        float swipeDistance = swipeDirection.magnitude;
        
        if (showInputDebug)
        {
            Debug.Log($"Swipe ended - Distance: {swipeDistance}, Time: {swipeTime}, Direction: {swipeDirection}");
        }
        
        //LMJ: Check if swipe meets criteria
        if (swipeDistance >= minSwipeDistance && swipeTime <= maxSwipeTime)
        {
            //LMJ: Determine swipe direction (horizontal priority)
            if (Mathf.Abs(swipeDirection.x) > Mathf.Abs(swipeDirection.y))
            {
                //LMJ: Horizontal swipe
                if (swipeDirection.x > 0)
                {
                    DodgeRight();
                }
                else
                {
                    DodgeLeft();
                }
            }
        }
        else if (showInputDebug)
        {
            Debug.Log($"Swipe rejected - Distance: {swipeDistance} (min: {minSwipeDistance}), Time: {swipeTime} (max: {maxSwipeTime})");
        }
        
        isDragging = false;
    }

    void HandleRotation()
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

        //LMJ: Force complete dodge if no rotation is happening but dodge is still active
        if (isDodgeActive && !isRotating)
        {
            float timeSinceRotationEnd = Time.time - lastRotationEndTime;
            if (timeSinceRotationEnd > 0.5f)
            {
                CompleteDodge();
            }
        }
    }

    //LMJ: Remove SetupButtons method since buttons are no longer used

    public void DodgeLeft()
    {
        if (isDodgeActive && isRotating) return;

        if (showInputDebug)
            Debug.Log("Dodge Left triggered!");

        targetRotation -= 90f;
        StartDodge();
    }

    public void DodgeRight()
    {
        if (isDodgeActive && isRotating) return;

        if (showInputDebug)
            Debug.Log("Dodge Right triggered!");

        targetRotation += 90f;
        StartDodge();
    }

    void StartDodge()
    {
        isDodgeActive = true;
        isRotating = true;

        //LMJ: Pause note time system
        NoteTimeManager.Instance.PauseNoteTime();
    }

    void CompleteRotation()
    {
        isRotating = false;
        lastRotationEndTime = Time.time;

        //LMJ: Start checking for dodge completion
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

    //LMJ: Public methods for runtime configuration
    public void SetSwipeEnabled(bool enabled)
    {
        enableSwipeInput = enabled;
    }

    public void SetKeyboardEnabled(bool enabled)
    {
        enableKeyboardInput = enabled;
    }

    public void SetSwipeDistance(float distance)
    {
        minSwipeDistance = distance;
    }

    public void SetSwipeTime(float time)
    {
        maxSwipeTime = time;
    }
}