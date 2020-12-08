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
    internal class ModlogAliasMode : ConfigEntity
    {
        public ModlogAliasMode()
        {
            ReadOnly = false;
            ConfigIdentifier = "ModlogAliasMode";
        }
        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            ModLogBinding ml = TestModuleService.MLbindings.FirstOrDefault(x => x.GuildID == Context.Guild.Id);
            if (ml != null)
            {
                return base.ExecuteView(_DiscordNet, Context, ml.UseAlias.ToString());
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
            await TestModuleService.MLSetAliasMode(Context, aliasmode);
            
        }
    }
}
