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

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Called when the crystal is destroyed
    /// Pauses the game and shows a defeat screen
    /// </summary>
    public void OnCrystalDestroyed()
    {
        finalScoreOnDefeat = ScoreManager.Instance.CurrentScore;

        if (GameSpeedManager.Instance != null)
        {
            GameSpeedManager.Instance.Pause();
        }
        else
        {
            Time.timeScale = 0f;
        }

        UIManager.Instance.ShowDefeatScreen(finalScoreOnDefeat);
    }

    public void GoToMainMenu()
    {
        if (GameSpeedManager.Instance != null)
        {
            GameSpeedManager.Instance.SetGameSpeed(1f);
        }
        else
        {
            Time.timeScale = 1f;
        }

        if (HideElementManager.Instance != null)
        {
            HideElementManager.Instance.HideAll();
        }

        IsDefeated = false;
        SceneManager.LoadScene("MainMenu");
    }

    public void NewGame()
    {
        IsDefeated = false;
        // TODO load new game scene
        Debug.LogWarning("NewGame() not implemented yet");
    }

    public void LoadGame()
    {
        IsDefeated = false;
        Debug.LogWarning("LoadGame() not implemented yet");
    }

    /// <summary>
    /// Loads the most recent save
    /// </summary>
    public void ContinueLastGame()
    {
        IsDefeated = false;
        // TODO load the most recent save 
        Debug.LogWarning("ContinueLastGame() not implemented yet");

        // TODO remove once proper load implemented
        SceneManager.LoadScene("SampleScene");
    }

    public void SaveGame()
    {
        // TODO save current game state
        Debug.LogWarning("SaveGame() not implemented yet");
    }
}