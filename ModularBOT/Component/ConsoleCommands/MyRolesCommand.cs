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
using ModularBOT.Component.ConsoleScreens;
using Discord.WebSocket;

namespace ModularBOT.Component.ConsoleCommands
{
    public class MyRolesCommand : ConsoleCommand
    {
        public MyRolesCommand()
        {
            CommandName = "myroles";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            short numpage = 1;
            string[] param = GetParameters(consoleInput);

            #region Parse Checking

            if (param.Length > 2)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "ROLES", "Too many arguments!"));
                return true;
            }
            if (param.Length < 1)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "ROLES", "Too few arguments!"));
                return true;
            }
            if (param.Length == 2)
            {
                if (!short.TryParse(param[1], out numpage))
                {
                    console.WriteEntry(new LogMessage(LogSeverity.Critical, "ROLES", "Page number must be a valid number."));
                    return true;
                }
                else if (numpage < 1)
                {
                    console.WriteEntry(new LogMessage(LogSeverity.Critical, "ROLES", "Page number must be no lower than 1."));
                    return true;
                }
            }

            if (!ulong.TryParse(param[0], out ulong id))
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "ROLES", "Guild ID was malformed!"));
                return true;
            }
            SocketGuild guild = discordNET.Client.GetGuild(id);

            if (guild == null)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "ROLES", "Guild not found!"));
                return true;
            }
            #endregion

            console.ShowConsoleScreen(new RolesScreen(discordNET, guild, guild.CurrentUser.Roles.ToList(), "Listing bot's roles", numpage), true);

            return true;

        }
    }
}
