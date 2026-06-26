using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple raycast-based projectile. It flies in a straight line and applies damage
/// to the first hostile ITarget it hits. It also ignores colliders that belong to the
/// source structure.
/// </summary>
public class TowerProjectile : MonoBehaviour
{
    [Header("Projectile")]

    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float hitRadius = 0.1f;

    [Header("Impact")]

    [Tooltip("If true, the projectile will stay attached to the target after hitting it")]
    [SerializeField] private bool stickOnHit = true;

    [Tooltip("If the projectile sticks, it will be destroyed after this many seconds")]
    [SerializeField] private float stuckLifetime = 12f;

    [Tooltip("Small offset pushed into the target so the projectile looks embedded")]
    [SerializeField] private float embedDepth = 0.05f;

    public float ProjectileSpeed => projectileSpeed;

    private float damage;
    private Faction sourceFaction;
    private Vector3 origin;
    private Vector3 direction;
    private Transform sourceRoot;
    private float remainingLifetime;

    private bool isStuck;

    private readonly RaycastHit[] raycastHits = new RaycastHit[16];

    private Unit boundUnit;
    private Structure boundStructure;

    public void Initialize(float damage, Faction sourceFaction, Vector3 origin,
        Vector3 direction, float speed, Transform sourceRoot)
    {
        this.damage = damage;
        this.sourceFaction = sourceFaction;
        this.origin = origin;
        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        this.sourceRoot = sourceRoot;
        projectileSpeed = speed;
        remainingLifetime = lifetime;
        isStuck = false;
    }

    private void Update()
    {
        if (isStuck)
        {
            return;
        }

        if (remainingLifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float stepDistance = projectileSpeed * Time.deltaTime;
        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = currentPosition + direction * stepDistance;

        if (TryHitSomething(currentPosition, direction, stepDistance))
        {
            return;
        }

        transform.position = nextPosition;

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        remainingLifetime -= Time.deltaTime;
    }

    /// <summary>
    /// Checks what the projectile hits during this step. 
    /// If it hits a hostile target, damage is applied
    /// </summary>
    private bool TryHitSomething(Vector3 startPosition, Vector3 moveDirection, float stepDistance)
    {
        int hitCount = Physics.SphereCastNonAlloc(startPosition, hitRadius, moveDirection,
            raycastHits, stepDistance, collisionMask, QueryTriggerInteraction.Collide);

        if (hitCount <= 0)
        {
            return false;
        }

        System.Array.Sort(raycastHits, 0, hitCount, new RaycastHitDistanceComparer());

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHits[i];
            Collider collider = hit.collider;

            if (collider == null)
            {
                continue;
            }

            if (sourceRoot != null && collider.transform.IsChildOf(sourceRoot))
            {
                continue;
            }

            if (TryGetTargetFromCollider(collider, out ITarget target, out Transform targetTransform))
            {
                if (target.IsAlive && target.Faction != sourceFaction)
                {
                    target.TakeDamage(damage, origin);

                    if (stickOnHit && target.IsAlive)
                    {
                        StickToTarget(targetTransform, hit);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }

                    return true;
                }

                continue;
            }

            Destroy(gameObject);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds an ITarget on the collider or any parent component
    /// </summary>
    private bool TryGetTargetFromCollider(Collider collider, out ITarget target, out Transform targetTransform)
    {
        target = null;
        targetTransform = null;

        if (collider == null)
        {
            return false;
        }

        MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>(false);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is ITarget candidate)
            {
                target = candidate;
                targetTransform = behaviour.transform;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Parents the projectile to the target so it remains visually attached after impact.
    /// Movement and lifetime updates stop immediately after attachment.
    /// </summary>
    private void StickToTarget(Transform targetTransform, RaycastHit hit)
    {
        if (targetTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        isStuck = true;
        UnbindDeathEvent();

        if (TryGetComponent<Collider>(out Collider projectileCollider))
        {
            projectileCollider.enabled = false;
        }

        if (TryGetComponent<Rigidbody>(out Rigidbody projectileRigidbody))
        {
            projectileRigidbody.linearVelocity = Vector3.zero;
            projectileRigidbody.angularVelocity = Vector3.zero;
            projectileRigidbody.isKinematic = true;
        }

        transform.SetPositionAndRotation(hit.point - direction * embedDepth, Quaternion.LookRotation(direction, Vector3.up));
        transform.SetParent(targetTransform, true);

        BindDeathEvent(targetTransform);

        if (stuckLifetime >= 0f)
        {
            Destroy(gameObject, stuckLifetime);
        }
    }

    /// <summary>
    /// Subscribes to the target's death event
    /// </summary>
    private void BindDeathEvent(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return;
        }

        boundUnit = targetTransform.GetComponentInParent<Unit>();
        if (boundUnit != null)
        {
            boundUnit.OnDeathStartedEvent += HandleTargetDeathStarted;
            return;
        }

        boundStructure = targetTransform.GetComponentInParent<Structure>();
        if (boundStructure != null)
        {
            boundStructure.OnDeathStartedEvent += HandleTargetDeathStarted;
        }
    }

    /// <summary>
    /// Unsubscribes from the target's death event
    /// </summary>
    private void UnbindDeathEvent()
    {
        if (boundUnit != null)
        {
            boundUnit.OnDeathStartedEvent -= HandleTargetDeathStarted;
            boundUnit = null;
        }

        if (boundStructure != null)
        {
            boundStructure.OnDeathStartedEvent -= HandleTargetDeathStarted;
            boundStructure = null;
        }
    }

    private void HandleTargetDeathStarted(ITarget target)
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        UnbindDeathEvent();
    }

    private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
    {
        public int Compare(RaycastHit first, RaycastHit second)
        {
            return first.distance.CompareTo(second.distance);
        }
    }
}