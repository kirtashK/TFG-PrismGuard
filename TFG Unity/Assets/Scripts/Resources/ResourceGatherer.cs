using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ResourceGatherer : MonoBehaviour
{
    [Tooltip("This gatherer will mark only the choosen categories")]
    public List<ResourceCategory> allowedCategories;

    [Tooltip("Detection radius")]
    public float detectionRadius = 10f;

    private readonly float detectionInterval = 2f;
    private readonly Collider[] scanResults = new Collider[5];
    private int nodeLayerMask;

    private void Awake()
    {
        nodeLayerMask = 1 << LayerMask.NameToLayer("ResourceNode");
    }

    private void Start()
    {
        StartCoroutine(DetectResourceNode());
    }

    private IEnumerator DetectResourceNode()
    {
        int numColliders = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRadius,
            scanResults,
            nodeLayerMask
        );

        for (int i = 0; i < numColliders; i++)
        {
            GatherResourceTask resourceNode = scanResults[i]
                .GetComponentInParent<GatherResourceTask>();
            ResourceInstance category = scanResults[i]
                .GetComponentInParent<ResourceInstance>();
            if (resourceNode != null 
                && !resourceNode.enabled
                && allowedCategories.Contains(category.category))
            {
                resourceNode.enabled = true;
                resourceNode.GetComponent<GatherResourceTask>().
                    gatherResourceRecipe = resourceNode.gatherResourceRecipe;
            }
        }
        yield return new WaitForSeconds(detectionInterval);
        StartCoroutine(DetectResourceNode());
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
