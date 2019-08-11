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
    internal class ModLogChannel : ConfigEntity
    {
        public ModLogChannel()
        {
            ReadOnly = true;
            ConfigIdentifier = "ModLogChannel";
        }
        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            return base.ExecuteView(_DiscordNet, Context,TestModuleService.MLbindings.FirstOrDefault(x=>x.GuildID == Context.Guild.Id)?.ChannelID.ToString() ?? "Not Configured");
        }
    }
}
