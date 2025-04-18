using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

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

        resource.gameObject.SetActive(true);
    }
}
