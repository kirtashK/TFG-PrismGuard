using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Research Data", menuName = "Data/Research/ResearchData")]
public class ResearchData : BaseData
{
    [Header("Research")]

    [TextArea(3, 8)]
    public string description;

    [Tooltip("Total research points required to complete this node")]
    public float requiredResearchPoints = 100f;

    [Tooltip("Base points awarded per bench cycle")]
    public float basePointsPerCycle = 10f;

    [Tooltip("ResearchDatas that must be researched before this research is availible")]
    public List<ResearchData> prerequisiteResearchDatas;

    [Tooltip("Effects that will be applied once this research completes")]
    public ResearchEffect[] effects;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
    }
#endif
}