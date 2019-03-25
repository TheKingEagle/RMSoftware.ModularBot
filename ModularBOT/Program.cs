using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Component;
using System.Threading;
using System.Diagnostics;

namespace ModularBOT
{
    class Program
    {
        public static ConfigurationManager configMGR;

        private static List<string> AppArguments = new List<string>();
        private static ConsoleIO ConsoleIOHelper;
        
        private static bool ShutdownCalled = false;
        private static bool RestartRequested = false;
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        public static int Main(string[] ARGS = null)
        {
            if (ARGS != null)
            {
                AppArguments.AddRange(ARGS);
            }

            ConsoleIOHelper = new Component.ConsoleIO(AppArguments);
            ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, "Initializing");
            
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Info, "Main", "Application started"));
#if (DEBUG)
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Critical, "ATTENTION:", "You are running a debug build!"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Warning, "Warning:", "This program may not be in a finished state!"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE Discord.NET integration"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE Setup Wizard"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE Configuration"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE Command System"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE Task manager"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE ONStart for Task manager"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE Module Loader"));
#endif
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Info, "Main", "Application started"));
            configMGR = new ConfigurationManager("modbot-config.cnf", ref ConsoleIOHelper);
            Task.Run(() => ConsoleIOHelper.GetConsoleInput(ref ShutdownCalled, ref RestartRequested));
            SpinWait.SpinUntil(BotShutdown);

            if (RestartRequested)
            {
                Process p = new Process();
                string flattened = "";
                foreach (string item in ARGS)
                {
                    flattened += item + " ";
                }
                p.StartInfo = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, flattened);
                p.Start();
                return 0x5BB;//code for RESTART NEEDED
            }

            return 0x000;//ok;
        }

        private static Func<bool> BotShutdown = delegate ()
        {
            return ShutdownCalled;
        };
    }
}
