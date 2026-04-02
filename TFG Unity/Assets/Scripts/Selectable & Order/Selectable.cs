using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class Selectable : MonoBehaviour, ISelectable
{
    [HideInInspector]
    public BaseData data;

    [Tooltip("Name shown in UI")]
    public string displayName;

    [Tooltip("Icon shown in UI")]
    public Sprite icon;

    [Tooltip("Prefab to show as visual when selected")]
    public GameObject selectionVisual;

    [Tooltip("Information about the selected unit/structure")]
    public GameObject InformationVisual;

    private GameObject instantiatedInformationVisual;

    private void Start()
    {
        if (data != null)
        {
            displayName = data.Name;
            icon = data.icon;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(BaseData)}");
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (SelectionManager.Instance == null)
        {
            yield return null;
        }

        SelectionManager.Instance.RegisterSelectable(this);
    }

    private void OnDisable()
    {
        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.UnregisterSelectable(this);
        }

        // Clean visuals
        if (instantiatedInformationVisual != null)
        {
            Destroy(instantiatedInformationVisual);
            instantiatedInformationVisual = null;
        }
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName))
        {
            return displayName;
        }
        return gameObject.name;
    }

    public Sprite GetIcon()
    {
        return icon;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void OnSelected()
    {
        if (selectionVisual != null)
        {
            selectionVisual.SetActive(true);
        }

        if (InformationVisual != null)
        {
            if (instantiatedInformationVisual == null)
            {
                instantiatedInformationVisual = Instantiate(InformationVisual, transform, worldPositionStays: false);
            }
            instantiatedInformationVisual.SetActive(true);
        }
    }

    public void OnDeselected()
    {
        if (selectionVisual != null)
        {
            selectionVisual.SetActive(false);
        }

        if (instantiatedInformationVisual != null)
        {
            instantiatedInformationVisual.SetActive(false);
        }
    }

    public object GetSelectionData()
    {
        return this;
    }
}