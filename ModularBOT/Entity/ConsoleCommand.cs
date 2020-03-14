using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Entity;
using ModularBOT.Component;

namespace ModularBOT.Entity
{
    public class ConsoleCommand
    {
        public string CommandName { get; set; }

        public virtual void ExecuteAsync(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET)
        {
            //override
            return;
        }
    }
}
