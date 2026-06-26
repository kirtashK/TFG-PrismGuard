using UnityEngine;

public abstract class ResearchEffect : BaseData
{
    [Header("Research effect")]

    [TextArea(2, 6)]
    public string description;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (icon == null)
        {
            Debug.LogWarning($"{name} missing {nameof(icon)}");
        }
        if (string.IsNullOrWhiteSpace(description))
        {
            Debug.LogWarning($"{name} missing {nameof(description)}");
        }
    }
#endif

    /// <summary>
    /// Called when the research of this effect completes
    /// </summary>
    public abstract void ApplyEffect(string researchId);

    /// <summary>
    /// Returns display name for UI
    /// </summary>
    public virtual string GetName()
    {
        return Name;
    }

    /// <summary>
    /// Returns description for UI
    /// </summary>
    public virtual string GetDescription()
    {
        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        return "";
    }
}