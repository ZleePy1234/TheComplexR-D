using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float gamepadDeadzone = 0.1f;
    [SerializeField] private float gamepadRotSmoothing = 1000f;

    [SerializeField] private bool isGamepad;
    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 aimInput;
    private Vector3 playerVelocity;

    private PlayerControls playerControls;
    private PlayerInput playerInput;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerControls = new PlayerControls();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }
    private void OnDisable()
    {
        playerControls.Disable();
    }

    void Update()
    {
        InputHandler();
        MovementHandler();
        RotationHandler();
    }

    void InputHandler()
    {
        moveInput = playerControls.Controls.Movement.ReadValue<Vector2>();
        aimInput = playerControls.Controls.Aim.ReadValue<Vector2>();
    }
    void MovementHandler()
    {
        Vector3 move = new(moveInput.x, 0, moveInput.y);
        controller.Move(speed * Time.deltaTime * move);

        playerVelocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
    void RotationHandler()
    {
        if (isGamepad)
        {
            if (Mathf.Abs(aimInput.x) > gamepadDeadzone || Mathf.Abs(aimInput.y) > gamepadDeadzone)
            {
                Vector3 playerDirection = Vector3.right * aimInput.x + Vector3.forward * aimInput.y;
                if (playerDirection.sqrMagnitude > 0.00f)
                {
                    Quaternion newrotation = Quaternion.LookRotation(playerDirection, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, newrotation, gamepadRotSmoothing * Time.deltaTime);
                }
            }
        }
        else
        {
            Ray ray = Camera.main.ScreenPointToRay(aimInput);
            Plane groundPlane = new(Vector3.up, Vector3.zero);
            float rayDistance;
            if (groundPlane.Raycast(ray, out rayDistance))
            {
                Vector3 point = ray.GetPoint(rayDistance);
                LookAt(point);
            }
        }
    }

    void LookAt(Vector3 lookPoint)
    {
        Vector3 heightCorrectedPoint = new(lookPoint.x, transform.position.y, lookPoint.z);
        transform.LookAt(heightCorrectedPoint);
    }   
    public void OnDeviceChange(PlayerInput pi)
    {
        isGamepad = pi.currentControlScheme.Equals("Gamepad");
    }
}
