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
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;

namespace ModularBOT
{
    class Program
    {
        public static ConfigurationManager configMGR;

        public static List<string> AppArguments = new List<string>();
        private static DiscordNET discord = new DiscordNET();
        internal static bool ShutdownCalled = false;
        public static bool RestartRequested = false;
        public static bool ImmediateTerm = false;
        private static ConsoleIO consoleIO;
        private static bool recoveredFromCrash = false;
        private delegate bool ConsoleCtrlHandlerDelegate(int sig);
        
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerDelegate handler, bool add);


        private static ConsoleCtrlHandlerDelegate _consoleCtrlHandler;

        static bool CheckTerminalCompatibility()
        {
            // Define the compatible values for DelegationConsole and DelegationTerminal.
            string[] compatibleValues = { "{B23D10C0-E52E-411E-9D5B-C09FDF709C7D}", "{00000000-0000-0000-0000-000000000000}" };

            Dictionary<string, string> terminalMapping = new Dictionary<string, string>
            {
                { "{2EACA947-7F5F-4CFA-BA87-8F7FBEEFBE69}", "Windows Terminal" },
                { "{00000000-0000-0000-0000-000000000000}", "Default" },
                { "{B23D10C0-E52E-411E-9D5B-C09FDF709C7D}", "Conhost" },
                { "{E12CFF52-A866-4C77-9A90-F570A7AA2C6B}", "Windows Terminal" }
            };

            // Check DelegationConsole value.
            string delegationConsoleValue = GetRegistryValue("Console\\%%Startup", "DelegationConsole");
            if (!IsCompatibleValue(delegationConsoleValue, compatibleValues))
            {
                Console.WriteLine($"Incompatible Console Host Detected: {terminalMapping[delegationConsoleValue]}");
                return false;
            }

            // Check DelegationTerminal value.
            string delegationTerminalValue = GetRegistryValue("Console\\%%Startup", "DelegationTerminal");
            if (!IsCompatibleValue(delegationTerminalValue, compatibleValues))
            {
                Console.WriteLine($"Incompatible Terminal Detected: {terminalMapping[delegationConsoleValue]}");
                return false;
            }

            return true;
        }

