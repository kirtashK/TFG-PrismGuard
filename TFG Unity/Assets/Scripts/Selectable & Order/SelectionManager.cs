using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    private readonly HashSet<ISelectable> allSelectables = new();

    private readonly List<ISelectable> selected = new();

    public event Action<IReadOnlyList<ISelectable>> OnSelectionChanged;

    public IReadOnlyCollection<ISelectable> AllSelectables => allSelectables;

    public IReadOnlyList<ISelectable> CurrentSelection => selected.AsReadOnly();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterSelectable(ISelectable selectable)
    {
        if (selectable == null)
        {
            return;
        }

        allSelectables.Add(selectable);
    }

    public void UnregisterSelectable(ISelectable selectable)
    {
        if (selectable == null)
        {
            return;
        }

        allSelectables.Remove(selectable);

        if (selected.Remove(selectable))
        {
            selectable.OnDeselected();
            OnSelectionChanged?.Invoke(selected.AsReadOnly());
        }
    }

    public void Select(ISelectable selectable, bool additive = false)
    {
        if (selectable == null)
        {
            if (!additive)
            {
                ClearSelection();
            }
            return;
        }

        if (!additive)
        {
            ClearSelection();
        }

        if (!selected.Contains(selectable))
        {
            selected.Add(selectable);
            selectable.OnSelected();
            OnSelectionChanged?.Invoke(selected.AsReadOnly());
        }
    }

    public void Select(IEnumerable<ISelectable> items, bool additive = false)
    {
        if (!additive)
        {
            ClearSelection();
        }

        bool changed = false;
        foreach (ISelectable selectable in items)
        {
            if (selectable != null && !selected.Contains(selectable))
            {
                selected.Add(selectable);
                selectable.OnSelected();
                changed = true;
            }
        }

        if (changed)
        {
            OnSelectionChanged?.Invoke(selected.AsReadOnly());
        }
    }

    public void Deselect(ISelectable selectable)
    {
        if (selectable == null)
        {
            return;
        }

        if (selected.Remove(selectable))
        {
            selectable.OnDeselected();
            OnSelectionChanged?.Invoke(selected.AsReadOnly());
        }
    }

    public void ClearSelection()
    {
        if (selected.Count == 0)
        {
            return;
        }

        foreach (ISelectable selectable in new List<ISelectable>(selected))
        {
            selectable.OnDeselected();
        }

        selected.Clear();
        OnSelectionChanged?.Invoke(selected.AsReadOnly());
    }
}