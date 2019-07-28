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
    internal class GlobalInitChannel : ConfigEntity
    {
        public GlobalInitChannel()
        {
            ReadOnly = false;
            ConfigIdentifier = "GlobalInitChannel";
        }
        public override async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _DiscordNet, ICommandContext Context, string value)
        {
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if (!ulong.TryParse(value, out ulong ulchid))
            {
                if (Client.GetChannel(ulchid) != null)
                {
                    if (Client.GetChannel(ulchid) is SocketTextChannel stc)
                    {
                        _DiscordNet.serviceProvider.GetRequiredService<Configuration>().LogChannel = ulchid;
                        _DiscordNet.serviceProvider.GetRequiredService<ConfigurationManager>().Save();
                        await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(_DiscordNet.serviceProvider.GetRequiredService<ConsoleIO>(),Context,"Config Updated", $"`GlobalInitChannel` updated to `{ulchid}`", Color.Green));
                        
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(_DiscordNet.serviceProvider.GetRequiredService<ConsoleIO>(), Context,"Invalid Channel", $"`{ulchid}` is not a valid Text Channel.", Color.Green));
                        return;
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(_DiscordNet.serviceProvider.GetRequiredService<ConsoleIO>(), Context,"Channel Not Found", $"`{ulchid}` did not match any available guild channels.", Color.Green));
                    return;
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(_DiscordNet.serviceProvider.GetRequiredService<ConsoleIO>(), Context, "Invalid Format", $"`{ulchid}` is not a valid `ulong` value.", Color.Green));
                return;
            }
        }

        public override EmbedFieldBuilder ExecuteView(DiscordNET _DiscordNet, ICommandContext Context, bool inline)
        {
            EmbedFieldBuilder efb = new EmbedFieldBuilder()
            {
                Value = $"`{_DiscordNet.serviceProvider.GetRequiredService<Configuration>().LogChannel.ToString()}`",
                Name = ConfigIdentifier,
                IsInline = inline
            };
            return efb;
        }
    }
}
