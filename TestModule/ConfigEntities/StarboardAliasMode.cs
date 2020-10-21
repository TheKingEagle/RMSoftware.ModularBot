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
    internal class StarboardAliasMode : ConfigEntity
    {
        public StarboardAliasMode()
        {
            ReadOnly = false;
            ConfigIdentifier = "StarboardAliasMode";
        }
        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            if(TestModuleService.SBBindings.TryGetValue(Context.Guild.Id,out StarboardBinding binding))
            {
                return base.ExecuteView(_DiscordNet, Context, binding.UseAlias.ToString());
            }
            return base.ExecuteView(_DiscordNet, Context, "Not Configured");
        }

        public override async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _discordNET, ICommandContext Context, string value)
        {
            if (_discordNET.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator && !(Context.User as SocketGuildUser).GuildPermissions.Has(Discord.GuildPermission.ManageChannels))
            {
                await Context.Channel.SendMessageAsync("", false, TestModuleService.GetEmbeddedMessage(Context, "Insufficient Permission", "You need the ability to manage channels OR have `AccessLevels.Administrator`", Discord.Color.DarkRed));
                return;
            }
            if (!bool.TryParse(value, out bool aliasmode))
            {
                await Context.Channel.SendMessageAsync("", false, 
                    TestModuleService.GetEmbeddedMessage(Context, "Unexpected Value", "Value must be `true` or `false`.", Discord.Color.DarkRed));
                return;
            }
            await TestModuleService.SBSetAliasMode(Context, aliasmode);
            
        }
    }
}
