using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class Blueprint : MonoBehaviour, IItemConsumer
{
    private StructureData data;

    [Tooltip("Spot where delivered items will be put")]
    public Transform dropSpot;

    private readonly Dictionary<string, int> delivered = new();
    private readonly Dictionary<string, int> pending = new();
    private readonly List<GameObject> storedObjects = new();

    public Vector3 GetReceivePosition() => dropSpot.transform.position;

    [SerializeField] private int priority = 3;
    [SerializeField] private float interactionRange = 1f;

    private bool isRegisteredToTaskManager;
    private bool isOccupied;
    private BuildTask registeredTask;
    private BuildSessionToken currentSession;
    private Worker currentWorker;

    public Vector3 TaskPosition => transform.position;
    public Vector3 TaskLookAt => transform.position;
    public int Priority => priority;
    public float InteractionRange => interactionRange;

    #region Unity methods

    private void Awake()
    {
        if (TryGetComponent<Structure>(out Structure structure))
        {
            data = structure.structureData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(structure)}");
        }

        if (dropSpot == null)
        {
            Debug.LogWarning($"{name} missing {nameof(dropSpot)}");
        }
    }

    private void OnEnable()
    {
        delivered.Clear();
        pending.Clear();
        storedObjects.Clear();

        foreach (StructureData.ResourceRequirement requirement in data.buildRequirements)
        {
            string itemId = requirement.itemData.id;

            delivered[itemId] = 0;
            pending[itemId] = 0;
        }

        isRegisteredToTaskManager = false;
        isOccupied = false;
        registeredTask = null;
        currentSession = null;
        currentWorker = null;

        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (ItemConsumerManager.Instance == null || TaskManager.Instance == null)
        {
            yield return null;
        }

        ItemConsumerManager.Instance.Register(this);
        RefreshConstructionTaskRegistration();
    }

    private void OnDisable()
    {
        Unregister();

        CancelCurrentSession();
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        Unregister();

        CancelCurrentSession();
        StopAllCoroutines();
    }

    private void Unregister()
    {
        if (ItemConsumerManager.Instance != null)
        {
            ItemConsumerManager.Instance.Unregister(this);
        }

        if (TaskManager.Instance != null && isRegisteredToTaskManager && registeredTask != null)
        {
            TaskManager.Instance.UnregisterTask(registeredTask);
        }

        isRegisteredToTaskManager = false;
        registeredTask = null;
    }

    #endregion

    public int DeliveredCount(ItemData item)
    {
        string itemId = item.id;

        if (delivered.TryGetValue(itemId, out int count))
        {
            return count;
        }
        return 0;
    }

    private bool CanConstruct()
    {
        foreach (StructureData.ResourceRequirement requirement in data.buildRequirements)
        {
            if (DeliveredCount(requirement.itemData) < requirement.quantity)
            {
                return false;
            }
        }

        return true;
    }

    #region Item Consumer

    public bool CanReceive(ItemData item)
    {
        string itemId = item.id;

        if (!delivered.ContainsKey(itemId))
        {
            return false;
        }

        int have = delivered[itemId];
        int inFlight = pending[itemId];
        int needed = data.buildRequirements
                          .Find(required => required.itemData.id == itemId).quantity;

        return (have + inFlight) < needed;
    }

    public bool Reserve(ItemData item)
    {
        string itemId = item.id;

        if (!delivered.ContainsKey(itemId))
        {
            return false;
        }

        int have = delivered[itemId];
        int inFlight = pending[itemId];
        int needed = data.buildRequirements
                          .Find(required => required.itemData.id == itemId).quantity;

        if ((have + inFlight) >= needed)
        {
            return false;
        }

        pending[itemId] = inFlight + 1;
        return true;
    }

    public void Release(ItemData item)
    {
        string itemId = item.id;

        if (pending.ContainsKey(itemId))
        {
            pending[itemId] = Mathf.Max(0, pending[itemId] - 1);
        }
    }

    public void OnReceived(GameObject itemObj, ItemData itemData)
    {
        Release(itemData);

        string itemId = itemData.id;
        if (delivered.ContainsKey(itemId))
        {
            delivered[itemId]++;
        }

        if (itemObj.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
        {
            itemInstance.SetVisible(true);
        }

        itemObj.transform.SetParent(dropSpot.transform, worldPositionStays: true);
        itemObj.transform.position = GetReceivePosition();

        storedObjects.Add(itemObj);

        RefreshConstructionTaskRegistration();
    }

    public void ConfirmRetrieval(GameObject item)
    {
        // Does nothing as Blueprint doesnt
        // store items to be picked up
    }

    #endregion

    #region Task session

    private void RefreshConstructionTaskRegistration()
    {
        if (TaskManager.Instance == null)
        {
            return;
        }

        bool shouldRegister = CanConstruct() && !isOccupied && enabled && gameObject.activeInHierarchy;

        if (shouldRegister && !isRegisteredToTaskManager)
        {
            registeredTask = new BuildTask(this);
            TaskManager.Instance.RegisterTask(registeredTask);
            isRegisteredToTaskManager = true;
        }
        else if (!shouldRegister && isRegisteredToTaskManager)
        {
            TaskManager.Instance.UnregisterTask(registeredTask);
            registeredTask = null;
            isRegisteredToTaskManager = false;
        }
    }

    public void HandleTaskExecute(Worker worker, Action onComplete)
    {
        if (worker == null)
        {
            return;
        }
        if (!CanConstruct())
        {
            onComplete?.Invoke();
            RefreshConstructionTaskRegistration();
            return;
        }
        if (isOccupied)
        {
            onComplete?.Invoke();
            return;
        }
        if (!TryBeginSession(worker, out BuildSessionToken token))
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(BuildRoutine(token, onComplete));
    }

    public void HandleTaskCancel(Worker requester)
    {
        if (currentWorker == requester)
        {
            CancelCurrentSession();
            RefreshConstructionTaskRegistration();
        }
    }

    private bool TryBeginSession(Worker worker, out BuildSessionToken token)
    {
        token = null;

        if (isOccupied)
        {
            return false;
        }

        isOccupied = true;
        currentWorker = worker;
        token = new BuildSessionToken(this, worker);
        currentSession = token;

        if (isRegisteredToTaskManager && TaskManager.Instance != null && registeredTask != null)
        {
            TaskManager.Instance.UnregisterTask(registeredTask);
            isRegisteredToTaskManager = false;
            registeredTask = null;
        }

        return true;
    }

    private IEnumerator BuildRoutine(BuildSessionToken session, Action onComplete)
    {
        float buildDuration = data != null ? Mathf.Max(0f, data.buildDuration) : 0f;
        float elapsedTime = 0f;

        while (elapsedTime < buildDuration)
        {
            if (session == null || session.IsCancelled)
            {
                EndSession(session);
                yield break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        bool completedSuccessfully = session != null && !session.IsCancelled;

        EndSession(session);

        if (completedSuccessfully)
        {
            onComplete?.Invoke();
            CompleteConstruction();
        }
        else
        {
            RefreshConstructionTaskRegistration();
        }
    }

    private void CompleteConstruction()
    {
        for (int i = storedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = storedObjects[i];
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        storedObjects.Clear();

        Instantiate(data.builtPrefab, transform.position, transform.rotation);

        Destroy(gameObject);
    }

    private void EndSession(BuildSessionToken session)
    {
        if (currentSession == session)
        {
            currentSession = null;
        }

        isOccupied = false;
        currentWorker = null;
    }

    private void CancelCurrentSession()
    {
        currentSession?.Cancel();
        currentSession = null;

        isOccupied = false;

        if (currentWorker != null)
        {
            if (currentWorker.CurrentTask is BuildTask)
            {
                currentWorker.CurrentTask = null;
                currentWorker.ChangeState(new IdleState());

                if (currentWorker.unit != null && currentWorker.unit.agent != null)
                {
                    currentWorker.unit.agent.SetDestination(currentWorker.transform.position);
                }
            }

            currentWorker = null;
        }
    }

    public class BuildSessionToken
    {
        public Blueprint Blueprint { get; }
        public Worker Worker { get; }
        public bool IsCancelled { get; private set; }

        public BuildSessionToken(Blueprint blueprint, Worker worker)
        {
            Blueprint = blueprint;
            Worker = worker;
        }

        public void Cancel()
        {
            IsCancelled = true;
        }
    }

    #endregion
}