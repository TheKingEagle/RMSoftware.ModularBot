using Discord;
using Discord.WebSocket;
using ModularBOT.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Component.CSFunctions
{
    public class CSFExit : CSFunction
    {
        public CSFExit()
        {
            Name = "EXIT";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            engine.EXIT = true;
            engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CoreScript", "Exit called. END OF SCRIPT"), ConsoleColor.Green);
            return await Task.FromResult(true);
        }
    }
}
