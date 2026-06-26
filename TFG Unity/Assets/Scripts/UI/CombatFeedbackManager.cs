using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Type of combat feedback shown above a target when health changes.
/// </summary>
public enum CombatFeedbackType
{
    Damage,
    Healing
}

public interface ICombatFeedbackSource
{
    event Action<float, Vector3> OnDamageTakenCombatFeedbackEvent;
    event Action<float, Vector3> OnHealedCombatFeedbackEvent;

    /// <summary>
    /// World position used as the spawn anchor for floating feedback
    /// </summary>
    Vector3 CombatFeedbackPosition { get; }
}

public class CombatFeedbackManager : MonoBehaviour
{
    public static CombatFeedbackManager Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private CombatText combatTextPrefab;
    [SerializeField] private Transform combatTextParent;
    [SerializeField] private int poolPrewarmCount = 16;

    [Header("Icons")]
    [SerializeField] private Sprite damageIcon;
    [SerializeField] private Sprite healingIcon;

    [Header("Colors")]
    [SerializeField] private Color damageColor = new(0.92f, 0.24f, 0.22f, 1f);
    [SerializeField] private Color healingColor = new(0.24f, 0.84f, 0.33f, 1f);

    [Header("Spawn")]
    [SerializeField] private float minSpawnHeight = 1.2f;
    [SerializeField] private float maxSpawnHeight = 2.0f;
    [SerializeField] private float horizontalJitter = 0.35f;
    [SerializeField] private float originBiasMultiplier = 0.35f;

    private readonly Queue<CombatText> pool = new();
    private readonly Dictionary<ICombatFeedbackSource, SourceSubscription> subscriptions = new();

    private sealed class SourceSubscription
    {
        public Action<float, Vector3> DamageHandler;
        public Action<float, Vector3> HealHandler;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        PrewarmPool();
    }

    public void Register(ICombatFeedbackSource source)
    {
        if (source == null || subscriptions.ContainsKey(source))
        {
            return;
        }

        SourceSubscription subscription = new()
        {
            DamageHandler = (amount, origin) =>
            {
                ShowFeedback(source, amount, CombatFeedbackType.Damage, origin);
            },

            HealHandler = (amount, origin) =>
            {
                ShowFeedback(source, amount, CombatFeedbackType.Healing, origin);
            }
        };

        source.OnDamageTakenCombatFeedbackEvent += subscription.DamageHandler;
        source.OnHealedCombatFeedbackEvent += subscription.HealHandler;

        subscriptions.Add(source, subscription);
    }

    public void Unregister(ICombatFeedbackSource source)
    {
        if (source == null || !subscriptions.TryGetValue(source, out SourceSubscription subscription))
        {
            return;
        }

        if (subscription.DamageHandler != null)
        {
            source.OnDamageTakenCombatFeedbackEvent -= subscription.DamageHandler;
        }

        if (subscription.HealHandler != null)
        {
            source.OnHealedCombatFeedbackEvent -= subscription.HealHandler;
        }

        subscriptions.Remove(source);
    }

    private void PrewarmPool()
    {
        if (combatTextPrefab == null)
        {
            return;
        }

        for (int i = 0; i < poolPrewarmCount; i++)
        {
            CombatText combatText = CreateInstance();
            ReturnToPool(combatText);
        }
    }

    private CombatText CreateInstance()
    {
        Transform parent = combatTextParent != null ? combatTextParent : transform;
        CombatText instance = Instantiate(combatTextPrefab, parent);
        instance.gameObject.SetActive(false);
        instance.SetReturnCallback(ReturnToPool);
        return instance;
    }

    private CombatText GetFromPool()
    {
        if (pool.Count > 0)
        {
            CombatText instance = pool.Dequeue();
            instance.gameObject.SetActive(true);
            return instance;
        }

        CombatText created = CreateInstance();
        created.gameObject.SetActive(true);
        return created;
    }

    private void ReturnToPool(CombatText combatText)
    {
        if (combatText == null)
        {
            return;
        }

        combatText.gameObject.SetActive(false);
        pool.Enqueue(combatText);
    }

    private void ShowFeedback(ICombatFeedbackSource source, float amount, CombatFeedbackType feedbackType, Vector3 eventOrigin)
    {
        if (combatTextPrefab == null || source == null)
        {
            return;
        }

        if (amount <= 0f)
        {
            return;
        }

        CombatText combatText = GetFromPool();

        Vector3 spawnPosition = source.CombatFeedbackPosition;
        spawnPosition.y += UnityEngine.Random.Range(minSpawnHeight, maxSpawnHeight);

        Vector3 originBias = spawnPosition - eventOrigin;
        originBias.y = 0f;
        if (originBias.sqrMagnitude > 0.0001f)
        {
            originBias.Normalize();
            originBias *= originBiasMultiplier;
        }

        Vector2 randomHorizontalOffset = UnityEngine.Random.insideUnitCircle * horizontalJitter;
        Vector3 worldOffset = new(randomHorizontalOffset.x, 0f, randomHorizontalOffset.y);

        Sprite icon = feedbackType == CombatFeedbackType.Damage ? damageIcon : healingIcon;
        Color color = feedbackType == CombatFeedbackType.Damage ? damageColor : healingColor;

        combatText.Play(amount, feedbackType, icon,
            color,spawnPosition + originBias + worldOffset, cameraTransform: Camera.main != null ? Camera.main.transform : null);
    }
}