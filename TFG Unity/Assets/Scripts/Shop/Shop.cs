using System.Collections;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [HideInInspector] public Structure structure;
    private StructureData data;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;

    private void Awake()
    {
        if (TryGetComponent<Structure>(out Structure structure))
        {
            this.structure = structure;
            data = structure.structureData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(structure)}");
        }

        CheckNullStats();

        if (data == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(data)}");
        }
    }

    private void Start()
    {
        RefreshStats();

        structure.currentHealth = structure.maxHealth;
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (StatModifierManager.Instance == null)
        {
            yield return null;
        }

        structure.OnDeathStartedEvent += OnDeathStarted;
        structure.OnDeathCleanupEvent += OnDeathCleanup;
        structure.OnDamageTakenEvent += OnDamageTaken;

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
    }

    private void OnDisable()
    {
        if (StatModifierManager.Instance != null)
        {
            StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
        }

        structure.OnDeathStartedEvent -= OnDeathStarted;
        structure.OnDeathCleanupEvent -= OnDeathCleanup;
        structure.OnDamageTakenEvent -= OnDamageTaken;
    }

    #region Stats

    private void CheckNullStats()
    {
        if (maxHealthStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(maxHealthStat)}");
        }
    }

    void HandleModifiersChanged(string targetId, string statKeyId)
    {
        if (targetId == data.id)
        {
            RefreshStats();
        }
        // Global modifier:
        else if (string.IsNullOrEmpty(targetId))
        {
            RefreshStats();
        }
    }

    public void RefreshStats()
    {
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxHealthStat, out float finalValue))
        {
            structure.maxHealth = finalValue;
        }
    }

    #endregion

    #region ITarget events

    private void OnDamageTaken(float amount, Vector3 attackOrigin)
    {

    }

    private void OnDeathStarted()
    {

    }

    private void OnDeathCleanup()
    {

    }

    #endregion
}
