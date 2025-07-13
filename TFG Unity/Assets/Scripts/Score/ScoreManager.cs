using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int currentScore = 0;
    public int CurrentScore => currentScore;

    /// <summary>
    /// Evento que notifica la nueva puntuación tras un cambio.
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
    /// Añade puntos a la puntuación y notifica a los suscriptores.
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

    /// <summary>
    /// Permite gastar puntos, usado por la tienda por el jugador.
    /// </summary>
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
