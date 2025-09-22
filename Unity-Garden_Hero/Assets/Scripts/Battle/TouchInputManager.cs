using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TouchInputManager : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button centerButton;
    
    [Header("Combo Button References")]
    [SerializeField] private Button leftCenterButton;
    [SerializeField] private Button rightCenterButton;

    [Header("Manager References")]
    [SerializeField] private RhythmGameSystem gameSystem;
    [SerializeField] private ShieldController shieldController;
    [SerializeField] private DirectionalShieldSystem directionalShieldSystem;

    [Header("Long Note Settings")]
    private bool isLeftHolding = false;
    private bool isRightHolding = false;
    private bool isCenterHolding = false;
    private bool isLeftCenterHolding = false;
    private bool isRightCenterHolding = false;

    void Start()
    {
        SetupButtonEvents();
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

        if (centerButton != null)
        {
            ClearButtonEvents(centerButton);
            AddButtonEvents(centerButton, "Up");
        }
        
        //LMJ: Setup combo buttons
        if (leftCenterButton != null)
        {
            ClearButtonEvents(leftCenterButton);
            AddButtonEvents(leftCenterButton, "LeftCenter");
        }
        
        if (rightCenterButton != null)
        {
            ClearButtonEvents(rightCenterButton);
            AddButtonEvents(rightCenterButton, "RightCenter");
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
        if (centerButton != null) ClearButtonEvents(centerButton);
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
        //LMJ: Handle combo buttons
        if (direction == "LeftCenter")
        {
            ProcessLeftCenterPress();
            return;
        }
        
        if (direction == "RightCenter")
        {
            ProcessRightCenterPress();
            return;
        }
        
        SetHoldingState(direction, true);

        if (gameSystem != null)
        {
            gameSystem.CheckHitWithLongNote(direction);
        }

        if (shieldController != null &&
            !(directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled(direction)))
        {
            ShowShield(direction, true);
        }

        AnimateButtonPress(direction, true);
    }

    public void OnButtonRelease(string direction)
    {
        //LMJ: Handle combo buttons
        if (direction == "LeftCenter")
        {
            ProcessLeftCenterRelease();
            return;
        }
        
        if (direction == "RightCenter")
        {
            ProcessRightCenterRelease();
            return;
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

        AnimateButtonPress(direction, false);
    }

    void ProcessLeftCenterPress()
    {
        //LMJ: Process Left and Center simultaneously
        SetHoldingState("LeftCenter", true);
        
        if (gameSystem != null)
        {
            gameSystem.CheckHitWithLongNote("Left");
            gameSystem.CheckHitWithLongNote("Up");
        }

        if (shieldController != null)
        {
            if (!(directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled("Left")))
                ShowShield("Left", true);
                
            if (!(directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled("Up")))
                ShowShield("Up", true);
        }

        AnimateButtonPress("LeftCenter", true);
    }
    
    void ProcessRightCenterPress()
    {
        //LMJ: Process Right and Center simultaneously
        SetHoldingState("RightCenter", true);
        
        if (gameSystem != null)
        {
            gameSystem.CheckHitWithLongNote("Right");
            gameSystem.CheckHitWithLongNote("Up");
        }

        if (shieldController != null)
        {
            if (!(directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled("Right")))
                ShowShield("Right", true);
                
            if (!(directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled("Up")))
                ShowShield("Up", true);
        }

        AnimateButtonPress("RightCenter", true);
    }
    
    void ProcessLeftCenterRelease()
    {
        if (IsHolding("LeftCenter"))
        {
            SetHoldingState("LeftCenter", false);

            if (gameSystem != null)
            {
                gameSystem.ReleaseLongNote("Left");
                gameSystem.ReleaseLongNote("Up");
            }
        }

        if (shieldController != null)
        {
            if (!(directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled("Left")))
                ShowShield("Left", false);
                
            if (!(directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled("Up")))
                ShowShield("Up", false);
        }

        AnimateButtonPress("LeftCenter", false);
    }
    
    void ProcessRightCenterRelease()
    {
        if (IsHolding("RightCenter"))
        {
            SetHoldingState("RightCenter", false);

            if (gameSystem != null)
            {
                gameSystem.ReleaseLongNote("Right");
                gameSystem.ReleaseLongNote("Up");
            }
        }

        if (shieldController != null)
        {
            if (!(directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled("Right")))
                ShowShield("Right", false);
                
            if (!(directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled("Up")))
                ShowShield("Up", false);
        }

        AnimateButtonPress("RightCenter", false);
    }

    public void OnButtonExit(string direction)
    {
        //LMJ: Treat as release if finger slides off button while holding
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
            case "LeftCenter":
                isLeftCenterHolding = holding;
                break;
            case "RightCenter":
                isRightCenterHolding = holding;
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
            case "LeftCenter":
                return isLeftCenterHolding;
            case "RightCenter":
                return isRightCenterHolding;
            default:
                return false;
        }
    }

    void ShowShield(string direction, bool show)
    {
        if (directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled(direction))
        {
            Debug.Log($"Shield {direction} is disabled, ignoring input");
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
            case "LeftCenter":
                targetButton = leftCenterButton;
                break;
            case "RightCenter":
                targetButton = rightCenterButton;
                break;
        }

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
        if (shieldController != null)
        {
            if (!IsShieldBroken("Left"))
            {
                if (Input.GetKey(KeyCode.A))
                    shieldController.ShowLeftShield(true);
                else
                    shieldController.ShowLeftShield(false);
            }

            if (!IsShieldBroken("Right"))
            {
                if (Input.GetKey(KeyCode.D))
                    shieldController.ShowRightShield(true);
                else
                    shieldController.ShowRightShield(false);
            }

            if (!IsShieldBroken("Up"))
            {
                if (Input.GetKey(KeyCode.W))
                    shieldController.ShowFrontShield(true);
                else
                    shieldController.ShowFrontShield(false);
            }
        }
    }

    bool IsShieldBroken(string direction)
    {
        return directionalShieldSystem != null && directionalShieldSystem.IsShieldDisabled(direction);
    }
}