using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private const float CheckInterval = 3f;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void NotifyResourceCollected(ResourceInstance resource)
    {
        StartCoroutine(RespawnRoutine(resource));
    }

    private IEnumerator RespawnRoutine(ResourceInstance resource)
    {
        resource.gameObject.SetActive(false);

        yield return new WaitForSeconds(resource.data.respawnTime);

        Collider col = resource.GetComponent<Collider>();
        float checkRadius = col != null
            ? col.bounds.extents.magnitude
            : 1f;

        while (true)
        {
            bool anyLogs = false;
            Collider[] hits = Physics.OverlapSphere(
                resource.transform.position,
                checkRadius
            );

            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Log"))
                {
                    anyLogs = true;
                    break;
                }
            }

            if (!anyLogs)
                break;

            yield return new WaitForSeconds(CheckInterval);
        }

        resource.gameObject.SetActive(true);
    }
}
