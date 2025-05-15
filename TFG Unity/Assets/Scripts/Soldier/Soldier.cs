using UnityEngine;
using UnityEngine.AI;

public class Soldier : MonoBehaviour, ICombatTarget
{
    public SoldierData data;

    private float currentHealth;

    [HideInInspector]
    public NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        currentHealth = data.maxHealth;

        agent.speed = data.moveSpeed;
        agent.stoppingDistance = data.attackRange;
    }

    public Vector3 Position => transform.position;

    public bool IsAlive => currentHealth > 0f;

    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"Salud de {name} = {currentHealth}/{data.maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} ha muerto");
        // TODO Animacion muerte, sonido
        Destroy(gameObject);
    }
}
