using UnityEngine;

public class MenuOrbitCamera : MonoBehaviour
{
    [Tooltip("The transform to orbit around. Assign the decorative crystal here")]
    [SerializeField] private Transform target;

    [Tooltip("Degrees per second the camera rotates around the target")]
    [SerializeField] private float orbitSpeed = 8f;

    [Tooltip("How smoothly the camera looks at the target each frame")]
    [SerializeField] private float lookSpeed = 5f;

    private Vector3 currentOffset;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning($"{name}: no target assigned");
            enabled = false;
            return;
        }

        currentOffset = transform.position - target.position;
    }

    private void Update()
    {
        Quaternion rotation = Quaternion.AngleAxis(
            orbitSpeed * Time.deltaTime, Vector3.up);
        currentOffset = rotation * currentOffset;

        transform.position = target.position + currentOffset;

        Quaternion targetRotation = Quaternion.LookRotation(
            target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation, lookSpeed * Time.deltaTime);
    }
}