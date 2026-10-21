using UnityEngine;

namespace Blacksite.Agents.Bastion
{
    public sealed class CoverPoint : MonoBehaviour
    {
        [SerializeField] private string coverId = "C01";
        [SerializeField] private Transform peekPosition;
        [SerializeField, Range(0f, 1f)] private float structuralSafety = .5f;
        [SerializeField] private bool occupied;

        public string CoverId => coverId;
        public Vector3 PeekPosition => peekPosition != null ? peekPosition.position : transform.position + Vector3.up;
        public float StructuralSafety => structuralSafety;
        public bool Occupied { get => occupied; set => occupied = value; }
        public void ConfigureSandbox(string id, Transform peek, float safety)
        {
            coverId = id;
            peekPosition = peek;
            structuralSafety = Mathf.Clamp01(safety);
        }
    }
}
