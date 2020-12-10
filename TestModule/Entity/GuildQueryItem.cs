using Discord;

namespace TestModule
{
    public class GuildQueryItem
    {
        public ITextChannel DefaultChannel { get; set; }
        public IRole RoleToAssign { get; set; }
        public string WelcomeMessage { get; set; }
    }

    
}
