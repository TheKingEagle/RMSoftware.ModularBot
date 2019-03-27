using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Component;
using System.Threading;
using System.Diagnostics;
using Discord;

namespace ModularBOT
{
    class Program
    {
        public static ConfigurationManager configMGR;

        private static List<string> AppArguments = new List<string>();
        private static DiscordNET discord = new DiscordNET();
        private static bool ShutdownCalled = false;
        private static bool RestartRequested = false;
        private static ConsoleIO consoleIO;
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        public static int Main(string[] ARGS = null)
        {
            if (ARGS != null)
            {
                AppArguments.AddRange(ARGS);
            }
            consoleIO = new ConsoleIO(AppArguments);
            configMGR = new ConfigurationManager("modbot-config.cnf",ref consoleIO);

            RunStartlogo();

            consoleIO.ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, "Application Running");

            #region DEBUG
#if (DEBUG)
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "ATTENTION:", "You are running a debug build!"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Warning, "Warning:", "This program may not be in a finished state!"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "TODO:", "WRITE Discord.NET integration"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "TODO:", "WRITE Command System"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "TODO:", "WRITE Task manager"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "TODO:", "WRITE ONStart for Task manager"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "TODO:", "WRITE Module Loader"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "TODO:", "WRITE Permissions system"));
#endif
            #endregion

            consoleIO.WriteEntry(new LogMessage(LogSeverity.Info, "Main", "Application started"));
            Task.Run(() => discord.Start(ref consoleIO, ref configMGR.CurrentConfig, ref ShutdownCalled));
            Task.Run(() => consoleIO.GetConsoleInput(ref ShutdownCalled, ref RestartRequested,ref discord));//Console reader

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
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Warning, "Session", "Ending session. and closing program in 3..."), ConsoleColor.Black, false);
            Console.CursorTop = consoleIO.PrvTop;

            Thread.Sleep(1000);

            consoleIO.WriteEntry(new LogMessage(LogSeverity.Warning, "Session", "Ending session. and closing program in 2..."), ConsoleColor.Black, false);
            Console.CursorTop = consoleIO.PrvTop;
            Thread.Sleep(1000);
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Warning, "Session", "Ending session. and closing program in 1..."), ConsoleColor.Black, false);
            Console.CursorTop = consoleIO.PrvTop;
            Thread.Sleep(1000);
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
                consoleIO.ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome", 79, 45);
                Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                Thread.Sleep(800);
                try
                {
                    if(configMGR.CurrentConfig.LogoPath == "INTERNAL")
                    {
                        consoleIO.ConsoleWriteImage(Properties.Resources.RMSoftwareICO);
                        return;
                    }
                    consoleIO.ConsoleWriteImage(new System.Drawing.Bitmap(configMGR.CurrentConfig.LogoPath));
                }
                catch (Exception ex)
                {
                    consoleIO.WriteErrorsLog("WARNING: Error rendering startup logo. Default logo used instead... Exception details below.", ex);
                    consoleIO.ConsoleWriteImage(Properties.Resources.RMSoftwareICO);

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
