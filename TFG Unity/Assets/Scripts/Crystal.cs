using System.Collections;
using UnityEngine;

public class Crystal : MonoBehaviour
{
    [HideInInspector] public Structure structure;

    [SerializeField] private GameObject crystal;
    [SerializeField] private GameObject crystalDestroyed;

    #region Unity methods

    private void Awake()
    {
        if (TryGetComponent<Structure>(out Structure structure))
        {
            this.structure = structure;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(structure)}");
        }
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
    }

    private void OnDisable()
    {
        structure.OnDeathStartedEvent -= OnDeathStarted;
        structure.OnDeathCleanupEvent -= OnDeathCleanup;
    }

    #endregion

    private void OnDeathStarted(ITarget deadTarget)
    {
        crystalDestroyed.SetActive(true);
        crystal.SetActive(false);

        GameManager.Instance.CrystalDestroyed();
    }

    private void OnDeathCleanup()
    {
        
    }
}
