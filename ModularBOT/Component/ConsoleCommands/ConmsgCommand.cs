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
using Discord.WebSocket;

namespace ModularBOT.Component.ConsoleCommands
{
    public class ConmsgCommand : ConsoleCommand
    {
        public ConmsgCommand()
        {
            CommandName = "conmsg";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string input = consoleInput.Remove(0,CommandName.Length).Trim();
            if (!(discordNET.Client.GetChannel(console.chID) is SocketTextChannel Channel))
            {
                console.WriteEntry(new LogMessage(LogSeverity.Error, "Console", "Invalid channel."));
                return true;
            }
            Channel.SendMessageAsync(input);
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
