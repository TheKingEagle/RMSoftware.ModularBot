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
    internal class LoadCoreModule : ConfigEntity
    {
        public LoadCoreModule()
        {
            ReadOnly = false;
            ConfigIdentifier = "LoadCoreModule";
        }

        public override EmbedFieldBuilder ExecuteView(DiscordNET _DiscordNet, ICommandContext Context, bool inline)
        {
            EmbedFieldBuilder efb = new EmbedFieldBuilder()
            {
                Value = _DiscordNet.serviceProvider.GetRequiredService<Configuration>().LoadCoreModule ? "`Yes`": "`No`",
                Name = ConfigIdentifier,
                IsInline = inline
            };
            return efb;
        }

        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            return base.ExecuteView(_DiscordNet, Context, _DiscordNet.serviceProvider.GetRequiredService<Configuration>().LoadCoreModule ? "Yes" : "No");
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
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(ConsoleIO, Context, "Invalid prefix", "Your prefix must not start with whitespace, or contain invalid characters!", Color.Red));
                return;
            }
            if(bool.TryParse(value,out bool v))
            {
                _DiscordNet.serviceProvider.GetRequiredService<Configuration>().LoadCoreModule = v;
                string disclaimer = !v ? "\r\n\r\n***You will need to have access to the bot's console to re-enable the core module!***\r\n\r\nConsole Command: `config.LoadCoreModule true`" : "";
                Color EmbedColor = !v ? Color.Red : Color.Orange;
                _DiscordNet.serviceProvider.GetRequiredService<ConfigurationManager>().Save();
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(ConsoleIO, Context, "Config Updated", $"`LoadCoreModule` updated to `{value}`", Color.Green));
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(ConsoleIO, Context, "WARNING", $"You will be required to restart the program for this setting to take effect." +
                    $"{disclaimer}", EmbedColor));
            }
            return;
        }

    }
}
