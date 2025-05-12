using UnityEngine;

public class Soldier : MonoBehaviour, ICombatTarget
{
    [Header("Stats")]
    public float maxHealth = 50f;
    private float currentHealth;

    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public Vector3 Position => transform.position;

    public bool IsAlive => currentHealth > 0f;

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"Salud de {name} = {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // TODO Animacion muerte, sonido
        Destroy(gameObject);
    }
}
