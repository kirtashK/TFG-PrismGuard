using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public WaveBannerController waveBanner;

    public TMP_Text enemyCountText;

    public CanvasGroup enemyCountGroup;

    // Event fired whenever a wave finishes
    public event Action<int> OnWaveCompleted;

    private int currentEnemiesAlive = 0;
    private int lastWaveStarted = 0;

    [Header("Score")]
    public TMP_Text scoreText;

    [Header("Defead screen")]
    public GameObject defeatPanel;
    public TMP_Text defeatScoreText;

    private void Start()
    {   
        // Initial values
        UpdateScoreDisplay(ScoreManager.Instance.CurrentScore);

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenScoreManagerReady());
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
        }
    }

    private IEnumerator RegisterWhenScoreManagerReady()
    {
        while (ScoreManager.Instance == null)
        {
            yield return null;
        }

        ScoreManager.Instance.OnScoreChanged += UpdateScoreDisplay;
    }

    private void UpdateScoreDisplay(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {newScore}";
        }
    }

    /// <summary>
    /// Shows defeat screen with the obtained score
    /// </summary>
    public void ShowDefeatScreen(int finalScore)
    {
        if (defeatScoreText != null)
        {
            defeatScoreText.text = $"Score obtained: {finalScore}";
        }
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
        }
    }


    /// <summary>
    /// Show wave banner when a new wave starts
    /// </summary>
    public void ShowWaveStartedBanner(int waveNumber)
    {
        if (waveBanner != null)
        {
            lastWaveStarted = waveNumber;
            waveBanner.ShowText($"Wave {lastWaveStarted} has started");
        }
    }

    /// <summary>
    /// Shows wave banner completed along with the score gained
    /// </summary>
    public void ShowWaveCompletedBanner(int waveNumber, int gainedScore, int waveScoreReward)
    {
        if (waveBanner != null)
        {
            waveBanner.ShowText($"Wave {waveNumber} completed!" +
                $"\nScore gained this wave: {gainedScore}" +
                $"\nReward for completing the wave: {waveScoreReward}");
        }
    }

    /// <summary>
    /// Show remaining time till next wave
    /// </summary>
    public void ShowTimeUntilWaveBanner(int waveNumber, float timeRemaining)
    {
        if (waveBanner != null)
        {
            waveBanner.ShowText($"Wave {waveNumber} will start in {timeRemaining} seconds!");
        }
    }

    /// <summary>
    /// Changes enemy counter (+1 or −1).
    /// If there is at least 1 enemy, shows the text,
    /// hides it otherwise
    /// </summary>
    public void ChangeEnemyCount(int delta)
    {
        currentEnemiesAlive = Mathf.Max(0, currentEnemiesAlive + delta);

        if (currentEnemiesAlive > 0)
        {
            enemyCountText.text = $"Enemies active: {currentEnemiesAlive}";
            enemyCountGroup.alpha = 1f;
            enemyCountGroup.blocksRaycasts = true;
        }
        else
        {
            enemyCountGroup.alpha = 0f;
            enemyCountGroup.blocksRaycasts = false;

            if (lastWaveStarted > 0)
            {
                if (waveBanner != null)
                {
                    // Fire event about wave completed
                    OnWaveCompleted?.Invoke(lastWaveStarted);

                    lastWaveStarted = 0;
                }
            }
        }
    }
}