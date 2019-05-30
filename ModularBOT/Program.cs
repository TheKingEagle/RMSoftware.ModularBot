using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Component;
using System.Threading;
using System.Diagnostics;
using Discord;
using System.IO;
namespace ModularBOT
{
    class Program
    {
        public static ConfigurationManager configMGR;

        private static List<string> AppArguments = new List<string>();
        private static DiscordNET discord = new DiscordNET();
        internal static bool ShutdownCalled = false;
        public static bool RestartRequested = false;
        private static ConsoleIO consoleIO;
        private static bool recoveredFromCrash = false;
        
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        public static int Main(string[] ARGS = null)
        {
            if (ARGS != null)
            {
                AppArguments.AddRange(ARGS);
            }
            recoveredFromCrash = AppArguments.Contains("-crashed");
            consoleIO = new ConsoleIO(AppArguments);
            
            configMGR = new ConfigurationManager("modbot-config.cnf",ref consoleIO);
            if (!Directory.Exists("guilds")) { Directory.CreateDirectory("guilds"); }
            if (!Directory.Exists("modules")) { Directory.CreateDirectory("modules"); }
            if (!Directory.Exists("ext")) { Directory.CreateDirectory("ext"); }
            RunStartlogo();
            
            consoleIO.ConsoleGUIReset(configMGR.CurrentConfig.ConsoleForegroundColor,
                configMGR.CurrentConfig.ConsoleBackgroundColor, "Active Session");
            Task.Run(() => consoleIO.ProcessQueue());//START ConsoleIO processing.
            #region DEBUG
#if (DEBUG)
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "ATTENTION:", "You are running a debug build!"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Warning:", "This program may not be in a finished state!"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "TODO:", "WRITE editcmd & getcmd"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "TODO:", "WRITE Task manager"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "TODO:", "WRITE External Module Loader"));
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
#endif
            #endregion

            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Main", "Application started"));

            Task.Run(() => discord.Start(ref consoleIO, ref configMGR.CurrentConfig, ref ShutdownCalled, ref RestartRequested,ref recoveredFromCrash));//Discord.NET thread
            Task r = Task.Run(() => consoleIO.GetConsoleInput(ref ShutdownCalled, ref RestartRequested,ref discord.InputCanceled,ref discord));//Console reader thread;
           
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
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Session", "Ending session. and closing program in 3..."), ConsoleColor.Black, false);
            //Console.CursorTop = consoleIO.PrvTop;
            Thread.Sleep(1000);
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Session", "Ending session. and closing program in 2..."), ConsoleColor.Black, false);
            //Console.CursorTop = consoleIO.PrvTop;
            Thread.Sleep(1000);
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Session", "Ending session. and closing program in 1..."), ConsoleColor.Black, false);
            //Console.CursorTop = consoleIO.PrvTop;
            Thread.Sleep(1000);
            return 0x000;//ok;
        }

        private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            //Thread.Sleep(300);
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "FirstChance", e.Exception.Message), ConsoleColor.DarkRed, false, false, true);
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "FirstChance", "DETAILS in Errors.log"), ConsoleColor.DarkRed, false, false, true);
            //Thread.Sleep(300);
            consoleIO.WriteErrorsLog(e.Exception);
        }

        private static readonly Func<bool> BotShutdown = delegate ()
        {
            return ShutdownCalled;
        };

        private static void RunStartlogo()
        {
            Console.CursorVisible = false;
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
                        Thread.Sleep(2000);
                        return;
                    }
                    consoleIO.ConsoleWriteImage(new System.Drawing.Bitmap(configMGR.CurrentConfig.LogoPath));
                    Thread.Sleep(2000);
                }
                catch (Exception ex)
                {
                    consoleIO.WriteErrorsLog("WARNING: Error rendering startup logo. Default logo used instead... Exception details below.", ex);
                    consoleIO.ConsoleWriteImage(Properties.Resources.RMSoftwareICO);
                    Thread.Sleep(2000);
                }
            }
            else
            {
                return;
            }
        }
    }
}
