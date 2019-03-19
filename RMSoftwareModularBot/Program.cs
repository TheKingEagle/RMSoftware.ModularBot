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
        #region STATIC fields & Properties DECLARE
        public static INIFile MainCFG { get; private set; } //Application configuration
        public static ConsoleColor ConsoleBackgroundColor { get; set; } //Cached background color
        public static ConsoleColor ConsoleForegroundColor { get; set; } //Cached foreground color
        public static CustomCommandManager ccmg { get; private set; } //Application Custom Command Manager
        public static CmdRoleManager rolemgt { get; private set; } //Application Role Manager
        public static char CommandPrefix { get; private set; } //Application command prefix

        public static bool Initialized = false; //Is bot actually ready?
        public static bool WizardDebug = false; //Show wizard if already set up?
        public static bool discon = false; //is the bot instructed to shutdown?
        public static bool CriticalError = false; //Did something explode?
        public static bool RestartRequested = false; //Did we request a restart?
        public static Exception crashException = null; //Cause of crash
        public static string[] ARGS = null; //Application arguments
        public static bool LOG_ONLY_MODE = false; //Launched in log-only?
        public static bool MessagesDisabled = false; //Did we disable message processing?
        public static ConsoleLogWriter writer = new ConsoleLogWriter(); //Application Log writer (Console UI friendly)
        public static int CursorPTop = 0; //Cached cursor top position

        public static DateTime StartTime; //Uptime origin
        public static DiscordSocketClient _client; // ModularBOT's discord client

        private static string AuthToken = ""; //Auth token (Specified in ARGS)
        private static bool recovered = false; //Bot restarted from crash (specified in ARGS)
        private static List<SocketMessage> messageQueue = new List<SocketMessage>(); //A queue for messages captured before Initalized=true;

        #endregion

        #region INSTANTIATED Fields & Properties DECLARE
        public CommandService cmdsvr = new Discord.Commands.CommandService(); //Application Command Service.

        public IServiceCollection serviceCollection; //Application Service collection
        public IServiceProvider services; //Application Service provider.
        
        private int timeout = 0; //Operation timeout value
        private bool timeoutStart = false; //Did the Operation timeout started?

        #endregion

        #region STATIC Methods

        public static int Main(string[] args)
        {
            if(!Directory.Exists("CMDModules"))
            {
                Directory.CreateDirectory("CMDModules");
            }
            if (!Directory.Exists("EXT"))
            {
                Directory.CreateDirectory("EXT");
            }
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
                        recovered = true;
                    }
                    if (item.StartsWith("-auth"))
                    {
                        AuthToken = item.Split(' ')[1];
                    }
                }
                SetupWizard();
                Initialized = false;
                ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome",79,45);
                Console.Title = "RMSoftware.ModularBOT";
                Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                System.Threading.Thread.Sleep(800);
                if(!MainCFG.GetCategoryByName("application").CheckForEntry("initLogo"))
                {

                    ConsoleWriteImage(Prog.res1.Resource1.RMSoftwareICO);
                }
                else
                {
                    string filename = MainCFG.GetCategoryByName("application").GetEntryByName("initLogo").GetAsString().Replace("\"","");

                    System.Drawing.Bitmap b = new System.Drawing.Bitmap(filename);
                    ConsoleWriteImage(b);
                }
                System.Threading.Thread.Sleep(3000);
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Program Starting");
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                #if(DEBUG)
                AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
                #endif
                

                if (AuthToken.Length == 0)
                {
                    ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, "Initializing");
                    writer.WriteEntry(new LogMessage(LogSeverity.Warning,"Program","Auth token not specified in parameters."));
                    writer.WriteEntry(new LogMessage(LogSeverity.Info, "Program", "Usage: RMSoftwareModularBot.exe -auth <token string>"), ConsoleColor.Cyan);
                    writer.WriteEntry(new LogMessage(LogSeverity.Info, "Program", "Using auth token from configuration."), ConsoleColor.Cyan);
                    AuthToken = Program.MainCFG.GetCategoryByName("Application").GetEntryByName("botToken").GetAsString();
                    Thread.Sleep(1000);
                }

                //set command prefix

                CommandPrefix = Convert.ToChar(MainCFG.GetCategoryByName("Application").GetEntryByName("cmdPrefix").GetAsInteger());
                Program.LogToConsole(new LogMessage(LogSeverity.Info, "Initialize", $"Using the command prefix: {(char)CommandPrefix}"));
                var prg = new Program();
                prg.MainAsync(AuthToken).GetAwaiter().GetResult();
                Task.Run(new Action(GetConsoleInput));
                SpinWait.SpinUntil(BotShutdown);//Wait until shutdown;
                Initialized = false;
                ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, "Disconnected");
                if (CriticalError)
                {
                    Program.LogToConsole(new LogMessage(LogSeverity.Critical, "Session", crashException.Message, crashException));
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
                discon = true;
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
                p.Start();
                return 0x00000c4;
            }
            writer.WriteEntry(new LogMessage(LogSeverity.Warning, "Program", "Termination."));

            if (CriticalError)
            {
                return 4007;//NOT OKAY
            }
            else
            {
                return 200;//OK status
            }
        }

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
            if(title.Length > 72)
            {
                title = title.Remove(71) + "...";
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
            if (title.Length > 72)
            {
                title = title.Remove(71) + "...";
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

        /// <summary>
        /// Log to console (Console UI Friendly)
        /// </summary>
        /// <param name="msg">The log message to write</param>
        /// <param name="showGT">Show '>' after output?</param>
        public static void LogToConsole(LogMessage msg, bool showGT=true)
        {
            if(!LOG_ONLY_MODE)
            {
                writer.WriteEntry(msg,ConsoleColor.Black,showGT);
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
        /// Log to console with custom entry color (Console UI Friendly)
        /// </summary>
        /// <param name="msg">The log message to write</param>
        /// <param name="entryColor">Custom color of log message</param>
        /// <param name="showGT">Show '>' after output?</param>
        public static void LogToConsole(LogMessage msg,ConsoleColor entryColor, bool showGT=true)
        {
            if (!LOG_ONLY_MODE)
            {
                writer.WriteEntry(msg,entryColor,showGT);
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
        /// Show 'CRASH' screen with custom title and call for app termination or restart.
        /// </summary>
        /// <param name="title">Title of killscreen</param>
        /// <param name="message">the point of the killscreen</param>
        /// <param name="autorestart">True: Prompt for auto restart in timeout period</param>
        /// <param name="timeout">auto restart timeout in seconds.</param>
        /// <param name="ex">The inner exception leading to the killscreen.</param>
        /// <returns></returns>
        public static Task ShowKillScreen(string title, string message, bool autorestart, int timeout = 5, Exception ex = null)
        {

            ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, title);
            LogToConsole(new LogMessage(LogSeverity.Critical, "MAIN", "The program encountered a problem, and was terminated. Details below."));
            LogMessage m = new LogMessage(LogSeverity.Critical, "CRITICAL", message);
            LogToConsole(m);
            using (FileStream fs = File.Create("CRASH.LOG"))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    
                    LogToConsole(new LogMessage(LogSeverity.Info, "MAIN", "writing error report to CRASH.LOG"));
                    sw.WriteLine(m.ToString());
                    sw.WriteLine("If you continue to get this error, please report it to the developer, including the stack below.");
                    sw.WriteLine();
                    sw.WriteLine("Developer STACK:");
                    sw.WriteLine("=================================================================================================================================");
                    sw.WriteLine(ex.ToString());
                    sw.Flush();
                }
            }
            using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    LogToConsole(new LogMessage(LogSeverity.Info, "MAIN", "Writing additional information to ERRORS.LOG"));
                    sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + ex.ToString());
                    sw.Flush();

                }
            }


            if (!autorestart)
            {
                LogToConsole(new LogMessage(LogSeverity.Info, "MAIN", "Press any key to terminate..."),true);
                Console.ReadKey();
                //CriticalError = true;
                discon = true;
                return Task.Delay(1);
            }
            else
            {
                //prompt for autorestart.
                for (int i = 0; i < timeout; i++)
                {
                    int l = Console.CursorLeft;
                    int t = Console.CursorTop;
                    
                    LogToConsole(new LogMessage(LogSeverity.Critical, "MAIN", $"Restarting in {timeout - i} second(s)..."),false);
                    
                    Console.CursorLeft = l;
                    Console.CursorTop = t;//reset.
                    Thread.Sleep(1000);
                }
                discon = true;
                RestartRequested = true;
                List<string> restart_args = new List<string>();
                restart_args.AddRange(ARGS);
                if (!restart_args.Contains("-crashed"))
                {
                    restart_args.Add("-crashed");
                }
                ARGS = restart_args.ToArray();
                //CriticalError = true;
                return Task.Delay(1);

            }

        }

        /// <summary>
        /// Initialize configuration file
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
                        WizardDebug = Program.MainCFG.GetCategoryByName("Application").GetEntryByName("Dev-ShowWizard").GetAsBool();
                        return WizardDebug;
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

        /// <summary>
        /// Process console commands
        /// </summary>
        private static void GetConsoleInput()
        {
            while (true)
            {
                if (Initialized) { break; }
            }
            ulong chID = 0;
            while (true)
            {
                if (discon)
                {
                    break;
                }
                if (!LOG_ONLY_MODE)
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write(">");//set cursor: 1,top & add black color (default)

                    Console.BackgroundColor = ConsoleBackgroundColor;
                    Console.ForegroundColor = ConsoleForegroundColor;
                }
                string input = Console.ReadLine();
                if (!LOG_ONLY_MODE)
                {
                    //Set another ptop
                    Console.SetCursorPosition(0, Console.CursorTop);

                }
                writer.WriteEntry(new LogMessage(LogSeverity.Info, "Console", input));
                if (discon)
                {
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        return;
                    }
                }
                if (input.ToLower() == "stopbot")
                {
                    writer.WriteEntry(new LogMessage(LogSeverity.Critical, "MAIN", "Console session called STOPBOT."));
                    writer.WriteEntry(new LogMessage(LogSeverity.Warning, "Session", "Ending session and closing program..."));
                    _client.SetGameAsync("");
                    System.Threading.Thread.Sleep(2000);
                    _client.SetStatusAsync(UserStatus.Invisible);
                    System.Threading.Thread.Sleep(2000);
                    Initialized = false;
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
                if (input.ToLower() == "rskill")
                {

                    ShowKillScreen("Test KS", "The program was instructed to run a test killscreen. This will auto restart the program.", true, 5, new ApplicationException("Command rskill triggered kill screen. USER INITIATED CRASH SCREEN."));
                    break;
                }
                if (input.ToLower() == "tskill")
                {
                    ShowKillScreen("Test KS", "The program was instructed to run a test killscreen. This will NOT auto restart the program.", false, 5, new ApplicationException("Command tskill triggered kill screen. USER INITIATED CRASH SCREEN."));
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
                    string status = input.Remove(0, 7).Trim();
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
                    input = input.Remove(0, varname.Length);
                    input = input.Trim();
                    ccmg.scriptService.Set(varname, input);
                }

            }
        }

        /// <summary>
        /// Run initial setup wizard
        /// </summary>
        private static void SetupWizard()
        {
            if (InitializeConfig())//If true, do setup.
            {
                #region Page 1
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Welcome");
                Console.WriteLine("\u2000Welcome to the initial setup wizard for your new modular discord bot...\r\n Some things to note before we get started:");
                Console.WriteLine("\u2000\t- This bot requires a token. If you don't know what that is," +
                    " please visit this site first!");
                Console.WriteLine("\u2000\t- https://discordapp.com/developers/docs/intro");

                Console.WriteLine("\u2000\t- Privacy notice: This application will output any message that mentions the bot, or messages that start with ! to the console.\r\n\t" +
                    " THEY DO NOT GET SAVED OR POSTED ANYWHERE ELSE... That would be creepy and probably illegal...");

                Console.WriteLine("\u2000\r\n\u2000Press ENTER to continue...");
                Console.ReadLine();
                #endregion

                #region Page 2
                ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Step 1: Token Configuration");

                Console.WriteLine("\u2000As mentioned before, this bot requires a token. It can be added in one of two ways.");
                Console.WriteLine("\u2000\t- METHOD 1: Program arguments. Simply launch the program from a batch file,\r\n\t- USAGE: RMSoftwareModularBot.exe <put token here>");
                Console.WriteLine("\u2000\t- METHOD 2: Configure it. Actually, let's do that now.");
                Console.WriteLine("\u2000Go ahead and find your token and copy/paste* it here, then press enter.");
                Console.WriteLine("\u2000*Only if your CMD console will let you... If not, that sucks... You can painfully type it in also...");
                Console.Write("\u2000> ");
                string conf_Token = Console.ReadLine();

                if (!WizardDebug)
                {
                    MainCFG.CreateCategory("Application");
                    MainCFG.CreateEntry("Application", "botToken", conf_Token);
                }

                Console.WriteLine("\u2000Okay! Please remember, You can also start the program with a token as an argument.");
                Console.WriteLine("\u2000Be advised, the token in the configuration is ignored when using the program parameter.");

                Console.WriteLine("\u2000\r\n\u2000Press ENTER to continue...");
                Console.ReadLine();
                #endregion

                #region Page 3
                ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Step 2: Control Channel & Command prefix");

                Console.WriteLine("\u2000Ah, yes... The Control channel...");
                Console.WriteLine("\u2000\t- Whenever you start your bot, it will execute AutoStart.bcmd, a script full of commands to prepare for that bot life...");
                Console.WriteLine("\u2000\t- However, in order to do this, the bot needs to know where to send these messages and commands...");
                Console.WriteLine("\u2000\t- Not doing this now would result in a crashing bot that doesn't know what to do with its life.");
                Console.WriteLine("\u2000\t- All I need is the ID of that channel to put into the configuration.");
                Console.WriteLine("\u2000\t- If you need a refresher on how to find a channel ID: https://rms0.org/?a=channels \r\n\t" +
                    "(It goes to the discord's official docs)");
                Console.WriteLine("\u2000copy/paste or painfully type in your Initialization Channel's id and press enter");
                Console.Write("\u2000> ");
                string conf_BotChannel = Console.ReadLine();
                if (!WizardDebug)
                {
                    Program.MainCFG.CreateEntry("Application", "botChannel", conf_BotChannel);
                }
                Console.WriteLine("\u2000Great! Now that channel will be the bot's main log channel...");
                Console.WriteLine("\u2000");
                Console.WriteLine("\u2000Now... The Command Prefix: Please enter a single character (Recommended: A symbol of some kind), to use as the bot's command prefix");
                Console.Write("\u2000> ");
                int conf_cmdPrefix = 0;
                CommandPrefix = (char)0;
                while (true)
                {
                    string r = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(r))
                    {
                        Console.WriteLine("\u2000This command prefix was invalid. The prefix cannot be empty, whitespace character, or otherwise. TRY AGAIN.");
                        Console.Write("\u2000> ");
                        continue;
                    }
                    else
                    {
                        conf_cmdPrefix = r[0];
                        CommandPrefix = (char)conf_cmdPrefix;
                        break;
                    }

                }

                if (!WizardDebug)
                {
                    Program.MainCFG.CreateEntry("Application", "cmdPrefix", conf_cmdPrefix);
                    Program.MainCFG.SaveConfiguration();//save
                }
                Console.WriteLine("\u2000Great! Your bot will use '" + Convert.ToChar(conf_cmdPrefix) + "' [without quotes] as a prefix for it's commands!");
                Console.WriteLine("\u2000\r\n\u2000Press ENTER to continue...");
                Console.ReadLine();

                #endregion

                #region Page 4
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Step 3: Final Notes & ProTips");
                Console.WriteLine("\u2000That is all the configuration for right now! Here are a few more things to know:");
                Console.WriteLine("\u2000\t- If you want to re-run this configuration wizard, delete the 'rmsftModBot.ini' file in the program directory.");
                Console.WriteLine("\u2000\t- The source code for this bot is available on http://rmsoftware.org");
                Console.WriteLine("\u2000\r\n\u2000CORE Command usage (in discord):");
                Console.WriteLine("\u2000\t- You will need to add command management roles to the bot, if you want other users to be able to add or remove commands\r\n\t  and interact with restricted commands.");
                Console.WriteLine("\u2000\t- Since you own the bot account that uses the token you provided, you are considered a bot owner. \r\n\t  This means you will automatically have access to all commands, regardless of the restrictions in place.");
                Console.WriteLine("\u2000\t- use {0}addmgrole [@roles] to add roles to the command user database.", CommandPrefix);
                Console.WriteLine("\u2000\t- usage: {0}addcmd <command name> <CmdMgmtOnly[true/false]> <LockToGuild[true/false]> <action>", CommandPrefix);
                Console.WriteLine("\u2000\t- Actions: Any text/emotes with optional formatting.");
                Console.WriteLine("\u2000\t- " + CommandPrefix + "addcmd hug false false You hug {{params}} for a long time");
                Console.WriteLine("\u2000\t- " + CommandPrefix + "addcmd grouphug false false You hug {{0}}, {{1}}, {{2}}, and {{3}} for a long time");
                Console.WriteLine("\u2000\t- More Action parameters: EXEC and CLI_EXEC ");
                Console.WriteLine("\u2000\t- " + CommandPrefix + "addcmd exectest falase false EXEC modname.dll ModNameSpace.ModClass StaticMethod {{params}}");
                Console.WriteLine("\u2000\t- " + CommandPrefix + "addcmd exectest falase false CLI_EXEC modname.dll ModNameSpace.ModClass StaticMethod {{params}}");
                Console.WriteLine("\u2000\t  - NOTE: splitparam is not supported for EXEC or CLI_EXEC");
                Console.WriteLine("\u2000\t  - NOTE: EXEC: Allows you to execute a class method for a more advanced command");
                Console.WriteLine("\u2000\t  - NOTE: CLI_EXEC is the same thing, but it gives the class access to the bot directly...");
                Console.WriteLine("\u2000Extra Configuration Options:");
                Console.WriteLine("\u2000\t  - Adding the line DisableCore=True to the [Application] section of the config file, will disable all core commands.");
                Console.WriteLine("\u2000\t     - Remember this will disable the ability to manage commands. putting the bot in a sort of 'read-only' state.");

                Console.WriteLine("\u2000Override core: ");
                Console.WriteLine("\u2000\t  - You can create custom commands that use the same name as core commands.\r\n\t   This is useful for overriding core commands for even more customization...");
                Console.WriteLine("\u2000\r\n\u2000Please visit https://rms0.org/?a=mbot for more information and documentation.");
                Console.WriteLine("\u2000\r\n\u2000Press ENTER to launch the bot!");
                Console.ReadLine();
                #endregion
            }
        }

        /// <summary>
        /// Threaded wait for MainAsync to return
        /// </summary>
        private static Func<bool> BotShutdown = delegate ()
        {
            return discon;
        };

        /// <summary>
        /// ConsoleGUIReset - Title decoration (TOP)
        /// </summary>
        private static void DecorateTop()
        {
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                if (i == 0)
                {
                    Console.Write("\u2554");
                    continue;
                }

                if (i == Console.WindowWidth - 1)
                {
                    Console.Write("\u2557");
                    break;
                }
                Console.Write("\u2550");
            }
        }

        /// <summary>
        /// ConsoleGUIReset - Title decoration (BOTTOM)
        /// </summary>
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
        
        #if (DEBUG)
        private static void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "FirstChance", e.Exception.ToString()));
        }
        #endif

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CriticalError = true;
            RestartRequested = true;
            crashException = (Exception)e.ExceptionObject;

            if (crashException == null)
            {
                crashException = new Exception("The bot crashed due to an unknown error...");
            }

            ShowKillScreen("Unhandled Error!", ((Exception)e.ExceptionObject).Message, true, 5, (Exception)e.ExceptionObject);
        }

        #endregion

        #region INSTANTIATED Methods
        /// <summary>
        /// Loads all Command modules located in CMDModules folder. *.DLL
        /// </summary>
        /// <returns></returns>
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
            ccmg = new CustomCommandManager(LOG_ONLY_MODE, CommandPrefix, writer, ref cmdsvr, ref services);
            //check cfg for disabled core.
            if (MainCFG.GetCategoryByName("Application").CheckForEntry("disableCore"))
            {
                if(MainCFG.GetCategoryByName("Application").GetEntryByName("disableCore").GetAsBool())
                {
                    LogToConsole(new LogMessage(LogSeverity.Warning,"Program", "RMSoftware.ModularBOT CORE was disabled in the configuration."));
                    LogToConsole(new LogMessage(LogSeverity.Warning,"Program", "You will not be able to manage existing commands, nor add new ones, via discord."));
                    return;//DO NOT LOAD CORE IF THIS IS TRUE.
                }
            }
            
            await cmdsvr.AddModulesAsync(Assembly.GetEntryAssembly());//ADD CORE.
        }

        /// <summary>
        /// ModularBOT's client thread
        /// </summary>
        /// <param name="token">The token required by Discord.NET API.</param>
        /// <returns></returns>
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
                _client.GuildAvailable += _client_GuildAvailable;
                _client.GuildUnavailable += _client_GuildUnavailable;

                await LoadModules();//ADD CORE AND EXTERNAL MODULES
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
                timeoutStart = true;
                if (!discon)
                {
                    //set timer 10 seconds
                    await Task.Run(delegate ()
                    {
                        while (timeoutStart)
                        {

                            Thread.Sleep(1000);
                            timeout++;
                            if (_client.ConnectionState == ConnectionState.Connected)
                            {
                                timeoutStart = false;
                                timeout = 0;
                                break;

                            }
                            if (timeout >= 10)
                            {

                                //Log(new LogMessage(LogSeverity.Critical, "ERR_504", "The client did not connect within 10 seconds. RESTART requested."));
                                ShowKillScreen("Connection Timeout", "The client did not connect within 10 seconds. Restarting", true, 5, new TimeoutException("The client did not connect within 10 seconds. Restarting"));
                                timeoutStart = false;
                                timeout = 0;
                                break;
                            }

                        }

                    });
                }
            }
            catch (Discord.Net.HttpException httex)
            {
                if (httex.HttpCode == System.Net.HttpStatusCode.Unauthorized)
                {

                    await ShowKillScreen("Unauthorized", "The server responded with error 401. Make sure your authorization token is correct.", false, 5, httex);
                }
                if(httex.DiscordCode == 4007)
                {
                    await ShowKillScreen("Invalid Client ID", "The server responded with error 4007.", true, 5, httex);
                }
                if (httex.DiscordCode == 5001)
                {
                    await ShowKillScreen("guild timed out", "The server responded with error 5001.", true, 5, httex);
                }

                else
                {
                    await ShowKillScreen("HTTP_EXCEPTION", "The server responded with an error. SEE Crash.LOG for more info.", true, 5, httex);
                }
            }

            catch (Exception ex)
            {
                await ShowKillScreen("Unexpected Error", ex.Message, true, 5, ex);
            }
        }

        private Task _client_GuildUnavailable(SocketGuild arg)
        {
            Console.Title = "RMSoftware.ModularBOT -> " + _client.CurrentUser.Username + " | Connected to " + _client.Guilds.Count + " guilds.";
            LogToConsole(new LogMessage(LogSeverity.Warning, "Guilds", $"A guild just vanished. [{arg.Name}] "));
            return Task.Delay(0);
        }

        private Task _client_GuildAvailable(SocketGuild arg)
        {
            Console.Title = "RMSoftware.ModularBOT -> " + _client.CurrentUser.Username + " | Connected to " + _client.Guilds.Count + " guilds.";
            LogToConsole(new LogMessage(LogSeverity.Info, "Guilds", $"A guild is available. <[{arg.Name}] Total users: {arg.Users.Count}> "));
            return Task.Delay(0);
        }

        private Task _client_Connected()
        {
            StartTime = DateTime.Now;
            LogToConsole(new LogMessage(LogSeverity.Info,"Uptime", "Reset uptime to ALL zero."));
            Console.Title = "RMSoftware.ModularBOT -> " + _client.CurrentUser.Username + " | Connected to " + _client.Guilds.Count + " guilds.";
            timeoutStart = false;
            timeout = 0;
            return Task.Delay(1);

        }
       
        private Task _client_Disconnected(Exception arg)
        {
            LogToConsole(new LogMessage(LogSeverity.Warning,"Session", "Disconnected: "+ arg.Message,arg));
            Console.Title = "RMSoftware.ModularBot - Disconnected";
            LogToConsole(new LogMessage(LogSeverity.Info,"Uptime", "The client is disconnected."));
            timeoutStart = true;
            if(!discon)
            {
                //set timer 10 seconds
                Task.Run(delegate () {
                    while (timeoutStart)
                    {

                        Thread.Sleep(1000);
                        timeout++;
                        if (_client.ConnectionState == ConnectionState.Connecting)
                        {
                            timeoutStart = false;
                            timeout = 0;
                            break;

                        }
                        if (timeout >= 10)
                        {

                            //Log(new LogMessage(LogSeverity.Critical, "ERR_504", "The client disconnected and did not attempt to reconnect within 10 seconds. RESTART requested."));
                            ShowKillScreen("Connection Timeout", "The client did not reconnect within 10 seconds. Restarting", true, 5, new TimeoutException("The client did not reconnect within 10 seconds. Restarting"));
                            timeoutStart = false;
                            timeout = 0;
                            break;
                        }
                        
                    }
                    
                });
            }
            return Task.Delay(3);
        }

        private async Task _ClientReady()
        {
            await Log(new LogMessage(LogSeverity.Info, "TaskMgr", "Running script: OnStart.Core"));
            await Task.Run(new Action(OffloadReady));
            Console.Title = "RMSoftware.ModularBOT -> " + _client.CurrentUser.Username + " | Connected to " + _client.Guilds.Count + " guilds.";
        }

        /// <summary>
        /// Initialization thread.
        /// </summary>
        private async void OffloadReady()
        {
            try
            {
                if (!Initialized)
                {
                    await _client.SetStatusAsync(UserStatus.DoNotDisturb);
                    ulong id = MainCFG.GetCategoryByName("Application").GetEntryByName("botChannel").GetAsUlong();
                    if (recovered)
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

                    IGuildChannel i = (IGuildChannel)_client.GetChannel(id);
                    if(i==null)
                    {
                        await ShowKillScreen("INVALID GUILD CHANNEL", "You specified an invalid guild channel ID. Please verify your guild channel's ID and try again.", false, 0, new ArgumentException("Guild channel was null.", "botChannel"));
                        return;
                    }

                    await ccmg.scriptService.EvaluateScriptFile("OnStart.core", ccmg.CmdDB, "OnStart.CORE", _client, new PsuedoMessage("", _client.CurrentUser, (IGuildChannel)_client.GetChannel(id), MessageSource.Bot));

                    Initialized = true;
                    //SET STATUS
                    string status = MainCFG.GetCategoryByName("application").CheckForEntry("readyStatus") ? MainCFG.GetCategoryByName("application").GetEntryByName("readyStatus").GetAsString() : "READY";
                    string orb = MainCFG.GetCategoryByName("application").CheckForEntry("readyOrb") ? MainCFG.GetCategoryByName("application").GetEntryByName("readyOrb").GetAsString() : "green";
                    switch (orb)
                    {
                        case ("red"):
                            await _client.SetStatusAsync(UserStatus.Online);
                            break;
                        case ("orange"):
                            await _client.SetStatusAsync(UserStatus.Idle);
                            break;
                        case ("green"):
                            await _client.SetStatusAsync(UserStatus.Online);
                            break;
                        default:
                            await _client.SetStatusAsync(UserStatus.Online);
                            break;
                    }
                    await _client.SetGameAsync(status);
                    LogToConsole(new LogMessage(LogSeverity.Info, "TaskMgr", "Task is complete."));
                    
                    LogToConsole(new LogMessage(LogSeverity.Info, "TaskMgr", "Processing Message Queue."));
                    foreach (var item in messageQueue)
                    {

                        await _client_MessageReceived(item);
                        await Task.Delay(500);
                    }
                    LogToConsole(new LogMessage(LogSeverity.Info, "TaskMgr", "Task is complete."));
                }
            }
            catch (Discord.Net.HttpException httx)
            {
                if(httx.DiscordCode == 50001)
                {
                    LogToConsole(new LogMessage(LogSeverity.Critical, "CRITICAL", "The bot was unable to perform needed operations. Please make sure it has the following permissions: Read messages, Read message history, Send Messages, Embed Links, Attach Files. (Calculated: 117760)", httx));
                }
            }
            catch (Exception ex)
            {
                Initialized = false;
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
            //mentions
            
            // Determine if the message is a command, based on if it starts with 'commandprefix' or a mention prefix. if not, ignore it.
            if (!(message.HasCharPrefix(CommandPrefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;

            
            if (!arg.Author.IsBot && !Initialized)
            {
                messageQueue.Add(arg);  //queue it up. The bcmdStarted check should 
                                        //provide PLENTY (if not an excessive amount) of time for modules,and extra commands 
                                        //to be fully loaded by the time it is set to true. Preemptively solving the "hey, you just ignored my commands completely" 
                                        //when the bot starts and doesn't respond to a command at first
                return;
            }
            
            
            // Create a Command Context for command modules
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
                    EmbedBuilder b = new EmbedBuilder();
                    b.WithColor(Color.Orange);
                    b.WithAuthor(_client.CurrentUser);
                    b.WithTitle("Command Error.");
                    b.WithDescription(result.ErrorReason);
                    b.AddField("Error Code", result.Error.Value);
                    await context.Channel.SendMessageAsync("", false, b.Build());
                }
                if (result.Error.Value.HasFlag(CommandError.BadArgCount))
                {
                    EmbedBuilder b = new EmbedBuilder();
                    b.WithColor(Color.Orange);
                    b.WithAuthor(_client.CurrentUser);
                    b.WithTitle("Command Error.");
                    b.WithDescription(result.ErrorReason);
                    b.AddField("Error Code", result.Error.Value);
                    await context.Channel.SendMessageAsync("", false, b.Build());
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
            if (msg.Source == "ERR_504")
            {
                _client.StopAsync();
                _client = null;
                crashException = msg.Exception;
                CriticalError = true;
                discon = true;
                Thread.Sleep(3000);
            }
            if (msg.Source == "ERR_401")
            {
                _client.StopAsync();
                _client = null;
                crashException = msg.Exception;
                CriticalError = true;
                discon = true;
                Thread.Sleep(3000);
            }
            if (msg.Exception != null)
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

        #region Console pixel art - Slight modification of https://stackoverflow.com/a/33715138/4655190
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
    }

    #region Retry clause - API error workaround [possibly obsolete] [https://stackoverflow.com/a/1563234/4655190]
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
