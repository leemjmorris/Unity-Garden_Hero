using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TouchInputManager : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button leftCenterButton; // LMJ: Left-side center button
    [SerializeField] private Button rightCenterButton; // LMJ: Right-side center button

    [Header("Manager References")]
    [SerializeField] private RhythmGameSystem gameSystem;
    [SerializeField] private ShieldController shieldController;
    [SerializeField] private DirectionalShieldSystem directionalShieldSystem;
    [SerializeField] private DodgeSystem dodgeSystem;

    [Header("Long Note Settings")]
    private bool isLeftHolding = false;
    private bool isRightHolding = false;
    private bool isCenterHolding = false;

    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 50f;
    [SerializeField] private PlayerManager playerManager;

    // Swipe detection variables
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private bool isTouching = false;

    [Header("Button Press Detection")]
    [SerializeField] private float simultaneousPressThreshold = 0.1f; // Time window for simultaneous press detection
    private float lastButtonPressTime = 0f;
    private int buttonsPressed = 0;

    [Header("DealingTime Control")]
    private bool inputsDisabled = false;
    private GameManager gameManager;

    [Header("Touch Tracking for Swipe Prevention")]
    private float lastButtonReleaseTime = 0f;
    private const float SWIPE_PREVENTION_WINDOW = 0.3f; // Prevent swipe for 0.3s after button release

    void Start()
    {
        SetupButtonEvents();

        // LMJ: Initialize PlayerManager if not assigned
        if (playerManager == null)
        {
            playerManager = PlayerManager.Instance;
        }

        // LMJ: Initialize DodgeSystem if not assigned
        if (dodgeSystem == null)
        {
            dodgeSystem = FindFirstObjectByType<DodgeSystem>();
        }

        // LMJ: Initialize GameManager
        gameManager = FindFirstObjectByType<GameManager>();

        // LMJ: Shield initialization is handled by ShieldController.Awake()
    }

    void SetupButtonEvents()
    {
        if (leftButton != null)
        {
            //LMJ: Clear existing event triggers
            ClearButtonEvents(leftButton);
            AddButtonEvents(leftButton, "Left");
        }

        if (rightButton != null)
        {
            ClearButtonEvents(rightButton);
            AddButtonEvents(rightButton, "Right");
        }

        if (leftCenterButton != null)
        {
            ClearButtonEvents(leftCenterButton);
            AddButtonEvents(leftCenterButton, "Center");
        }

        if (rightCenterButton != null)
        {
            ClearButtonEvents(rightCenterButton);
            AddButtonEvents(rightCenterButton, "Center");
        }
    }

    void ClearButtonEvents(Button button)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            trigger.triggers.Clear();
        }
    }

    void OnDestroy()
    {
        //LMJ: Clean up all event triggers
        if (leftButton != null) ClearButtonEvents(leftButton);
        if (rightButton != null) ClearButtonEvents(rightButton);
        if (leftCenterButton != null) ClearButtonEvents(leftCenterButton);
        if (rightCenterButton != null) ClearButtonEvents(rightCenterButton);
    }

    void AddButtonEvents(Button button, string direction)
    {
        // EventTrigger for press and release
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // Clear existing entries
        trigger.triggers.Clear();

        // Add pointer down event
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => OnButtonPress(direction, button));
        trigger.triggers.Add(pointerDown);

        // Add pointer up event
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => OnButtonRelease(direction, button));
        trigger.triggers.Add(pointerUp);

        // Add pointer exit event (for when finger slides off)
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => OnButtonExit(direction, button));
        trigger.triggers.Add(pointerExit);
    }

    public void OnButtonPress(string direction, Button pressedButton = null)
    {
        // LMJ: Block input during DealingTime
        if (inputsDisabled) return;

        SetHoldingState(direction, true);

        // LMJ: Track button press timing for simultaneous press detection
        float currentTime = Time.time;
        if (currentTime - lastButtonPressTime <= simultaneousPressThreshold)
        {
            buttonsPressed++;
        }
        else
        {
            buttonsPressed = 1; // Reset count for new press sequence
        }
        lastButtonPressTime = currentTime;

        // LMJ: Register touch start for swipe detection prevention
        if (dodgeSystem != null)
        {
            dodgeSystem.RegisterTouchStart();

            // LMJ: Block swipe immediately when any button is pressed
            dodgeSystem.BlockSwipeTemporarily();

            // LMJ: If multiple buttons pressed simultaneously, extend the block duration
            if (buttonsPressed > 1)
            {
                dodgeSystem.ExtendSwipeBlock();
            }
        }

        if (gameSystem != null)
        {
            gameSystem.CheckHitWithLongNote(direction);
        }

        if (shieldController != null &&
            !(directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled(direction)))
        {
            ShowShield(direction, true);
        }

        // LMJ: Play defence animation when button is pressed
        if (playerManager != null)
        {
            playerManager.PlayDefenceAnimation();
        }

        // LMJ: Only animate the specific button that was pressed
        if (pressedButton != null)
        {
            AnimateSingleButton(pressedButton, true);
        }
        else
        {
            AnimateButtonPress(direction, true);
        }
    }

    public void OnButtonRelease(string direction, Button pressedButton = null)
    {
        // LMJ: Block input during DealingTime
        if (inputsDisabled) return;

        // LMJ: Check if all buttons are released
        SetHoldingState(direction, false);
        if (!isLeftHolding && !isRightHolding && !isCenterHolding)
        {
            lastButtonReleaseTime = Time.time;
        }

        // LMJ: Register touch end and extend swipe block after release
        if (dodgeSystem != null)
        {
            dodgeSystem.RegisterTouchEnd();
            dodgeSystem.BlockSwipeTemporarily(); // Block swipe briefly after button release
        }

        if (IsHolding(direction))
        {
            SetHoldingState(direction, false);

            if (gameSystem != null)
            {
                gameSystem.ReleaseLongNote(direction);
            }
        }

        if (shieldController != null &&
            !(directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled(direction)))
        {
            ShowShield(direction, false);
        }

        // LMJ: Only animate the specific button that was pressed
        if (pressedButton != null)
        {
            AnimateSingleButton(pressedButton, false);
        }
        else
        {
            AnimateButtonPress(direction, false);
        }
    }

    public void OnButtonExit(string direction, Button pressedButton = null)
    {
        //LMJ: Treat as release if finger slides off button while holding
        if (IsHolding(direction))
        {
            OnButtonRelease(direction, pressedButton);
        }
    }

    void SetHoldingState(string direction, bool holding)
    {
        switch (direction)
        {
            case "Left":
                isLeftHolding = holding;
                break;
            case "Right":
                isRightHolding = holding;
                break;
            case "Center":
                isCenterHolding = holding;
                break;
        }
    }

    bool IsHolding(string direction)
    {
        switch (direction)
        {
            case "Left":
                return isLeftHolding;
            case "Right":
                return isRightHolding;
            case "Center":
                return isCenterHolding;
            default:
                return false;
        }
    }

    void ShowShield(string direction, bool show)
    {
        if (directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled(direction))
        {
            return;
        }

        switch (direction)
        {
            case "Left":
                shieldController.ShowLeftShield(show);
                break;
            case "Right":
                shieldController.ShowRightShield(show);
                break;
            case "Center":
                shieldController.ShowFrontShield(show);
                break;
        }
    }

    void AnimateButtonPress(string direction, bool pressed)
    {
        Button targetButton = null;

        switch (direction)
        {
            case "Left":
                targetButton = leftButton;
                break;
            case "Right":
                targetButton = rightButton;
                break;
            case "Center":
                // LMJ: Animate both center buttons when Center is pressed
                AnimateSingleButton(leftCenterButton, pressed);
                AnimateSingleButton(rightCenterButton, pressed);
                return;
        }

        AnimateSingleButton(targetButton, pressed);
    }

    void AnimateSingleButton(Button targetButton, bool pressed)
    {
        if (targetButton != null)
        {
            RectTransform rect = targetButton.GetComponent<RectTransform>();
            Image image = targetButton.GetComponent<Image>();

            if (pressed)
            {
                //LMJ: Press animation
                rect.localScale = Vector3.one * 0.95f;
                if (image != null)
                {
                    Color color = image.color;
                    color.a = 0.8f;
                    image.color = color;
                }
            }
            else
            {
                //LMJ: Release animation
                rect.localScale = Vector3.one;
                if (image != null)
                {
                    Color color = image.color;
                    color.a = 1f;
                    image.color = color;
                }
            }
        }
    }

    void Update()
    {
        HandleSwipeInput();
        HandleKeyboardInput();
    }

    void HandleKeyboardInput()
    {
        // LMJ: Handle keyboard input for PC testing - same as button touches
        // A key for Left button
        if (Input.GetKeyDown(KeyCode.A))
        {
            OnButtonPress("Left");
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            OnButtonRelease("Left");
        }

        // D key for Right button
        if (Input.GetKeyDown(KeyCode.D))
        {
            OnButtonPress("Right");
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            OnButtonRelease("Right");
        }

        // W key for Center button
        if (Input.GetKeyDown(KeyCode.W))
        {
            OnButtonPress("Center");
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            OnButtonRelease("Center");
        }
    }

    bool IsShieldBroken(string direction)
    {
        return directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled(direction);
    }

    void HandleSwipeInput()
    {
        // Handle touch input for mobile
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                startTouchPosition = touch.position;
                isTouching = true;
            }
            else if (touch.phase == TouchPhase.Ended && isTouching)
            {
                endTouchPosition = touch.position;
                DetectSwipe();
                isTouching = false;
            }
        }
        // Handle mouse input for testing in editor
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                startTouchPosition = Input.mousePosition;
                isTouching = true;
            }
            else if (Input.GetMouseButtonUp(0) && isTouching)
            {
                endTouchPosition = Input.mousePosition;
                DetectSwipe();
                isTouching = false;
            }
        }
    }

    void DetectSwipe()
    {
        // LMJ: Don't process swipe if any buttons were pressed recently (prevent conflict)
        if (Time.time - lastButtonPressTime <= simultaneousPressThreshold * 2)
        {
            return;
        }

        Vector2 swipeDirection = endTouchPosition - startTouchPosition;
        float swipeDistance = swipeDirection.magnitude;

        if (swipeDistance < swipeThreshold)
            return;

        // LMJ: Additional check - don't swipe if multiple buttons are currently being held
        int currentlyHeld = 0;
        if (isLeftHolding) currentlyHeld++;
        if (isRightHolding) currentlyHeld++;
        if (isCenterHolding) currentlyHeld++;

        if (currentlyHeld > 0)
        {
            return; // Don't swipe if any buttons are being held
        }

        // Normalize the swipe direction
        swipeDirection.Normalize();

        // Check if it's a horizontal swipe (left to right or right to left)
        if (Mathf.Abs(swipeDirection.x) > Mathf.Abs(swipeDirection.y))
        {
            if (swipeDirection.x > 0)
            {
                // Left to Right swipe - Roll Left
                OnSwipeLeftToRight();
            }
            else
            {
                // Right to Left swipe - Roll Right
                OnSwipeRightToLeft();
            }
        }
    }

    void OnSwipeLeftToRight()
    {
        // Animation is now handled by DodgeSystem events
    }

    void OnSwipeRightToLeft()
    {
        // Animation is now handled by DodgeSystem events
    }

    // LMJ: Public methods for enabling/disabling inputs during DealingTime
    public void DisableAllInputs()
    {
        Debug.Log("[TouchInputManager] DisableAllInputs called");

        // Set flag first to prevent any new inputs
        inputsDisabled = true;

        // Force release all currently held buttons
        bool wasLeftHolding = isLeftHolding;
        bool wasRightHolding = isRightHolding;
        bool wasCenterHolding = isCenterHolding;

        // Clear all holding states
        isLeftHolding = false;
        isRightHolding = false;
        isCenterHolding = false;

        // Release any active long notes
        if (gameSystem != null)
        {
            if (wasLeftHolding) gameSystem.ReleaseLongNote("Left");
            if (wasRightHolding) gameSystem.ReleaseLongNote("Right");
            if (wasCenterHolding) gameSystem.ReleaseLongNote("Center");
        }

        // Hide all shields using the controller's method
        if (shieldController != null)
        {
            shieldController.HideAllShields();
            Debug.Log("[TouchInputManager] Called shieldController.HideAllShields()");
        }
        else
        {
            Debug.LogWarning("[TouchInputManager] shieldController is null!");
        }

        // Reset button visuals
        AnimateSingleButton(leftButton, false);
        AnimateSingleButton(rightButton, false);
        AnimateSingleButton(leftCenterButton, false);
        AnimateSingleButton(rightCenterButton, false);

        // Reset dodge system touch tracking
        if (dodgeSystem != null)
        {
            dodgeSystem.ResetSwipeBlock();
        }
    }

    public void EnableAllInputs()
    {
        inputsDisabled = false;
    }
}