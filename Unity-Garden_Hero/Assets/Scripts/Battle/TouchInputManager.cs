using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TouchInputManager : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button centerButton;

    [Header("Manager References")]
    [SerializeField] private RhythmGameSystem gameSystem;
    [SerializeField] private ShieldController shieldController;

    [Header("Long Note Settings")]
    private bool isLeftHolding = false;
    private bool isRightHolding = false;
    private bool isCenterHolding = false;

    void Start()
    {
        SetupButtonEvents();
    }

    void SetupButtonEvents()
    {
        if (leftButton != null)
        {
            // LMJ: Clear existing event triggers
            ClearButtonEvents(leftButton);
            AddButtonEvents(leftButton, "Left");
        }

        if (rightButton != null)
        {
            ClearButtonEvents(rightButton);
            AddButtonEvents(rightButton, "Right");
        }

        if (centerButton != null)
        {
            ClearButtonEvents(centerButton);
            AddButtonEvents(centerButton, "Up");
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
        // LMJ: Clean up all event triggers
        if (leftButton != null) ClearButtonEvents(leftButton);
        if (rightButton != null) ClearButtonEvents(rightButton);
        if (centerButton != null) ClearButtonEvents(centerButton);
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
        pointerDown.callback.AddListener((data) => OnButtonPress(direction));
        trigger.triggers.Add(pointerDown);

        // Add pointer up event
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => OnButtonRelease(direction));
        trigger.triggers.Add(pointerUp);

        // Add pointer exit event (for when finger slides off)
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => OnButtonExit(direction));
        trigger.triggers.Add(pointerExit);
    }

    public void OnButtonPress(string direction)
    {
        // Set holding state
        SetHoldingState(direction, true);

        if (gameSystem != null)
        {
            // Use new method that handles long notes
            gameSystem.CheckHitWithLongNote(direction);
        }

        // Show shield when button pressed
        if (shieldController != null)
        {
            ShowShield(direction, true);
        }

        // Visual feedback
        AnimateButtonPress(direction, true);
    }

    public void OnButtonRelease(string direction)
    {
        // Check if was holding
        if (IsHolding(direction))
        {
            SetHoldingState(direction, false);

            if (gameSystem != null)
            {
                // Release long note
                gameSystem.ReleaseLongNote(direction);
            }
        }

        // Hide shield when button released
        if (shieldController != null)
        {
            ShowShield(direction, false);
        }

        // Visual feedback
        AnimateButtonPress(direction, false);
    }

    public void OnButtonExit(string direction)
    {
        // Treat as release if finger slides off button while holding
        if (IsHolding(direction))
        {
            OnButtonRelease(direction);
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
            case "Up":
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
            case "Up":
                return isCenterHolding;
            default:
                return false;
        }
    }

    void ShowShield(string direction, bool show)
    {
        switch (direction)
        {
            case "Left":
                shieldController.ShowLeftShield(show);
                break;
            case "Right":
                shieldController.ShowRightShield(show);
                break;
            case "Up":
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
            case "Up":
                targetButton = centerButton;
                break;
        }

        if (targetButton != null)
        {
            RectTransform rect = targetButton.GetComponent<RectTransform>();
            Image image = targetButton.GetComponent<Image>();

            if (pressed)
            {
                // Press animation
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
                // Release animation
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

    // Keyboard support for testing
    void Update()
    {
        // Left lane (A key)
        if (Input.GetKeyDown(KeyCode.A))
            OnButtonPress("Left");
        if (Input.GetKeyUp(KeyCode.A))
            OnButtonRelease("Left");

        // Right lane (D key)
        if (Input.GetKeyDown(KeyCode.D))
            OnButtonPress("Right");
        if (Input.GetKeyUp(KeyCode.D))
            OnButtonRelease("Right");

        // Up lane (W key)
        if (Input.GetKeyDown(KeyCode.W))
            OnButtonPress("Up");
        if (Input.GetKeyUp(KeyCode.W))
            OnButtonRelease("Up");
    }
}