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
    internal class SniperQueueSize : ConfigEntity
    {
        public SniperQueueSize()
        {
            ReadOnly = false;
            ConfigIdentifier = "SniperQueueSize";
        }
        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {

            var sniper = TestModuleService.SniperGuilds.FirstOrDefault(x => x.GuildID == Context.Guild.Id);
            if (sniper == null) return base.ExecuteView(_DiscordNet, Context, "Not Configured");
            else return base.ExecuteView(_DiscordNet, Context, sniper.QueueSize.ToString());
        }

        public override async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _discordNET, ICommandContext Context, string value)
        {
            if (_discordNET.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator && !(Context.User as SocketGuildUser).GuildPermissions.Has(Discord.GuildPermission.ManageChannels))
            {
                await Context.Channel.SendMessageAsync("", false, TestModuleService.GetEmbeddedMessage(Context, "Insufficient Permission", "You need the ability to manage channels OR have `AccessLevels.Administrator`", Discord.Color.DarkRed));
                return;
            }
            if(!int.TryParse(value, out int v))
            {
                await Context.Channel.SendMessageAsync("", false, TestModuleService.GetEmbeddedMessage(Context, "Unexpected Value", $"Please specify a value between `1` and `{int.MaxValue}`", Discord.Color.DarkRed));
                return;
            }
            await TestModuleService.SetSniperQueueSize(Context, v);

        }
    }
}
