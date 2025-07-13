using System;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Tooltip("Referencia a WaveBannerController en escena")]
    public WaveBannerController waveBanner;

    [Header("Contador de Enemigos")]
    public TMP_Text enemyCountText;
    [Tooltip("CanvasGroup dl texto de enemigos")]
    public CanvasGroup enemyCountGroup;

    // Evento que se dispara cuando termina una oleada
    public event Action<int> OnWaveCompleted;

    private int currentEnemiesAlive = 0;
    private int lastWaveStarted = 0;

    [Header("Puntuación")]
    [Tooltip("Texto para mostrar la puntuación actual")]
    public TMP_Text scoreText;

    private void Start()
    {
        // Suscribirse al evento de cambio de puntuacion
        ScoreManager.Instance.OnScoreChanged += UpdateScoreDisplay;
        
        // Mostrar valor inicial al iniciarse
        UpdateScoreDisplay(ScoreManager.Instance.CurrentScore);
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

    private void OnDestroy()
    {
        // Limpiar suscripción si se destruye UIManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
        }
    }


    private void UpdateScoreDisplay(int newScore)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {newScore}";
    }


    /// <summary>
    /// Mostrar el banner de oleada al iniciarse una oleada.
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
    /// Muestra el banner de ola completada con puntuación
    /// </summary>
    public void ShowWaveCompletedBanner(int waveNumber, int gainedScore)
    {
        if (waveBanner != null)
        {
            waveBanner.ShowText($"Wave {waveNumber} completed!" +
                $"\nPuntuación ganada: {gainedScore}");
        }
    }

    /// <summary>
    /// Mostrar tiempo restante para que empieze la siguiente oleada.
    /// </summary>
    public void ShowTimeUntilWaveBanner(int waveNumber, float timeRemaining)
    {
        if (waveBanner != null)
        {
            waveBanner.ShowText($"Wave {waveNumber} will start in {timeRemaining} seconds!");
        }
    }

    /// <summary>
    /// Cambia el contador de enemigos vivos (+1 o −1).
    /// Si queda al menos 1 enemigo, muestra el texto,
    /// si llega a 0, lo oculta
    /// </summary>
    public void ChangeEnemyCount(int delta)
    {
        currentEnemiesAlive = Mathf.Max(0, currentEnemiesAlive + delta);

        if (currentEnemiesAlive > 0)
        {
            enemyCountText.text = $"Enemigos con vida: {currentEnemiesAlive}";
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
                    // Disparar el evento indicando que oleada ha terminado
                    OnWaveCompleted?.Invoke(lastWaveStarted);

                    lastWaveStarted = 0;
                }
            }
        }
    }
}
