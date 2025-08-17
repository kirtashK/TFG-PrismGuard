using UnityEngine;

public class ItemInstance : MonoBehaviour
{
    public ItemData itemData;

    [HideInInspector]
    public Worker carrier;

    private Renderer[] renderers;
    private Collider[] colliders;

    private void Start()
    {
        if (itemData == null)
        {
            Debug.LogError($"{name} has null ItemData");
        }
    }

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        colliders = GetComponentsInChildren<Collider>(includeInactive: true);
    }

    /// <summary>
    /// Sets model and colliders of the object 
    /// and its children to true/false depending on parameter visible
    /// </summary>
    /// /// <param name="visible">True to enable, False to disable</param>
    public void SetVisible(bool visible)
    {
        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = visible;
                }
            }
        }

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = visible;
                }
            }
        }
    }
}