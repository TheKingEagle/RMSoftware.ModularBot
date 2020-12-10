using System;
using System.Collections.Generic;
using Discord;

namespace TestModule
{
    public class DeletedEmbed
    {
        public string EAuthorIconURL { get; set; }
        public string EAuthorName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<DeletedEmbedField> Fields { get; set; }
        public string ThumbnailURL { get; set; }
        public string ImageURL { get; set; }
        public string FooterImageURL { get; set; }
        public string FooterText { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public Color Color { get; set; }
    }

    
}
