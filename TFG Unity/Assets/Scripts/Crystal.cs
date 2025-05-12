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

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"Salud de {name} = {currentHealth}/{maxHealth}");

        // TODO Efectos, sonido

        if (currentHealth <= 0f)
        {
            OnDestroyed();
        }
    }

    private void OnDestroyed()
    {
        Debug.Log("El cristal ha sido destruido!");
        // TODO Notifica al GameManager la derrota
        // GameManager.Instance.OnCrystalDestroyed();

        // TODO OnCrystalDestroyed() se encarga de mostrar pantalla de derrota
        // y mover la camara al cristal, cambiar modelo de cristal a uno roto
        // desactivar enemigos o similar

        gameObject.SetActive(false);
        //Destroy(gameObject);
    }
}
