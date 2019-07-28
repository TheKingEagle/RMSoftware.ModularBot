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
    internal class GlobalCommandPrefix : ConfigEntity
    {
        public GlobalCommandPrefix()
        {
            ReadOnly = false;
            ConfigIdentifier = "GlobalCommandPrefix";
        }
        public override async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _DiscordNet, ICommandContext Context, string value)
        {
            var ConsoleIO = _DiscordNet.serviceProvider.GetRequiredService<ConsoleIO>();
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if (string.IsNullOrWhiteSpace(value) || value.Contains('`'))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(ConsoleIO,Context,"Invalid prefix", "Your prefix must not start with whitespace, or contain invalid characters!", Color.Red));
                return;
            }
            _DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix = value;
            _DiscordNet.serviceProvider.GetRequiredService<ConfigurationManager>().Save();
            await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(ConsoleIO, Context, "Config Updated", $"`GlobalCommandPrefix` updated to `{value}`", Color.Green));
            return;
        }

        public override EmbedFieldBuilder ExecuteView(DiscordNET _DiscordNet, ICommandContext Context, bool inline)
        {
            

            EmbedFieldBuilder efb = new EmbedFieldBuilder()
            {
                Value = $"`{_DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix}`",
                Name = ConfigIdentifier,
                IsInline = inline
            };
            return efb;
        }

        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            return base.ExecuteView(_DiscordNet, Context, _DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix);
        }
    }
}
