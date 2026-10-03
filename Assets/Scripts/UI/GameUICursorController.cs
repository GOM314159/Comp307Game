using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameUICursorController : MonoBehaviour
{
    public static GameUICursorController Instance { get; private set; }

    [Header("Cursor")]
    [SerializeField] private Texture2D cursorTexture;

    [Tooltip("Hotspot in texture pixels measured from the TOP-LEFT.")]
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

    [Header("Controller")]
    [SerializeField] private float controllerCursorSpeed = 1200f;
    [SerializeField] private float stickDeadzone = 0.15f;
    [SerializeField] private float stickAcceleration = 1f;
    [SerializeField] private bool lockCursorToScreen = true;

    [Header("Behaviour")]
    [SerializeField] private bool hideWhenCursorIsLocked = true;

    private bool usingGamepad;
    private bool forcedHidden;
    private bool gameplayCursorLocked;
    private Coroutine cursorLockRoutine;
    private float gameplayLockUntil;

    private Vector2 controllerCursorPosition;

    private PointerEventData pointerEventData;
    private EventSystem pointerEventSystem;
    private GameObject hoveredObject;
    private bool eventSystemNavigationStateSaved;
    private bool originalSendNavigationEvents = true;

    // Controller inventory drag state.
    private InventoryItem controllerDraggedItem;
    private Transform controllerDragOriginalParent;
    private Vector3 controllerDragOriginalPosition;
    private bool controllerDragActive;

    private float ignoreMouseUntil;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyCursorTexture();
        SyncCursorVisibility();
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Start()
    {
        RefreshEventSystem();

        if (Mouse.current != null)
        {
            controllerCursorPosition = Mouse.current.position.ReadValue();
        }
        else
        {
            controllerCursorPosition = new Vector2(
                Screen.width * 0.5f,
                Screen.height * 0.5f
            );
        }

        ApplyCursorTexture();
        SyncCursorVisibility();
    }

    private void Update()
    {
        RefreshEventSystem();

        HandleControllerInput();
        HandleMouseInput();

        SyncEventSystemNavigation();
        SyncCursorVisibility();

        if (usingGamepad && IsCursorAllowedToBeVisible())
        {
            UpdateHover();
        }
        else
        {
            ClearHover();
        }
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void HandleControllerInput()
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
            return;

        Vector2 stick = gamepad.leftStick.ReadValue();

        bool moved =
            stick.sqrMagnitude >
            stickDeadzone * stickDeadzone;

        bool selectPressed =
            gamepad.buttonSouth.wasPressedThisFrame ||
            gamepad.buttonWest.wasPressedThisFrame;

        bool cancelPressed =
            gamepad.buttonEast.wasPressedThisFrame;

        bool startPressed =
            gamepad.startButton.wasPressedThisFrame;

        bool gamepadActivity =
            moved ||
            selectPressed ||
            cancelPressed ||
            startPressed ||
            gamepad.dpad.ReadValue().sqrMagnitude > 0.01f;

        // Register controller use even when Unity's normal EventSystem is
        // handling menu navigation. Previously, pressing a controller
        // button in the main menu could leave this cursor controller in
        // mouse mode until a mouse event occurred.
        if (!gamepadActivity)
            return;

        SwitchToGamepad();

        if (!IsCursorAllowedToBeVisible())
            return;

        if (moved)
        {
            MoveControllerCursor(stick);

            if (controllerDragActive)
            {
                DragControllerInventoryItem();
            }
        }

        if (selectPressed)
            SelectAtCursor();

        if (cancelPressed)
            CancelCurrentUI();
    }

    private void HandleMouseInput()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        bool mouseMoved =
            mouse.delta.ReadValue().sqrMagnitude > 0.01f;

        bool mouseClicked =
            mouse.leftButton.wasPressedThisFrame ||
            mouse.rightButton.wasPressedThisFrame ||
            mouse.middleButton.wasPressedThisFrame;

        if ((mouseMoved || mouseClicked) &&
            Time.unscaledTime >= ignoreMouseUntil)
        {
            SwitchToMouse();
        }

        if (!usingGamepad)
        {
            controllerCursorPosition =
                mouse.position.ReadValue();
        }
    }

    private void MoveControllerCursor(Vector2 stick)
    {
        float magnitude = stick.magnitude;

        if (magnitude <= stickDeadzone)
            return;

        float normalized =
            Mathf.InverseLerp(
                stickDeadzone,
                1f,
                magnitude
            );

        float speedMultiplier =
            Mathf.Lerp(
                0.25f,
                1f,
                normalized
            );

        controllerCursorPosition +=
            stick.normalized *
            controllerCursorSpeed *
            speedMultiplier *
            Mathf.Max(0.01f, stickAcceleration) *
            Time.unscaledDeltaTime;

        if (lockCursorToScreen)
        {
            controllerCursorPosition.x =
                Mathf.Clamp(
                    controllerCursorPosition.x,
                    0f,
                    Screen.width - 1f
                );

            controllerCursorPosition.y =
                Mathf.Clamp(
                    controllerCursorPosition.y,
                    0f,
                    Screen.height - 1f
                );
        }

        Mouse mouse = Mouse.current;

        if (mouse != null)
        {
            mouse.WarpCursorPosition(
                controllerCursorPosition
            );

            // Ignore the artificial mouse movement generated by
            // WarpCursorPosition so the controller doesn't immediately
            // switch itself back to mouse mode.
            ignoreMouseUntil =
                Time.unscaledTime + 0.08f;
        }
    }

    // =========================================================
    // DEVICE SWITCHING
    // =========================================================

    private void SwitchToGamepad()
    {
        if (!usingGamepad)
        {
            usingGamepad = true;

            if (Mouse.current != null)
            {
                controllerCursorPosition =
                    Mouse.current.position.ReadValue();
            }
        }

        // The custom controller cursor is the ONE controller UI input path.
        // Disable EventSystem navigation so InputSystemUIInputModule cannot
        // also submit the same South button. Without this, a controller press
        // could execute a Button.onClick twice.
        SyncEventSystemNavigation();

        ApplyCursorTexture();
        SyncCursorVisibility();

        GameInputDevice.SetLastUsedDevice(
            Gamepad.current
        );
    }

    private void SwitchToMouse()
    {
        if (!gameplayCursorLocked)
        {
            forcedHidden = false;
        }

        if (usingGamepad)
        {
            usingGamepad = false;
            ApplyCursorTexture();
        }

        SyncEventSystemNavigation();
        SyncCursorVisibility();

        GameInputDevice.SetLastUsedDevice(
            Mouse.current
        );
    }

    private void SyncEventSystemNavigation()
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
            return;

        if (!eventSystemNavigationStateSaved)
        {
            originalSendNavigationEvents =
                eventSystem.sendNavigationEvents;

            eventSystemNavigationStateSaved = true;
        }

        bool customControllerUIActive =
            usingGamepad &&
            IsCursorAllowedToBeVisible();

        eventSystem.sendNavigationEvents =
            customControllerUIActive
                ? false
                : originalSendNavigationEvents;
    }

    private void OnDeviceChange(
        InputDevice device,
        InputDeviceChange change)
    {
        // Connecting a controller must not steal control from the mouse.
    }

    // =========================================================
    // CURSOR
    // =========================================================

    private void ApplyCursorTexture()
    {
        if (cursorTexture != null)
        {
            Cursor.SetCursor(
                cursorTexture,
                cursorHotspot,
                CursorMode.Auto
            );
        }
    }

    private void SyncCursorVisibility()
    {
        bool cutsceneActive =
            GameStateManager.Instance != null &&
            GameStateManager.Instance.IsGameMode(
                GameStateManager.GameMode.Cutscene
            );

        bool mustHide =
            gameplayCursorLocked ||
            forcedHidden ||
            cutsceneActive;

        if (mustHide)
        {
            ForceCursorHiddenAndLocked();
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ApplyCursorTexture();
    }

    private bool IsCursorAllowedToBeVisible()
    {
        if (gameplayCursorLocked || forcedHidden)
        {
            return false;
        }

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.IsGameMode(
                GameStateManager.GameMode.Cutscene
            ))
        {
            return false;
        }

        if (hideWhenCursorIsLocked &&
            Cursor.lockState == CursorLockMode.Locked)
        {
            return false;
        }

        return true;
    }

    private void ForceCursorHiddenAndLocked()
    {
        // Lock FIRST, then hide. In the Editor Unity can temporarily restore
        // the OS cursor when the Game view changes focus. Re-applying both
        // values makes the gameplay state authoritative.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private IEnumerator ReassertGameplayCursorLock()
    {
        float endTime =
            Time.unscaledTime + 1.0f;

        while (Time.unscaledTime < endTime)
        {
            if (!gameplayCursorLocked)
            {
                cursorLockRoutine = null;
                yield break;
            }

            ForceCursorHiddenAndLocked();

            yield return null;
        }

        // Leave the gameplay lock itself active. The coroutine only exists
        // to survive the menu/button/focus transition.
        if (gameplayCursorLocked)
        {
            ForceCursorHiddenAndLocked();
        }

        cursorLockRoutine = null;
    }

#if UNITY_EDITOR
    private void FocusGameViewForControllerTesting()
    {
        // Gamepad input can continue reaching Play Mode even when the Unity
        // Game view itself is not focused. The native cursor cannot be locked
        // to an unfocused Game view, which is why a mouse click appeared to
        // "fix" the problem. This Editor-only focus step makes controller-only
        // testing behave like a standalone build.
        System.Type gameViewType =
            System.Type.GetType("UnityEditor.GameView,UnityEditor");

        if (gameViewType == null)
        {
            return;
        }

        UnityEditor.EditorWindow gameView =
            UnityEditor.EditorWindow.GetWindow(gameViewType);

        if (gameView != null)
        {
            gameView.Focus();
            ForceCursorHiddenAndLocked();
        }
    }
#endif

    private void LateUpdate()
    {
        bool cutsceneActive =
            GameStateManager.Instance != null &&
            GameStateManager.Instance.IsGameMode(
                GameStateManager.GameMode.Cutscene
            );

        if (gameplayCursorLocked ||
            cutsceneActive ||
            Time.unscaledTime < gameplayLockUntil)
        {
            ForceCursorHiddenAndLocked();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            return;
        }

        SyncCursorVisibility();
    }

    public void HideControllerCursor()
    {
        CancelControllerInventoryDrag();

        forcedHidden = true;

        ClearHover();

        ForceCursorHiddenAndLocked();
    }

    public void ShowControllerCursor()
    {
        gameplayCursorLocked = false;
        forcedHidden = false;
        gameplayLockUntil = 0f;

        if (cursorLockRoutine != null)
        {
            StopCoroutine(cursorLockRoutine);
            cursorLockRoutine = null;
        }

        SyncEventSystemNavigation();
        ApplyCursorTexture();
        SyncCursorVisibility();
    }

    public void ShowCursorForUI()
    {
        gameplayCursorLocked = false;
        forcedHidden = false;
        gameplayLockUntil = 0f;

        if (cursorLockRoutine != null)
        {
            StopCoroutine(cursorLockRoutine);
            cursorLockRoutine = null;
        }

        SyncEventSystemNavigation();
        ApplyCursorTexture();
        SyncCursorVisibility();
    }

    public void HideCursorForGameplay()
    {
        CancelControllerInventoryDrag();

        gameplayCursorLocked = true;
        forcedHidden = true;

        // Keep the lock authoritative while the UI button that started the
        // game finishes its EventSystem processing. This is especially
        // important when the game is started with a controller and no mouse
        // click ever focuses the Game view.
        gameplayLockUntil = Time.unscaledTime + 1.0f;

        ClearHover();
        SyncEventSystemNavigation();
        ForceCursorHiddenAndLocked();

        if (cursorLockRoutine != null)
        {
            StopCoroutine(cursorLockRoutine);
        }

        cursorLockRoutine =
            StartCoroutine(ReassertGameplayCursorLock());

#if UNITY_EDITOR
        // Controller input can reach Play Mode without the Game view being
        // the focused editor window. A mouse click normally fixes that, which
        // is why the old behaviour looked as if the cursor only disappeared
        // after mouse input. Explicitly focus the Game view instead.
        UnityEditor.EditorApplication.delayCall +=
            FocusGameViewForControllerTesting;
#endif
    }

    public void ResetControllerCursorForUI()
    {
        CancelControllerInventoryDrag();

        gameplayCursorLocked = false;
        forcedHidden = false;
        gameplayLockUntil = 0f;

        if (cursorLockRoutine != null)
        {
            StopCoroutine(cursorLockRoutine);
            cursorLockRoutine = null;
        }

        if (Mouse.current != null)
        {
            controllerCursorPosition =
                Mouse.current.position.ReadValue();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SyncEventSystemNavigation();
        ApplyCursorTexture();
        SyncCursorVisibility();
    }

    public bool IsUsingGamepad()
    {
        return usingGamepad;
    }

    public bool IsControllerCursorVisible()
    {
        return Cursor.visible && !forcedHidden;
    }

    // =========================================================
    // EVENT SYSTEM
    // =========================================================

    private void RefreshEventSystem()
    {
        EventSystem current =
            EventSystem.current;

        if (current == null)
        {
            pointerEventData = null;
            pointerEventSystem = null;
            hoveredObject = null;
            return;
        }

        if (pointerEventData == null ||
            pointerEventSystem != current)
        {
            pointerEventSystem = current;

            pointerEventData =
                new PointerEventData(current);

            hoveredObject = null;
            eventSystemNavigationStateSaved = false;
        }
    }

    private GameObject RaycastUI(
        Vector2 screenPosition)
    {
        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null ||
            pointerEventData == null)
        {
            return null;
        }

        pointerEventData.position =
            screenPosition;

        List<RaycastResult> results =
            new List<RaycastResult>();

        eventSystem.RaycastAll(
            pointerEventData,
            results
        );

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject == null)
                continue;

            Selectable selectable =
                results[i].gameObject
                    .GetComponentInParent<Selectable>();

            if (selectable != null &&
                selectable.interactable)
            {
                return selectable.gameObject;
            }
        }

        return null;
    }

    private void UpdateHover()
    {
        if (!usingGamepad ||
            !IsCursorAllowedToBeVisible())
        {
            return;
        }

        GameObject newHovered =
            RaycastUI(
                controllerCursorPosition
            );

        if (newHovered == hoveredObject)
        {
            if (newHovered != null &&
                EventSystem.current != null)
            {
                EventSystem.current
                    .SetSelectedGameObject(
                        newHovered
                    );
            }

            return;
        }

        if (hoveredObject != null &&
            pointerEventData != null)
        {
            ExecuteEvents.Execute(
                hoveredObject,
                pointerEventData,
                ExecuteEvents.pointerExitHandler
            );
        }

        hoveredObject = newHovered;

        if (hoveredObject != null &&
            EventSystem.current != null)
        {
            ExecuteEvents.Execute(
                hoveredObject,
                pointerEventData,
                ExecuteEvents.pointerEnterHandler
            );

            EventSystem.current
                .SetSelectedGameObject(
                    hoveredObject
                );
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current
                .SetSelectedGameObject(null);
        }
    }

    private void ClearHover()
    {
        if (hoveredObject != null &&
            pointerEventData != null)
        {
            ExecuteEvents.Execute(
                hoveredObject,
                pointerEventData,
                ExecuteEvents.pointerExitHandler
            );
        }

        hoveredObject = null;
    }

    // =========================================================
    // SELECT
    // =========================================================

    private void SelectAtCursor()
    {
        if (!IsCursorAllowedToBeVisible())
            return;

        if (controllerDragActive)
        {
            FinishControllerInventoryDrag();
            return;
        }

        InventorySlotUI inventorySlot =
            RaycastInventorySlot(controllerCursorPosition);

        if (inventorySlot != null)
        {
            InventoryItem inventoryItem =
                inventorySlot.GetComponentInChildren<InventoryItem>();

            if (inventoryItem != null &&
                inventoryItem.item != null)
            {
                BeginControllerInventoryDrag(
                    inventoryItem
                );

                return;
            }
        }

        GameObject target =
            RaycastUI(
                controllerCursorPosition
            );

        if (target == null ||
            pointerEventData == null)
        {
            return;
        }

        pointerEventData.button =
            PointerEventData.InputButton.Left;

        pointerEventData.pressPosition =
            controllerCursorPosition;

        pointerEventData.position =
            controllerCursorPosition;

        pointerEventData.clickCount = 1;

        // RaycastUI returns the Selectable's GameObject, so when the target
        // is a Button we can invoke its actual onClick event directly. This
        // prevents controller selection from only moving the UI cursor while
        // failing to execute actions such as PauseMenu.ContinueButton().
        Button button = target.GetComponent<Button>();

        if (button != null && button.interactable)
        {
            button.onClick.Invoke();
        }
        else
        {
            ExecuteEvents.Execute(
                target,
                pointerEventData,
                ExecuteEvents.pointerDownHandler
            );

            ExecuteEvents.Execute(
                target,
                pointerEventData,
                ExecuteEvents.pointerUpHandler
            );

            ExecuteEvents.Execute(
                target,
                pointerEventData,
                ExecuteEvents.pointerClickHandler
            );
        }

        if (EventSystem.current != null)
        {
            EventSystem.current
                .SetSelectedGameObject(target);
        }

        StartCoroutine(
            RefreshControllerCursorAfterSelection()
        );
    }

    private InventorySlotUI RaycastInventorySlot(
        Vector2 screenPosition)
    {
        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null ||
            pointerEventData == null)
        {
            return null;
        }

        pointerEventData.position =
            screenPosition;

        List<RaycastResult> results =
            new List<RaycastResult>();

        eventSystem.RaycastAll(
            pointerEventData,
            results
        );

        for (int i = 0; i < results.Count; i++)
        {
            GameObject hit =
                results[i].gameObject;

            if (hit == null)
                continue;

            InventorySlotUI slot =
                hit.GetComponentInParent<InventorySlotUI>();

            if (slot != null)
            {
                return slot;
            }
        }

        return null;
    }

    private void BeginControllerInventoryDrag(
        InventoryItem item)
    {
        if (item == null ||
            item.item == null ||
            item.image == null ||
            pointerEventData == null)
        {
            return;
        }

        controllerDraggedItem = item;
        controllerDragOriginalParent = item.transform.parent;
        controllerDragOriginalPosition = item.transform.position;
        controllerDragActive = true;

        pointerEventData.button =
            PointerEventData.InputButton.Left;
        pointerEventData.position =
            controllerCursorPosition;
        pointerEventData.pressPosition =
            controllerCursorPosition;
        pointerEventData.pointerPress = item.gameObject;
        pointerEventData.pointerDrag = item.gameObject;
        pointerEventData.pointerCurrentRaycast =
            CreateRaycastResult(item.gameObject);

        // IMPORTANT: use the same Unity event path as mouse drag-and-drop.
        // InventoryItem.OnBeginDrag is therefore still responsible for
        // moving the item out of its slot and disabling its raycast target.
        ExecuteEvents.Execute(
            item.gameObject,
            pointerEventData,
            ExecuteEvents.beginDragHandler
        );

        ExecuteEvents.Execute(
            item.gameObject,
            pointerEventData,
            ExecuteEvents.dragHandler
        );

        ClearHover();

        Debug.Log(
            "[GameUICursorController] Controller inventory drag started: " +
            item.gameObject.name
        );
    }

    private void DragControllerInventoryItem()
    {
        if (!controllerDragActive ||
            controllerDraggedItem == null ||
            pointerEventData == null)
        {
            return;
        }

        pointerEventData.position =
            controllerCursorPosition;

        pointerEventData.delta = Vector2.zero;

        // This calls InventoryItem.OnDrag, preserving the original
        // BeginDragHandler / IDragHandler behaviour.
        ExecuteEvents.Execute(
            controllerDraggedItem.gameObject,
            pointerEventData,
            ExecuteEvents.dragHandler
        );
    }

    private void FinishControllerInventoryDrag()
    {
        if (!controllerDragActive ||
            controllerDraggedItem == null ||
            pointerEventData == null)
        {
            return;
        }

        InventoryItem item = controllerDraggedItem;

        pointerEventData.position =
            controllerCursorPosition;
        pointerEventData.pointerDrag =
            item.gameObject;

        InventorySlotUI targetSlot =
            RaycastInventorySlot(controllerCursorPosition);

        if (targetSlot != null)
        {
            pointerEventData.pointerCurrentRaycast =
                CreateRaycastResult(targetSlot.gameObject);

            // This invokes InventorySlotUI.OnDrop, which in turn uses the
            // existing InventoryUI.HandleItemDrop / SwapSlots behaviour.
            ExecuteEvents.Execute(
                targetSlot.gameObject,
                pointerEventData,
                ExecuteEvents.dropHandler
            );
        }
        else
        {
            pointerEventData.pointerCurrentRaycast =
                new RaycastResult();
        }

        // Always finish through the original IEndDragHandler implementation.
        // If there was no valid drop target, InventoryItem.OnEndDrag restores
        // the item's original parent/position.
        ExecuteEvents.Execute(
            item.gameObject,
            pointerEventData,
            ExecuteEvents.endDragHandler
        );

        pointerEventData.pointerDrag = null;
        pointerEventData.pointerPress = null;

        controllerDraggedItem = null;
        controllerDragOriginalParent = null;
        controllerDragActive = false;

        Debug.Log(
            "[GameUICursorController] Controller inventory drag ended."
        );
    }

    private void CancelControllerInventoryDrag()
    {
        if (!controllerDragActive ||
            controllerDraggedItem == null ||
            pointerEventData == null)
        {
            controllerDraggedItem = null;
            controllerDragOriginalParent = null;
            controllerDragActive = false;
            return;
        }

        InventoryItem item = controllerDraggedItem;

        // B / Circle cancels the drag. Point the event back at the original
        // slot before ending the drag so InventoryItem.OnEndDrag restores it
        // exactly where it came from and no drop occurs.
        pointerEventData.position =
            controllerDragOriginalPosition;
        pointerEventData.pointerCurrentRaycast =
            CreateRaycastResult(
                controllerDragOriginalParent != null
                    ? controllerDragOriginalParent.gameObject
                    : item.gameObject
            );
        pointerEventData.pointerDrag = item.gameObject;

        ExecuteEvents.Execute(
            item.gameObject,
            pointerEventData,
            ExecuteEvents.endDragHandler
        );

        pointerEventData.pointerDrag = null;
        pointerEventData.pointerPress = null;

        controllerDraggedItem = null;
        controllerDragOriginalParent = null;
        controllerDragActive = false;

        Debug.Log(
            "[GameUICursorController] Controller inventory drag cancelled."
        );
    }

    private RaycastResult CreateRaycastResult(GameObject target)
    {
        RaycastResult result = new RaycastResult();
        result.gameObject = target;
        result.module = null;
        result.screenPosition = controllerCursorPosition;
        result.index = 0;
        result.depth = 0;
        result.sortingLayer = 0;
        result.sortingOrder = 0;
        return result;
    }

    private void RestoreControllerDraggedItem()
    {
        // Kept as a safety fallback for resets/scene changes. Normal
        // controller cancellation now goes through IEndDragHandler.
        if (controllerDraggedItem != null)
        {
            InventoryItem item = controllerDraggedItem;

            if (controllerDragOriginalParent != null)
            {
                item.transform.SetParent(
                    controllerDragOriginalParent
                );
            }

            item.transform.position =
                controllerDragOriginalPosition;

            if (item.image != null)
            {
                item.image.raycastTarget = true;
            }
        }

        if (pointerEventData != null)
        {
            pointerEventData.pointerDrag = null;
            pointerEventData.pointerPress = null;
        }

        controllerDraggedItem = null;
        controllerDragOriginalParent = null;
        controllerDragActive = false;
    }

    private IEnumerator RefreshControllerCursorAfterSelection()
    {
        yield return null;

        if (!usingGamepad)
            yield break;

        // If gameplay/cutscene has locked the cursor, hide it immediately.
        if (Cursor.lockState ==
            CursorLockMode.Locked)
        {
            forcedHidden = true;
            ClearHover();
            Cursor.visible = false;
            yield break;
        }

        // Check the UI again after the selected button has finished
        // opening/closing screens or starting the game.
        GameObject uiUnderCursor =
            RaycastUI(
                controllerCursorPosition
            );

        GameObject selected =
            EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

        // If there is no longer any selectable UI, the menu that the
        // controller was operating has closed. Hide the SAME native cursor.
        if (uiUnderCursor == null &&
            selected == null)
        {
            forcedHidden = true;

            ClearHover();

            Cursor.visible = false;
        }
        else
        {
            forcedHidden = false;
            ApplyCursorTexture();
            SyncCursorVisibility();
        }
    }

    // =========================================================
    // CANCEL
    // =========================================================

    private void CancelCurrentUI()
    {
        if (controllerDragActive)
        {
            CancelControllerInventoryDrag();
            return;
        }

        bool handled = false;

        SettingsMenu settings =
            FindFirstObjectByType<SettingsMenu>();

        if (settings != null &&
            settings.IsOpen())
        {
            settings.CloseButton();
            handled = true;
        }

        if (!handled)
        {
            PlayerInventory inventory =
                FindFirstObjectByType<PlayerInventory>();

            if (inventory != null &&
                inventory.IsInventoryOpen())
            {
                inventory.CloseInventory();
                handled = true;
            }
        }

        if (!handled)
        {
            PauseMenu pause =
                PauseMenu.Instance;

            if (pause != null &&
                pause.IsPaused())
            {
                pause.ContinueButton();
                handled = true;
            }
        }

        if (!handled &&
            EventSystem.current != null &&
            pointerEventData != null)
        {
            GameObject selected =
                EventSystem.current
                    .currentSelectedGameObject;

            if (selected != null)
            {
                ExecuteEvents.Execute(
                    selected,
                    pointerEventData,
                    ExecuteEvents.cancelHandler
                );

                handled = true;
            }
        }

        if (handled)
        {
            ClearHover();

            if (EventSystem.current != null)
            {
                EventSystem.current
                    .SetSelectedGameObject(null);
            }

            // Do not wait for a mouse click to update the cursor.
            // If the cancel action caused gameplay to begin, the next
            // frame's selection check and Cursor.lockState will hide it.
            StartCoroutine(
                RefreshControllerCursorAfterCancel()
            );
        }
    }

    private IEnumerator RefreshControllerCursorAfterCancel()
    {
        yield return null;

        if (!usingGamepad)
            yield break;

        if (Cursor.lockState ==
            CursorLockMode.Locked)
        {
            forcedHidden = true;
            Cursor.visible = false;
            ClearHover();
            yield break;
        }

        GameObject uiUnderCursor =
            RaycastUI(
                controllerCursorPosition
            );

        GameObject selected =
            EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

        if (uiUnderCursor == null &&
            selected == null)
        {
            forcedHidden = true;
            Cursor.visible = false;
            ClearHover();
        }
        else
        {
            forcedHidden = false;
            ApplyCursorTexture();
            SyncCursorVisibility();
        }
    }
}