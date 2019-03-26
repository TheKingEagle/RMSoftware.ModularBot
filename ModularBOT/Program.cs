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
            configMGR = new ConfigurationManager("modbot-config.cnf", ref ConsoleIOHelper);

            RunStartlogo();

            ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, "Initializing");

            #region DEBUG
            #if (DEBUG)
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Critical, "ATTENTION:", "You are running a debug build!"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Warning, "Warning:", "This program may not be in a finished state!"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE Discord.NET integration"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE Command System"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE Task manager"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE ONStart for Task manager"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE Module Loader"));
            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Debug, "TODO:", "WRITE Permissions system"));
            #endif
            #endregion

            ConsoleIOHelper.WriteEntry(new Discord.LogMessage(Discord.LogSeverity.Info, "Main", "Application started"));
            
            Task.Run(() => ConsoleIOHelper.GetConsoleInput(ref ShutdownCalled, ref RestartRequested));//Console reader

            SpinWait.SpinUntil(BotShutdown);//HOLD THREAD

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

        private static readonly Func<bool> BotShutdown = delegate ()
        {
            return ShutdownCalled;
        };

        private static void RunStartlogo()
        {

            if (configMGR.CurrentConfig.LogoPath != "NONE")
            {
                ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome", 79, 45);
                Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                Thread.Sleep(800);
                try
                {
                    if(configMGR.CurrentConfig.LogoPath == "INTERNAL")
                    {
                        ConsoleIOHelper.ConsoleWriteImage(Properties.Resources.RMSoftwareICO);
                        return;
                    }
                    ConsoleIOHelper.ConsoleWriteImage(new System.Drawing.Bitmap(configMGR.CurrentConfig.LogoPath));
                }
                catch (Exception ex)
                {
                    ConsoleIOHelper.WriteErrorsLog("WARNING: Error rendering startup logo. Default logo used instead... Exception details below.", ex);
                    ConsoleIOHelper.ConsoleWriteImage(Properties.Resources.RMSoftwareICO);

                }
                Thread.Sleep(3000);
            }
            else
            {
                return;
            }
        }
    }
}
