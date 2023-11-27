using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Entity;
using ModularBOT.Component;

namespace ModularBOT.Entity
{
    [Obsolete("ConsoleCommand is being phased out in favor of direct interaction via Discord and the upcoming web interface.")]
    public class ConsoleCommand
    {
        public string CommandName { get; set; }

        public virtual bool Execute(string consoleInput,
                                    ref bool ShutdownCalled,
                                    ref bool RestartRequested,
                                    ref bool InputCanceled,
                                    ref DiscordNET discordNET,
                                    ref ConsoleIO console) => true;

        public string[] GetParameters(string consoleInput) => consoleInput.Remove(0, CommandName.Length).Trim().Split(' ').Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray();
    }
}
