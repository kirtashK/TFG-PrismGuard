using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public EnemyData data;

    [Header("Target")]
    public Transform crystalTransform;

    [HideInInspector] 
    public NavMeshAgent agent;

    private float currentHealth;

    private IEnemyState currentState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Idle al ser generados, WaveManager le asigna objetivo y cambia de estado
        Debug.Log("Cambiando a estado idle");
        ChangeState(new EnemyIdleState());
    }

    private void Start()
    {
        currentHealth = data.maxHealth;
        agent.speed = data.moveSpeed;
        agent.stoppingDistance = data.attackRange;

        if (crystalTransform == null)
            crystalTransform = GameObject.FindWithTag("Crystal").transform;
    }

    private void Update()
    {
        currentState.UpdateState(this);
    }

    public void ChangeState(IEnemyState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    public void TakeDamage(float dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        // TODO
        // animacion
        // notificar a WaveManager de un enemigo menos
        // Añadir experiencia al soldado que ha matado este enemigo
        // Añadir oro a GameManager que el jugador puede usar para comprar en la tienda

        Destroy(gameObject);
    }
}
