using UnityEngine;
using System.Collections;
using Mono.Cecil;
using System.Collections.Generic;

public class GatherResourceTask : MonoBehaviour, ITask
{
    public GatherResourceRecipe gatherResourceRecipe;

    [Tooltip("List of possible positions where resource will be spawned. Within range of minResourcesToSpawn & maxResourcesToSpawn from data")]
    [SerializeField]
    private List<Transform> resourceSpawnPoints = new();

    public Vector3 TaskPosition
    {
        get
        {
            return transform.position;
        }
    }

    public int Priority
    {
        get
        {
            return gatherResourceRecipe.priority;
        }
    }

    public float InteractionRange
    {
        get
        {
            return gatherResourceRecipe.interactionRange;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (TaskManager.Instance == null)
        {
            yield return null;
        }
        TaskManager.Instance.RegisterTask(this);
    }

    private void OnDisable()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.UnregisterTask(this);
        }
    }

    public void Execute(Worker worker, System.Action onComplete)
    {
        //Debug.Log(name + " - gathering resource by " + worker.name);
        StartCoroutine(WorkCoroutine(onComplete));
    }

    private IEnumerator WorkCoroutine(System.Action onComplete)
    {
        yield return new WaitForSeconds(gatherResourceRecipe.workDuration);

        int resourcesToSpawn = Random.Range(gatherResourceRecipe.minResourcesToSpawn, gatherResourceRecipe.maxResourcesToSpawn + 1);
        for (int i = 0; i < resourcesToSpawn; i++)
        {
            Vector3 spawnPos = resourceSpawnPoints[i].position;

            GameObject spawnedItem = ItemManager.Instance.CreateItem(spawnPos, gatherResourceRecipe.resourceItemPrefab);
            
            MoveItemTask moveTask;
            if (spawnedItem.TryGetComponent<MoveItemTask>(out MoveItemTask existingMoveItemTask))
            {
                moveTask = existingMoveItemTask;
            }
            else
            {
                moveTask = spawnedItem.AddComponent<MoveItemTask>();
            }

            moveTask.TaskData = gatherResourceRecipe.resourceItemData;
        }

        onComplete?.Invoke();

        ResourceManager.Instance.NotifyResourceCollected(gameObject.GetComponent<ResourceInstance>());

        enabled = false;
    }
}
