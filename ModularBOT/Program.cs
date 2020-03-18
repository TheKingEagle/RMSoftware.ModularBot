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
using System.Runtime.InteropServices;
using ModularBOT.Component.ConsoleCommands;
namespace ModularBOT
{
    class Program
    {
        public static ConfigurationManager configMGR;

        public static List<string> AppArguments = new List<string>();
        private static DiscordNET discord = new DiscordNET();
        internal static bool ShutdownCalled = false;
        public static bool RestartRequested = false;
        private static ConsoleIO consoleIO;
        private static bool recoveredFromCrash = false;
        private delegate bool ConsoleCtrlHandlerDelegate(int sig);

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerDelegate handler, bool add);

        private static ConsoleCtrlHandlerDelegate _consoleCtrlHandler;

        /// <summary>
        /// Application Entry Point.
        /// </summary>        
        public static int Main(string[] ARGS = null)
        {
            _consoleCtrlHandler += s =>
            {
                if(discord!=null)
                {
                    discord.Stop(ref ShutdownCalled);
                    
                    Thread.Sleep(3000);
                }
                return false;
            };
            SetConsoleCtrlHandler(_consoleCtrlHandler, true);
            if (ARGS != null)
            {
                AppArguments.AddRange(ARGS);
            }
            recoveredFromCrash = AppArguments.Contains("-crashed");
            consoleIO = new ConsoleIO();

            #region ConsoleIO Command hooks
            //Populate command list.
            consoleIO.ConsoleCommands.Add(new AboutCommand());            //about
            consoleIO.ConsoleCommands.Add(new CfgLCMCommand());           //config.loadcoremodule
            consoleIO.ConsoleCommands.Add(new ChannelsCommand());         //channels
            consoleIO.ConsoleCommands.Add(new CLSCommand());              //cls
            consoleIO.ConsoleCommands.Add(new ConfigCFUCommand());        //config.checkforupdates
            consoleIO.ConsoleCommands.Add(new ConfigDCEvtLLCommand());    //config.discordeventloglevel
            consoleIO.ConsoleCommands.Add(new ConfigSCCommand());         //config.setcolors
            consoleIO.ConsoleCommands.Add(new ConfigSLPCommand());        //config.setlogo
            consoleIO.ConsoleCommands.Add(new ConfigUPRCCommand());       //config.useprereleasechannel
            consoleIO.ConsoleCommands.Add(new ConmsgCommand());           //conmsg
            consoleIO.ConsoleCommands.Add(new DisableCMDCommand());       //disablecmd
            consoleIO.ConsoleCommands.Add(new EnableCMDCommand());        //enablecmd
            consoleIO.ConsoleCommands.Add(new GuildNameCommand());        //guildname
            consoleIO.ConsoleCommands.Add(new GuildsCommand());           //guilds
            consoleIO.ConsoleCommands.Add(new LeaveCommand());            //leave
            consoleIO.ConsoleCommands.Add(new MBotDataCommand());         //mbotdata
            consoleIO.ConsoleCommands.Add(new MyRolesCommand());          //myroles
            consoleIO.ConsoleCommands.Add(new RolesCommand());            //roles
            consoleIO.ConsoleCommands.Add(new RSKillCommand());           //rskill
            consoleIO.ConsoleCommands.Add(new SearchCommand());           //search
            consoleIO.ConsoleCommands.Add(new SetgchCommand());           //setgch
            consoleIO.ConsoleCommands.Add(new SetvarCommand());           //setvar
            consoleIO.ConsoleCommands.Add(new StatusCommand());           //status
            consoleIO.ConsoleCommands.Add(new StopCommand());             //stopbot
            consoleIO.ConsoleCommands.Add(new TSKillCommand());           //tskill
            consoleIO.ConsoleCommands.Add(new UsersCommand());            //users
            consoleIO.ConsoleCommands.Add(new ListCommand());             //list
            #endregion

            configMGR = new ConfigurationManager("modbot-config.cnf",ref consoleIO);
            if (!Directory.Exists("guilds")) { Directory.CreateDirectory("guilds"); }
            if (!Directory.Exists("modules")) { Directory.CreateDirectory("modules"); }
            if (!Directory.Exists("ext")) { Directory.CreateDirectory("ext"); }
            if (!Directory.Exists("attachments")) { Directory.CreateDirectory("attachments"); }
            RunStartlogo();
            
            consoleIO.ConsoleGUIReset(configMGR.CurrentConfig.ConsoleForegroundColor,
                configMGR.CurrentConfig.ConsoleBackgroundColor, "Active Session");
            Task.Run(() => consoleIO.ProcessQueue());//START ConsoleIO processing.

            #region DEBUG
#if (DEBUG)
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "ATTENTION:", "You are running a debug build!"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Warning:", "This program is not intended for the production environment."));
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
#endif
            #endregion

            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Main", "Application started"));

            Task.Run(() => discord.Start(ref consoleIO, ref configMGR.CurrentConfig, ref ShutdownCalled, ref RestartRequested,ref recoveredFromCrash));//Discord.NET thread
            Task r = Task.Run(() => consoleIO.GetConsoleInput(ref ShutdownCalled, ref RestartRequested,ref discord.InputCanceled,ref discord));//Console reader thread;
           
            SpinWait.SpinUntil(() => ShutdownCalled);//HOLD THREAD
            if (RestartRequested)
            {
                Process p = new Process();
                string flattened = "";
                foreach (string item in AppArguments)
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
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "FirstChance", e.Exception.Message), ConsoleColor.DarkRed, true, false, true);
            
            consoleIO.WriteErrorsLog(e.Exception);
        }
        
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
