using System.Collections.Generic;
using UnityEngine;

public class HideElementManager : MonoBehaviour
{
    public static HideElementManager Instance { get; private set; }

    private readonly HashSet<IHideElement> registered = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        registered.Clear();
    }

    public void Register(IHideElement element)
    {
        if (element == null)
        {
            return;
        }
        registered.Add(element);
    }

    public void Unregister(IHideElement element)
    {
        if (element == null)
        {
            return;
        }
        registered.Remove(element);
    }

    public void ShowOnly(IHideElement keepOpen)
    {
        if (registered.Count == 0)
        {
            return;
        }

        foreach (IHideElement element in new List<IHideElement>(registered))
        {
            if (element == null)
            {
                registered.Remove(element);
                continue;
            }

            // If keepOpen null, hide everything
            if (keepOpen == null)
            {
                element.HidePanel();
                continue;
            }

            // Skip keepOpen
            if (ReferenceEquals(element, keepOpen))
            {
                continue;
            }

            element.HidePanel();
        }
    }

    public void HideAll()
    {
        ShowOnly(null);
    }
}