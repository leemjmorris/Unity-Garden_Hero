using UnityEngine;
using UnityEngine.Events;

public class DodgeSystem : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private bool enableSwipeInput = true;
    [SerializeField] private bool enableKeyboardInput = true;
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private float maxSwipeTime = 1f;

    [Header("System Control")]
    private bool isDodgeSystemEnabled = true;

    [Header("Rotation Settings")]
    [SerializeField] private Transform mapTransform;
    [SerializeField] private float rotationSpeed = 2f;

    [Header("Shield System")]
    [SerializeField] private DirectionalShieldSystem directionalShieldSystem;

    [Header("Game Manager")]
    [SerializeField] private GameManager gameManager;

    [Header("Dodge Events")]
    public UnityEvent OnDodgeLeft;
    public UnityEvent OnDodgeRight;

    // [Header("Debug")]
    // [SerializeField] private bool showInputDebug = false;

    private bool isRotating = false;
    private bool isDodgeActive = false;
    private float targetRotation = 0f;
    private float lastRotationEndTime = 0f;

    // LMJ: Track active touch IDs to prevent swipe during multi-touch
    private int activeTouchIdCount = 0;

    // LMJ: Temporary swipe blocking for simultaneous button presses
    private bool isSwipeTemporarilyBlocked = false;
    private float swipeBlockEndTime = 0f;
    [SerializeField] private float swipeBlockDuration = 0.2f;

    // Swipe input variables
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private float startTime;
    private bool isDragging = false;

    void Start()
    {
        // Find GameManager if not assigned
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    void Update()
    {
        HandleInput();
        HandleRotation();
    }

    void HandleInput()
    {
        // Don't handle any input if dodge system is disabled
        if (!isDodgeSystemEnabled) return;

        // Don't handle input during DealingTime
        if (gameManager != null && gameManager.IsDealingTimeActive())
        {
            // Debug.Log("DodgeSystem: Input blocked during DealingTime");
            return;
        }

        // LMJ: Check and update temporary swipe block
        if (isSwipeTemporarilyBlocked && Time.time >= swipeBlockEndTime)
        {
            isSwipeTemporarilyBlocked = false;
        }

        if (enableSwipeInput && !isSwipeTemporarilyBlocked)
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

        // LMJ: Don't process swipe if multiple touches are active (button inputs)
        if (activeTouchIdCount > 1)
        {
            isDragging = false;
            return;
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
                    // Left to Right swipe -> DodgeLeft
                    DodgeLeft();
                }
                else
                {
                    // Right to Left swipe -> DodgeRight
                    DodgeRight();
                }
            }
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
        if (!isDodgeSystemEnabled) return;
        if (isDodgeActive && isRotating) return;

        // Invoke the dodge left event
        OnDodgeLeft?.Invoke();

        // LMJ: Clear Defense Notes on screen when dodging
        ClearDefenseNotesOnDodge();

        targetRotation -= 90f;  // Counter-clockwise rotation
        StartDodge();
    }

    public void DodgeRight()
    {
        if (!isDodgeSystemEnabled) return;
        if (isDodgeActive && isRotating) return;

        // Invoke the dodge right event
        OnDodgeRight?.Invoke();

        // LMJ: Clear Defense Notes on screen when dodging
        ClearDefenseNotesOnDodge();

        targetRotation += 90f;  // Clockwise rotation
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

    // LMJ: System enable/disable methods
    public void SetDodgeSystemEnabled(bool enabled)
    {
        isDodgeSystemEnabled = enabled;

        if (!enabled)
        {
            // Stop any ongoing rotation
            isRotating = false;
            isDodgeActive = false;
        }
    }

    public bool IsDodgeSystemEnabled()
    {
        return isDodgeSystemEnabled;
    }

    // LMJ: Public methods for TouchInputManager to track button touches
    public void RegisterTouchStart()
    {
        activeTouchIdCount++;
    }

    public void RegisterTouchEnd()
    {
        activeTouchIdCount = Mathf.Max(0, activeTouchIdCount - 1);
    }

    // LMJ: Method to temporarily block swipe when buttons are pressed simultaneously
    public void BlockSwipeTemporarily()
    {
        isSwipeTemporarilyBlocked = true;
        swipeBlockEndTime = Time.time + swipeBlockDuration;
    }

    // LMJ: Clear Dodge Notes when dodging
    void ClearDefenseNotesOnDodge()
    {
        RhythmGameSystem rhythmSystem = FindFirstObjectByType<RhythmGameSystem>();
        if (rhythmSystem != null)
        {
            rhythmSystem.ClearDodgeNotesOnScreen();
        }
        else
        {
        }
    }
}