using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float verticalSpeed = 10f;
    public float zoomSpeed = 100f;
    
    void Start()
    {
        
    }

    void Update()
    {
        // Movimiento horizontal en el plano XZ
        float horizontal = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 horizontalMovement = new Vector3(horizontal, 0f, verticalInput);
        transform.Translate(horizontalMovement * movementSpeed * Time.deltaTime, Space.World);

        // Movimiento vertical con E (subir) y Q (bajar)
        if (Input.GetKey(KeyCode.E))
        {
            transform.Translate(Vector3.up * verticalSpeed * Time.deltaTime, Space.World);
        }
        
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Translate(Vector3.down * verticalSpeed * Time.deltaTime, Space.World);
        }

        // Zoom usando la rueda del mouse
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            transform.Translate(Vector3.forward * scroll * zoomSpeed * Time.deltaTime, Space.Self);
        }
    }
}
