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

    /// <summary>
    /// Mostrar el banner de oleada al iniciarse una oleada.
    /// </summary>
    public void ShowWaveBanner(int waveNumber)
    {
        if (waveBanner != null)
        {
            lastWaveStarted = waveNumber;
            waveBanner.Show($"Wave {lastWaveStarted} has started");
        }
    }

    /// <summary>
    /// Cambia el contador de enemigos vivos (+1 o −1).
    /// Si queda al menos 1 enemigo, muestra el texto,
    /// si llega a 0, lo oculta
    /// y muestra texto indicando que la oleada ha acabado
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
                    waveBanner.Show($"Wave {lastWaveStarted} completed");

                    // Disparar el evento indicando que oleada ha terminado
                    OnWaveCompleted?.Invoke(lastWaveStarted);

                    lastWaveStarted = 0;
                }
            }
        }
    }
}
