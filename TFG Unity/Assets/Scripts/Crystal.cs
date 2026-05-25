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
    }

    private void OnDisable()
    {
        structure.OnDeathStartedEvent -= OnDeathStarted;
    }

    #endregion

    private void OnDeathStarted(ITarget deadTarget)
    {
        crystalDestroyed.SetActive(true);
        crystal.SetActive(false);

        // TrainingManager only exists on ML Agent training scene
        if (TrainingManager.Instance != null)
        {
            TrainingManager.Instance.NotifyEpisodeEnd();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.CrystalDestroyed();
        }
    }
}
