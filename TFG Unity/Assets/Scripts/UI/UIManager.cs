using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Tooltip("Referencia a WaveBannerController en escena")]
    public WaveBannerController waveBanner;

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
}
