﻿using System;
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
        private static Socket ping = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
            consoleIO.ConsoleCommands.Add(new AboutCommand());            //about
            consoleIO.ConsoleCommands.Add(new ChannelsCommand());         //channels
            consoleIO.ConsoleCommands.Add(new CLSCommand());              //cls
            consoleIO.ConsoleCommands.Add(new ConfigCFUCommand());        //config.checkforupdates
            consoleIO.ConsoleCommands.Add(new CfgDebugWizardCommand());   //config.debugwizard
            consoleIO.ConsoleCommands.Add(new ConfigDCEvtLLCommand());    //config.discordeventloglevel
            consoleIO.ConsoleCommands.Add(new CfgLCMCommand());           //config.loadcoremodule
            consoleIO.ConsoleCommands.Add(new ConfigResetCommand());      //config.reset
            consoleIO.ConsoleCommands.Add(new ConfigSCCommand());         //config.setcolors
            consoleIO.ConsoleCommands.Add(new ConfigSLPCommand());        //config.setlogo
            consoleIO.ConsoleCommands.Add(new ConfigUPRCCommand());       //config.useprereleasechannel
            consoleIO.ConsoleCommands.Add(new ConmsgCommand());           //conmsg
            consoleIO.ConsoleCommands.Add(new DisableCMDCommand());       //disablecmd
            consoleIO.ConsoleCommands.Add(new EnableCMDCommand());        //enablecmd
            consoleIO.ConsoleCommands.Add(new GuildNameCommand());        //guildname
            consoleIO.ConsoleCommands.Add(new GuildsCommand());           //guilds
            consoleIO.ConsoleCommands.Add(new ConIOCrashCommand());       //iocrash
            consoleIO.ConsoleCommands.Add(new LeaveCommand());            //leave
            consoleIO.ConsoleCommands.Add(new ListCommand());             //list
            consoleIO.ConsoleCommands.Add(new MBotDataCommand());         //mbotdata
            consoleIO.ConsoleCommands.Add(new MyRolesCommand());          //myroles
            consoleIO.ConsoleCommands.Add(new RolesCommand());            //roles
            consoleIO.ConsoleCommands.Add(new RSKillCommand());           //rskill
            consoleIO.ConsoleCommands.Add(new SetgchCommand());           //setgch
            consoleIO.ConsoleCommands.Add(new SetvarCommand());           //setvar
            consoleIO.ConsoleCommands.Add(new StatusCommand());           //status
            consoleIO.ConsoleCommands.Add(new StopCommand());             //stopbot
            consoleIO.ConsoleCommands.Add(new TestScreenCommand());       //testscreen
            consoleIO.ConsoleCommands.Add(new TSKillCommand());           //tskill
            consoleIO.ConsoleCommands.Add(new UsersCommand());            //users
            consoleIO.ConsoleCommands.Add(new UpdateCommand());           //update
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

            #region DEBUG
#if (DEBUG)
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "ATTENTION:", "You are running a debug build!"));
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Warning:", "This program is not intended for the production environment."));
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
#endif
            #endregion

            consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Main", "Application started"));

            Task.Run(() => discord.Start(ref consoleIO, ref configMGR.CurrentConfig, ref ShutdownCalled, ref RestartRequested, ref recoveredFromCrash));//Discord.NET thread
            Task r = Task.Run(() => consoleIO.GetConsoleInput(ref ShutdownCalled, ref RestartRequested, ref discord.InputCanceled, ref discord));//Console reader thread;
            if (configMGR.CurrentConfig.ICMPPort.HasValue && configMGR.CurrentConfig.ICMPPort.Value > 0)
            {
                Task s = Task.Run(() =>
                {

                    ping.Bind(new IPEndPoint(IPAddress.Any, configMGR.CurrentConfig.ICMPPort.Value));
                    consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "PING", "Listening for connection on " + ping.LocalEndPoint));
                    ping.Listen(1);
                    while (!ShutdownCalled)
                    {
                        consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "PING", "Waiting for more connections. " + ping.LocalEndPoint));

                        allDone.Reset();
                        ping.BeginAccept(new AsyncCallback(icMPAccept), ping);
                        allDone.WaitOne();
                        consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "PING", "End loop " + ping.LocalEndPoint));

                    }
                });
            }
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

        private static void icMPAccept(IAsyncResult ar)
        {
            allDone.Set();
            var cli = ping.EndAccept(ar);
            consoleIO.WriteEntry(new LogMessage(LogSeverity.Debug, "ICMP", "Accepted client connection: " + cli.RemoteEndPoint.ToString()));
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
