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
    class WelcomeMessage:ConfigEntity
    {
        public WelcomeMessage()
        {
            ReadOnly = true;
            ConfigIdentifier = "WelcomeMessage";

        }

        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            if (TestModuleService.BoundItems.TryGetValue(Context.Guild.Id, out GuildQueryItem gqi))
            {
                return base.ExecuteView(_DiscordNet, Context, gqi.WelcomeMessage.ToString());
            }
            return base.ExecuteView(_DiscordNet, Context, "Not Configured");
        }
    }
}
