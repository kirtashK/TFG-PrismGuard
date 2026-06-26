using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Data/Enemy")]
public class EnemyData : UnitData
{
    [Header("Enemy")]

    [Header("Wave")]

    [Tooltip("Cost to spawn this enemy, its also the score added once defeated")]
    [Range(0f, 1000f)]
    public int spawnCost = 5;

    [Header("Rewards")]

    [Tooltip("Experience added to the soldier that killed this enemy")]
    [Range(0f, 1000f)]
    public int rewardExperience = 2;

    [Header("Guard")]

    [Tooltip("Radius to guard")]
    [Range(0f, 50f)]
    public float guardRadius = 8f;

    [Tooltip("Extra buffer allowed beyond guardRadius while chasing")]
    [Range(0f, 50f)]
    public float guardChaseBuffer = 3f;

    [Tooltip("Minimum wait time between patrol in seconds")]
    [Range(0f, 50f)]
    public float patrolDelayMin = 1f;

    [Tooltip("Maximum wait time between patrol in seconds")]
    [Range(0f, 50f)]
    public float patrolDelayMax = 3f;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        faction = Faction.Enemy;
        isPlayerControllable = false;
    }
#endif
}