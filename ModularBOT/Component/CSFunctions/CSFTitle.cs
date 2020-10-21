﻿using Discord;
using Discord.WebSocket;
using ModularBOT.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Component.CSFunctions
{
    public class CSFTitle : CSFunction
    {
        public CSFTitle()
        {
            Name = "TITLE";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            engine.OutputCount++;
            string output = line.Remove(0, Name.Length).Trim();
            string processed = engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (engine.OutputCount > 2)
            {
                errorEmbed.WithDescription($"`{Name}` Function Error: Preemptive rate limit reached.\r\n```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            if (cmd.CommandAccessLevel < AccessLevels.Administrator)
            {
                errorEmbed.WithDescription($"`{Name}` Function error: This requires the calling context to be `AccessLevels.Administrator`");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            await ((DiscordShardedClient)client).SetGameAsync(processed,null,ActivityType.Playing);
            return true;
        }
    }
}
