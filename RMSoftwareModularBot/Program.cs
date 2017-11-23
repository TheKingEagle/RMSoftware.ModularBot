using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System.Net.Sockets;
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
        public static DateTime StartTime;
        public static DiscordSocketClient _client;
        private IServiceProvider services;
        public static bool BCMDStarted = false;
        public static CustomCommandManager ccmg { get; private set; }
        public static Discord.Commands.CommandService cmdsvr = new Discord.Commands.CommandService();
        public static CancellationToken t;
        public static bool WizardDebug = false;
        public static bool discon = false;
        public static bool InvalidSession = false;
        public static bool RestartRequested = false;
        static List<SocketMessage> messageQueue = new List<SocketMessage>();
        /// <summary>
        /// Application's main configuration file
        /// </summary>
        public static INIFile MainCFG { get; private set;}
        /// <summary>
        /// Program EntryPoint
        /// </summary>
        /// <param name="args">The only argument that should be in here should be your discord application token</param>
        /// <returns></returns>
        public static int Main(string[] args)
        {
            BCMDStarted = false;
            ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome",79,45);
            Console.Title = "RMSoftware ModularBot";
            Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
            System.Threading.Thread.Sleep(800);
            ConsoleWriteImage(Prog.res1.Resource1.RMSoftwareICO);
            System.Threading.Thread.Sleep(3000);
            ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Program Starting");
            ccmg = new CustomCommandManager();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            string[] NonConfigArgs = args;
            if (InitializeConfig())//If true, do setup.
            {
                #region Page 1
                ConsoleGUIReset(ConsoleColor.Cyan,ConsoleColor.Black,"Welcome");

                Console.WriteLine("Welcome to the initial setup wizard for your new modular discord bot...\r\n Some things to note before we get started:");
                Console.WriteLine("\t- This bot requires a token. If you don't know what that is,"+
                    " please visit this site first!");
                Console.WriteLine("\t- https://discordapp.com/developers/docs/intro");

                Console.WriteLine("\t- Privacy notice: This application will output any message that mentions the bot, or messages that start with ! to the console.\r\n\t"+
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
                if(!WizardDebug)
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
                ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Step 2: Master-Guild commands");

                Console.WriteLine("Ah, yes... Master-guild commands.... These commands will only work in the set guild.");
                Console.WriteLine("\t- Look it as like a main 'guild' or discord server, your bot will start in...");
                Console.WriteLine("\t- All we need is the ID of that guild/server to put into the configuration.");
                Console.WriteLine("\t- If you need a refresher on how to find a guild ID: https://goo.gl/nqAbhw \r\n\t"+
                    "(It goes to the discord's official docs)");
                Console.WriteLine("copy/paste or painfully type in your 'master guild' id and press enter");
                Console.Write("> ");
                string conf_MasterGuild = Console.ReadLine();
                //TODO: UNCOMMENT THIS WHEN READY
                if(!WizardDebug)
                {
                    MainCFG.CreateEntry("Application", "masterGuild", conf_MasterGuild);
                }
                
                Console.WriteLine("Cool! Now that guild id will be the only one you can use to access Master-guild Commands...");
                Console.WriteLine();
                Console.WriteLine("Okay, Now you will set up your master-guild's dedicated bot channel.");
                Console.WriteLine("This will be where the bot will post messages and commands from OnStart.bcmd\r\nIt is best to use a channel from your master guild,\r\n"+
                    "but any channel ID will work, so long as your bot has access to it");
                Console.WriteLine("Please copy/paste or painfully type in your 'dedicated bot' channel id and press enter");
                Console.Write("> ");
                string conf_BotChannel = Console.ReadLine();
                if(!WizardDebug)
                {
                    Program.MainCFG.CreateEntry("Application", "botChannel", conf_BotChannel);
                    Program.MainCFG.SaveConfiguration();//save
                }
                Console.WriteLine("Great! Now that channel will be the bot's main log or channel.");
                Console.WriteLine("Be advised, if you want to change this (or any other settings),\r\nyou will have to manually edit the config file.");
                Console.WriteLine("\r\nPress ENTER to continue...");
                Console.ReadLine();

                #endregion

                #region Page 4
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Step 3: Final Notes & ProTips");
                Console.WriteLine("That is all the configuration for right now! Here are a few more things to know:");
                Console.WriteLine("\t- If you want to re-run this configuration wizard, delete the 'rmsftModBot.ini' file in the program directory.");
                Console.WriteLine("\t- The source code for this bot is available on http://rmsoftware.org");
                Console.WriteLine("\r\nCORE Command usage (in discord):");
                Console.WriteLine("\t- Create three roles in your server. DevCommand & BotMaster, and a custom role (Optional) to give to your bot.");
                Console.WriteLine("\t- (OPTIONAL) You could give the custom role administrative rights, just so you ensure everything works for your bot.");
                Console.WriteLine("\t- usage: !addcmd <command name> <DevCommandOnly[true/false]> <LockToGuild[true/false]> <action>");
                Console.WriteLine("\t- Actions: Any text/emotes with optional formatting.");
                Console.WriteLine("\t- !addcmd sample1SplitParam false false splitparam 3|"+
                    " This is a sample of splitparam. Var1: {0} var2: {1} and var3: {2} all walked into a bar");
                Console.WriteLine("\t- !addcmd hug false false You hug {params} for a long time");
                Console.WriteLine("\t- More Action parameters: EXEC and CLI_EXEC ");
                Console.WriteLine("\t- !addcmd exectest falase false EXEC modname.dll ModNameSpace.ModClass StaticMethod {params}");
                Console.WriteLine("\t- !addcmd exectest falase false CLI_EXEC modname.dll ModNameSpace.ModClass StaticMethod {params}");
                Console.WriteLine("\t  - NOTE: splitparam is not supported for EXEC or CLI_EXEC");
                Console.WriteLine("\t  - NOTE: EXEC: Allows you to execute a class method for a more advanced command");
                Console.WriteLine("\t  - NOTE: CLI_EXEC is the same thing, but it gives the class access to the bot directly...");
                Console.WriteLine("\r\nPlease visit http://rmsoftware.org/rmsoftwareModularBot for more information and documentation.");
                Console.WriteLine("\r\nPress ENTER to launch the bot!");
                Console.ReadLine();
                #endregion
            }
            if (NonConfigArgs.Length == 0)
            {
                ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, "Initializing");
                Console.WriteLine("You didn't specify an authorization token as parameter....");
                Console.WriteLine("usage: RMSoftwareModularBot.exe <authToken>");
                Console.WriteLine("No big deal though, Will use configuration instead");
                NonConfigArgs = new string[] { Program.MainCFG.GetCategoryByName("Application").GetEntryByName("botToken").GetAsString()};
            }

            
            new Program().MainAsync(NonConfigArgs[0]).GetAwaiter().GetResult();
            
            SpinWait.SpinUntil(BotShutdown);//Wait until shutdown;
            BCMDStarted = false;
            ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, "Disconnected");
            if(InvalidSession)
            {
                LogToConsole("Session", "Failed to resume previous session. Please restart the application");
                
                _client = null;
            }
            if(RestartRequested)
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
            if(!InvalidSession)
            {

                return 4007;//NOT OKAY
            }
            else
            {

                return 200;//OK status
            }
        }


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
        private static void ConsoleGUIReset(ConsoleColor fore, ConsoleColor back, string title)
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

        private static void ConsoleGUIReset(ConsoleColor fore, ConsoleColor back, string title,short w, short h)
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

        public static async Task LoadModules()
        {
            cmdsvr = new CommandService();//clear and re-add
            await cmdsvr.AddModulesAsync(Assembly.GetEntryAssembly());//ADD CORE.
            foreach (string item in Directory.EnumerateFiles("CMDModules","*.dll",SearchOption.TopDirectoryOnly))
            {
                LogToConsole("Modules", "Adding commands from module library: " + item);
                try
                {
                    await cmdsvr.AddModulesAsync(Assembly.LoadFile(Path.GetFullPath(item)));//ADD EXTERNAL.
                }
                catch (Exception ex)
                {

                    LogToConsole("CritERR", ex.Message);
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    LogToConsole("ExStack\r\n\r\n", ex.StackTrace);
                    Console.ForegroundColor = Last;
                }
            }
        }

        public async Task MainAsync(string token)
        {
            ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, "Application Running");
            t = new CancellationToken(false);
            _client = new DiscordSocketClient();
            _client.Log += Log;
            services = new ServiceCollection().BuildServiceProvider();
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
            return Task.Delay(1);
        }

        private Task _client_Disconnected(Exception arg)
        {
            LogToConsole("Session", "Disconnected: "+ arg.Message);
            return Task.Delay(3);
        }

        private async Task _ClientReady()
        {
            await Log(new LogMessage(LogSeverity.Info, "Taskman", "Running a task"));
            await Task.Run(new Action(OffloadReady));
          
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
                                    await ch.SendMessageAsync(line);
                                    await Task.Delay(1500);
                                }
                            }
                        }
                    }
                    BCMDStarted = true;
                    await _client.SetGameAsync("READY!");
                    await _client.SetStatusAsync(UserStatus.Online);
                    foreach (var item in messageQueue)
                    {
                        await _client_MessageReceived(item);
                        await Task.Delay(500);
                    }
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
        public static void LogToConsole(string category,string logText)
        {
            Console.WriteLine("{0} {1}{2}", DateTime.Now.ToString("HH:mm:ss"), category.PadRight(12), logText);
        }

        private async Task _client_MessageReceived(SocketMessage arg)
        {
            
            //DEBUG: Output the bot-mentioned chat message to the console
            foreach (SocketUser item in arg.MentionedUsers)
            {
                if(item.Mention == _client.CurrentUser.Mention)
                {
                    LogToConsole("Mention", "<[" + arg.Channel.Name + "] " + arg.Author.Username + " >: " + arg.Content);
                }
            }
            //DEBUG: output ! prefixed messages to console.
            if (arg.Content.StartsWith("!"))
            {
                LogToConsole("Command", "<[" + arg.Channel.Name + "] " + arg.Author.Username + " >: " + arg.Content);

            }
            
            // Don't process the command if it was a System Message
            var message = arg as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix. if not, ignore it.
            if(!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;
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
            ccmg.Process(arg);
            var context = new CommandContext(_client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await cmdsvr.ExecuteAsync(context, argPos, services);
            //If the result is unsuccessful AND not unknown, send the error.
            if(result.Error.HasValue)
            {
                if (!result.Error.Value.HasFlag(CommandError.UnknownCommand)&& !result.IsSuccess)
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
            if(msg.Message == "Failed to resume previous session")
            {
                _client.StopAsync();
                InvalidSession = true;
                discon = true;
            }
            return Task.Delay(0);
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
                if(Program.MainCFG.CheckForCategory("Application"))
                {
                    if(Program.MainCFG.GetCategoryByName("Application").CheckForEntry("Dev-ShowWizard"))
                    {
                        WizardDebug = true;
                        return Program.MainCFG.GetCategoryByName("Application").GetEntryByName("Dev-ShowWizard").GetAsBool();
                    }
                }
            }
            return MainCFG.GeneratedNewFile;
            
        }

//=============STACKOVERFLOW THANK YOU!==================
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
    }

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


}
