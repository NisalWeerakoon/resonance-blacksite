using UnityEngine;

namespace Blacksite.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypePlayerController : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float mouseSensitivity = 1.5f;
        private CharacterController character;
        private float verticalSpeed;
        private float pitch;

        private void Awake()
        {
            character = GetComponent<CharacterController>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (Input.GetMouseButtonDown(0)) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                transform.Rotate(0f, Input.GetAxis("Mouse X") * mouseSensitivity, 0f);
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -85f, 85f);
                if (viewCamera != null) viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            Vector3 move = transform.right * Input.GetAxisRaw("Horizontal") +
                           transform.forward * Input.GetAxisRaw("Vertical");
            move = Vector3.ClampMagnitude(move, 1f) * moveSpeed;
            if (character.isGrounded) verticalSpeed = -1f;
            verticalSpeed -= 9.81f * Time.deltaTime;
            character.Move((move + Vector3.up * verticalSpeed) * Time.deltaTime);
        }
    }
}
