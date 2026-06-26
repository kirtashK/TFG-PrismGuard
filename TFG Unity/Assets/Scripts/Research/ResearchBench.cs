using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ResearchBench : MonoBehaviour, ITask
{
    [HideInInspector] public Structure structure;
    private StructureData data;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey healOnWaveCompletedStat;
    [SerializeField] private StatKey researchSpeedStat;

    private float researchSpeed = 1;

    [Header("Task Settings")]

    public int priority = 2;

    [Tooltip("Range workers must be within to be considered at the bench")]
    public float interactionRange = 1f;

    [Header("Cycle")]
    [Tooltip("Seconds per research cycle")]
    public float baseCycleDuration = 5f;

    [Tooltip("Multiplier applied to base points per cycle")]
    public float benchPointsMultiplier = 1f;
    private float basePointsMultiplier = 1f;

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

    #region Unity methods

    private void Awake()
    {
        if (TryGetComponent<Structure>(out Structure structure))
        {
            this.structure = structure;
            data = structure.structureData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(structure)}");
        }

        CheckNullStats();

        if (data == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(data)}");
        }
    }

    private void Start()
    {
        RefreshStats();

        structure.currentHealth = structure.maxHealth;
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (TaskManager.Instance == null
            || ResearchManager.Instance == null || !ResearchManager.Instance.IsLoaded
            || StatModifierManager.Instance == null)
        {
            yield return null;
        }

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;

        StartCoroutine(PollForAvailability());
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
        if (isRegisteredToTaskManager && TaskManager.Instance != null)
        {
            TaskManager.Instance.UnregisterTask(registeredTask);
            isRegisteredToTaskManager = false;
            registeredTask = null;
        }

        if (StatModifierManager.Instance != null)
        {
            StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
        }
    }

    #endregion

    #region Stats

    private void CheckNullStats()
    {
        if (maxHealthStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(maxHealthStat)}");
        }
        if (healOnWaveCompletedStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(healOnWaveCompletedStat)}");
        }
        if (researchSpeedStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(researchSpeedStat)}");
        }
    }

    void HandleModifiersChanged(string targetId, string statKeyId)
    {
        if (targetId == data.id)
        {
            RefreshStats();
        }
        // Global modifier:
        else if (string.IsNullOrEmpty(targetId))
        {
            RefreshStats();
        }
    }

    public void RefreshStats()
    {
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxHealthStat, out float finalValue))
        {
            structure.maxHealth = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, healOnWaveCompletedStat, out finalValue))
        {
            structure.healOnWaveCompleted = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, researchSpeedStat, out finalValue))
        {
            researchSpeed = finalValue;
            benchPointsMultiplier = basePointsMultiplier * researchSpeed;
        }
    }

    #endregion

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
        currentSession?.Cancel();
        currentSession = null;

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