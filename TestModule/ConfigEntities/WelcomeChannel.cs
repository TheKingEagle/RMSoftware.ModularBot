using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using ModularBOT.Component;
using ModularBOT.Entity;
namespace TestModule.ConfigEntities
{
    public class WelcomeChannel:ConfigEntity
    {
        public WelcomeChannel()
        {
            ReadOnly = true;
            ConfigIdentifier = "WelcomeChannel";
        }
        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            if (TestModuleService.BoundItems.TryGetValue(Context.Guild.Id, out GuildQueryItem gqi))
            {
                return base.ExecuteView(_DiscordNet, Context, gqi.DefaultChannel?.Id.ToString());
            }
            return base.ExecuteView(_DiscordNet, Context, "Not Configured");
        }
    }
}
