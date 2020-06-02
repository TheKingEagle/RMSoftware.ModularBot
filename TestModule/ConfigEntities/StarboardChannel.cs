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
            if(!(Context.User as SocketGuildUser).GuildPermissions.Has(Discord.GuildPermission.ManageChannels))
            {
                await Context.Channel.SendMessageAsync("",false,TestModuleService.GetEmbeddedMessage(Context, "Insufficient Permission", "You need the ability to manage channels.",Discord.Color.DarkRed));
                return;
            }
            if(!ulong.TryParse(value, out ulong channelid))
            {
                await Context.Channel.SendMessageAsync("", false, 
                    TestModuleService.GetEmbeddedMessage(Context, "Invalid ID", "Value must be a valid number.", Discord.Color.DarkRed));
                return;
            }
            if(channelid == 0)
            {
                await TestModuleService.UnbindStarboard(Context);
                return;
            }
            if ((await Context.Guild.GetChannelAsync(channelid) as SocketTextChannel) == null)
            {
                await Context.Channel.SendMessageAsync("", false,
                    TestModuleService.GetEmbeddedMessage(Context, "Invalid Channel", "the ID must point to a text channel.", Discord.Color.DarkRed));
                return;
            }

            await TestModuleService.BindStarboard(Context, channelid);
            
        }
    }
}
