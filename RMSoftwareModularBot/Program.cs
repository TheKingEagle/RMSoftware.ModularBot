using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Discord;
using Discord.WebSocket;
using System.Reflection;
using Discord.Commands;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using RMSoftware.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Runtime.ExceptionServices;

namespace RMSoftware.ModularBot
{
    class Program
    {
        #region STATIC fields & properties
        public static DateTime StartTime;
        public static bool BCMDStarted = false;
        public static bool WizardDebug = false;
        public static bool discon = false;
        public static bool CriticalError = false;
        public static bool RestartRequested = false;
        public static Exception crashException = null;
        public static ConsoleColor ConsoleBackgroundColor { get; set; }
        public static ConsoleColor ConsoleForegroundColor { get; set; }
        static string auth = "";
        static bool CRASH = false;
        public static string[] ARGS = null;
        public static CustomCommandManager ccmg { get; private set; }
        public static CmdRoleManager rolemgt { get; private set; }
        public static char CommandPrefix { get; private set; }
        public static DiscordSocketClient _client;
        static List<SocketMessage> messageQueue = new List<SocketMessage>();
        public static bool LOG_ONLY_MODE = false;//Set by args on start. if true, console header and resets are ignored. Should only be done if hosting the process in a UI.
        public static bool MessagesDisabled = false;
        public static ConsoleLogWriter writer = new ConsoleLogWriter();
        /// <summary>
        /// Application's main configuration file
        /// </summary>
        public static INIFile MainCFG { get; private set;}
        #endregion

        #region Instance fields & properties
        public IServiceCollection serviceCollection;
        public IServiceProvider services;
        public CommandService cmdsvr = new Discord.Commands.CommandService();
        #endregion

        #region STATIC methods

        public static int Main(string[] args)
        {

            rolemgt = new CmdRoleManager();
            ARGS = args;
            try
            {
                foreach (string item in args)
                {
                    if (item.ToLower() == "-log_only")
                    {
                        LOG_ONLY_MODE = true;
                    }
                    if (item.ToLower() == "-crashed")
                    {
                        CRASH = true;
                    }
                    if (item.StartsWith("-auth"))
                    {
                        auth = item.Split(' ')[1];
                    }
                }
                BCMDStarted = false;
                ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome",79,45);
                Console.Title = "RMSoftware.ModularBOT";
                Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                System.Threading.Thread.Sleep(800);
                ConsoleWriteImage(Prog.res1.Resource1.RMSoftwareICO);
                System.Threading.Thread.Sleep(3000);
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Program Starting");
                

                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                //AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

                SetupWizard();

                if (auth.Length == 0)
                {
                    ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, "Initializing");
                    writer.WriteEntry(new LogMessage(LogSeverity.Warning,"Program","Auth token not specified in parameters."));
                    writer.WriteEntry(new LogMessage(LogSeverity.Info, "Program", "Usage: RMSoftwareModularBot.exe -auth <token string>"), ConsoleColor.Cyan);
                    writer.WriteEntry(new LogMessage(LogSeverity.Info, "Program", "Using auth token from configuration."), ConsoleColor.Cyan);
                    auth = Program.MainCFG.GetCategoryByName("Application").GetEntryByName("botToken").GetAsString();
                    Thread.Sleep(1000);
                }

                //set command prefix

                CommandPrefix = Convert.ToChar(MainCFG.GetCategoryByName("Application").GetEntryByName("cmdPrefix").GetAsInteger());
                Program.LogToConsole(new LogMessage(LogSeverity.Info, "Initialize", $"Using the command prefix: {(char)CommandPrefix}"));
                var prg = new Program();
                prg.MainAsync(auth).GetAwaiter().GetResult();
                Task.Run(new Action(ReadConsole));
                SpinWait.SpinUntil(BotShutdown);//Wait until shutdown;
                BCMDStarted = false;
                ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, "Disconnected");
                if (CriticalError)
                {
                    Program.LogToConsole(new LogMessage(LogSeverity.Critical, "Session", "Failed to resume previous session.",crashException));
                    using (FileStream fs = File.Create("CRASH.LOG"))
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            LogMessage m = new LogMessage(LogSeverity.Critical, "Session", crashException.Message, crashException);
                            sw.WriteLine(m.ToString());
                            List<string> restart_args = new List<string>();
                            restart_args.AddRange(ARGS);
                            if (!restart_args.Contains("-crashed"))
                            {
                                restart_args.Add("-crashed");
                            }
                            ARGS = restart_args.ToArray();
                            sw.Flush();
                        }
                    }
                    if (!LOG_ONLY_MODE)
                    {
                        Program.LogToConsole(new LogMessage(LogSeverity.Warning, "Application", "Restarting in 4 seconds..."));
                        Thread.Sleep(1000);
                    }

