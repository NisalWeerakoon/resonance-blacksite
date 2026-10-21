using UnityEngine;
using UnityEngine.Rendering;

namespace Blacksite.Agents.Rift
{
    /// <summary>
    /// Ensures RIFT's actual model meshes cast a character-shaped shadow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RiftCharacterShadow : MonoBehaviour
    {
        private Renderer[] modelRenderers;

        private void OnEnable()
        {
            modelRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (var modelRenderer in modelRenderers)
            {
                modelRenderer.shadowCastingMode = ShadowCastingMode.On;
                modelRenderer.receiveShadows = true;
            }
        }
    }
}
