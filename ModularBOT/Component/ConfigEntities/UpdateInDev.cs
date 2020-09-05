using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using ModularBOT.Entity;
using ModularBOT.Component;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace ModularBOT.Component.ConfigEntities
{
    internal class UpdateInDev : ConfigEntity
    {
        public UpdateInDev()
        {
            ReadOnly = false;
            ConfigIdentifier = "UpdateInDev";
        }

        public override EmbedFieldBuilder ExecuteView(DiscordNET _DiscordNet, ICommandContext Context, bool inline)
        {
            EmbedFieldBuilder efb = new EmbedFieldBuilder()
            {
                Value = _DiscordNet.serviceProvider.GetRequiredService<Configuration>().UseInDevChannel.Value ? "`True`" : "`False`",
                Name = ConfigIdentifier,
                IsInline = inline
            };
            return efb;
        }

        public override string ExecuteView(DiscordNET _discordNET, ICommandContext Context)
        {
            return base.ExecuteView(_discordNET, Context, 
                _discordNET.serviceProvider.GetRequiredService<Configuration>().UseInDevChannel.Value ? "True" : "False");
        }

        public override Task ExecuteSet(DiscordShardedClient Client, DiscordNET _discordNET, ICommandContext Context, string value)
        {
            if(_discordNET.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                return Context.Channel.SendMessageAsync("", false, _discordNET.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
            }
            if(!bool.TryParse(value,out bool newval))
            {
                return Context.Channel.SendMessageAsync("", false, _discordNET.CustomCMDMgr.GetEmbeddedMessage(Context, 
                    "Unexpected Value", "This configuration only accepts a boolean value `True` or `False`.", Color.DarkRed));
            }
            _discordNET.serviceProvider.GetRequiredService<Configuration>().UseInDevChannel = newval;
            _discordNET.serviceProvider.GetRequiredService<ConfigurationManager>().Save();

            return Context.Channel.SendMessageAsync("", false, this.GetEmbeddedMessage(_discordNET.serviceProvider.GetRequiredService<ConsoleIO>(),Context,
                    "Configuration Updated", $"Program uses pre-release update channel: `{newval}`.\r\n\r\nThis change will not be effective until the program is restarted.", Color.Green));
        }
    }
}