                    RestartRequested = true;
                    _client = null;
                }
            }
            catch (Exception ex)
            {
                crashException = ex;
                ConsoleGUIReset(ConsoleColor.White,ConsoleColor.DarkRed,"Critical Application Error.");
                Program.LogToConsole(new LogMessage(LogSeverity.Critical, "Exception", ex.Message, ex));
                CriticalError = true;
                RestartRequested = true;
                using (FileStream fs = File.Create("CRASH.LOG"))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        LogMessage m = new LogMessage(LogSeverity.Critical, "Session", ex.Message, ex);
                        sw.WriteLine(m.ToString());
                        List<string> restart_args = new List<string>();
                        restart_args.AddRange(ARGS);
                        if (!restart_args.Contains("-crashed"))
                        {
                            restart_args.Add("-crashed");
                        }
                        ARGS = restart_args.ToArray();
                        sw.Flush();

                    }
                }
                using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {

                        sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + ex.ToString());
                        sw.Flush();

                    }
                }
            }
            if (RestartRequested)
            {
                Process p = new Process();
                string flattened = "";
                foreach (string item in ARGS)
                {
                    flattened += item + " ";
                }
                p.StartInfo = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, flattened);
                ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, "Disconnected");
                writer.WriteEntry(new LogMessage(LogSeverity.Warning, "Program", "Restarting in 3..."));//using direct writer to try and prevent blanks.

                Thread.Sleep(1000);
                ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, "Disconnected");
                writer.WriteEntry(new LogMessage(LogSeverity.Warning, "Program", "Restarting in 2..."));

                Thread.Sleep(1000);
                ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, "Disconnected");
                writer.WriteEntry(new LogMessage(LogSeverity.Warning, "Program", "Restarting in 1..."));

                Thread.Sleep(1000);
                p.Start();
                return 0x00000c4;
            }
            writer.WriteEntry(new LogMessage(LogSeverity.Warning, "Program", "Termination."));

            if (!CriticalError)
            {

                return 4007;//NOT OKAY
            }
            else
            {

                return 200;//OK status
            }
        }

        private static void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            writer.WriteEntry(new LogMessage(LogSeverity.Critical, "FirstERR", e.Exception.ToString()));
            Console.ReadLine();
        }

        static void ReadConsole()
        {
            ulong chID = 0;
            while (true)
            {
                if (discon)
                {
                    break;
                }
                Console.BackgroundColor = ConsoleColor.Black;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(">");//set cursor: 1,top & add black color (default)
                
                Console.BackgroundColor = ConsoleBackgroundColor;
                Console.ForegroundColor = ConsoleForegroundColor;
                string input = Console.ReadLine();
                Console.SetCursorPosition(0, Console.CursorTop-1);
                writer.WriteEntry(new LogMessage(LogSeverity.Info, "Console", input));
                if (discon)
                {
                    if(string.IsNullOrWhiteSpace(input))
                    {
                        return;
                    }
                }
                if (input.ToLower() == "stopbot")
                {
                    _client.SetGameAsync("");
                    System.Threading.Thread.Sleep(2000);
                    _client.SetStatusAsync(UserStatus.Invisible);
                    System.Threading.Thread.Sleep(2000);
                    BCMDStarted = false;
                    _client.StopAsync();
                    System.Threading.Thread.Sleep(3000);//Allow the bot to shut down fully before telling Main() to scream at user to finger the keyboard to close the console.
                    discon = true;
                    break;
                }
                if (input.ToLower() == "cn_term")
                {
                    Console.WriteLine("Termination");
                    break;
                }
                if (input.ToLower() == "disablecmd")
                {
                    LogToConsole(new LogMessage(LogSeverity.Warning, "Console", "Command processing disabled!"));

                    _client.SetStatusAsync(UserStatus.DoNotDisturb);
                    _client.SetGameAsync("");
                    MessagesDisabled = true;
                }
                if (input.ToLower() == "enablecmd")
                {
                    LogToConsole(new LogMessage(LogSeverity.Info, "Console", "Command processing enabled."));

                    _client.SetStatusAsync(UserStatus.Online);
                    _client.SetGameAsync("READY!");
                    MessagesDisabled = false;
                }
                if (input.ToLower().StartsWith("status"))
                {
                    string status = input.Remove(0, 10).Trim();
                    LogToConsole(new LogMessage(LogSeverity.Warning, "Client", "client status changed."));

                    _client.SetGameAsync(status);
                }
                if (input.StartsWith("setgch"))
                {
                    input = input.Remove(0, 6).Trim();
                    if (!ulong.TryParse(input, out chID))
                    {
                        LogToConsole(new LogMessage(LogSeverity.Error, "Console", "Invalid ULONG."));
                        continue;
                    }
                }
                if (input.StartsWith("conmsg"))
                {
                    input = input.Remove(0, 6).Trim();
                    SocketTextChannel Channel = _client.GetChannel(chID) as SocketTextChannel;
                    if (Channel == null)
                    {
                        LogToConsole(new LogMessage(LogSeverity.Warning, "Console", "Invalid channel."));
                        continue;
                    }
                    Channel.SendMessageAsync(input);
                }
                if (input.StartsWith("setvar"))
                {
                    input = input.Remove(0, 6).Trim();
                    string varname = input.Split(' ')[0];
                    input = input.Remove(0,varname.Length);
                    input = input.Trim();
                    ccmg.scriptService.Set(varname, input);
                }

            }
        }

        private static void SetupWizard()
        {
            if (InitializeConfig())//If true, do setup.
            {
                #region Page 1
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Welcome");
                Console.WriteLine("Welcome to the initial setup wizard for your new modular discord bot...\r\n Some things to note before we get started:");
                Console.WriteLine("\t- This bot requires a token. If you don't know what that is," +
                    " please visit this site first!");
                Console.WriteLine("\t- https://discordapp.com/developers/docs/intro");

                Console.WriteLine("\t- Privacy notice: This application will output any message that mentions the bot, or messages that start with ! to the console.\r\n\t" +
                    " THEY DO NOT GET SAVED OR POSTED ANYWHERE ELSE... That would be creepy and probably illegal...");

                Console.WriteLine("\r\nPress ENTER to continue...");
                Console.ReadLine();
                #endregion

                #region Page 2
                ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Step 1: Token Configuration");

                Console.WriteLine("As mentioned before, this bot requires a token. It can be added in one of two ways.");
                Console.WriteLine("\t- METHOD 1: Program arguments. Simply launch the program from a batch file,\r\n\t- USAGE: RMSoftwareModularBot.exe <put token here>");
                Console.WriteLine("\t- METHOD 2: Configure it. Actually, let's do that now.");
                Console.WriteLine("Go ahead and find your token and copy/paste* it here, then press enter.");
                Console.WriteLine("*Only if your CMD console will let you... If not, that sucks... You can painfully type it in also...");
                Console.Write("> ");
                string conf_Token = Console.ReadLine();
                
                if (!WizardDebug)
                {
                    MainCFG.CreateCategory("Application");
                    MainCFG.CreateEntry("Application", "botToken", conf_Token);
                }

                Console.WriteLine("Okay! Please remember, You can also start the program with a token as an argument.");
                Console.WriteLine("Be advised, the token in the configuration is ignored when using the program parameter.");

                Console.WriteLine("\r\nPress ENTER to continue...");
                Console.ReadLine();
                #endregion

                #region Page 3
                ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Step 2: Control Channel & Command prefix");

                Console.WriteLine("Ah, yes... The Control channel...");
                Console.WriteLine("\t- Whenever you start your bot, it will execute AutoStart.bcmd, a script full of commands to prepare for that bot life...");
                Console.WriteLine("\t- However, in order to do this, the bot needs to know where to send these messages and commands...");
                Console.WriteLine("\t- Not doing this now would result in a crashing bot that doesn't know what to do with its life.");
                Console.WriteLine("\t- All I need is the ID of that channel to put into the configuration.");
                Console.WriteLine("\t- If you need a refresher on how to find a channel ID: https://goo.gl/nqAbhw \r\n\t" +
                    "(It goes to the discord's official docs)");
                Console.WriteLine("copy/paste or painfully type in your Initialization Channel's id and press enter");
                Console.Write("> ");
                string conf_BotChannel = Console.ReadLine();
                if (!WizardDebug)
                {
                    Program.MainCFG.CreateEntry("Application", "botChannel", conf_BotChannel);
                }
                Console.WriteLine("Great! Now that channel will be the bot's main log channel...");
                Console.WriteLine();
                Console.WriteLine("Now... The Command Prefix: Please enter a single character (Recommended: A symbol of some kind), to use as the bot's command prefix");
                Console.Write("> ");
                int conf_cmdPrefix = Console.Read();
                CommandPrefix = (char)conf_cmdPrefix;
                if (!WizardDebug)
                {
                    Program.MainCFG.CreateEntry("Application", "cmdPrefix", conf_cmdPrefix);
                    Program.MainCFG.SaveConfiguration();//save
                }
                Console.WriteLine("Great! Your bot will use '" + Convert.ToChar(conf_cmdPrefix) + "' [without quotes] as a prefix for it's commands!");
                Console.WriteLine("\r\nPress ENTER to continue...");
                Console.ReadLine();

                #endregion

                #region Page 4
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Step 3: Final Notes & ProTips");
                Console.WriteLine("That is all the configuration for right now! Here are a few more things to know:");
                Console.WriteLine("\t- If you want to re-run this configuration wizard, delete the 'rmsftModBot.ini' file in the program directory.");
                Console.WriteLine("\t- The source code for this bot is available on http://rmsoftware.org");
                Console.WriteLine("\r\nCORE Command usage (in discord):");
                Console.WriteLine("\t- You will need to add command management roles to the bot, if you want other users to be able to add or remove commands\r\n\t  and interact with restricted commands.");
                Console.WriteLine("\t- Since you own the bot account that uses the token you provided, you are considered a bot owner. \r\n\t  This means you will automatically have access to all commands, regardless of the restrictions in place.");
                Console.WriteLine($"\t- use {CommandPrefix}addmgrole [@roles] to add roles to the command user database.");
                Console.WriteLine($"\t- usage: {CommandPrefix}addcmd <command name> <CmdMgmtOnly[true/false]> <LockToGuild[true/false]> <action>");
                Console.WriteLine("\t- Actions: Any text/emotes with optional formatting.");
                Console.WriteLine($"\t- {CommandPrefix}addcmd hug false false You hug {{params}} for a long time");
                Console.WriteLine($"\t- {CommandPrefix}addcmd grouphug false false You hug {{0}}, {{1}}, {{2}}, and {{3}} for a long time");
                Console.WriteLine("\t- More Action parameters: EXEC and CLI_EXEC ");
                Console.WriteLine($"\t- {CommandPrefix}addcmd exectest falase false EXEC modname.dll ModNameSpace.ModClass StaticMethod {{params}}");
                Console.WriteLine($"\t- {CommandPrefix}addcmd exectest falase false CLI_EXEC modname.dll ModNameSpace.ModClass StaticMethod {{params}}");
                Console.WriteLine("\t  - NOTE: splitparam is not supported for EXEC or CLI_EXEC");
                Console.WriteLine("\t  - NOTE: EXEC: Allows you to execute a class method for a more advanced command");
                Console.WriteLine("\t  - NOTE: CLI_EXEC is the same thing, but it gives the class access to the bot directly...");
                Console.WriteLine("Extra Configuration Options:");
                Console.WriteLine("\t  - Adding the line DisableCore=True to the [Application] section of the config file, will disable all core commands.");
                Console.WriteLine("\t     - Remember this will disable the ability to manage commands. putting the bot in a sort of 'read-only' state.");
           
                Console.WriteLine("Override core: ");
                Console.WriteLine("\t  - You can create custom commands that use the same name as core commands.\r\n\t   This is useful for overriding core commands for even more customization...");
                Console.WriteLine("\r\nPlease visit http://rmsoftware.org/rmsoftwareModularBot for more information and documentation.");
                Console.WriteLine("\r\nPress ENTER to launch the bot!");
                Console.ReadLine();
                #endregion
            }
        }


        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CriticalError = true;
            RestartRequested = true;
            crashException = (Exception)e.ExceptionObject;
            using (FileStream fs = File.Create("CRASH.LOG"))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    LogMessage m = new LogMessage(LogSeverity.Critical, "Session", crashException.Message, crashException);
                    sw.WriteLine(m.ToString());
                    List<string> restart_args = new List<string>();
                    restart_args.AddRange(ARGS);
                    if (!restart_args.Contains("-crashed"))
                    {
                        restart_args.Add("-crashed");
                    }
                    ARGS = restart_args.ToArray();
                    sw.Flush();

                }
            }
            using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {

                    sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + ((Exception)e.ExceptionObject).ToString());
                    sw.Flush();

                }
            }
        }
        static Func<bool> BotShutdown = delegate ()
        {
            return discon;
        };

        /// <summary>
        /// Reset the console layout using specified values
        /// </summary>
        /// <param name="fore">Text color</param>
        /// <param name="back">Background color</param>
        /// <param name="title">Console's header title (not window title)</param>
        public static void ConsoleGUIReset(ConsoleColor fore, ConsoleColor back, string title)
        {
            if(LOG_ONLY_MODE)
            {
                return;
            }
            Console.Clear();
            Console.SetWindowSize(144, 32);//Seems to be a reasonable console size.
            Console.SetBufferSize(144, 512);//Extra buffer room just because why not.
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
            Console.Clear();
            
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            DecorateTop();

            string WTitle = (""+DateTime.Now.ToString("HH:mm:ss") + " " + title + " - RMSoftwareModularBot v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            string pTitle = WTitle.PadLeft(71+WTitle.Length/2);
            pTitle += "".PadRight(71-WTitle.Length/2);
            Console.Write("\u2551{0}\u2551", pTitle);

            DecorateBottom();

            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;

            ConsoleBackgroundColor = back;
            ConsoleForegroundColor = fore;
        }

        /// <summary>
        /// Reset the console layout using specified values
        /// </summary>
        /// <param name="fore">Text color</param>
        /// <param name="back">Background color</param>
        /// <param name="title">Console's header title (not window title)</param>
        /// <param name="w">Console window & buffer width</param>
        /// <param name="h">Console window & buffer height</param>
        public static void ConsoleGUIReset(ConsoleColor fore, ConsoleColor back, string title,short w, short h)
        {
            if (LOG_ONLY_MODE)
            {
                return;
            }
            Console.Clear();
            Console.SetWindowSize(w, h);
            Console.SetBufferSize(w, h);
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
            Console.Clear();

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            DecorateTop();

            string WTitle = ("" + DateTime.Now.ToString("HH:mm:ss") + " " + title + " - RMSoftwareModularBot v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            string pTitle = WTitle.PadLeft(((w/2)+2) + WTitle.Length / 2);
            pTitle += "".PadRight(((w / 2)-3) - WTitle.Length / 2);
            Console.Write("\u2551{0}\u2551", pTitle);

            DecorateBottom();

            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
            ConsoleBackgroundColor = back;
        }

        private static void DecorateTop()
        {
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                if(i==0)
                {
                    Console.Write("\u2554");
                    continue;
                }
                
                if (i == Console.WindowWidth-1)
                {
                    Console.Write("\u2557");
                    break;
                }
                Console.Write("\u2550");
            }
        }

        private static void DecorateBottom()
        {
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                if (i == 0)
                {
                    Console.Write("\u255A");
                    continue;
                }

                if (i == Console.WindowWidth - 1)
                {
                    Console.Write("\u255D");
                    break;
                }

                Console.Write("\u2550");
            }
        }

        public static void LogToConsole(LogMessage msg)
        {
            if(!LOG_ONLY_MODE)
            {
                writer.WriteEntry(msg);
            }
            else
            {
                Console.WriteLine(JsonConvert.SerializeObject(msg));
            }
            if (msg.Exception != null)
            {
                using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + msg.ToString());
                        sw.Flush();
                    }
                }
            }
        }

        public static void LogToConsole(LogMessage msg,ConsoleColor entryColor)
        {
            if (!LOG_ONLY_MODE)
            {
                writer.WriteEntry(msg,entryColor);
            }
            else
            {
                Console.WriteLine(JsonConvert.SerializeObject(msg));
            }
            if (msg.Exception != null)
            {
                using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + msg.ToString());
                        sw.Flush();
                    }
                }
            }
        }

        /// <summary>
        /// Initialize configuration.
        /// </summary>
        /// <returns>True if it generated a new file, or has zero categories in said file.</returns>
        private static bool InitializeConfig()
        {
            ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Initializing Configuration");
            Program.MainCFG = new INIFile("rmsftModBot.ini");

            if (Program.MainCFG.Categories.Count == 0)
            {
                return true;
            }
            else
            {
                if (Program.MainCFG.CheckForCategory("Application"))
                {
                    if (Program.MainCFG.GetCategoryByName("Application").CheckForEntry("Dev-ShowWizard"))
                    {
                        WizardDebug = true;
                        return Program.MainCFG.GetCategoryByName("Application").GetEntryByName("Dev-ShowWizard").GetAsBool();
                    }

                    //REQUIRED entries.
                    if (!Program.MainCFG.GetCategoryByName("Application").CheckForEntry("botToken") ||
                        !Program.MainCFG.GetCategoryByName("Application").CheckForEntry("botChannel") ||
                        !Program.MainCFG.GetCategoryByName("Application").CheckForEntry("cmdPrefix"))
                    {
                        Program.LogToConsole(new LogMessage(LogSeverity.Warning, "Warning", "Invalid or missing config. Reset all settings."));

                        File.Delete("rmsftModBot.ini");
                        Program.MainCFG = new INIFile("rmsftModBot.ini");//Re-write from scratch.
                        return true;
                    }
                }
            }
            return false;//basically the file is found and nothing broke.
        }


        #region Console pixel art - Courtesy of Stackoverflow.com
        static int[] cColors = { 0x000000, 0x000080, 0x008000, 0x008080, 0x800000, 0x800080, 0x808000, 0xC0C0C0, 0x808080, 0x0000FF, 0x00FF00, 0x00FFFF, 0xFF0000, 0xFF00FF, 0xFFFF00, 0xFFFFFF };
        

        public static void ConsoleWritePixel(System.Drawing.Color cValue)
        {
            System.Drawing.Color[] cTable = cColors.Select(x => System.Drawing.Color.FromArgb(x)).ToArray();
            char[] rList = new char[] { (char)9617, (char)9618, (char)9619, (char)9608 }; // 1/4, 2/4, 3/4, 4/4
            int[] bestHit = new int[] { 0, 0, 4, int.MaxValue }; //ForeColor, BackColor, Symbol, Score

            for (int rChar = rList.Length; rChar > 0; rChar--)
            {
                for (int cFore = 0; cFore < cTable.Length; cFore++)
                {
                    for (int cBack = 0; cBack < cTable.Length; cBack++)
                    {
                        int R = (cTable[cFore].R * rChar + cTable[cBack].R * (rList.Length - rChar)) / rList.Length;
                        int G = (cTable[cFore].G * rChar + cTable[cBack].G * (rList.Length - rChar)) / rList.Length;
                        int B = (cTable[cFore].B * rChar + cTable[cBack].B * (rList.Length - rChar)) / rList.Length;
                        int iScore = (cValue.R - R) * (cValue.R - R) + (cValue.G - G) * (cValue.G - G) + (cValue.B - B) * (cValue.B - B);
                        if (!(rChar > 1 && rChar < 4 && iScore > 50000)) // rule out too weird combinations
                        {
                            if (iScore < bestHit[3])
                            {
                                bestHit[3] = iScore; //Score
                                bestHit[0] = cFore;  //ForeColor
                                bestHit[1] = cBack;  //BackColor
                                bestHit[2] = rChar;  //Symbol
                            }
                        }
                    }
                }
            }
            Console.ForegroundColor = (ConsoleColor)bestHit[0];
            Console.BackgroundColor = (ConsoleColor)bestHit[1];
            Console.Write(rList[bestHit[2] - 1]);
        }

        public static void ConsoleWriteImage(System.Drawing.Bitmap source)
        {
            if (LOG_ONLY_MODE)
            {
                return;
            }
            int sMax = 39;
            decimal percent = Math.Min(decimal.Divide(sMax, source.Width), decimal.Divide(sMax, source.Height));
            System.Drawing.Size dSize = new System.Drawing.Size((int)(source.Width * percent), (int)(source.Height * percent));
            System.Drawing.Bitmap bmpMax = new System.Drawing.Bitmap(source, dSize.Width * 2, dSize.Height);
            for (int i = 0; i < dSize.Height; i++)
            {
                for (int j = 0; j < dSize.Width; j++)
                {
                    ConsoleWritePixel(bmpMax.GetPixel(j * 2, i));
                    ConsoleWritePixel(bmpMax.GetPixel(j * 2 + 1, i));
                }
                System.Console.WriteLine();
            }
            Console.ResetColor();
        }
        #endregion

        #endregion

        #region Async tasks
        public async Task LoadModules()
        {
            cmdsvr = new CommandService();
            


            foreach (string item in Directory.EnumerateFiles("CMDModules","*.dll",SearchOption.TopDirectoryOnly))
            {
                LogToConsole(new LogMessage(LogSeverity.Info,"Modules", $"Adding commands from module library: {item}"),ConsoleColor.DarkGreen);
                try
                {
                    Assembly asmb = Assembly.LoadFile(Path.GetFullPath(item));
                    string serviceFilename = $"CMDModules\\services.{Path.GetFileNameWithoutExtension(item)}.ini";
                    string servicefilePath = Path.GetFullPath(serviceFilename);
                    if(File.Exists(servicefilePath))
                    {
                        INIFile serviceFile = new INIFile(servicefilePath);
                        foreach (INIEntry entryitem in serviceFile.GetCategoryByName("Services").Entries)
                        {
                            string typename = entryitem.GetAsString();
                            LogToConsole(new LogMessage(LogSeverity.Verbose,"Modules", $"Injecting service: {typename} from {asmb.GetName().Name}"));
                            serviceCollection = serviceCollection.AddSingleton(asmb.GetType(typename));
                        }
                    }
                    await cmdsvr.AddModulesAsync(asmb);//ADD EXTERNAL.
                }
                catch (Exception ex)
                {
                    Program.LogToConsole(new LogMessage(LogSeverity.Error, "CritERR",ex.Message,ex));

                    using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + ex.ToString());
                            sw.Flush();
                        }
                    }
                }
            }
            serviceCollection = serviceCollection.AddSingleton(_client);
            serviceCollection = serviceCollection.AddSingleton(writer);
            serviceCollection = serviceCollection.AddSingleton(cmdsvr);
            services = serviceCollection.BuildServiceProvider();
            //check cfg for disabled core.
            if(MainCFG.GetCategoryByName("Application").CheckForEntry("disableCore"))
            {
                if(MainCFG.GetCategoryByName("Application").GetEntryByName("disableCore").GetAsBool())
                {
                    LogToConsole(new LogMessage(LogSeverity.Warning,"Program", "RMSoftware.ModularBOT CORE was disabled in the configuration."));
                    LogToConsole(new LogMessage(LogSeverity.Warning,"Program", "You will not be able to manage existing commands, nor add new ones, via discord."));
                    return;//DO NOT LOAD CORE IF THIS IS TRUE.
                }
            }
            await cmdsvr.AddModulesAsync(Assembly.GetEntryAssembly());//ADD CORE.
            ccmg = new CustomCommandManager(ref cmdsvr, ref services);
        }

        public async Task MainAsync(string token)
        {
            try
            {
                ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, "Application Running");

                _client = new DiscordSocketClient();
                _client.Log += Log;
                serviceCollection = new ServiceCollection();

                _client.Ready += _ClientReady;
                _client.Connected += _client_Connected;
                _client.MessageReceived += _client_MessageReceived;
                _client.Disconnected += _client_Disconnected;

                await LoadModules();//ADD CORE AND EXTERNAL MODULES
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
            }
            catch (Exception ex)
            {
                CriticalError = true;
                RestartRequested = true;
                crashException = ex;
                using (FileStream fs = File.Create("CRASH.LOG"))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        LogMessage m = new LogMessage(LogSeverity.Critical, "Exception", crashException.Message, crashException);
                        sw.WriteLine(m.ToString());
                        List<string> restart_args = new List<string>();
                        restart_args.AddRange(ARGS);
                        if (!restart_args.Contains("-crashed"))
                        {
                            restart_args.Add("-crashed");
                        }
                        ARGS = restart_args.ToArray();
                        sw.Flush();
                    }
                }
                using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {

                        sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + ex.ToString());
                        sw.Flush();

                    }
                }
            }
                   
        }

        private Task _client_Connected()
        {
            StartTime = DateTime.Now;
            LogToConsole(new LogMessage(LogSeverity.Info,"Uptime", "Reset uptime to ALL zero."));
            Console.Title = "RMSoftware.ModularBOT -> " + _client.CurrentUser.Username + " | Connected to " + _client.Guilds.Count + " guilds.";
            return Task.Delay(1);
        }

        private Task _client_Disconnected(Exception arg)
        {
            LogToConsole(new LogMessage(LogSeverity.Warning,"Session", "Disconnected: "+ arg.Message,arg));
            Console.Title = "RMSoftware.ModularBot - Disconnected";
            LogToConsole(new LogMessage(LogSeverity.Info,"Uptime", "The client is disconnected."));
            return Task.Delay(3);
        }

        private async Task _ClientReady()
        {
            await Log(new LogMessage(LogSeverity.Info, "TaskMgr", "Running Onstart.bcmd - task"));
            await Task.Run(new Action(OffloadReady));
            Console.Title = "RMSoftware.ModularBOT -> " + _client.CurrentUser.Username + " | Connected to " + _client.Guilds.Count + " guilds.";
        }

        private async void OffloadReady()
        {
            try
            {
                if (!BCMDStarted)
                {
                    await _client.SetStatusAsync(UserStatus.DoNotDisturb);
                    ulong id = MainCFG.GetCategoryByName("Application").GetEntryByName("botChannel").GetAsUlong();
                    if (CRASH)
                    {
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithAuthor(_client.CurrentUser);
                        builder.WithTitle("WARNING");
                        builder.WithDescription("The program was auto-restarted due to a crash. Please see `Crash.LOG` and `Errors.LOG` for details.");
                        builder.WithColor(new Color(255, 255, 0));
                        builder.WithFooter("RMSoftware.ModularBOT Core");
                        await ((SocketTextChannel)_client.GetChannel(id)).SendMessageAsync("",false,builder.Build());
                        LogToConsole(new LogMessage(LogSeverity.Warning, "TaskMgr", "The program auto-restarted due to a crash. Please see Crash.LOG."));
                    }
                    //PROCESS THE AutoEXEC file
                    

                    await ccmg.scriptService.EvaluateScriptFile("OnStart.core", ccmg.CmdDB, "", _client, new PsuedoMessage("", _client.CurrentUser, (IGuildChannel)_client.GetChannel(id), MessageSource.Bot));

                    BCMDStarted = true;
                    await _client.SetGameAsync("READY!");
                    LogToConsole(new LogMessage(LogSeverity.Info, "TaskMgr", "Task is complete."));
                    await _client.SetStatusAsync(UserStatus.Online);
                    LogToConsole(new LogMessage(LogSeverity.Info, "TaskMgr", "Running Message Queue - task"));
                    foreach (var item in messageQueue)
                    {

                        await _client_MessageReceived(item);
                        await Task.Delay(500);
                    }
                    LogToConsole(new LogMessage(LogSeverity.Info, "TaskMgr", "Task is complete"));
                }
            }
            catch (Exception ex)
            {
                BCMDStarted = false;
                LogToConsole(new LogMessage(LogSeverity.Error, "TaskMgr", ex.Message, ex));

            }
        }

        private async Task _client_MessageReceived(SocketMessage arg)
        {
            if(MessagesDisabled)
            {
                return;
            }
            if(rolemgt.UserBlacklisted(arg.Author))
            {
                return;
            }
            #region Console Output debug
            //DEBUG: Output the bot-mentioned chat message to the console
            foreach (SocketUser item in arg.MentionedUsers)
            {
                if (item.Mention == _client.CurrentUser.Mention)
                {
                    LogToConsole(new LogMessage(LogSeverity.Info,"Mention", "<[" + arg.Channel.Name + "] " + arg.Author.Username + " >: " + arg.Content));
                }
            }
            //DEBUG: output ! prefixed messages to console.
            if (arg.Content.StartsWith(CommandPrefix.ToString()))
            {
                LogToConsole(new LogMessage(LogSeverity.Info,"Command", "<[" + arg.Channel.Name + "] " + arg.Author.Username + " >: " + arg.Content));

            }

            #endregion

            // Don't process the command if it was a System Message
            var message = arg as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with 'commandprefix' or a mention prefix. if not, ignore it.
            if (!(message.HasCharPrefix(CommandPrefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;
            if (!arg.Author.IsBot && !BCMDStarted)
            {
                messageQueue.Add(arg);  //queue it up. The bcmdStarted check should 
                                        //provide PLENTY (if not an excessive amount) of time for modules,and extra commands 
                                        //to be fully loaded by the time it is set to true. Preemptively solving the "hey, you just ignored my commands completely" 
                                        //when the bot starts and doesn't respond to a command at first
                return;
            }
            // Create a Command Context for command modules
            //Process CoreCustom commands
            if (await ccmg.Process(arg))
            {
                return;//If the message contained a custom command from config, No need to continue.
            };
            var context = new CommandContext(_client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await cmdsvr.ExecuteAsync(context, argPos, services);
            //If the result is unsuccessful AND not unknown command, send the error details.
            if (result.Error.HasValue)
            {
                if (!result.Error.Value.HasFlag(CommandError.UnknownCommand) && !result.IsSuccess)
                {
                    await arg.Channel.SendMessageAsync(result.ErrorReason);
                }
                if (result.Error.Value.HasFlag(CommandError.BadArgCount))
                {
                    await arg.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }

        private Task Log(LogMessage msg)
        {
            if(!LOG_ONLY_MODE)
            {
                LogToConsole(msg);
            }
            else
            {
                Console.WriteLine(JsonConvert.SerializeObject(msg));
            }
            if (msg.Message == "Failed to resume previous session")
            {
                _client.StopAsync();
                _client = null;
                crashException = new Exception("Failed to resume previous session. See ERRORS.LOG for any additional information.");
                CriticalError = true;
                discon = true;
                Thread.Sleep(3000);
            }
            if(msg.Exception != null)
            {
                using (FileStream fs = new FileStream("ERRORS.LOG",FileMode.Append))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        
                        sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy")+"   "+msg.ToString());
                        sw.Flush();

                    }
                }
            }
            return Task.Delay(0);
        }


        #endregion
    }
    
    #region Retry clause - API error workaround [possibly obsolete] [Stackoverflow.com]
    public static class Retry
    {
        public static void Do(
            Action action,
            TimeSpan retryInterval,
            int maxAttemptCount = 3)
        {
            Do<object>(() =>
            {
                 action();
                return null;
            }, retryInterval, maxAttemptCount);
        }

        public static T Do<T>(
            Func<T> action,
            TimeSpan retryInterval,
            int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }
                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    Program.LogToConsole(new LogMessage(LogSeverity.Warning,"API_RETRY", ex.Message,ex));
                    Program.LogToConsole(new LogMessage(LogSeverity.Warning,"API_RETRY", "Retrying " + (maxAttemptCount-attempted) + " more time(s)"));
                }
            }
            throw new AggregateException(exceptions);
        }
    }

    #endregion

}
