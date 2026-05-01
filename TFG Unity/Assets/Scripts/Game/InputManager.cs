using System;
using System.Collections.Generic;
using UnityEngine;
using static InputSystem_Actions;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public enum InputMode
    {
        Gameplay,
        UI,
        Construction,
        PauseMenu,
    }

    [Header("Default")]
    [SerializeField] private InputMode defaultMode = InputMode.Gameplay;

    public event Action OnPauseToggleRequested;
    public event Action OnDebugToggleRequested;

    public event Action OnGameSpeedPausedRequested;
    public event Action OnGameSpeedNormalRequested;
    public event Action OnGameSpeedFastRequested;
    public event Action OnGameSpeedVeryFastRequested;

    public event Action<InputMode> OnInputModeChanged;

    private InputSystem_Actions inputActions;
    private readonly Stack<InputMode> modeStack = new();

    public GlobalActions Global => inputActions.Global;
    public UIActions UI => inputActions.UI;
    public GameplayActions Gameplay => inputActions.Gameplay;
    public CameraActions Camera => inputActions.Camera;
    public ConstructionActions Construction => inputActions.Construction;

    public InputMode CurrentMode { get; private set; }

    #region Unity methods

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        inputActions = new InputSystem_Actions();
        ApplyMode(defaultMode);
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            ApplyMode(CurrentMode);
        }
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        inputActions?.Dispose();
    }

    private void Update()
    {
        if (inputActions == null)
        {
            return;
        }

        ProcessGlobalInput();
    }

    #endregion

    public Vector2 PointerPosition
    {
        get
        {
            return Global.Pointer.ReadValue<Vector2>();
        }
    }

    private void ProcessGlobalInput()
    {
        if (Global.DebugToggle.WasPressedThisFrame())
        {
            OnDebugToggleRequested?.Invoke();
        }

        if (CurrentMode == InputMode.Gameplay)
        {
            if (Global.PauseMenuToggle.WasPressedThisFrame())
            {
                OnPauseToggleRequested?.Invoke();
            }
        }

        if (CurrentMode == InputMode.Gameplay || CurrentMode == InputMode.Construction)
        {
            if (Global.GameSpeedPaused.WasPressedThisFrame())
            {
                OnGameSpeedPausedRequested?.Invoke();
            }

            if (Global.GameSpeedNormal.WasPressedThisFrame())
            {
                OnGameSpeedNormalRequested?.Invoke();
            }

            if (Global.GameSpeedFast.WasPressedThisFrame())
            {
                OnGameSpeedFastRequested?.Invoke();
            }

            if (Global.GameSpeedVeryFast.WasPressedThisFrame())
            {
                OnGameSpeedVeryFastRequested?.Invoke();
            }
        }
    }

    public void SetMode(InputMode mode)
    {
        modeStack.Clear();
        ApplyMode(mode);
    }

    public void PushMode(InputMode mode)
    {
        if (CurrentMode == mode)
        {
            return;
        }

        modeStack.Push(CurrentMode);
        ApplyMode(mode);
    }

    public void PopMode()
    {
        if (modeStack.Count > 0)
        {
            ApplyMode(modeStack.Pop());
            return;
        }

        ApplyMode(defaultMode);
    }

    private void ApplyMode(InputMode mode)
    {
        CurrentMode = mode;

        if (inputActions == null)
        {
            return;
        }

        inputActions.Global.Enable();

        inputActions.UI.Disable();
        inputActions.Gameplay.Disable();
        inputActions.Camera.Disable();
        inputActions.Construction.Disable();

        switch (mode)
        {
            case InputMode.Gameplay:
                inputActions.Gameplay.Enable();
                inputActions.Camera.Enable();
                break;

            case InputMode.UI:
                inputActions.UI.Enable();
                inputActions.Camera.Enable();
                break;

            case InputMode.Construction:
                inputActions.Construction.Enable();
                inputActions.Camera.Enable();
                break;

            case InputMode.PauseMenu:
                inputActions.UI.Enable();
                break;
        }

        OnInputModeChanged?.Invoke(CurrentMode);
    }
}