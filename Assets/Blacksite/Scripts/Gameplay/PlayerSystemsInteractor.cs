using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class PlayerSystemsInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private ThrowableDecoy decoyPrefab;
        [SerializeField, Min(0.5f)] private float interactRange = 3f;
        [SerializeField, Min(1f)] private float throwImpulse = 12f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode throwKey = KeyCode.G;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        public void ConfigureSandbox(Camera camera, ThrowableDecoy prefab)
        {
            viewCamera = camera;
            decoyPrefab = prefab;
        }

        private void Update()
        {
            if (viewCamera == null) return;
            if (BlacksiteInput.Pressed(interactKey) &&
                Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                    out RaycastHit hit, interactRange, ~0, QueryTriggerInteraction.Collide))
            {
                DoorPortal door = hit.collider.GetComponentInParent<DoorPortal>();
                if (door != null) door.Toggle();
                else
                {
                    MovableBarricade barricade = hit.collider.GetComponentInParent<MovableBarricade>();
                    if (barricade != null) barricade.Push(viewCamera.transform.forward);
                }
            }
            if (BlacksiteInput.Pressed(throwKey) && decoyPrefab != null)
            {
                Vector3 position = viewCamera.transform.position + viewCamera.transform.forward * 0.8f;
                ThrowableDecoy decoy = Instantiate(decoyPrefab, position, Quaternion.identity);
                decoy.GetComponent<Rigidbody>().AddForce(viewCamera.transform.forward * throwImpulse,
                    ForceMode.Impulse);
            }
        }
    }
}
