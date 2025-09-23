using UnityEngine;

public class Crystal : MonoBehaviour, ICombatTarget
{
    [Header("Stats")]
    public float maxHealth = 200f;

    private float currentHealth;


    private void Start()
    {
        currentHealth = maxHealth;
    }

    public Vector3 Position => transform.position;
    
    public bool IsAlive => currentHealth > 0f;

    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"Health of {name}: {currentHealth}/{maxHealth}");

        // TODO Efectos, sonido

        if (currentHealth <= 0f)
        {
            OnDestroyed();
        }
    }

    private void OnDestroyed()
    {
        Debug.Log("El cristal ha sido destruido!");
        // Fire event
        GameManager.Instance.OnCrystalDestroyed();

        // TODO mover la camara al cristal, cambiar modelo de cristal a uno roto

        gameObject.SetActive(false);
    }
}
