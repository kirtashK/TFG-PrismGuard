using System.Collections;
using UnityEngine;

/// <summary>
/// When activated, smoothly orbits the camera around a pivot.
/// The pivot is either obtained automatically based on what the camera
/// was looking at, or provided by another script.
/// The camera glides in a horizontal circle at a fixed height, 
/// always facing the pivot point
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    [Tooltip("Degrees per second the camera rotates around the pivot")]
    [SerializeField] private float orbitSpeed = 10f;

    [Tooltip("How quickly the camera transitions into and out of the orbit")]
    [SerializeField] private float transitionSpeed = 2f;

    [Tooltip("Maximum distance to raycast forward to find a pivot point")]
    [SerializeField] private float maxRaycastDistance = 200f;

    [Tooltip("Layer mask for the raycast that finds the pivot point")]
    [SerializeField] private LayerMask pivotLayerMask = ~0;

    private Vector3 pivotPoint;
    private Vector3 currentOffset;
    private bool isOrbiting = false;

    private CameraController cameraController;

    private Vector3 savedPosition;
    private Quaternion savedRotation;

    private bool restorePositionAfterOrbit = false;

    #region Unity methods

    private void Awake()
    {
        cameraController = GetComponent<CameraController>();
        if (cameraController == null)
        {
            Debug.LogWarning($"{name}: null {nameof(cameraController)}");
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (GameManager.Instance == null)
        {
            yield return null;
        }

        GameManager.Instance.OnGamePaused += BeginOrbit;
        GameManager.Instance.OnGameResumed += EndOrbit;
        GameManager.Instance.OnCrystalDestroyed += BeginOrbitOnCrystal;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused -= BeginOrbit;
            GameManager.Instance.OnGameResumed -= EndOrbit;
            GameManager.Instance.OnCrystalDestroyed -= BeginOrbitOnCrystal;
        }
    }

    private void Update()
    {
        if (!isOrbiting)
        {
            return;
        }

        Quaternion rotation = Quaternion.AngleAxis(orbitSpeed * Time.unscaledDeltaTime, Vector3.up);
        currentOffset = rotation * currentOffset;

        Vector3 targetPosition = pivotPoint + currentOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            transitionSpeed * Time.unscaledDeltaTime
        );

        Vector3 directionToPivot = pivotPoint - transform.position;
        if (directionToPivot != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToPivot, Vector3.up);
        }
    }

    #endregion

    public void BeginOrbit()
    {
        savedPosition = transform.position;
        savedRotation = transform.rotation;

        Ray ray = new(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, 
            pivotLayerMask, QueryTriggerInteraction.Ignore))
        {
            pivotPoint = hit.point;
        }
        else
        {
            // Fallback
            pivotPoint = transform.position + transform.forward * maxRaycastDistance;
        }

        restorePositionAfterOrbit = false;
        StartOrbit();
    }

    public void BeginOrbitOnCrystal()
    {
        GameObject crystal = GameObject.FindWithTag("Crystal");
        if (crystal != null)
        {
            pivotPoint = crystal.transform.position;
        }
        else
        {
            Debug.LogError($"{name}: missing crystal");
        }

        restorePositionAfterOrbit = true;
        StartOrbit();
    }

    private void StartOrbit()
    {
        currentOffset = transform.position - pivotPoint;
        isOrbiting = true;

        if (cameraController != null)
        {
            cameraController.enabled = false;
        }
    }

    public void EndOrbit()
    {
        isOrbiting = false;

        if (restorePositionAfterOrbit)
        {
            return;
        }

        transform.SetPositionAndRotation(savedPosition, savedRotation);

        if (cameraController != null)
        {
            cameraController.enabled = true;
        }
    }
}