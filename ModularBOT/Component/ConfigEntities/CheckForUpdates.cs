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
    internal class CheckForUpdates : ConfigEntity
    {
        public CheckForUpdates()
        {
            ReadOnly = true;
            ConfigIdentifier = "CheckForUpdates";
        }

        public override EmbedFieldBuilder ExecuteView(DiscordNET _DiscordNet, ICommandContext Context, bool inline)
        {
            EmbedFieldBuilder efb = new EmbedFieldBuilder()
            {
                Value = _DiscordNet.serviceProvider.GetRequiredService<Configuration>().CheckForUpdates.Value ? "`Yes`" : "`No`",
                Name = ConfigIdentifier,
                IsInline = inline
            };
            return efb;
        }
    }
}
