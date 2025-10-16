using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour, IAddressableInstance, ICombatTarget
{
    public EnemyData data;
    public Transform crystalTransform;

    private float currentHealth;

    [HideInInspector] 
    public NavMeshAgent agent;

    private IEnemyState currentState;

    public ICombatTarget MainTarget { get; private set; }

    private readonly Collider[] aggroBuffer = new Collider[16];

    private AsyncOperationHandle<GameObject> addressableInstanceHandle;
    private bool hasAddressableHandle = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        MainTarget = crystalTransform != null
            ? crystalTransform.GetComponent<ICombatTarget>()
            : GameObject.FindWithTag("Crystal")
                .GetComponent<ICombatTarget>();
    }

    private void Start()
    {
        UIManager.Instance.ChangeEnemyCount(1);

        currentHealth = data.maxHealth;

        agent.speed = data.moveSpeed;
        agent.stoppingDistance = data.attackRange;

        ChangeState(new EnemyChaseState(MainTarget));
    }

    private void Update()
    {
        currentState?.UpdateState(this);
    }

    public void ChangeState(IEnemyState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    public Vector3 Position => transform.position;

    public bool IsAlive => currentHealth > 0f;

    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        //Debug.Log($"Health of {name}: {currentHealth}/{data.maxHealth}");

        if (currentHealth == 0f)
            Die();
    }

    public void SetAddressableInstanceHandle(AsyncOperationHandle<GameObject> handle)
    {
        addressableInstanceHandle = handle;
        hasAddressableHandle = handle.IsValid();
    }

    private void Die()
    {
        //Debug.Log($"{name} has died");

        // Add score after defeating the enemy
        ScoreManager.Instance.AddScore(data.spawnCost);

        // Change enemy count in UI
        UIManager.Instance.ChangeEnemyCount(-1);

        // TODO Sonido, animaciones, efectos

        // Release addressable handle
        if (hasAddressableHandle  && addressableInstanceHandle.IsValid())
        {
            Addressables.ReleaseInstance(addressableInstanceHandle);
            hasAddressableHandle = false;
            return;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Finds the closests player unit inside data.AggroRadius
    /// </summary>
    public ICombatTarget FindNearestPlayerUnit()
    {
        int hitCount = Physics.OverlapSphereNonAlloc
            (transform.position,
            data.AggroRadius,
            aggroBuffer,
            LayerMask.GetMask("PlayerUnit"));

        ICombatTarget best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            ICombatTarget playerUnit = aggroBuffer[i].GetComponentInParent<ICombatTarget>();
            if (playerUnit != null 
                && playerUnit.IsAlive 
                && playerUnit is not Crystal)
            {
                float dist = Vector3.Distance(transform.position, playerUnit.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = playerUnit;
                }
            }
        }
        return best;
    }
}