using UnityEngine;

[System.Serializable]
public struct WaveEntry
{
    public GameObject enemyPrefab;

    public EnemyData enemyData;

    [Tooltip("Numero de instancias a spawnear, solo para testing")]
    public int count;

    public float spawnDelay;
}
