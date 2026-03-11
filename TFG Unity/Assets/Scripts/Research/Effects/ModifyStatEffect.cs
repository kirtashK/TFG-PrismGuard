using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "Modify Stat Effect", menuName = "Data/Research/Effects/ModifyStat")]
public class ModifyStatEffect : ResearchEffect
{
    public enum ModifierType
    {
        Additive,
        Multiplicative
    }

    [Header("ModifyStat")]

    [Tooltip("Optional: specific data this effect targets, effect is global otherwise")]
    public BaseData targetData;

    [Tooltip("Stat to modify")]
    public StatKey statKey;

    [Header("Modifier")]
    public ModifierType modifierType = ModifierType.Additive;
    public float value = 0f;

    protected override void OnValidate()
    {
        base.OnValidate();

        Name = targetData != null ? $"Modify {targetData.Name} : {statKey.name}" : $"Modify {statKey.name}";
        description = $"Modifies {(statKey == null ? "[stat]" : statKey.Name)} of {(targetData != null ? targetData.Name : "global")} ({modifierType} {value})";
    }

    public override void ApplyEffect(string researchId)
    {
        if (StatModifierManager.Instance == null)
        {
            Debug.LogError($"{name}: {nameof(StatModifierManager)} is null");
            return;
        }

        StatModifierManager.ModifierKind kind = modifierType == ModifierType.Additive ? StatModifierManager.ModifierKind.Additive : StatModifierManager.ModifierKind.Multiplicative;
        
        // Convert percent to decimal
        float value = this.value;
        if (kind == StatModifierManager.ModifierKind.Multiplicative)
        {
            value /= 100f;
        }

        string targetId = "";
        // TargetData null means its global
        if (targetData != null)
        {
            targetId = targetData.id;
        }

        StatModifierManager.Instance.AddModifier(id, targetId, statKey, kind, value);
    }
}