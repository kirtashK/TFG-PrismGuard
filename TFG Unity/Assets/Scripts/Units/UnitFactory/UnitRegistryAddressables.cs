using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UnitRegistryAddressables : MonoBehaviour
{
    [Tooltip("Label used in Addressables for UnitData")]
    public string unitsLabel = "Unit";

    public List<UnitData> allUnits = new();

    private AsyncOperationHandle<IList<UnitData>> loadHandle;
    private bool isLoaded = false;

    public event Action<List<UnitData>> OnUnitsLoaded;

    private void Start()
    {
        StartCoroutine(LoadUnitsCoroutine());
    }

    private IEnumerator LoadUnitsCoroutine()
    {
        if (isLoaded)
        {
            yield break;
        }

        loadHandle = Addressables.LoadAssetsAsync<UnitData>(
            unitsLabel,
            unitData => { /* per-item callback (currently null...) */ }
        );

        yield return loadHandle;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            allUnits = new List<UnitData>(loadHandle.Result);
            isLoaded = true;

            // Notify listeners
            OnUnitsLoaded?.Invoke(allUnits);
        }
        else
        {
            Debug.LogWarning($"{name}: failed to load UnitData addressables");
        }
    }

    private void OnDestroy()
    {
        if (isLoaded && loadHandle.IsValid())
        {
            Addressables.Release(loadHandle);
            allUnits.Clear();
            isLoaded = false;
        }
    }
}