using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Unlock Structure Effect", menuName = "Data/Research/Effects/UnlockStructure")]
public class UnlockStructureEffect : ResearchEffect
{
    [Header("Unlock Structure")]

    [Tooltip("StructureData that will be unlocked when this research completes")]
    public StructureData structureData;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (string.IsNullOrEmpty(description))
        {
            description = $"Unlocks structure: {structureData.Name}";
            EditorUtility.SetDirty(this);
        }
        if (icon == null)
        {
            icon = structureData.icon;
            EditorUtility.SetDirty(this);
        }
    }

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
                    effectType = ResearchManager.ResearchEffectEvent.EffectType.UnlockStructure,
                    targetId = structureData.id,
                    effectId = id,
                }
            );
        }
    }
}