using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int currentScore = 0;
    public int CurrentScore => currentScore;

    /// <summary>
    /// Event that notifies new score after a change
    /// </summary>
    public event Action<int> OnScoreChanged;

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
            //DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Adds points to the score and notifies subscribers
    /// </summary>
    public void AddScore(int points)
    {
        if (points <= 0)
        {
            return;
        }
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);
    }

    public bool SpendScore(int cost)
    {
        if (currentScore < cost)
        {
            return false;
        }
        currentScore -= cost;
        OnScoreChanged?.Invoke(currentScore);
        return true;
    }
}
