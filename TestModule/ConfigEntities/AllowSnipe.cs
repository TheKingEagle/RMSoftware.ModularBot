using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using ModularBOT.Component;
using ModularBOT.Entity;
namespace TestModule.ConfigEntities
{
    internal class AllowSnipe : ConfigEntity
    {
        public AllowSnipe()
        {
            ReadOnly = false;
            ConfigIdentifier = "AllowSnipe";
        }
        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            if(TestModuleService.SniperGuilds?.Count <=0)
            {
                _DiscordNet.serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Warning, "SNIPER", "Snipe list was empty. Attempting to re-fetch data from disk."));

                TestModuleService.ReloadSnipeList();
                if(TestModuleService.SniperGuilds?.Count <= 0)
                {
                    _DiscordNet.serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Warning, "SNIPER", "TMS Reloaded snipe list, but the list still yielded no data."));
                }
            }
            var sniper = TestModuleService.SniperGuilds.FirstOrDefault(x => x.GuildID == Context.Guild.Id);
            
            if (sniper == null) return base.ExecuteView(_DiscordNet, Context, "False");
            else return base.ExecuteView(_DiscordNet, Context, "True");
        }

        public override async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _discordNET, ICommandContext Context, string value)
        {
            if (_discordNET.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator && !(Context.User as SocketGuildUser).GuildPermissions.Has(Discord.GuildPermission.ManageGuild))
            {
                await Context.Channel.SendMessageAsync("", false, TestModuleService.GetEmbeddedMessage(Context, "Insufficient Permission", "You need the ability to manage guild OR have `AccessLevels.Administrator`", Discord.Color.DarkRed));
                return;
            }

            if (bool.TryParse(value, out bool result))
            {
                if (result) await TestModuleService.BindSniper(Context);
                if (!result) await TestModuleService.UnBindSniper(Context);
            }
            else
            {
                await Context.Channel.SendMessageAsync("", false, TestModuleService.GetEmbeddedMessage(Context, "Unexpected Value", "This item must be a BOOLEAN value. `True` or `False`.", Discord.Color.DarkRed));
                return;
            }
            await Task.Delay(1);

        }
    }
}
