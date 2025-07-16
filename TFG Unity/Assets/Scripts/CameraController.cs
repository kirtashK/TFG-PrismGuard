using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    [Header("Velocidades")]

    [SerializeField]
    [Tooltip("Velocidad de movimiento horizontal")]
    [Range(5f, 30f)]
    private float movementSpeed = 15f;

    [SerializeField]
    [Tooltip("Velocidad de movimiento vertical")]
    [Range(1f, 20f)]
    private float verticalSpeed = 10f;

    [SerializeField]
    [Tooltip("Velocidad del zoom")]
    [Range(50f, 150f)]
    private float zoomSpeed = 100f;

    [SerializeField]
    [Tooltip("Multiplicador de velocidad al pulsar Shift")]
    [Range(1f, 5f)]
    private float speedMultiplier = 2f;


    [Header("Sensibilidad")]

    [SerializeField]
    [Tooltip("Sensibilidad")]
    [Range(0.01f, 1f)]
    private float lookSensitivity = 0.1f;


    [Header("Pitch")]

    [SerializeField]
    [Tooltip("Ángulo mínimo de pitch")]
    [Range(-90f, 0f)]
    private float pitchMin = -45f;

    [SerializeField]
    [Tooltip("Ángulo máximo de pitch")]
    [Range(0f, 90f)]
    private float pitchMax = 45f;


    [Header("Field Of View")]

    [SerializeField]
    [Range(15f, 60f)]
    private float fovMin = 15f;

    [SerializeField]
    [Range(15f, 60f)]
    private float fovMax = 60f;


    private InputAction moveAction;
    private InputAction verticalAction;
    private InputAction zoomAction;
    private InputAction lookAction;

    private float yaw;
    private float pitch;
    private Camera cam;

    private void Awake()
    {
        moveAction = new InputAction("Move");
        moveAction.AddCompositeBinding("2DVector")
        .With("Up", "<Keyboard>/w")
        .With("Down", "<Keyboard>/s")
        .With("Left", "<Keyboard>/a")
        .With("Right", "<Keyboard>/d");

        verticalAction = new InputAction("Vertical");
        verticalAction.AddCompositeBinding("1DAxis")
        .With("Positive", "<Keyboard>/e")
        .With("Negative", "<Keyboard>/q");

        zoomAction = new InputAction("Zoom", binding: "<Mouse>/scroll");

        lookAction = new InputAction("Look", binding: "<Mouse>/delta");

        moveAction.Enable();
        verticalAction.Enable();
        zoomAction.Enable();
        lookAction.Enable();

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("Este objeto no tiene Camera.");
        }
    }

    private void OnDestroy()
    {
        moveAction.Dispose();
        verticalAction.Dispose();
        zoomAction.Dispose();
        lookAction.Dispose();
    }

    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            HandleMovement();
            HandleRotation();
            HandleZoom();
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        float multiplier = 1f;
        if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
        {
            multiplier = speedMultiplier;
        }

        Vector3 horizontalMove = (right * moveInput.x + forward * moveInput.y) * movementSpeed * multiplier * Time.deltaTime;

        float verticalInput = verticalAction.ReadValue<float>();
        Vector3 verticalMove = Vector3.up * verticalInput * verticalSpeed * multiplier * Time.deltaTime;

        transform.Translate(horizontalMove + verticalMove, Space.World);
    }

    private void HandleRotation()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        yaw += lookInput.x * lookSensitivity;
        pitch += -lookInput.y * lookSensitivity;

        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        transform.eulerAngles = new Vector3(pitch, yaw, 0f);
    }

    private void HandleZoom()
    {
        Vector2 scrollInput = zoomAction.ReadValue<Vector2>();
        float scroll = scrollInput.y;

        if (Mathf.Abs(scroll) > 0.01f && cam != null)
        {
            float newFOV = cam.fieldOfView - scroll * zoomSpeed * Time.deltaTime;
            cam.fieldOfView = Mathf.Clamp(newFOV, fovMin, fovMax);
        }
    }
}
