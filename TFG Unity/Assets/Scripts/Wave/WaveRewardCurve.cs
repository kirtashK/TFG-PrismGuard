using UnityEngine;

/// <summary>
/// Define la recompensa en función del número de ola mediante una curva editable.
/// </summary>
[CreateAssetMenu(menuName = "Config/WaveRewardCurve", fileName = "WaveRewardCurve")]
public class WaveRewardCurve : ScriptableObject
{
    [Tooltip("X: Wave number; Y: Score reward")]
    public AnimationCurve rewardByWave = AnimationCurve.Linear(0, 0, 50, 500);
}