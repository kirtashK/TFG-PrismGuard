using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Configuración de Oleadas")]
    [Tooltip("Definición de cada entrada en la ola")]
    public List<WaveEntry> waveEntries = new List<WaveEntry>();

    [Tooltip("Punto de salida de los enemigos")]
    public Transform spawnPoint;

    [Tooltip("Tiempo en segundos entre oleadas (la primera oleada sale al inicio)")]
    public float waveInterval = 100f;

    private int waveCount = 0;

    private void Start()
    {
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        // TODO modificar para que no haya primera oleada inmediata
        // Primera ola inmediata, para poder testear
        yield return SpawnWave();

        while (true)
        {
            yield return new WaitForSeconds(waveInterval);
            yield return SpawnWave();
        }
    }

    private IEnumerator SpawnWave()
    {
        waveCount++;
        Debug.Log($"Oleada {waveCount} iniciada");

        foreach (WaveEntry entry in waveEntries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                GameObject gameObject = Instantiate(entry.enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                Enemy enemy = gameObject.GetComponent<Enemy>();
                if (enemy == null)
                {
                    Debug.LogError("Enemy prefab sin componente Enemy.");
                    continue;
                }

                enemy.data = entry.enemyData;
                if (enemy.crystalTransform == null)
                    enemy.crystalTransform = GameObject.FindWithTag("Crystal")?.transform;

                enemy.ChangeState(new EnemyChaseState(enemy.MainTarget));

                yield return new WaitForSeconds(entry.spawnDelay);
            }
        }

        yield return null;
    }
}
