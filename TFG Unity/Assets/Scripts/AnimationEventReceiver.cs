using UnityEngine;

/// <summary>
/// Animation Events fired by the clip will be forwarded to the nearest Unit in the parent chain
/// </summary>
public class AnimationEventReceiver : MonoBehaviour
{
    Unit unit;

    private void Awake()
    {
        unit = GetComponentInParent<Unit>();
        if (unit == null)
        {
            Debug.LogWarning($"{name}: Couldnt get component {nameof(Unit)}");
        }
    }
    public void OnDeathAnimationComplete()
    {
        unit.OnDeathAnimationComplete();
    }

    public void OnAttackHit()
    {
        unit.OnAttackHit();
    }
}