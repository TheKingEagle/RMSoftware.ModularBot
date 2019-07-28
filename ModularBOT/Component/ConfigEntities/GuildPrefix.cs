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
    internal class GuildPrefix : ConfigEntity
    {
        public GuildPrefix()
        {
            ReadOnly = true;
            ConfigIdentifier = "GuildPrefix";
        }

        public override EmbedFieldBuilder ExecuteView(DiscordNET _DiscordNet, ICommandContext Context, bool inline)
        {
            string p = _DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
            var ConsoleIO = _DiscordNet.serviceProvider.GetRequiredService<ConsoleIO>();
            GuildObject g = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
            p = g?.CommandPrefix;
            if (g == null)
            {
                ConsoleIO.WriteEntry(new LogMessage(LogSeverity.Warning, "GPrefix", "Warning: The guild object was null, this means the guild's file doesn't exist!!"));
                p = _DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
            }
            EmbedFieldBuilder efb = new EmbedFieldBuilder()
            {
                Value = $"`{p}`",
                Name = ConfigIdentifier,
                IsInline = inline
            };
            return efb;
        }
    }
}
