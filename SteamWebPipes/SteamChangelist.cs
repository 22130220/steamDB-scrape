using System.Collections.Generic;

namespace SteamWebPipes
{
    public class SteamChangelist
    {
        public uint ChangeNumber { get; set; }
        public IEnumerable<uint> Apps { get; set; }
        public IEnumerable<uint> Packages { get; set; }
    }
}
