using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Entity;
using Discord;
using Discord.Net;
using ModularBOT.Component;
using Microsoft.Extensions.DependencyInjection;
namespace ModularBOT.Component.ConsoleCommands
{
    public class GuildNameCommand : ConsoleCommand
    {
        public GuildNameCommand()
        {
            CommandName = "guildname";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string input = consoleInput;


            #region Parse Checking

            if (input.Split(' ').Length > 2)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too many arguments!"));
                return true;
            }
            if (input.Split(' ').Length < 2)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too few arguments!"));
                return true;
            }

            if (input.Split(' ').Length == 2)
            {
                input = input.Split(' ')[1];
            }
            if (!ulong.TryParse(input, out ulong id))
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Leave", "Invalid Guild ID format"));
                return true;
            }

            #endregion Parse Checking
            var G = discordNET.Client.GetGuild(id);
            if (G == null)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "GuildName", "This guild isn't valid."));
                return true;
            }
            console.WriteEntry(new LogMessage(LogSeverity.Critical, "GuildName", $"Full Guild Name: {G.Name}"));
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
