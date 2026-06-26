using Unity.MLAgents.Sensors;
using UnityEngine;

public struct AbilityResult
{
    public bool wasExecuted;
    public int targetsHit;
    public float totalDamageDealt;
    public float totalHealingDone;
    public float healingWasted;
}

public abstract class HeroAbility : ScriptableObject
{
    public string abilityName;
    public Sprite icon;

    public float cooldown = 10f;

    public const int ObservationsPerSlot = 2;

    /// <summary>
    /// Adds ObservationsPerSlot observations to the sensor.
    /// Pad with zeros if fewer context is needed
    /// </summary>
    public abstract void CollectObservations(AgentSoldier agent, VectorSensor sensor);

    public abstract AbilityResult Execute(AgentSoldier agent, ITarget target);

    public abstract bool IsUsable(AgentSoldier agent, ITarget target);

    public abstract float CalculateReward(AgentSoldier agent, AbilityResult result);
}