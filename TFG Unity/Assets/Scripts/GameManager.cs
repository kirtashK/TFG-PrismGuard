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
    /// Called when the crystal is destroyed
    /// Pauses the game and shows a defeat screen
    /// </summary>
    public void OnCrystalDestroyed()
    {
        finalScoreOnDefeat = ScoreManager.Instance.CurrentScore;

        // Pause game
        Time.timeScale = 0f;

        UIManager.Instance.ShowDefeatScreen(finalScoreOnDefeat);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadGame()
    {
        Debug.LogWarning("LoadGame() not implemented yet");
    }
}