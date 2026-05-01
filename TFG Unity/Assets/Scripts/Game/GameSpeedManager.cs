using System;
using System.Collections;
using UnityEngine;

public class GameSpeedManager : MonoBehaviour
{
    public static GameSpeedManager Instance { get; private set; }

    public event Action<float, bool> OnGameSpeedChanged;

    [Header("Default Speeds")]
    [SerializeField] private float pausedSpeed = 0f;
    [SerializeField] private float normalSpeed = 1f;
    [SerializeField] private float fastSpeed = 2f;
    [SerializeField] private float veryFastSpeed = 3f;

    private float baseFixedDeltaTime;
    private float previousSpeedBeforePause = 1f;

    public bool IsPaused { get; private set; }
    public float CurrentSpeed { get; private set; } = 1f;

    #region Unity methods

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (InputManager.Instance == null)
        {
            yield return null;
        }

        InputManager.Instance.OnGameSpeedPausedRequested += TogglePause;
        InputManager.Instance.OnGameSpeedNormalRequested += HandleGameSpeedNormalRequested;
        InputManager.Instance.OnGameSpeedFastRequested += HandleGameSpeedFastRequested;
        InputManager.Instance.OnGameSpeedVeryFastRequested += HandleGameSpeedVeryFastRequested;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnGameSpeedPausedRequested -= TogglePause;
            InputManager.Instance.OnGameSpeedNormalRequested -= HandleGameSpeedNormalRequested;
            InputManager.Instance.OnGameSpeedFastRequested -= HandleGameSpeedFastRequested;
            InputManager.Instance.OnGameSpeedVeryFastRequested -= HandleGameSpeedVeryFastRequested;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SetGameSpeed(normalSpeed);
            Instance = null;
        }
    }

    #endregion

    private void HandleGameSpeedNormalRequested()
    {
        SetGameSpeed(normalSpeed);
    }

    private void HandleGameSpeedFastRequested()
    {
        SetGameSpeed(fastSpeed);
    }

    private void HandleGameSpeedVeryFastRequested()
    {
        SetGameSpeed(veryFastSpeed);
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