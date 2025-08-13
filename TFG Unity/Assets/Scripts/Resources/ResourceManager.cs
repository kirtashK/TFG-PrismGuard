using System.Collections;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private const float CheckInterval = 3f;

    private readonly Collider[] scanResults = new Collider[16];

    [Tooltip("Layers that will be considered 'blocking' for the respawn check. Default = Everything.")]
    public LayerMask blockingLayers = ~0;

    [Tooltip("Optional tags to ignore when checking for blocking colliders.")]
    public string[] ignoreTags = new string[0];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
    }

    public void NotifyResourceCollected(ResourceInstance resource)
    {
        StartCoroutine(RespawnRoutine(resource));
    }

    private IEnumerator RespawnRoutine(ResourceInstance resource)
    {
        if (resource == null)
        {
            yield break;
        }

        resource.gameObject.SetActive(false);

        yield return new WaitForSeconds(resource.data.respawnTime);

        Collider collider = resource.GetComponent<Collider>();
        float checkRadius = (collider != null)
            ? collider.bounds.extents.magnitude
            : 1f;

        while (true)
        {
            int results = Physics.OverlapSphereNonAlloc(
            resource.transform.position,
            checkRadius,
            scanResults,
            blockingLayers);

            bool blocked = false;

            for (int i = 0; i < results; i++)
            {
                Collider hit = scanResults[i];
                if (hit == null)
                {
                    continue;
                }

                if (hit.isTrigger)
                {
                    continue;
                }

                bool skipByTag = false;
                if (ignoreTags != null && ignoreTags.Length > 0)
                {
                    for (int countTag = 0; countTag < ignoreTags.Length; countTag++)
                    {
                        string tag = ignoreTags[countTag];
                        if (!string.IsNullOrEmpty(tag) && hit.CompareTag(tag))
                        {
                            skipByTag = true;
                            break;
                        }
                    }
                }
                if (skipByTag)
                {
                    continue;
                }

                blocked = true;
                break;
            }

            if (!blocked)
            {
                break;
            }

            yield return new WaitForSeconds(CheckInterval);
        }

        resource.gameObject.SetActive(true);
    }
}