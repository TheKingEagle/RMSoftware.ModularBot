using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using ModularBOT.Component;
using ModularBOT.Entity;
namespace TestModule.ConfigEntities
{
    internal class StarboardChannel : ConfigEntity
    {
        public StarboardChannel()
        {
            ReadOnly = false;
            ConfigIdentifier = "StarboardChannel";
        }
        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            //throw new NotImplementedException("Pending implementation");
            //TODO: Implement StarboardChannel Config Entity;.SpyroLove
            if(TestModuleService.SBBindings.TryGetValue(Context.Guild.Id,out StarboardBinding binding))
            {
                return base.ExecuteView(_DiscordNet, Context, binding.ChannelID.ToString());
            }
            return base.ExecuteView(_DiscordNet, Context, "Not Configured");
        }

        public override async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _discordNET, ICommandContext Context, string value)
        {
            if(!ulong.TryParse(value, out ulong channelid))
            {
                await Context.Channel.SendMessageAsync("", false, 
                    TestModuleService.GetEmbeddedMessage(Context, "Invalid ID", "Value must be a valid number.", Discord.Color.DarkRed));
            }
            await TestModuleService.BindStarboard(Context,channelid);
            
        }
    }
}
