using System;
using System.Collections.Generic;
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

    // Reserved scores map
    private readonly Dictionary<Guid, int> reservations = new();
    private int reservedTotal = 0;

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

    public int GetAvailableScore()
    {
        return Mathf.Max(0, currentScore - reservedTotal);
    }

    /// <summary>
    /// Adds points to the score and notifies subscribers
    /// </summary>
    public void AddScore(int points)
    {
        points = Mathf.Abs(points);
        currentScore += points;
        OnScoreChanged?.Invoke(GetAvailableScore());
    }

    public bool CanSpendScore(int cost)
    {
        cost = Mathf.Abs(cost);
        if (currentScore < cost)
        {
            return false;
        }
        return true;
    }

    public bool SpendScore(int cost)
    {
        cost = Mathf.Abs(cost);
        if (!CanSpendScore(cost))
        {
            return false;
        }
        currentScore -= cost;
        OnScoreChanged?.Invoke(GetAvailableScore());
        return true;
    }

    /// <summary>
    /// Try to reserve score. Returns Guid token if success, Guid.Empty if failed
    /// Reservation does NOT deduct currentScore until CommitReservation is called
    /// </summary>
    public Guid ReserveScore(int amount)
    {
        if (amount <= 0)
        {
            return Guid.Empty;
        }

        int available = GetAvailableScore();
        if (available < amount)
        {
            return Guid.Empty;
        }

        Guid token = Guid.NewGuid();
        reservations[token] = amount;
        reservedTotal += amount;
        OnScoreChanged?.Invoke(GetAvailableScore());
        return token;
    }

    /// <summary>
    /// Commit a previously reserved token: 
    /// deducts the reserved amount from currentScore
    /// Returns true if successful
    /// </summary>
    public bool CommitReservation(Guid token)
    {
        if (token == Guid.Empty)
        {
            return false;
        }
        if (!reservations.TryGetValue(token, out int amount))
        {
            return false;
        }

        // Deduct from total score
        currentScore = Mathf.Max(0, currentScore - amount);

        // Remove reservation
        reservations.Remove(token);
        reservedTotal = Mathf.Max(0, reservedTotal - amount);

        OnScoreChanged?.Invoke(GetAvailableScore());
        return true;
    }

    /// <summary>
    /// Releases the reservation without consuming score
    /// </summary>
    public bool ReleaseReservation(Guid token)
    {
        if (token == Guid.Empty)
        {
            return false;
        }
        if (!reservations.TryGetValue(token, out int amount))
        {
            return false;
        }

        reservations.Remove(token);
        reservedTotal = Mathf.Max(0, reservedTotal - amount);
        OnScoreChanged?.Invoke(GetAvailableScore());
        return true;
    }
}