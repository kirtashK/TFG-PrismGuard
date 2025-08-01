using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private const float CheckInterval = 3f;

    private int itemLayerMask;
    private readonly Collider[] scanResults = new Collider[5];

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

            itemLayerMask = 1 << LayerMask.NameToLayer("Item");
        }
    }

    public void NotifyResourceCollected(ResourceInstance resource)
    {
        StartCoroutine(RespawnRoutine(resource));
    }

    private IEnumerator RespawnRoutine(ResourceInstance resource)
    {
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
            itemLayerMask);

            if (results == 0)
            {
                break;
            }

            yield return new WaitForSeconds(CheckInterval);
        }

        resource.gameObject.SetActive(true);
    }
}
