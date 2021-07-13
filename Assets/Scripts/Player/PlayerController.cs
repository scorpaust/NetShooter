using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform viewPoint;

    public float mouseSensitivity = 1f;

    public float moveSpeed = 5f, runSpeed = 8f;

    public float jumpForce = 5f;

    public float gravityMode = 2.5f;

    public bool invertLook;

    public Transform groundCheckPoint;

    public LayerMask groundLayer;

    public CharacterController charControl;

    private float verticalRotStore;

    private Vector2 mouseInput;

    private Vector3 moveDir, movement;

    private float activeMoveSpeed;

    private Camera cam;

    private bool isGrounded;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;

        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        verticalRotStore += mouseInput.y;

        verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);

        if (invertLook)
		{
            viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }
        else
		{
            viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }

        moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if (Input.GetKey(KeyCode.LeftShift))
		{
            activeMoveSpeed = runSpeed;
		}
        else
		{
            activeMoveSpeed = moveSpeed;
		}

        float yVel = movement.y;

        movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;

        if (!charControl.isGrounded)
            movement.y = yVel;
        else
            movement.y = 0f;


        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, 0.25f, groundLayer);

        if (Input.GetButtonDown("Jump") && isGrounded)
		{
            movement.y = jumpForce;
		}

        movement.y += Physics.gravity.y * Time.deltaTime * gravityMode;

        charControl.Move(movement * Time.deltaTime);

        if (Input.GetMouseButtonDown(0))
		{
            Shoot();
		}

        if (Input.GetKeyDown(KeyCode.Escape))
		{
            Cursor.lockState = CursorLockMode.None;
		} else if (Cursor.lockState == CursorLockMode.None)
		{
            if (Input.GetMouseButtonDown(0))
			{
                Cursor.lockState = CursorLockMode.Locked;
			}
		}
    }

    private void Shoot()
	{
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        ray.origin = cam.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit)) 
        {
            Debug.Log("We hit " + hit.collider.gameObject.name);
        }
	}

	private void LateUpdate()
	{
        cam.transform.position = viewPoint.position;

        cam.transform.rotation = viewPoint.rotation;
	}
}
