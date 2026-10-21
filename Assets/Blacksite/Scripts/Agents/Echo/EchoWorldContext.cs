using System;
using System.Collections.Generic;
using Blacksite.Shared;
using UnityEngine;

namespace Blacksite.Agents.Echo
{
    /// <summary>
    /// Demo world adapter. Keeps acoustic portals and blocked spatial portals in sync.
    /// Does not depend on the World Builder's separate _BLACKSITE scripts.
    /// </summary>
    public sealed class EchoWorldContext : MonoBehaviour
    {
        public event Action<string> PortalChanged;
        private EchoAcousticGraph acoustic;
        private EchoSpatialWaypointGraph spatial;
        private readonly HashSet<string> blocked = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, EchoDemoDoor> doors =
            new Dictionary<string, EchoDemoDoor>(StringComparer.Ordinal);

        public EchoAcousticGraph Acoustic => acoustic ?? (acoustic = EchoAcousticGraph.CreateDefault());
        public EchoSpatialWaypointGraph Spatial => spatial ?? (spatial = new EchoSpatialWaypointGraph());
        public IReadOnlyCollection<string> BlockedPortalIds => blocked;

        public string ClosestRoom(Vector3 worldPosition)
        {
            string room = EchoRoomIds.Hub;
            float nearest = float.PositiveInfinity;
            foreach (string candidate in EchoRoomIds.All)
            {
                float sq = (Spatial.Position(candidate) - new Vector3(worldPosition.x, 0f, worldPosition.z)).sqrMagnitude;
                if (sq < nearest) { nearest = sq; room = candidate; }
            }
            return room;
        }

        public void RegisterDoor(EchoDemoDoor door)
        {
            if (door == null || string.IsNullOrEmpty(door.PortalId)) return;
            doors[door.PortalId] = door;
            if (Acoustic.TryGetPortalState(door.PortalId, out EchoDoorAcousticState state))
                door.SetVisuals(state);
        }

        public bool SetPortal(string portalId, EchoDoorAcousticState newState)
        {
            if (!Acoustic.TrySetPortalState(portalId, newState)) return false;
            if (newState == EchoDoorAcousticState.Open) blocked.Remove(portalId);
            else blocked.Add(portalId);
            if (doors.TryGetValue(portalId, out EchoDemoDoor door) && door != null)
                door.SetVisuals(newState);
            PortalChanged?.Invoke(portalId);
            return true;
        }

        public bool TogglePortal(string portalId)
        {
            if (!Acoustic.TryGetPortalState(portalId, out EchoDoorAcousticState current)) return false;
            return SetPortal(portalId, current == EchoDoorAcousticState.Open
                ? EchoDoorAcousticState.Closed : EchoDoorAcousticState.Open);
        }

        /// <summary>Physical path via the team's GraphAStar; no movement performed here.</summary>
        public bool TrySpatialPath(Vector3 start, Vector3 target, List<Vector3> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            var nodes = new List<string>();
            string a = ClosestRoom(start);
            string b = ClosestRoom(target);
            // Preserve actual bypass topology: when replanning near its bend,
            // a larger project can replace this demo adapter with its real waypoint provider.
            if (!GraphAStar.TryFind(Spatial, a, b, blocked, nodes)) return false;
            for (int i = 0; i < nodes.Count; i++)
                output.Add(Spatial.Position(nodes[i]));
            if (output.Count > 0) output[0] = new Vector3(start.x, 0f, start.z);
            if (output.Count > 0) output.Add(new Vector3(target.x, 0f, target.z));
            return true;
        }
    }
}