        static string GetRegistryValue(string registryPath, string valueName)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath))
            {
                if (key != null)
                {
                    object value = key.GetValue(valueName);
                    if (value != null)
                    {
                        return value.ToString();
                    }
                }
            }
            return null;
        }

        static bool IsCompatibleValue(string value, string[] compatibleValues)
        {
            if(value == null)
            {
                return true;//assume compatible.
            }
            // Check if the value is in the list of compatible values.
            return Array.Exists(compatibleValues, v => v.Equals(value, StringComparison.OrdinalIgnoreCase));
        }


        /// <summary>
        /// Application Entry Point.
        /// </summary>        
        public static int Main(string[] ARGS = null)
        {
            if (!CheckTerminalCompatibility())
            {
                Console.WriteLine("WARNING: The configured default terminal is not compatible with this application.");
                Console.WriteLine("WARNING: The program may run incorrectly, or outright crash.");
                Console.WriteLine("NOTICE: If you are running with cmd.exe or conhost, you may disregard this message.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            
            _consoleCtrlHandler += s =>
            {
                if (discord != null)
                {
                    discord.Stop(ref ShutdownCalled);
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
            ConsoleIO.ConsoleCommands.Add(new AboutCommand());            //about
            ConsoleIO.ConsoleCommands.Add(new ChannelsCommand());         //channels
            ConsoleIO.ConsoleCommands.Add(new CLSCommand());              //cls
            ConsoleIO.ConsoleCommands.Add(new CfgDebugWizardCommand());   //config.debugwizard
            ConsoleIO.ConsoleCommands.Add(new ConfigCFUCommand());        //config.checkforupdates
            ConsoleIO.ConsoleCommands.Add(new ConfigDCEvtLLCommand());    //config.discordeventloglevel
            ConsoleIO.ConsoleCommands.Add(new CfgLCMCommand());           //config.loadcoremodule
            ConsoleIO.ConsoleCommands.Add(new ConfigResetCommand());      //config.reset
            ConsoleIO.ConsoleCommands.Add(new ConfigSCCommand());         //config.setcolors
            
            ConsoleIO.ConsoleCommands.Add(new ConfigUPRCCommand());       //config.useprereleasechannel
            ConsoleIO.ConsoleCommands.Add(new ConmsgCommand());           //conmsg
            ConsoleIO.ConsoleCommands.Add(new DisableCMDCommand());       //disablecmd
            ConsoleIO.ConsoleCommands.Add(new EnableCMDCommand());        //enablecmd
            ConsoleIO.ConsoleCommands.Add(new GuildNameCommand());        //guildname
            ConsoleIO.ConsoleCommands.Add(new GuildsCommand());           //guilds
            ConsoleIO.ConsoleCommands.Add(new ConIOCrashCommand());       //iocrash
            ConsoleIO.ConsoleCommands.Add(new LeaveCommand());            //leave
            ConsoleIO.ConsoleCommands.Add(new ListCommand());             //list
            ConsoleIO.ConsoleCommands.Add(new MBotDataCommand());         //mbotdata
            ConsoleIO.ConsoleCommands.Add(new MyRolesCommand());          //myroles
            ConsoleIO.ConsoleCommands.Add(new RolesCommand());            //roles
            ConsoleIO.ConsoleCommands.Add(new RSKillCommand());           //rskill
            ConsoleIO.ConsoleCommands.Add(new SetgchCommand());           //setgch
            ConsoleIO.ConsoleCommands.Add(new SetvarCommand());           //setvar
            ConsoleIO.ConsoleCommands.Add(new StatusCommand());           //status
            ConsoleIO.ConsoleCommands.Add(new StopCommand());             //stopbot
            ConsoleIO.ConsoleCommands.Add(new TestScreenCommand());       //testscreen
            ConsoleIO.ConsoleCommands.Add(new TSKillCommand());           //tskill
            ConsoleIO.ConsoleCommands.Add(new UsersCommand());            //users
            ConsoleIO.ConsoleCommands.Add(new UpdateCommand());           //update
            #endregion

            configMGR = new ConfigurationManager("modbot-config.cnf", ref consoleIO);
            Directory.CreateDirectory("guilds");
            Directory.CreateDirectory("modules");
            Directory.CreateDirectory("ext");
            Directory.CreateDirectory("attachments");
            RunStartlogo();

            consoleIO.ConsoleGUIReset(configMGR.CurrentConfig.ConsoleForegroundColor,
                configMGR.CurrentConfig.ConsoleBackgroundColor, "Active Session");
            Task.Run(() => consoleIO.ProcessQueue());//START ConsoleIO processing.


            Task.Run(() =>
            {

                consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "WebPortal", $"Listening on http://localhost:{configMGR.CurrentConfig.WebPortalPort}"));
                WebPortal wp = new WebPortal(configMGR.CurrentConfig.WebPortalPort.Value, "localhost");

            });
            #region DEBUG
#if (DEBUG)
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "ATTENTION:", "You are running a debug build!"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Warning:", "This program is not intended for the production environment."));
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
#endif
            #endregion

            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Main", "Application started"));

            Task.Run(() => discord.Start(ref consoleIO, ref configMGR.CurrentConfig, ref ShutdownCalled, ref RestartRequested, ref recoveredFromCrash));//Discord.NET thread
            //Task r = Task.Run(() => consoleIO.GetConsoleInput(ref ShutdownCalled, ref RestartRequested, ref discord.InputCanceled, ref discord));//Console reader thread;
            
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
            if (!ImmediateTerm)
            {

                consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Session", "Ending session. and closing program in 3..."), ConsoleColor.Black, false);
                //Console.CursorTop = consoleIO.PrvTop;
                Thread.Sleep(1000);
                consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Session", "Ending session. and closing program in 2..."), ConsoleColor.Black, false);
                //Console.CursorTop = consoleIO.PrvTop;
                Thread.Sleep(1000);
                consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Session", "Ending session. and closing program in 1..."), ConsoleColor.Black, false);
                //Console.CursorTop = consoleIO.PrvTop;
                Thread.Sleep(1000);

            }
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
                    if (configMGR.CurrentConfig.LogoPath == "INTERNAL")
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
