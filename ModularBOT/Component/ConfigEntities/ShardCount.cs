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
    internal class ShardCount : ConfigEntity
    {
        public ShardCount()
        {
            ReadOnly = false;
            ConfigIdentifier = "ShardCount";
        }

        public override EmbedFieldBuilder ExecuteView(DiscordNET _DiscordNet, ICommandContext Context, bool inline)
        {
            EmbedFieldBuilder efb = new EmbedFieldBuilder()
            {
                Value = $"`{_DiscordNet.serviceProvider.GetRequiredService<Configuration>().ShardCount}`",
                Name = ConfigIdentifier,
                IsInline = inline
            };
            return efb;
        }
        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            return base.ExecuteView(_DiscordNet, Context,_DiscordNet.serviceProvider.GetRequiredService<Configuration>().ShardCount.ToString());
        }

        public override Task ExecuteSet(DiscordShardedClient Client, DiscordNET _discordNET, ICommandContext Context, string value)
        {
            if (_discordNET.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                return Context.Channel.SendMessageAsync("", false, _discordNET.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
            }
            if (!int.TryParse(value, out int newval))
            {
                return Context.Channel.SendMessageAsync("", false, _discordNET.CustomCMDMgr.GetEmbeddedMessage(Context,
                    "Unexpected Value", "This configuration only accepts a valid 32-bit integer greater than zero.", Color.DarkRed));
            }
            if (newval < 0)
            {
                return Context.Channel.SendMessageAsync("", false, _discordNET.CustomCMDMgr.GetEmbeddedMessage(Context,
                    "Unexpected Value", "This configuration only accepts a valid 32-bit integer greater than zero.", Color.DarkRed));
            }
            _discordNET.serviceProvider.GetRequiredService<Configuration>().ShardCount = newval;
            _discordNET.serviceProvider.GetRequiredService<ConfigurationManager>().Save();
            return Context.Channel.SendMessageAsync("", false, _discordNET.CustomCMDMgr.GetEmbeddedMessage(Context,
                    "Configuration Updated", $"Client will connect to Discord with `{newval}` shard(s).\r\n\r\nThis change will not be effective until the program is restarted.", Color.Green));
        }
    }
}
