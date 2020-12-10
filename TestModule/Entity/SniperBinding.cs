using System.Collections.Generic;

namespace TestModule
{
    public class SniperBinding
    {
        public ulong GuildID { get; set; }
        public List<DeletedMessage> DeletedMessages { get; set; }
        public int QueueSize { get; set; } = 100;
    }

    
}
