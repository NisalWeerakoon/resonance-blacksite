using UnityEngine;

namespace Blacksite.Agents.Rift
{
    /// <summary>
    /// Adds a soft ground shadow beneath RIFT. The integration scene uses mostly
    /// point lights, so a small blob shadow keeps the character grounded even
    /// when no shadow-casting directional light reaches it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RiftBlobShadow : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float width = 1.2f;
        [SerializeField, Min(0.1f)] private float depth = 0.72f;
        [SerializeField, Range(0.05f, 0.9f)] private float opacity = 0.72f;
        [SerializeField] private float groundOffset = 0.06f;
        [SerializeField, Min(8)] private int textureResolution = 64;

        private GameObject shadowObject;
        private Material shadowMaterial;
        private Texture2D shadowTexture;
        private Mesh shadowMesh;

        private void OnEnable()
        {
            CreateShadow();
        }

        private void OnDisable()
        {
            if (shadowObject != null)
                Destroy(shadowObject);
            if (shadowMaterial != null)
                Destroy(shadowMaterial);
            if (shadowTexture != null)
                Destroy(shadowTexture);
            if (shadowMesh != null)
                Destroy(shadowMesh);
        }

        private void CreateShadow()
        {
            if (shadowObject != null)
                return;

            var shader = Shader.Find("Blacksite/RIFT Soft Ground Shadow");
            if (shader == null)
                return;

            shadowTexture = CreateRadialTexture();
            shadowMaterial = new Material(shader) { name = "RIFT Soft Ground Shadow" };
            if (shadowMaterial.HasProperty("_BaseMap"))
                shadowMaterial.SetTexture("_BaseMap", shadowTexture);
            if (shadowMaterial.HasProperty("_MainTex"))
                shadowMaterial.SetTexture("_MainTex", shadowTexture);
            if (shadowMaterial.HasProperty("_BaseColor"))
                shadowMaterial.SetColor("_BaseColor", new Color(0.08f, 0.28f, 0.32f, 1f));
            if (shadowMaterial.HasProperty("_Color"))
                shadowMaterial.SetColor("_Color", new Color(0.08f, 0.28f, 0.32f, 1f));

            shadowObject = new GameObject("RIFT Ground Shadow");
            shadowObject.transform.SetParent(transform, false);
            shadowObject.transform.localPosition = new Vector3(0f, groundOffset, 0f);
            shadowObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadowObject.transform.localScale = new Vector3(width, depth, 1f);

            var filter = shadowObject.AddComponent<MeshFilter>();
            shadowMesh = CreateQuad();
            filter.sharedMesh = shadowMesh;
            var renderer = shadowObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = shadowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        }

        private Texture2D CreateRadialTexture()
        {
            int size = Mathf.Max(8, textureResolution);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RIFT Soft Shadow Texture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float radius = Mathf.Sqrt(nx * nx + ny * ny);
                float edge = 1f - Mathf.SmoothStep(0.18f, 1f, radius);
                // Keep the texel white so the material tint remains visible;
                // black RGB here would multiply the teal tint back to black.
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, edge * opacity));
            }

            texture.Apply(false, true);
            return texture;
        }

        private static Mesh CreateQuad()
        {
            var mesh = new Mesh { name = "RIFT Shadow Quad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
