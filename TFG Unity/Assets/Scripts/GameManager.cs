using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private int finalScoreOnDefeat;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
        //DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Llamado cuando el cristal es destruido
    /// Pausa el juego y muestra la pantalla de derrota
    /// </summary>
    public void OnCrystalDestroyed()
    {
        finalScoreOnDefeat = ScoreManager.Instance.CurrentScore;

        // Pausar el juego
        Time.timeScale = 0f;

        UIManager.Instance.ShowDefeatScreen(finalScoreOnDefeat);
    }

    /// <summary>
    /// Handler para el boton "Main Menu" en la pantalla de derrota
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Handler para el boton "Load Game"
    /// </summary>
    public void LoadGame()
    {
        Debug.LogWarning("LoadGame() not implemented yet");
    }
}
