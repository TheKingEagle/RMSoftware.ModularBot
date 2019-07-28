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
    internal class LockPrefix : ConfigEntity
    {
        public LockPrefix()
        {
            ReadOnly = false;
            ConfigIdentifier = "LockPrefix";
        }
        public override async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _DiscordNet, ICommandContext Context, string value)
        {
            var g = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
            var ConsoleIO = _DiscordNet.serviceProvider.GetRequiredService<ConsoleIO>();

            if (Context.User is SocketGuildUser SGU)
            {
                if (!SGU.GuildPermissions.Has(GuildPermission.ManageGuild))
                {
                    if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                    {
                        await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(ConsoleIO,Context,"Access Denied!", $"You must either have permission to `Manage Server`, " +
                            $"or be registered to the bot permission system with `AccessLevels.{AccessLevels.Administrator.ToString()}`", Color.DarkRed));
                        return;
                    }
                }
                if (!bool.TryParse(value, out bool configvalue))
                {
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(ConsoleIO, Context, "Invalid Value!", $"This item must be a BOOLEAN value. `True` or `False`.", Color.DarkRed));
                    return;
                }
                else
                {
                    g.LockPFChanges = configvalue;
                    g.SaveJson();
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(ConsoleIO, Context, "Config Updated", $"`LockPrefix` updated to `{value}` for {Context.Guild.Name}", Color.Green));

                }
            }


        }

        public override EmbedFieldBuilder ExecuteView(DiscordNET _DiscordNet, ICommandContext Context, bool inline)
        {
            var g = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);

            EmbedFieldBuilder efb = new EmbedFieldBuilder()
            {
                Value = g.LockPFChanges ? "`Yes`": "`No`",
                Name = ConfigIdentifier,
                IsInline = inline
            };
            return efb;
        }

        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            var g = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
            return base.ExecuteView(_DiscordNet, Context, g.LockPFChanges ? "Yes" : "No");
        }
    }
}
