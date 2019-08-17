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
    internal class ServerMuteRole : ConfigEntity
    {
        public ServerMuteRole()
        {
            ReadOnly = true;
            ConfigIdentifier = "ServerMuteRole";
        }
        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            return base.ExecuteView(_DiscordNet, Context,TestModuleService.MLbindings.FirstOrDefault(x=>x.GuildID == Context.Guild.Id)?.MuteRoleID.ToString() ?? "Not Configured");
        }
    }
}
