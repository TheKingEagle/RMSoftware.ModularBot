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
    public class SetvarCommand : ConsoleCommand
    {
        public SetvarCommand()
        {
            CommandName = "setvar";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string input = consoleInput.Remove(0, CommandName.Length).Trim();
            string varname = input.Split(' ')[0];
            input = input.Remove(0, varname.Length);
            input = input.Trim();
            discordNET.CustomCMDMgr.coreScript.Set(varname, input);
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
