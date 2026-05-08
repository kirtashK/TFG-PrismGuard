using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Speeds")]

    [SerializeField]
    [Tooltip("Horizontal pan speed (keyboard)")]
    [Range(1f, 100f)]
    private float panSpeed = 15f;

    [SerializeField]
    [Tooltip("Multiplier when holding Shift")]
    [Range(1f, 5f)]
    private float speedMultiplier = 2f;

    [SerializeField]
    [Tooltip("Mouse drag pan sensitivity (right mouse)")]
    [Range(0.01f, 5f)]
    private float dragPanSensitivity = 0.5f;

    [SerializeField]
    [Tooltip("Rotate sensitivity (mouse delta while holding middle mouse)")]
    [Range(0.01f, 3f)]
    private float rotateSensitivity = 0.2f;

    [SerializeField]
    [Tooltip("Vertical (height) change speed via scroll wheel")]
    [Range(5f, 200f)]
    private float verticalScrollSpeed = 80f;


    [Header("Pitch")]

    [SerializeField]
    [Tooltip("Minimun pitch angle")]
    [Range(-89f, 0f)]
    private float pitchMin = -60f;

    [SerializeField]
    [Tooltip("Maximun pitch angle")]
    [Range(0f, 89f)]
    private float pitchMax = 60f;


    [Header("Zoom / Height limits")]

    [SerializeField]
    [Tooltip("Minimum camera height")]
    private float minHeight = 5f;

    [SerializeField]
    [Tooltip("Maximum camera height")]
    private float maxHeight = 80f;

    private float yaw;
    private float pitch;

    private Camera cam;

    private InputSystem_Actions.CameraActions cameraActions;
    private bool inputReady;

    #region Unity methods

    private void Awake()
    {
        cam = GetComponent<Camera>();

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (InputManager.Instance == null)
        {
            yield return null;
        }

        cameraActions = InputManager.Instance.Camera;
        inputReady = true;
    }

    private void OnDisable()
    {
        inputReady = false;
    }

    private void Update()
    {
        if (!inputReady)
        {
            return;
        }

        HandleKeyboardPan();

        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (!pointerOverUI)
        {
            HandleRightDragPan();
            HandleRotationMode();
            HandleScrollVertical();
        }
        else
        {
            RestoreCursorIfNeeded();
        }
    }

    #endregion

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleKeyboardPan()
    {
        Vector2 moveInput = cameraActions.Move.ReadValue<Vector2>();

        float multiplier = 1f;
        if (cameraActions.SpeedModifier.IsPressed())
        {
            multiplier = speedMultiplier;
        }

        if (moveInput.sqrMagnitude > 0.0001f)
        {
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = cam.transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 move = multiplier * panSpeed * Time.unscaledDeltaTime * (right * moveInput.x + forward * moveInput.y);
            transform.Translate(move, Space.World);
        }
    }

    private void HandleRightDragPan()
    {
        float dragPanHeld = cameraActions.DragPan.ReadValue<float>();
        if (dragPanHeld > 0f)
        {
            Vector2 mouseDelta = cameraActions.Look.ReadValue<Vector2>();

            Vector3 right = cam.transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 deltaWorld = dragPanSensitivity * Time.unscaledDeltaTime * (-right * mouseDelta.x + -forward * mouseDelta.y);
            transform.Translate(deltaWorld, Space.World);
        }
    }

    private void HandleRotationMode()
    {
        float rotateHeld = cameraActions.RotateMode.ReadValue<float>();
        if (rotateHeld > 0f)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Vector2 delta = cameraActions.Look.ReadValue<Vector2>();
            yaw += delta.x * rotateSensitivity;
            pitch += -delta.y * rotateSensitivity;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }
        else
        {
            RestoreCursorIfNeeded();
        }
    }

    private void RestoreCursorIfNeeded()
    {
        if (Cursor.lockState != CursorLockMode.None || Cursor.visible == false)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleScrollVertical()
    {
        Vector2 scroll = cameraActions.Zoom.ReadValue<Vector2>();
        float scrollMove = -scroll.y;

        if (Mathf.Abs(scrollMove) > 0.001f)
        {
            float multiplier = 1f;
            if (cameraActions.SpeedModifier.IsPressed())
            {
                multiplier = speedMultiplier;
            }

            Vector3 position = transform.position;
            position.y += scrollMove * verticalScrollSpeed * multiplier * Time.unscaledDeltaTime;
            position.y = Mathf.Clamp(position.y, minHeight, maxHeight);
            transform.position = position;
        }
    }
}
