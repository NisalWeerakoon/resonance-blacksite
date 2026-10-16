using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Blacksite7.WorldBuilder
{
    // Test-only first-person controller. Remove when your team's player controller is ready.
    [RequireComponent(typeof(CharacterController))]
    public class BeginnerFPSController : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float mouseSensitivity = 0.12f;
        public Transform cameraPivot;
        private CharacterController controller;
        private float pitch;
        private float verticalSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraPivot == null) cameraPivot = GetComponentInChildren<Camera>()?.transform;
        }

        private void Update()
        {
            Vector2 movement = Vector2.zero;
            Vector2 look = Vector2.zero;
            bool click = false;
            bool escape = false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var m = Mouse.current;
            if (k != null)
            {
                movement.x = (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0);
                movement.y = (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0);
                escape = k.escapeKey.wasPressedThisFrame;
            }
            if (m != null)
            {
                look = m.delta.ReadValue();
                click = m.leftButton.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 12f;
            click = Input.GetMouseButtonDown(0);
            escape = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (click) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (escape) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                transform.Rotate(Vector3.up, look.x * mouseSensitivity);
                pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, -80f, 80f);
                if (cameraPivot) cameraPivot.localEulerAngles = Vector3.right * pitch;
            }
            Vector3 planar = transform.TransformDirection(new Vector3(movement.x, 0f, movement.y).normalized) * moveSpeed;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -1f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            controller.Move((planar + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        private void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
