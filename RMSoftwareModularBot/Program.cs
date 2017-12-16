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
namespace RMSoftware.ModularBot
{
    class Program
    {
        #region STATIC fields & properties
        public static DateTime StartTime;
        public static bool BCMDStarted = false;
        public static bool WizardDebug = false;
        public static bool discon = false;
        public static bool InvalidSession = false;
        public static bool RestartRequested = false;
        public static CustomCommandManager ccmg { get; private set; }//TODO: Inject into service provider.
        public static CmdRoleManager rolemgt { get; private set; }//TODO: Inject into service provider.
        public static char CommandPrefix { get; private set; }//TODO: Inject into service provider.
        public static DiscordSocketClient _client;
        static List<SocketMessage> messageQueue = new List<SocketMessage>();
        
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
            BCMDStarted = false;
            ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome", 79, 45);
            Console.Title = "RMSoftware.ModularBOT";
            Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
            System.Threading.Thread.Sleep(800);
            ConsoleWriteImage(Prog.res1.Resource1.RMSoftwareICO);
            System.Threading.Thread.Sleep(3000);
            ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Program Starting");
            ccmg = new CustomCommandManager();
            rolemgt = new CmdRoleManager();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            #if DEBUG
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            #endif
            string[] NonConfigArgs = args;

            SetupWizard();

            if (NonConfigArgs.Length == 0)
            {
                ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, "Initializing");
                Console.WriteLine("You didn't specify an authorization token as parameter....");
                Console.WriteLine("usage: RMSoftwareModularBot.exe <authToken>");
                Console.WriteLine("No big deal though, Will use configuration instead");
                NonConfigArgs = new string[] { Program.MainCFG.GetCategoryByName("Application").GetEntryByName("botToken").GetAsString() };
            }

            //set command prefix

            CommandPrefix = Convert.ToChar(MainCFG.GetCategoryByName("Application").GetEntryByName("cmdPrefix").GetAsInteger());
            LogToConsole("Initialize", "Using command prefix: " + CommandPrefix.ToString());
            new Program().MainAsync(NonConfigArgs[0]).GetAwaiter().GetResult();

