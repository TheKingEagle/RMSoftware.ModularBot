using System;
using System.Collections.Generic;

namespace TestModule
{
    public class DeletedMessage
    {
        public ulong ID { get; set; }
        public string Content { get; set; }
        public List<DeletedEmbed> Embeds { get; set; }
        public ulong AuthorID { get; set; }
        public string AuthorName { get; set; }
        public string AuthorDiscriminator { get; set; }
        public string AuthorAvatarURL { get; set; }
        public ulong ChannelID { get; set; }
        public ulong GuildID { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    
}
