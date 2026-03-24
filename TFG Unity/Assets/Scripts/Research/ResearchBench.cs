using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ResearchBench : MonoBehaviour, ITask
{
    [Header("Task Settings")]

    public int priority = 2;

    [Tooltip("Range workers must be within to be considered at the bench")]
    public float interactionRange = 1f;

    [Header("Cycle")]
    [Tooltip("Seconds per research cycle")]
    public float baseCycleDuration = 5f;

    [Tooltip("Multiplier applied to base points per cycle")]
    public float benchPointsMultiplier = 1f;

    private const float pollInterval = 1f;

    private bool isRegisteredToTaskManager = false;
    private bool isOccupied = false;

    private ResearchTask registeredTask = null;
    private ResearchSessionToken currentSession = null;
    private Worker currentWorker = null;

    public Vector3 TaskPosition => SitSpot.transform.position;
    public Vector3 TaskLookAt => transform.position;
    public bool IsOccupied => isOccupied;

    Vector3 ITask.TaskPosition => SitSpot.transform.position;
    int ITask.Priority => priority;
    float ITask.InteractionRange => interactionRange;

    [SerializeField]
    private Transform SitSpot;

    public WorkType WorkType => WorkType.None;

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private void OnDisable()
    {
        if (isRegisteredToTaskManager && TaskManager.Instance != null)
        {
            TaskManager.Instance.UnregisterTask(registeredTask);
            isRegisteredToTaskManager = false;
            registeredTask = null;
        }

        CancelCurrentSession();
        StopAllCoroutines();
    }

    private IEnumerator RegisterWhenReady()
    {
        while (TaskManager.Instance == null || ResearchManager.Instance == null || !ResearchManager.Instance.IsLoaded)
        {
            yield return null;
        }

        StartCoroutine(PollForAvailability());
    }

    private IEnumerator PollForAvailability()
    {
        WaitForSeconds wait = new(pollInterval);

        while (true)
        {
            bool shouldBeRegistered = !string.IsNullOrEmpty(ResearchManager.Instance.ActiveResearchId) 
                && !isOccupied && enabled && gameObject.activeInHierarchy;

            if (shouldBeRegistered && !isRegisteredToTaskManager)
            {
                registeredTask = new ResearchTask(this);
                TaskManager.Instance.RegisterTask(registeredTask);
                isRegisteredToTaskManager = true;
            }
            else if ((!shouldBeRegistered) && isRegisteredToTaskManager)
            {
                TaskManager.Instance.UnregisterTask(registeredTask);
                registeredTask = null;
                isRegisteredToTaskManager = false;
            }

            yield return wait;
        }
    }

    void ITask.Execute(Worker worker, System.Action onComplete)
    {
        HandleTaskExecute(worker, onComplete);
    }

    void ITask.Cancel(Worker requester)
    {
        HandleTaskCancel(requester);
    }

    public void HandleTaskExecute(Worker worker, System.Action onComplete)
    {
        if (worker == null)
        {
            return;
        }

        if (isOccupied)
        {
            Debug.Log($"{name}: already occupied");
            return;
        }

        if (ResearchManager.Instance == null || string.IsNullOrEmpty(ResearchManager.Instance.ActiveResearchId))
        {
            Debug.Log($"{name}: no active research");
            return;
        }

        if (!TryBeginSession(worker, out ResearchSessionToken token))
        {
            Debug.LogWarning($"{name}: failed to reserve for worker {worker.name}");
            return;
        }

        StartCoroutine(ResearchCycleRoutine(token, onComplete));
    }

    public void HandleTaskCancel(Worker requester)
    {
        if (currentWorker == requester)
        {
            CancelCurrentSession();
        }
    }

    private bool TryBeginSession(Worker worker, out ResearchSessionToken token)
    {
        token = null;

        if (isOccupied)
        {
            return false;
        }

        isOccupied = true;
        currentWorker = worker;
        token = new ResearchSessionToken(this, worker);
        currentSession = token;

        if (isRegisteredToTaskManager && TaskManager.Instance != null && registeredTask != null)
        {
            TaskManager.Instance.UnregisterTask(registeredTask);
            isRegisteredToTaskManager = false;
            registeredTask = null;
        }

        return true;
    }

    private IEnumerator ResearchCycleRoutine(ResearchSessionToken session, System.Action onComplete)
    {
        bool completedSuccessfully = false;

        while (session != null && !session.IsCancelled)
        {
            if (ResearchManager.Instance == null || string.IsNullOrEmpty(ResearchManager.Instance.ActiveResearchId))
            {
                break;
            }

            float elapsed = 0f;
            while (elapsed < baseCycleDuration)
            {
                if (session.IsCancelled)
                {
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (session.IsCancelled)
            {
                break;
            }

            string activeId = ResearchManager.Instance.ActiveResearchId;

            if (!string.IsNullOrEmpty(activeId) && ResearchManager.Instance.HasResearch(activeId) && !ResearchManager.Instance.HasCompleted(activeId))
            {
                ResearchData researchData = null;

                foreach (ResearchData researchDataAux in ResearchManager.Instance.AllResearchData())
                {
                    if (researchDataAux != null && researchDataAux.id == activeId)
                    {
                        researchData = researchDataAux;
                        break;
                    }
                }

                float basePoints = (researchData != null) ? researchData.basePointsPerCycle : 1f;
                float points = basePoints * benchPointsMultiplier;

                ResearchManager.Instance.AddProgress(activeId, points);
            }

            string checkActive = ResearchManager.Instance != null ? ResearchManager.Instance.ActiveResearchId : null;
            if (string.IsNullOrEmpty(checkActive) || (ResearchManager.Instance != null && ResearchManager.Instance.HasCompleted(checkActive)))
            {
                completedSuccessfully = true;
                break;
            }
        }

        if (completedSuccessfully)
        {
            onComplete.Invoke();
        }
        
        EndSession(session);
    }

    private void EndSession(ResearchSessionToken session)
    {
        if (currentSession == session)
        {
            currentSession = null;
        }

        isOccupied = false;
        currentWorker = null;
    }

    public void CancelCurrentSession()
    {
        if (currentSession != null)
        {
            currentSession.Cancel();
            currentSession = null;
        }

        isOccupied = false;
        currentWorker = null;
    }

    public class ResearchSessionToken
    {
        public ResearchBench Bench { get; private set; }
        public Worker Worker { get; private set; }
        public bool IsCancelled { get; private set; }

        public ResearchSessionToken(ResearchBench bench, Worker worker)
        {
            Bench = bench;
            Worker = worker;
            IsCancelled = false;
        }

        public void Cancel()
        {
            IsCancelled = true;
        }
    }
}