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

    private int currentEnemiesAlive = 0;

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
    /// Mostrar el banner de oleada.
    /// </summary>
    public void ShowWaveBanner(int waveNumber)
    {
        if (waveBanner != null)
        {
            waveBanner.Show(waveNumber);
        }
    }

    /// <summary>
    /// Cambia el contador de enemigos vivos (+1 o −1).
    /// Si queda al menos 1 enemigo, muestra el texto,
    /// si llega a 0, lo oculta.
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
        }
    }
}
