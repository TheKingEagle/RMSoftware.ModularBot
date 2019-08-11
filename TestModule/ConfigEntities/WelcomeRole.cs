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
    class WelcomeRole : ConfigEntity
    {
        public WelcomeRole()
        {
            ReadOnly = true;
            ConfigIdentifier = "WelcomeRole";
        }

        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            if(TestModuleService.BoundItems.TryGetValue(Context.Guild.Id,out GuildQueryItem gqi))
            {
                return base.ExecuteView(_DiscordNet, Context, gqi.RoleToAssign?.Id.ToString());
            }
            return base.ExecuteView(_DiscordNet, Context, "Not Configured");

        }
    }
}
