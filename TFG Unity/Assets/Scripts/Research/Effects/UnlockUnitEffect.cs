using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Unlock Unit Effect", menuName = "Data/Research/Effects/UnlockUnit")]
public class UnlockUnitEffect : ResearchEffect
{
    [Header("Unlock Unit")]

    [Tooltip("UnitData that will be unlocked when this research completes")]
    public UnitData unitData;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (string.IsNullOrEmpty(description))
        {
            description = $"Unlocks unit: {unitData.Name}";
            EditorUtility.SetDirty(this);
        }
        if (icon == null)
        {
            icon = unitData.icon;
            EditorUtility.SetDirty(this);
        }
    }
#endif

    public override void ApplyEffect(string researchId)
    {
        // Notify via ResearchManager event:
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.NotifyEffectApplied
            (
                new ResearchManager.ResearchEffectEvent
                {
                    researchId = researchId,
                    effectType = ResearchManager.ResearchEffectEvent.EffectType.UnlockUnit,
                    targetId = unitData.id,
                    effectId = id,
                }
            );
        }
    }
}