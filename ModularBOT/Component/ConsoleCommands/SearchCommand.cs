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
using static ModularBOT.Component.ConsoleIO;

namespace ModularBOT.Component.ConsoleCommands
{
    public class SearchCommand : ConsoleCommand
    {
        public SearchCommand()
        {
            CommandName = "search";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string input = consoleInput.Remove(0, CommandName.Length);

            #region Parse Checking
            if (input.Split(' ').Length < 2)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too few arguments!"));
                return true;
            }

            string rl = input.Split(' ')[0];

            if (!ulong.TryParse(rl, out ulong guild))
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "SEARCH", "Invalid guild ID"));
                return true;
            }

            #endregion

            string query = input.Remove(0, rl.Length + 1);//guildID length + space
            string PRV_TITLE = console.ConsoleTitle;
            List<LogEntry> v = new List<LogEntry>();
            //---------------start modal---------------
            bool ModalResult = console.ListUsers(ref discordNET, guild, query);
            if (!ModalResult)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "SEARCH", "The guild was not found..."));

                return true;
            }
            //----------------End modal----------------
            if (ModalResult)
            {

                console.ConsoleGUIReset(Program.configMGR.CurrentConfig.ConsoleForegroundColor,
                    Program.configMGR.CurrentConfig.ConsoleBackgroundColor, PRV_TITLE);
                ScreenModal = false;
                v.AddRange(console.LogEntries);
                console.LogEntries.Clear();//clear buffer.
                                   //output previous logEntry.
                foreach (var item in v)
                {
                    console.WriteEntry(item.LogMessage, item.EntryColor);
                }
            }
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