            SpinWait.SpinUntil(BotShutdown);//Wait until shutdown;
            BCMDStarted = false;
            ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, "Disconnected");
            if (InvalidSession)
            {
                LogToConsole("Session", "Failed to resume previous session. Please restart the application");

                _client = null;
            }
            if (RestartRequested)
            {
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName);
                ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, "Disconnected");
                LogToConsole("Program", "Restarting the bot... this console window will close in 3 seconds.");
                Thread.Sleep(1000);
                ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, "Disconnected");
                LogToConsole("Program", "Restarting the bot... this console window will close in 2 seconds.");
                Thread.Sleep(1000);
                ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, "Disconnected");
                LogToConsole("Program", "Restarting the bot... this console window will close in 1 second.");
                Thread.Sleep(1000);
                p.Start();
                return 0x00000c4;
            }
            LogToConsole("Program", "Press any key to terminate...");
            Console.ReadKey();
            if (!InvalidSession)
            {

                return 4007;//NOT OKAY
            }
            else
            {

                return 200;//OK status
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
                //TODO: Uncomment when needed.
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
                Console.WriteLine("Finally, the Command Prefix: Please enter a single character (Recommended: A symbol of some kind), to use as the bot's command prefix");
                Console.Write("> ");
                int conf_cmdPrefix = Console.Read();
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
                Console.WriteLine("\t- use <prefix>addmgrole [@roles] to add roles to the command user database.");
                Console.WriteLine("\t- usage: <prefix>addcmd <command name> <CmdMgmtOnly[true/false]> <LockToGuild[true/false]> <action>");
                Console.WriteLine("\t- Actions: Any text/emotes with optional formatting.");
                Console.WriteLine("\t- <prefix>addcmd sample1SplitParam false false splitparam 3|" +
                    " This is a sample of splitparam. Var1: {0} var2: {1} and var3: {2} all walked into a bar");
                Console.WriteLine("\t- <prefix>addcmd hug false false You hug {params} for a long time");
                Console.WriteLine("\t- More Action parameters: EXEC and CLI_EXEC ");
                Console.WriteLine("\t- <prefix>addcmd exectest falase false EXEC modname.dll ModNameSpace.ModClass StaticMethod {params}");
                Console.WriteLine("\t- <prefix>addcmd exectest falase false CLI_EXEC modname.dll ModNameSpace.ModClass StaticMethod {params}");
                Console.WriteLine("\t  - NOTE: splitparam is not supported for EXEC or CLI_EXEC");
                Console.WriteLine("\t  - NOTE: EXEC: Allows you to execute a class method for a more advanced command");
                Console.WriteLine("\t  - NOTE: CLI_EXEC is the same thing, but it gives the class access to the bot directly...");
                Console.WriteLine("\r\nPlease visit http://rmsoftware.org/rmsoftwareModularBot for more information and documentation.");
                Console.WriteLine("\r\nPress ENTER to launch the bot!");
                Console.ReadLine();
                #endregion
            }
        }

        #if DEBUG
        private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            
            LogToConsole("AppException", e.Exception.ToString());
            
        }
        #endif

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            LogToConsole("CritERR", ex.Message);
            ConsoleColor Last = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            LogToConsole("ExStack\r\n\r\n", ex.StackTrace);
            Console.ForegroundColor = Last;
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
            Console.Clear();
            Console.SetWindowSize(144, 32);
            Console.SetBufferSize(144, 256);
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
            Console.Clear();
            
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            DecorateTop();
            string WTitle = (""+DateTime.Now.ToString("HH:mm:ss") + " " + title + " - RMSoftwareModularBot v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            string pTitle = WTitle.PadLeft(71+WTitle.Length/2);
            pTitle += "".PadRight(71-WTitle.Length/2);
            Console.Write("\u2551{0}\u2551", pTitle);
            DecorateBottom();
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
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
            Console.Clear();
            Console.SetWindowSize(w, h);
            Console.SetBufferSize(w, h);
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
            Console.Clear();

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            DecorateTop();
            string WTitle = ("" + DateTime.Now.ToString("HH:mm:ss") + " " + title + " - RMSoftwareModularBot v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            string pTitle = WTitle.PadLeft(((w/2)+2) + WTitle.Length / 2);
            pTitle += "".PadRight(((w / 2)-3) - WTitle.Length / 2);
            Console.Write("\u2551{0}\u2551", pTitle);


            DecorateBottom();
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
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

        public static void LogToConsole(string category, string logText)
        {
            LogMessage l = new LogMessage(LogSeverity.Info, category, logText);
            Console.WriteLine(l);
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
                }
            }
            return MainCFG.GeneratedNewFile;

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
            serviceCollection = serviceCollection.AddSingleton(_client);
            serviceCollection = serviceCollection.AddSingleton(cmdsvr);

            foreach (string item in Directory.EnumerateFiles("CMDModules","*.dll",SearchOption.TopDirectoryOnly))
            {
                LogToConsole("Modules", $"Adding commands from module library: {item}");
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
                            LogToConsole("Modules", $"Injecting service: {typename} from {asmb.GetName().Name}");
                            serviceCollection = serviceCollection.AddSingleton(asmb.GetType(typename),asmb.CreateInstance(typename));
                        }
                    }
                    await cmdsvr.AddModulesAsync(asmb);//ADD EXTERNAL.
                }
                catch (Exception ex)
                {
                    LogToConsole("CritERR", ex.Message);
                }
            }
            services = serviceCollection.BuildServiceProvider();
            await cmdsvr.AddModulesAsync(Assembly.GetEntryAssembly());//ADD CORE.
        }

        public async Task MainAsync(string token)
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

        private Task _client_Connected()
        {
            StartTime = DateTime.Now;
            LogToConsole("Uptime", "The client is connected. reset uptime.");
            return Task.Delay(1);
        }

        private Task _client_Disconnected(Exception arg)
        {
            LogToConsole("Session", "Disconnected: "+ arg.Message);
            Console.Title = "RMSoftware.ModularBot - Disconnected";
            LogToConsole("Uptime", "The client is disconnected.");
            return Task.Delay(3);
        }

        private async Task _ClientReady()
        {

            LogToConsole("Uptime", "The client is ready for initialization.");
            await Log(new LogMessage(LogSeverity.Info, "Taskman", "Running Onstart.bcmd task"));
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
                    //PROCESS THE AutoEXEC file
                    ulong id = MainCFG.GetCategoryByName("Application").GetEntryByName("botChannel").GetAsUlong();
                    using (StreamReader fs = File.OpenText("OnStart.bcmd"))
                    {
                        while (!fs.EndOfStream)
                        {
                            string line = await fs.ReadLineAsync();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                if (!line.StartsWith(@"\\"))
                                {
                                    ISocketMessageChannel ch = _client.GetChannel(id) as ISocketMessageChannel;
                                    line = line.Replace("%p_", CommandPrefix.ToString());
                                    if (!line.StartsWith(CommandPrefix.ToString()))
                                    {

                                        await ch.SendMessageAsync(line);
                                    }
                                    else
                                    {


                                        if (await ccmg.Process(new PsuedoMessage(line, _client.CurrentUser, ch, MessageSource.Bot)))
                                        {
                                            LogToConsole("OnStart", line);
                                            LogToConsole("OnStart", "CustomCMD Success");
                                            continue;
                                        }
                                        var context = new CommandContext(_client, new PsuedoMessage(line,_client.CurrentUser,ch,MessageSource.User));
                                        // Execute the command. (result does not indicate a return value, 
                                        // rather an object stating if the command executed successfully)
                                        var result = await cmdsvr.ExecuteAsync(context, 1, services);
                                        LogToConsole("OnStart", line);
                                        LogToConsole("OnStart", result.ToString());
                                    }
                                    await Task.Delay(1500);
                                }
                            }
                        }
                    }
                    BCMDStarted = true;
                    await _client.SetGameAsync("READY!");
                    LogToConsole("Taskman", "Task is complete.");
                    await _client.SetStatusAsync(UserStatus.Online);
                    LogToConsole("Taskman", "Processing message queue");
                    foreach (var item in messageQueue)
                    {
                        
                        await _client_MessageReceived(item);
                        await Task.Delay(500);
                    }
                    LogToConsole("Taskman","The queue has been processed. READY!");
                }
            }
            catch (Exception ex)
            {
                BCMDStarted = false;
                LogToConsole("CritERR", ex.Message);
                ConsoleColor Last = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Gray;
                LogToConsole("ExStack\r\n\r\n", ex.StackTrace);
                Console.ForegroundColor = Last;

            }
        }

        private async Task _client_MessageReceived(SocketMessage arg)
        {

            #region Console Output debug
            //DEBUG: Output the bot-mentioned chat message to the console
            foreach (SocketUser item in arg.MentionedUsers)
            {
                if (item.Mention == _client.CurrentUser.Mention)
                {
                    LogToConsole("Mention", "<[" + arg.Channel.Name + "] " + arg.Author.Username + " >: " + arg.Content);
                }
            }
            //DEBUG: output ! prefixed messages to console.
            if (arg.Content.StartsWith(CommandPrefix.ToString()))
            {
                LogToConsole("Command", "<[" + arg.Channel.Name + "] " + arg.Author.Username + " >: " + arg.Content);

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
                                        //to be fully loaded by the time it is set to true. Preemptively solving the "hey can you hear me" 
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
            Console.WriteLine(msg.ToString());
            if (msg.Message == "Failed to resume previous session")
            {
                _client.StopAsync();
                InvalidSession = true;
                discon = true;
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
                    Program.LogToConsole("CmdWARN", ex.Message);
                    Program.LogToConsole("CmdINFO", "Retrying " + (maxAttemptCount-attempted) + " more time(s)");
                }
            }
            throw new AggregateException(exceptions);
        }
    }

    #endregion

    #region Advanced Command Processing
    public class CmdRoleManager
    {
        INIFile mgmt;
        public CmdRoleManager()
        {
            mgmt = new INIFile("cmdMgr.ini");
        }
        public void AddCommandManagerRole(SocketRole role)
        {
            string guildCat = role.Guild.Id.ToString();
            bool check = mgmt.CheckForCategory(guildCat);
            int indx = -1;
            if(check)
            {
                indx = mgmt.GetCategoryByName(guildCat).Entries.Count-1;
                mgmt.CreateEntry(guildCat, "role" + (indx + 1), role.Id);
            }
            else
            {
                mgmt.CreateCategory(guildCat);
                indx = mgmt.GetCategoryByName(guildCat).Entries.Count - 1;
                mgmt.CreateEntry(guildCat, "role" + (indx + 1), role.Id);
            }
            mgmt.SaveConfiguration();
        }
        public string DeleteCommandManager(SocketRole role)
        {
            string guildCat = role.Guild.Id.ToString();
            bool check = mgmt.CheckForCategory(guildCat);
            int indx = -1;
            if (check)
            {
                indx = mgmt.GetCategoryByName(guildCat).Entries.Count - 1;
                try
                {
                    mgmt.DeleteEntry(guildCat, mgmt.GetCategoryByName(guildCat).Entries.Find(x => x.GetAsUlong() == role.Id).Name);
                }
                catch (Exception ex)
                {

                    return ex.Message;
                }
                
            }
            else
            {
                return "No results. The guild ID didn't exist in the database.";
            }
            mgmt.SaveConfiguration();
            return "Command Manager database updated.";
        }
        public bool CheckUserRole(SocketGuildUser user)
        {
            string guildcat = user.Guild.Id.ToString();//if the category does not exist, return false... can't have that;
            if(!mgmt.CheckForCategory(guildcat))
            {
                return false;
            }
            foreach (var role in user.Roles)
            {
                ulong id = role.Id;
                if (mgmt.GetCategoryByName(guildcat).Entries.Exists(x => x.GetAsUlong() == id))
                {
                    return true;//keep doing it until it returns true.
                }
            }
            return false;//default;
        }
    }
    public class PsuedoMessage : IMessage, IUserMessage
    {
        string _content = "";
        SocketUser _author;
        ISocketMessageChannel _c;
        MessageSource _source;
        public PsuedoMessage(string content,SocketUser author,ISocketMessageChannel ch ,MessageSource source)
        {
            _content = content;
            _author = author;
            _c = ch;
            _source = source;
        }


        IReadOnlyCollection<IAttachment> IMessage.Attachments
        {
            get;
        }

        IUser IMessage.Author
        {
            get { return _author; }
        }

        IMessageChannel IMessage.Channel
        {
            get { return _c; }
        }

        string IMessage.Content
        {
            get { return _content; }
        }

        DateTimeOffset ISnowflakeEntity.CreatedAt
        {
            get;
        }

        DateTimeOffset? IMessage.EditedTimestamp
        {
            get;
        }

        IReadOnlyCollection<IEmbed> IMessage.Embeds
        {
            get;
        }

        ulong IEntity<ulong>.Id
        {
            get { return (ulong)new Random().Next(0,int.MaxValue); }
        }

        bool IMessage.IsPinned
        {
            get { return false; }
        }

        bool IMessage.IsTTS
        {
            get { return false; }
        }

        IReadOnlyCollection<ulong> IMessage.MentionedChannelIds
        {
            get;
        }

        IReadOnlyCollection<ulong> IMessage.MentionedRoleIds
        {
            get;
        }

        IReadOnlyCollection<ulong> IMessage.MentionedUserIds
        {
            get;
        }

        MessageSource IMessage.Source
        {
            get { return _source; }
        }

        IReadOnlyCollection<ITag> IMessage.Tags
        {
            get;
        }

        DateTimeOffset IMessage.Timestamp
        {
            get;
        }

        MessageType IMessage.Type
        {
            get;
        }

        public IReadOnlyDictionary<IEmote, ReactionMetadata> Reactions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Task IDeletable.DeleteAsync(RequestOptions options)
        {
            return Task.Delay(0);
        }

        public Task ModifyAsync(Action<MessageProperties> func, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task PinAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task UnpinAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task AddReactionAsync(IEmote emote, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task RemoveReactionAsync(IEmote emote, IUser user, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAllReactionsAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IUser>> GetReactionUsersAsync(string emoji, int limit = 100, ulong? afterUserId = default(ulong?), RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public string Resolve(TagHandling userHandling = TagHandling.Name, TagHandling channelHandling = TagHandling.Name, TagHandling roleHandling = TagHandling.Name, TagHandling everyoneHandling = TagHandling.Ignore, TagHandling emojiHandling = TagHandling.Name)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
