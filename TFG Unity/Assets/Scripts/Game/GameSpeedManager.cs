using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameSpeedManager : MonoBehaviour
{
    public static GameSpeedManager Instance { get; private set; }

    public event Action<float, bool> OnGameSpeedChanged;

    [Header("Default Speeds")]
    [SerializeField] private float pausedSpeed = 0f;
    [SerializeField] private float normalSpeed = 1f;
    [SerializeField] private float fastSpeed = 2f;
    [SerializeField] private float veryFastSpeed = 3f;

    private InputAction pauseAction;
    private InputAction normalSpeedAction;
    private InputAction fastSpeedAction;
    private InputAction veryFastSpeedAction;

    private float baseFixedDeltaTime;
    private float previousSpeedBeforePause = 1f;

    public bool IsPaused { get; private set; }
    public float CurrentSpeed { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        baseFixedDeltaTime = Time.fixedDeltaTime;

        pauseAction = new InputAction("PauseGame", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/space");
        normalSpeedAction = new InputAction("GameSpeed1x", InputActionType.Button);
        normalSpeedAction.AddBinding("<Keyboard>/1");
        fastSpeedAction = new InputAction("GameSpeed2x", InputActionType.Button);
        fastSpeedAction.AddBinding("<Keyboard>/2");
        veryFastSpeedAction = new InputAction("GameSpeed3x", InputActionType.Button);
        veryFastSpeedAction.AddBinding("<Keyboard>/3");
    }

    private void OnEnable()
    {
        pauseAction?.Enable();
        normalSpeedAction?.Enable();
        fastSpeedAction?.Enable();
        veryFastSpeedAction?.Enable();
    }

    private void OnDisable()
    {
        pauseAction?.Disable();
        normalSpeedAction?.Disable();
        fastSpeedAction?.Disable();
        veryFastSpeedAction?.Disable();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SetGameSpeed(1f);
            Instance = null;
        }

        pauseAction?.Dispose();
        normalSpeedAction?.Dispose();
        fastSpeedAction?.Dispose();
        veryFastSpeedAction?.Dispose();
    }

    private void Update()
    {
        if (pauseAction != null && pauseAction.WasPressedThisFrame())
        {
            TogglePause();
        }

        if (normalSpeedAction != null && normalSpeedAction.WasPressedThisFrame())
        {
            SetGameSpeed(normalSpeed);
        }
        else if (fastSpeedAction != null && fastSpeedAction.WasPressedThisFrame())
        {
            SetGameSpeed(fastSpeed);
        }
        else if (veryFastSpeedAction != null && veryFastSpeedAction.WasPressedThisFrame())
        {
            SetGameSpeed(veryFastSpeed);
        }
    }

    /// <summary>
    /// Toggles between paused and the last speed
    /// </summary>
    public void TogglePause()
    {
        if (IsPaused)
        {
            ResumePreviousSpeed();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }

        previousSpeedBeforePause = CurrentSpeed > pausedSpeed ? CurrentSpeed : normalSpeed;
        IsPaused = true;
        ApplySpeed(pausedSpeed);
    }

    /// <summary>
    /// Resumes gameplay using the speed that was active before pausing
    /// </summary>
    public void ResumePreviousSpeed()
    {
        if (!IsPaused)
        {
            return;
        }

        float speedToRestore = previousSpeedBeforePause > pausedSpeed ? previousSpeedBeforePause : normalSpeed;
        IsPaused = false;
        ApplySpeed(speedToRestore);
    }

    public void SetGameSpeed(float speed)
    {
        if (speed <= pausedSpeed)
        {
            Pause();
            return;
        }

        previousSpeedBeforePause = speed;
        IsPaused = false;
        ApplySpeed(speed);
    }

    private void ApplySpeed(float speed)
    {
        CurrentSpeed = speed;
        Time.timeScale = speed;
        Time.fixedDeltaTime = baseFixedDeltaTime * speed;

        OnGameSpeedChanged?.Invoke(CurrentSpeed, IsPaused);
    }
}