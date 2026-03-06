using UnityEngine;

public abstract class BaseData : ScriptableObject
{
    [Header("Base")]

    public string Name;
    public string id;
    public Sprite icon;

    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(Name))
        {
            Debug.LogWarning($"{name} missing {nameof(Name)}");
        }

        id = Name;
    }
}