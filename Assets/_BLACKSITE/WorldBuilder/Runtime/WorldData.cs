using UnityEngine;

namespace Blacksite7.WorldBuilder
{
    // This data is intentionally small and independent from teammates' scripts.
    // Replace/adapt it to the agreed project contracts during integration.
    public class RoomVolumeData : MonoBehaviour
    {
        public string roomId;
        public string displayName;
        public Vector2 footprint;
    }

    public class DoorPortalData : MonoBehaviour
    {
        public string doorId;
        public string roomA;
        public string roomB;
        public bool isOpen = true;
        public bool isLocked;
        public bool blocksNavigation;
        [Min(0)] public float openAcousticCost = 1f;
        [Min(0)] public float closedAcousticCost = 4f;
        [Min(0)] public float sealedAcousticCost = 6f;

        public float AcousticCost => isLocked ? sealedAcousticCost : (isOpen ? openAcousticCost : closedAcousticCost);
    }

    public class CoverPointData : MonoBehaviour
    {
        public string coverId;
    }

    public class InterceptPointData : MonoBehaviour
    {
        public string pointId;
    }
}
