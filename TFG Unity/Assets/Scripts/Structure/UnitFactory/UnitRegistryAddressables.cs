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

    public List<UnitData> loadedUnits = new();

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
            unitData => {  }
        );

        yield return loadHandle;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            loadedUnits = new List<UnitData>(loadHandle.Result);
            isLoaded = true;

            // Notify listeners
            OnUnitsLoaded?.Invoke(loadedUnits);
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
            loadedUnits.Clear();
            isLoaded = false;
        }
    }
}