using UnityEngine;

namespace Blacksite.Agents.Echo
{
    /// <summary>
    /// Optional Scene/Inspector probe for Stage 1. Does not change real doors.
    /// Attach to an empty GameObject in your personal ECHO test scene.
    /// </summary>
    public sealed class EchoGraphProbe : MonoBehaviour
    {
        private EchoAcousticGraph graph;
        public EchoAcousticGraph Graph => graph ?? (graph = EchoAcousticGraph.CreateDefault());

        [ContextMenu("ECHO / Print Graph Summary")]
        public void PrintGraphSummary()
        {
            Debug.Log($"ECHO acoustic graph: {Graph.RoomCount} rooms, {Graph.PortalCount} portals", this);
            foreach (string roomId in EchoRoomIds.All)
            {
                foreach (EchoAcousticNeighbor neighbor in Graph.Neighbors(roomId))
                    Debug.Log($"{roomId} -> {neighbor.RoomId} via {neighbor.PortalId}: direct cost {neighbor.Cost}", this);
            }
        }

        [ContextMenu("ECHO / Toggle D02 Acoustic State")]
        public void ToggleD02AcousticState()
        {
            Graph.TryGetPortalState("D02", out EchoDoorAcousticState current);
            EchoDoorAcousticState next = current == EchoDoorAcousticState.Open
                ? EchoDoorAcousticState.Closed : EchoDoorAcousticState.Open;
            Graph.TrySetPortalState("D02", next);
            Graph.TryGetDirectCost(EchoRoomIds.Hub, EchoRoomIds.Acoustic, out float cost);
            Debug.Log($"ECHO D02 acoustics: {next}; HUB <-> ACOUSTIC DIRECT edge cost = {cost}", this);
        }
    }
}
