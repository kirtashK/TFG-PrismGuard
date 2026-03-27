using System.Collections;
using UnityEngine;

public class Crystal : MonoBehaviour
{
    [HideInInspector] public Structure structure;


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

    private void OnDeathStarted()
    {
        Debug.Log($"{name} has been destroyed! Game over!");

        // TODO Move camera near crystal
        // TODO Change model to broken crystal
    }

    private void OnDeathCleanup()
    {
        GameManager.Instance.OnCrystalDestroyed();
    }
}
