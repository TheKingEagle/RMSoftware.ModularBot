using System.Collections.Generic;

namespace TestModule
{
    public class StarboardBinding
    {
        public ulong ChannelID { get; set; }
        public bool UseAlias { get; set; } = false;
        public List<SBEntry> StarboardData { get; set; }
    }

    
}
