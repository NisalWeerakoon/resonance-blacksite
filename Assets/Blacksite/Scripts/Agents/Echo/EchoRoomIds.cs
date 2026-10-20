using System;
using System.Collections.Generic;

namespace Blacksite.Agents.Echo
{
    /// <summary>Stable room identifiers shared with the world builder.</summary>
    public static class EchoRoomIds
    {
        public const string Entry = "ENTRY";
        public const string Hub = "HUB";
        public const string Acoustic = "ACOUSTIC";
        public const string Security = "SECURITY";
        public const string Maintenance = "MAINT";
        public const string Core = "CORE";

        private static readonly IReadOnlyList<string> KnownRooms = Array.AsReadOnly(new[]
        {
            Entry, Hub, Acoustic, Security, Maintenance, Core
        });

        public static IReadOnlyList<string> All => KnownRooms;

        public static bool IsValid(string roomId)
        {
            foreach (string knownRoom in KnownRooms)
                if (string.Equals(knownRoom, roomId, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
