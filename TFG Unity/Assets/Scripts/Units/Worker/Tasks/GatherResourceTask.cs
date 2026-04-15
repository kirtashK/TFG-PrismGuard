using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GatherResourceTask : MonoBehaviour, ITask
{
    public GatherResourceRecipe gatherResourceRecipe;

    [Tooltip("List of possible positions where resource will be spawned. Within range of minResourcesToSpawn & maxResourcesToSpawn from data")]
    [SerializeField]
    private List<Transform> resourceSpawnPoints = new();

    private int currentResourceAmount;

    public WorkType WorkType => selectedWorkType;

    [SerializeField]
    private WorkType selectedWorkType = WorkType.None;

    public Vector3 TaskPosition => transform.position;

    public Vector3 TaskLookAt => transform.position;

    public int Priority => gatherResourceRecipe.priority;

    public float InteractionRange => gatherResourceRecipe.interactionRange;


    private void Awake()
    {
        if (gatherResourceRecipe.resourceAmount > 0)
        {
            int minRange = Mathf.Max(1, gatherResourceRecipe.resourceAmount - gatherResourceRecipe.resourceMaxDeviation);
            int maxRange = gatherResourceRecipe.resourceAmount + gatherResourceRecipe.resourceMaxDeviation;
            currentResourceAmount = Random.Range(minRange, maxRange + 1);

            //Debug.Log($"{name} initial resources: {currentResourceAmount}. Deviation: {currentResourceAmount - gatherResourceRecipe.resourceAmount}");
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
        StartCoroutine(WorkCoroutine(onComplete));
    }

    public void Cancel(Worker requester)
    {
        // Force reset so the task registers again in a clean state
        TaskManager.Instance.UnregisterTask(this);
        StopAllCoroutines();
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator WorkCoroutine(System.Action onComplete)
    {
        yield return new WaitForSeconds(gatherResourceRecipe.workDuration);

        int resourcesToSpawn = Random.Range(gatherResourceRecipe.minResourcesToSpawn, gatherResourceRecipe.maxResourcesToSpawn + 1);
        for (int i = 0; i < resourcesToSpawn; i++)
        {
            Vector3 spawnPos = resourceSpawnPoints[i].position;

            GameObject spawnedItem = ItemManager.Instance.CreateItem(spawnPos, gatherResourceRecipe.resourceItemData.itemPrefab);
            
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

        // Node isnt limited, it can respawn:
        if (gatherResourceRecipe.resourceAmount == 0)
        {
            ResourceManager.Instance.NotifyResourceCollected(gameObject.GetComponent<ResourceInstance>());
            enabled = false;
        }
        // Node has a limited amount of resources and doesnt respawn:
        else
        {
            currentResourceAmount = Mathf.Max(0, currentResourceAmount - resourcesToSpawn);

            Debug.Log($"{name} has [{currentResourceAmount}/{gatherResourceRecipe.resourceAmount}] resources remaining ({resourcesToSpawn} consumed)");

            // Resource is depleted:
            if (currentResourceAmount == 0)
            {
                Debug.Log($"{name} has been depleted");
                Destroy(gameObject);
                yield break;
            }

            ResourceInstance resourceInstance = GetComponent<ResourceInstance>();
            if (resourceInstance == null || !resourceInstance.IsCovered())
            {
                TaskManager.Instance.UnregisterTask(this);
                enabled = false;
                yield break;
            }

            TaskManager.Instance.UnregisterTask(this);
            TaskManager.Instance.RegisterTask(this);
        }
    }
}