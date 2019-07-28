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
    internal class EventLogLevel : ConfigEntity
    {
        public EventLogLevel()
        {
            ReadOnly = false;
            ConfigIdentifier = "EventLogLevel";
        }
        public override async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _DiscordNet, ICommandContext Context, string value)
        {
           
            var ConsoleIO = _DiscordNet.serviceProvider.GetRequiredService<ConsoleIO>();

            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            if (!Enum.TryParse(value, out LogSeverity log))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(ConsoleIO, Context, "Invalid Log Level", 
                    $"`EventLogLevel` needs to be one of the following:\r\n\r\n" +
                    $"• `Critical`\r\n" +
                    $"• `Error`\r\n" +
                    $"• `Warning`\r\n" +
                    $"• `Info`\r\n" +
                    $"• `Verbose`\r\n" +
                    $"• `Debug`", Color.Red));
                return;

            }
            else
            {
                _DiscordNet.serviceProvider.GetRequiredService<Configuration>().DiscordEventLogLevel = log;
                _DiscordNet.serviceProvider.GetRequiredService<ConfigurationManager>().Save();
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(ConsoleIO, Context, "Config Updated", $"`EventLogLevel` updated to `{value}`.\r\n" +
                    $"**You will need to restart the program for this to take affect**", Color.Green));
            }
        }

        public override EmbedFieldBuilder ExecuteView(DiscordNET _DiscordNet, ICommandContext Context, bool inline)
        {
            
            EmbedFieldBuilder efb = new EmbedFieldBuilder()
            {
                Value = $"`{_DiscordNet.serviceProvider.GetRequiredService<Configuration>().DiscordEventLogLevel}`",
                Name = ConfigIdentifier,
                IsInline = inline
            };
            return efb;
        }
        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            return base.ExecuteView(_DiscordNet, Context, _DiscordNet.serviceProvider.GetRequiredService<Configuration>().DiscordEventLogLevel.ToString());
        }
    }
}
