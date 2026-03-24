using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    [Tooltip("Prefab used to show move destination")]
    public GameObject moveMarkerPrefab;

    public float markerDuration = 5f;

    private GameObject activeMarker;
    private Coroutine hideMarkerCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void IssueMoveOrder(IEnumerable<IOrderable> units, Vector3 target, MoveOrderOptions options)
    {

        if (units == null)
        {
            return;
        }

        List<IOrderable> list = units.Where(unit => unit != null).ToList();
        if (list.Count == 0)
        {
            return;
        }

        ShowMoveMarker(target);

        List<Vector3> offsets = FormationHelper.CalculateGridOffsets(list.Count, options.formationSpacing);

        for (int i = 0; i < list.Count; i++)
        {
            IOrderable unit = list[i];
            Vector3 destination = target + offsets[i];

            unit.ReceiveMoveOrder(destination, options);
        }
    }

    private void ShowMoveMarker(Vector3 position)
    {
        if (moveMarkerPrefab == null)
        {
            return;
        }

        // Make it float a bit so it doesnt mix with the floor
        position.y += 0.05f;

        // If marker doesn't exist yet, instantiate it
        if (activeMarker == null)
        {
            activeMarker = Instantiate(moveMarkerPrefab, position, moveMarkerPrefab.transform.rotation);
        }
        else
        {
            activeMarker.transform.position = position;
            activeMarker.SetActive(true);
        }

        // Restart hide coroutine
        if (hideMarkerCoroutine != null)
        {
            StopCoroutine(hideMarkerCoroutine);
        }
        hideMarkerCoroutine = StartCoroutine(HideMarkerAfterDelay(markerDuration));
    }

    private System.Collections.IEnumerator HideMarkerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (activeMarker != null)
        {
            activeMarker.SetActive(false);
        }

        hideMarkerCoroutine = null;
    }
}

// Helper to make a formation
public static class FormationHelper
{
    public static List<Vector3> CalculateGridOffsets(int count, float spacing)
    {
        List<Vector3> offsets = new(count);
        if (count <= 0)
        {
            return offsets;
        }

        int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / columns);

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int column = i % columns;
            float offsetX = (column - (columns - 1) / 2.0f) * spacing;
            float offsetZ = (row - (rows - 1) / 2.0f) * spacing;
            offsets.Add(new Vector3(offsetX, 0f, offsetZ));
        }

        return offsets;
    }
}