using UnityEngine;

namespace Blacksite.Agents.Trace
{
    public sealed class TraceInterceptPoint : MonoBehaviour
    {
        [SerializeField] private string nodeId;
        [Range(0f, 1f)] [SerializeField] private float chokeImportance = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float historicalUse = 0.5f;
        public string NodeId => nodeId;
        public float ChokeImportance => chokeImportance;
        public float HistoricalUse => historicalUse;
    }
}
