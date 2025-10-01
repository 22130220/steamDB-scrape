using System.Collections.Generic;
using System.Linq;
using MySqlConnector;
using Dapper;

namespace SteamWebPipes.Events
{
    internal class ChangelistEvent : AbstractEvent
    {
        private struct AppData
        {
            public uint AppID { get; set; }
            public string Name { get; set; }
            public string LastKnownName { get; set; }
        }

        private struct PackageData
        {
            public uint SubID { get; set; }
            public string LastKnownName { get; set; }
        }

        public uint ChangeNumber { get; private set; }
        public Dictionary<string, string> Apps { get; private set; }
        public Dictionary<string, string> Packages { get; private set; }

        public ChangelistEvent(SteamChangelist changelist)
            : base("Changelist")
        {
        }
    }
}
