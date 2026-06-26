using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;

    [Header("Color")]
    [SerializeField] private Color playerFactionColor;
    [SerializeField] private Color enemyFactionColor;

    [Header("Position")]
    [SerializeField] private Vector3 worldOffset = new(0f, 2f, 0f);
    [SerializeField] private bool faceCamera = true;

    private ITarget target;
    private Camera mainCamera;

    private void Awake()
    {
        target = GetComponentInParent<ITarget>();
        if (target == null)
        {
            Debug.LogError($"{name}: Missing {nameof(ITarget)} in parent");
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
    }

    private void FactionColor()
    {
        if (target.Faction == Faction.Player)
        {
            fillImage.color = playerFactionColor;
        }
        else
        {
            fillImage.color = enemyFactionColor;
        }
    }

    private void OnEnable()
    {
        target.OnDamageTakenEvent += HandleHealthChanged;
        target.OnHealedEvent += HandleHealthChanged;
        target.OnDeathStartedEvent += HandleDeathStarted;

        UpdateHealthBar();

        FactionColor();
    }

    private void OnDisable()
    {
        if (target == null)
        {
            return;
        }

        target.OnDamageTakenEvent -= HandleHealthChanged;
        target.OnHealedEvent -= HandleHealthChanged;
        target.OnDeathStartedEvent -= HandleDeathStarted;
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void HandleHealthChanged(float _, Vector3 __)
    {
        UpdateHealthBar();
    }

    private void HandleHealthChanged(float _)
    {
        UpdateHealthBar();
    }

    private void HandleDeathStarted(ITarget deadTarget)
    {
        backgroundImage.gameObject.SetActive(false);
    }

    private void UpdateHealthBar()
    {
        float maxHealth = target.MaxHealth;
        float currentHealth = target.CurrentHealth;

        if (maxHealth <= 0f)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        float normalized = currentHealth / maxHealth;
        fillImage.fillAmount = normalized;

        // Hide when full to avoid clutter:
        if (normalized >= 0.999f)
        {
            if (backgroundImage.gameObject.activeSelf)
            {
                backgroundImage.gameObject.SetActive(false);
            }
        }
        else
        {
            if (!backgroundImage.gameObject.activeSelf)
            {
                backgroundImage.gameObject.SetActive(true);
            }
        }
    }

    private void UpdatePosition()
    {
        transform.position = target.Position + worldOffset;

        if (faceCamera && mainCamera != null)
        {
            transform.forward = mainCamera.transform.forward;
        }
    }
}