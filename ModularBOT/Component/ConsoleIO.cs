using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using System.IO;
using Discord.WebSocket;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;

namespace ModularBOT.Component
{
    //public static class ConsoleIOWait
    //{
    //    public static bool Writing { get; set; }
    //}
    public class ConsoleIO
    {
        public bool Writing { get; private set; }

        public bool QueueProcessStarted { get; private set; } = false;

        /// <summary>
        /// TRUE if a console command resets the screen temporarily.
        /// </summary>
        public bool ScreenBusy { get; private set; }//If console is resetting or rendering new ui.

        public bool ScreenModal { get; private set; }//If there is a screen showing above discord logs

        List<LogEntry> LogEntries { get; set; } = new List<LogEntry>();

        public static Queue<LogEntry> Backlog { get; private set; } = new Queue<LogEntry>();
        public int CurTop { get; private set; }
        public int PrvTop { get; private set; }

        private string currentTitle = "";
        private ConsoleColor ConsoleForegroundColor = ConsoleColor.Gray;
        private ConsoleColor ConsoleBackgroundColor = ConsoleColor.Black;

        public ConsoleIO()
        {
            Writing = false;
        }

        public void ProcessQueue()
        {

            if(QueueProcessStarted)
            {
                WriteEntry(new LogMessage(LogSeverity.Critical, "ConsoleIO", 
                    "An attempt was made to start the queue processor task, but it is already started..."));

                return;
            }
            QueueProcessStarted = true;
            WriteEntry(new LogMessage(LogSeverity.Info, "ConsoleIO", 
                "Console Queue has initialized. Processing any incoming log events."));
            
            while (true)
            {
                SpinWait.SpinUntil(() => Backlog.Count > 0);        //will this solve the random deadlock? who knows.
                if (ScreenBusy) { continue; }
                LogEntry qitem = Backlog.Dequeue();                 //take a new entry out of queue.

                LogMessage message = qitem.LogMessage;              //Entry's log message data.
                ConsoleColor? Entrycolor = qitem.EntryColor;        //left margin color

                bool bypassFilter = qitem.BypassFilter;             //will this entry obey application log level?
                bool bypassScreenLock = qitem.BypassScreenLock;     //will this entry show up through a modal screen?
                bool showCursor = qitem.ShowCursor;                 //will this entry output and show the console cursor?

                if (message.Severity > Program.configMGR
                    .CurrentConfig
                    .DiscordEventLogLevel && !bypassFilter)
                {
                    continue;                                       //Do not output
                }

                LogEntries.Add(new LogEntry(message, Entrycolor));  //Add the entry to buffer. Ignore screen modal, for outputting when modal is closed.

                if(LogEntries.Count > Console.BufferHeight - 3)
                {
                    LogEntries.Remove(LogEntries.First());          //keep the buffer tidy.
                }

                if (ScreenModal && !bypassScreenLock)
                {
                    continue;                                       //Do not output
                }

                SpinWait.SpinUntil(() => !Writing);                 //This will help prevent the console from being sent into a mess of garbled words. 
                                                                    //(In theory)
                Writing = true;
                PrvTop = Console.CursorTop;
                Console.SetCursorPosition(0, Console.CursorTop);    //Reset line position.
                LogMessage l = new LogMessage(message.Severity, 
                    message.Source.PadRight(11, '\u2000'), 
                    message.Message, message.Exception);
                string[] lines = WordWrap(l.ToString()).Split('\n');
                ConsoleColor bglast = ConsoleBackgroundColor;
                int prt = Console.CursorTop;
                for (int i = 0; i < lines.Length; i++)
                {

                    if (lines[i].Length == 0)
                    {
                        continue;
                    }
                    ConsoleColor bg = ConsoleColor.Black;
                    ConsoleColor fg = ConsoleColor.Black;

                    #region setup entry color.
                    if (!Entrycolor.HasValue)
                    {
                        switch (message.Severity)
                        {
                            case LogSeverity.Critical:
                                bg = ConsoleColor.Red;
                                fg = ConsoleColor.Red;
                                break;
                            case LogSeverity.Error:
                                fg = ConsoleColor.DarkRed;
                                bg = ConsoleColor.DarkRed;
                                break;
                            case LogSeverity.Warning:
                                fg = ConsoleColor.Yellow;
                                bg = ConsoleColor.Yellow;
                                break;
                            case LogSeverity.Info:
                                fg = ConsoleColor.Black;
                                bg = ConsoleColor.Black;
                                break;
                            case LogSeverity.Verbose:
                                fg = ConsoleColor.Magenta;
                                bg = ConsoleColor.Cyan;
                                break;
                            case LogSeverity.Debug:
                                fg = ConsoleColor.DarkGray;
                                bg = ConsoleColor.DarkGray;
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        bg = Entrycolor.Value;
                        fg = Entrycolor.Value;
                    }
                    #endregion

                    Console.BackgroundColor = bg;
                    Console.ForegroundColor = fg;
                    Console.Write((char)9617);                          //Write the colored space.
                    Console.BackgroundColor = bglast;                   //restore previous color.
                    Console.ForegroundColor = ConsoleForegroundColor;   //previous FG.
                    Console.Write("\u2551");                            //uileft ║
                    
                    if (i == 0)
                    {
                        Console.WriteLine(lines[i].PadRight(Console.BufferWidth - 2,'\u2000')); //write current line in queue.
                        Console.CursorTop = Console.CursorTop - 1;
                    }
                    if (i > 0)
                    {
                        //write current line in queue, padded by 21 enQuads to preserve line format.
                        Console.WriteLine(lines[i].PadLeft(lines[i].Length + 21, '\u2000').PadRight(Console.BufferWidth - 2  )); 
                        Console.CursorTop = Console.CursorTop - 1;
                    }

                }
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                if (showCursor)
                {
                    Console.Write(">");//Write the input indicator.

                }
                if (!showCursor)
                {
                    Console.CursorTop = prt;
                }
                Console.BackgroundColor = ConsoleBackgroundColor;
                Console.ForegroundColor = ConsoleForegroundColor;
                Console.CursorVisible = showCursor;
                if (showCursor)
                {
                    Console.Write("\u2551");
                }
                
                CurTop = Console.CursorTop;
                Writing = false;
            }
            
        }

        /// <summary>
        /// Reset the console layout using specified values
        /// </summary>
        /// <param name="fore">Text color</param>
        /// <param name="back">Background color</param>
        /// <param name="title">Console's header title (not window title)</param>
        public void ConsoleGUIReset(ConsoleColor fore, ConsoleColor back, string title)
        {

            ScreenBusy = true;
            if (title.Length > 72)
            {
                title = title.Remove(71) + "...";
            }
            currentTitle = title;
            Console.Clear();
            Console.SetWindowSize(144, 32);//Seems to be a reasonable console size.
            Console.SetBufferSize(144, 512);//Extra buffer room just because why not.
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
            Console.Clear();

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            DecorateTop();

            string WTitle = ("" + DateTime.Now.ToString("HH:mm:ss") + " " + title + " - RMSoftwareModularBot v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            string pTitle = WTitle.PadLeft(71 + WTitle.Length / 2);
            pTitle += "".PadRight(71 - WTitle.Length / 2);
            Console.Write("\u2551{0}\u2551", pTitle);

            DecorateBottom();
            Console.CursorVisible = false;
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
            ConsoleBackgroundColor = back;
            ConsoleForegroundColor = fore;
            int ct = Console.CursorTop;
            for (int i = ct; i < 34; i++)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Black;
                //Thread.Sleep(1);
                Console.Write((char)9617);//Write the colored space.
                                          //Thread.Sleep(1);
                Console.BackgroundColor = ConsoleBackgroundColor;//restore previous color.
                Console.ForegroundColor = ConsoleForegroundColor;
                //Thread.Sleep(1);
                Console.Write("\u2551");//uileft
                                        //Thread.Sleep(1);
                Console.CursorTop = i;
                Console.CursorLeft = 0;
            }
            Console.CursorTop = 0;
            Console.CursorTop = ct;
            ScreenBusy = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fore"></param>
        /// <param name="back"></param>
        /// <param name="title"></param>
        /// <param name="ProgressValue"></param>
        /// <param name="ProgressMax"></param>
        /// <param name="ProgressColor"></param>
        public void ConsoleGUIReset(ConsoleColor fore, ConsoleColor back, string title, short ProgressValue, short ProgressMax, ConsoleColor ProgressColor)
        {
            ScreenBusy = true;
            if (title.Length > 72)
            {
                title = title.Remove(71) + "...";
            }
            currentTitle = title;
            Console.Clear();
            Console.SetWindowSize(144, 32);//Seems to be a reasonable console size.
            Console.SetBufferSize(144, 512);//Extra buffer room just because why not.
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
            Console.Clear();

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            DecorateTop();

            string WTitle = ("" + DateTime.Now.ToString("HH:mm:ss") + " " + title + " - RMSoftwareModularBot v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            string pTitle = WTitle.PadLeft(71 + WTitle.Length / 2);
            pTitle += "".PadRight(71 - WTitle.Length / 2);
            Console.Write("\u2551{0}\u2551", pTitle);
            Console.Write("\u2551{0}\u2551", "".PadLeft(142));
            string progressBAR = "";
            float f = (float)(ProgressValue / (float)ProgressMax);

            int amt = (int)(44 * (float)f);

            for (int i = 0; i < 44; i++)
            {
                if (i <= amt)
                {

                    progressBAR += "\u2588";
                }
                else
                {
                    progressBAR += "\u2591";
                }
            }
            progressBAR += $" PAGE {ProgressValue} OF {ProgressMax}";
            string pbar = progressBAR.PadLeft(71 + progressBAR.Length / 2);
            pbar += "".PadRight(71 - progressBAR.Length / 2);
            Console.Write("\u2551");
            Console.ForegroundColor = ProgressColor;
            Console.Write("{0}", pbar);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\u2551");
            DecorateBottom();


            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;

            ConsoleBackgroundColor = back;
            ConsoleForegroundColor = fore;
            Thread.Sleep(5);
            ScreenBusy = false;
        }

        /// <summary>
        /// Reset the console layout using specified values
        /// </summary>
        /// <param name="fore">Text color</param>
        /// <param name="back">Background color</param>
        /// <param name="title">Console's header title (not window title)</param>
        /// <param name="w">Console window & buffer width</param>
        /// <param name="h">Console window & buffer height</param>
        public void ConsoleGUIReset(ConsoleColor fore, ConsoleColor back, string title, short w, short h)
        {
            ScreenBusy = true;
            if (title.Length > 72)
            {
                title = title.Remove(71) + "...";
            }
            currentTitle = title;
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
            string pTitle = WTitle.PadLeft(((w / 2) + 2) + WTitle.Length / 2);
            pTitle += "".PadRight(((w / 2) - 3) - WTitle.Length / 2);
            Console.Write("\u2551{0}\u2551", pTitle);

            DecorateBottom();

            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
            ConsoleBackgroundColor = back;
            ScreenBusy = false;
        }

        //Heavily tweaked from: https://stackoverflow.com/questions/20534318/make-console-writeline-wrap-words-instead-of-letters
        //Fixed a bug where wrap would fail if no spaces & even if space, characters longer than console width would break)
        public string WordWrap(string paragraph, int consoleoffset = 24)
        {
            paragraph = new Regex(@" {2,}").Replace(paragraph.Trim(), @" ");
            //paragraph = new Regex(@"\r\n{2,}").Replace(paragraph.Trim(), @" ");
            //paragraph = new Regex(@"\r{2,}").Replace(paragraph.Trim(), @" ");
            var lines = new List<string>();
            string returnstring = "";
            int i = 0;
            while (paragraph.Length > 0)
            {
                lines.Add(paragraph.Substring(0, Math.Min(Console.WindowWidth - consoleoffset, paragraph.Length)));
                int NewLinePos = lines[i].LastIndexOf("\r\n");
                if (NewLinePos > 0)
                {
                    lines[i] = lines[i].Remove(NewLinePos);
                    paragraph = paragraph.Substring(Math.Min(lines[i].Length, paragraph.Length));
                    returnstring += (lines[i].Trim()) + "\n";
                    i++;
                    continue;
                    //lines.Add(paragraph.Substring(NewLinePos, paragraph.Length-NewLinePos));
                    //lines[i] = lines[i].Remove(length).PadRight(Console.WindowWidth - 2, '\u2000');
                }
                var length = lines[i].LastIndexOf(" ");

                if (length == -1 && lines[i].Length > Console.WindowWidth - consoleoffset) //23 (█00:00:00 MsgSource00)
                {
                    int l = Console.WindowWidth - consoleoffset;
                    lines[i] = lines[i].Remove(l);
                    //lines[i] = lines[i].Remove(l).PadRight(Console.WindowWidth-2,'\u2000');
                }
                if (length > 20 && paragraph.Length > Console.WindowWidth - consoleoffset)
                {
                    lines[i] = lines[i].Remove(length);

                    //lines[i] = lines[i].Remove(length).PadRight(Console.WindowWidth - 2, '\u2000');
                }
                paragraph = paragraph.Substring(Math.Min(lines[i].Length, paragraph.Length));
                returnstring += (lines[i].Trim()) + "\n";
                i++;
            }
            if (lines.Count > 1)
            {

                returnstring += "\u00a0";
            }
            return returnstring;
        }

        /// <summary>
        /// Write a color coordinated log message to console. Function is intended for full mode.
        /// </summary>
        /// <param name="message">The Discord.NET Log message</param>
        /// <param name="Entrycolor">An optional entry color. If none (or black), the message.LogSeverity is used for color instead.</param>
        public void WriteEntry(LogMessage message, ConsoleColor? Entrycolor = null, bool showCursor = true, bool bypassScreenLock = false, bool bypassFilter = false)
        {

            Backlog.Enqueue(new LogEntry(message, Entrycolor, bypassFilter, bypassScreenLock, showCursor));
        }

        /// <summary>
        /// Write a color coordinated log message to console. Function is intended for full mode.
        /// </summary>
        /// <param name="message">The Discord.NET Log message</param>
        /// <param name="Entrycolor">An optional entry color. If none (or black), the message.LogSeverity is used for color instead.</param>
        public void WriteEntry(string message, ConsoleColor Entrycolor = ConsoleColor.Black, bool showCursor = true)
        {


            SpinWait.SpinUntil(() => !Writing);//This will help prevent the console from being sent into a mess of garbled words.
            
            
            Writing = true;
            PrvTop = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);//Reset line position.

            string[] lines = WordWrap(message, 1).Split('\n');
            ConsoleColor bglast = ConsoleBackgroundColor;

            for (int i = 0; i < lines.Length; i++)
            {

                if (lines[i].Length == 0)
                {
                    continue;
                }
                ConsoleColor bg = ConsoleColor.Black;
                ConsoleColor fg = ConsoleColor.Black;
                bg = Entrycolor;
                fg = Entrycolor;
                Console.BackgroundColor = bg;
                Console.ForegroundColor = fg;
                //Thread.Sleep(1);//safe.
                Console.Write((char)9617);//Write the colored space.
                Console.BackgroundColor = bglast;//restore previous color.
                Console.ForegroundColor = ConsoleForegroundColor;
               // Thread.Sleep(1);//safe.
                if (i == 0)
                {
                    Console.WriteLine(lines[i]);//write current line in queue.
                }
                if (i > 0)
                {
                    Console.WriteLine(lines[i]);//write current line in queue, padded by 21 enQuads to preserve line format.
                }

            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            //Thread.Sleep(1);//safe.
            if (showCursor)
            {
                Console.Write(">");//Write the input indicator.

            }
            //Program.CursorPTop = Console.CursorTop;//Set the cursor position, this will delete ALL displayed input from console when it is eventually reset.
            //Thread.Sleep(1);//safe.
            Console.BackgroundColor = ConsoleBackgroundColor;
            Console.ForegroundColor = ConsoleForegroundColor;
            Console.CursorVisible = showCursor;
            CurTop = Console.CursorTop;
            Writing = false;
        }

        /// <summary>
        /// ConsoleGUIReset - Title decoration (TOP)
        /// </summary>
        private void DecorateTop()
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
        private void DecorateBottom()
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

        /// <summary>
        /// Show 'CRASH' screen with custom title and call for app termination or restart.
        /// </summary>
        /// <param name="title">Title of killscreen</param>
        /// <param name="message">the point of the killscreen</param>
        /// <param name="autorestart">True: Prompt for auto restart in timeout period</param>
        /// <param name="timeout">auto restart timeout in seconds.</param>
        /// <param name="ex">The inner exception leading to the killscreen.</param>
        /// <returns></returns>
        public Task<bool> ShowKillScreen(string title, string message, bool autorestart, ref bool ProgramShutdownFlag, ref bool ProgramRestartFlag, int timeout = 5, Exception ex = null)
        {
            ScreenModal = true;
            ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, title);
            WriteEntry(new LogMessage(LogSeverity.Critical, "MAIN", "The program encountered a problem, and was terminated. Details below."),null,true,true);
            LogMessage m = new LogMessage(LogSeverity.Critical, "CRITICAL", message);
            WriteEntry(m, null, true, true, false);

            WriteEntry(new LogMessage(LogSeverity.Info, "MAIN", "writing error report to CRASH.LOG"), null, true, true, false);
            CreateCrashLog(ex, m);
            WriteEntry(new LogMessage(LogSeverity.Info, "MAIN", "Writing additional information to ERRORS.LOG"), null, true, true, false);
            WriteErrorsLog(ex);

            if (!autorestart)
            {
                WriteEntry(new LogMessage(LogSeverity.Info, "MAIN", "Press any key to terminate..."), null, true, true, false);
                Console.ReadKey();
            }
            else
            {
                //prompt for autorestart.
                for (int i = 0; i < timeout; i++)
                {
                    int l = Console.CursorLeft;
                    int t = Console.CursorTop;

                    WriteEntry(new LogMessage(LogSeverity.Info, "MAIN", $"Restarting in {timeout - i} second(s)..."), null, false, true, false);

                    Console.CursorLeft = l;
                    Console.CursorTop = t;//reset.
                    Thread.Sleep(1000);
                }
                
                if (!Program.AppArguments.Contains("-crashed"))
                {
                    Program.AppArguments.Add("-crashed");
                }

            }

            ScreenModal = false;
            ProgramShutdownFlag = true;
            ProgramRestartFlag = autorestart;//redundancy
            //ScreenBusy = false;
            return Task.FromResult(autorestart);//redundancy

        }
        /// <summary>
        /// Create a new Crash.LOG file
        /// </summary>
        /// <param name="ex">Exception data</param>
        /// <param name="m">Log message data</param>
        public void CreateCrashLog(Exception ex, LogMessage m)
        {
            using (FileStream fs = File.Create("CRASH.LOG"))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(m.ToString());
                    sw.WriteLine("If you continue to get this error, please report it to the developer, including the stack below.");
                    sw.WriteLine();
                    sw.WriteLine("Developer STACK:");
                    sw.WriteLine("=================================================================================================================================");
                    sw.WriteLine(ex.ToString());
                    sw.Flush();
                }
            }
        }

        public void WriteErrorsLog(Exception ex)
        {
            using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + ex.ToString());
                    sw.Flush();
                    sw.Close();
                    Thread.Sleep(150);
                }
            }
        }

        public void WriteErrorsLog(string message, Exception ex = null)
        {
            using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + " - " + message);

                    if (ex != null)
                    {
                        sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + " - " + ex.ToString());
                    }

                    sw.Flush();
                    sw.Close();
                    Thread.Sleep(150);
                }
            }
        }
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();
        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        public const int VK_RETURN = 0x0D;
        public const int WM_KEYDOWN = 0x100;
        /// <summary>
        /// Process console commands
        /// </summary>
        internal Task GetConsoleInput(ref bool ShutdownCalled, ref bool RestartRequested,ref bool InputCanceled, ref DiscordNET discordNET)
        {
            ulong chID = 0;

            while (true)
            {

                string input = Console.ReadLine();
                string unproc = input;
                if (InputCanceled)
                {
                    return Task.Delay(0);
                }
                if (ScreenBusy)
                {
                    continue;
                }
                Console.CursorTop = CurTop;
                
                if (input.ToLower() == "stopbot")
                {
                    WriteEntry(new LogMessage(LogSeverity.Critical, "MAIN", "Console session called STOPBOT."));

                    discordNET.Stop(ref ShutdownCalled);
                    RestartRequested = false;
                    break;
                }

                if (input.ToLower() == "rskill")
                {

                    RestartRequested = ShowKillScreen("Test KS", "The program was instructed to run a test killscreen. This will auto restart the program.", true, ref ShutdownCalled,ref RestartRequested, 5, new ApplicationException("Command rskill triggered kill screen. USER INITIATED CRASH SCREEN.")).GetAwaiter().GetResult();
                    break;
                }

                if (input.ToLower() == "cls" || input.ToLower() == "clear")
                {
                    LogEntries.Clear();//remove buffer.
                    ConsoleGUIReset(ConsoleForegroundColor, ConsoleBackgroundColor, currentTitle);
                    SpinWait.SpinUntil(() => !ScreenBusy);
                    WriteEntry(new LogMessage(LogSeverity.Info, "Console", "Console cleared!"), null, true, false, true);
                }

                if (input.ToLower() == "tskill")
                {
                    RestartRequested = ShowKillScreen("Test KS", "The program was instructed to run a test killscreen. This will prompt you to terminate the program.", false, ref ShutdownCalled, ref RestartRequested, 5, new ApplicationException("Command rskill triggered kill screen. USER INITIATED CRASH SCREEN.")).GetAwaiter().GetResult();
                    break;
                }

                if (input.ToLower() == "disablecmd")
                {
                    WriteEntry(new LogMessage(LogSeverity.Warning, "Console", "Command processing disabled!"));


                    discordNET.Client.SetStatusAsync(UserStatus.DoNotDisturb);
                    discordNET.Client.SetGameAsync("");
                    discordNET.DisableMessages = true;
                }

                if (input.ToLower() == "enablecmd")
                {
                    WriteEntry(new LogMessage(LogSeverity.Info, "Console", "Command processing enabled."));

                    discordNET.Client.SetStatusAsync(UserStatus.Online);
                    discordNET.Client.SetGameAsync("for commands!", null, ActivityType.Watching);
                    discordNET.DisableMessages = false;
                }

                if (input.ToLower() == "mbotdata")
                {
                    WriteEntry(new LogMessage(LogSeverity.Info, "Console", "Opening ModularBOT's installation directory."));
                    Process.Start(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location));
                }

                if (input.ToLower().StartsWith("status"))
                {
                    string status = input.Remove(0, 7).Trim();
                    WriteEntry(new LogMessage(LogSeverity.Info, "Client", "client status changed."));
                    discordNET.Client.SetGameAsync(status);
                    //_client.SetGameAsync(status);
                }

                if (input.ToLower().StartsWith("setgch"))
                {
                    input = input.Remove(0, 6).Trim();
                    if (!ulong.TryParse(input, out chID))
                    {
                        WriteEntry(new LogMessage(LogSeverity.Error, "Console", "Invalid ULONG."));
                        continue;
                    }
                    WriteEntry(new LogMessage(LogSeverity.Error, "Console", "Set guild channel id."));

                }

                if (input.ToLower().StartsWith("conmsg"))
                {
                    input = input.Remove(0, 6).Trim();
                    if (!(discordNET.Client.GetChannel(chID) is SocketTextChannel Channel))
                    {
                        WriteEntry(new LogMessage(LogSeverity.Error, "Console", "Invalid channel."));
                        continue;
                    }
                    Channel.SendMessageAsync(input);
                }

                if (input.ToLower().StartsWith("setvar"))
                {
                    input = input.Remove(0, 6).Trim();
                    string varname = input.Split(' ')[0];
                    input = input.Remove(0, varname.Length);
                    input = input.Trim();
                    discordNET.CustomCMDMgr.coreScript.Set(varname, input);
                }

                if (input.ToLower().StartsWith("config.loglevel"))
                {
                    if (input.Split(' ').Length > 2)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too many arguments!"));
                        continue;
                    }
                    if (input.Split(' ').Length < 2)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too few arguments!"));
                        continue;
                    }
                    input = input.Remove(0, 16).Trim();
                    if (Enum.TryParse(input, true, out LogSeverity result))
                    {
                        Program.configMGR.CurrentConfig.DiscordEventLogLevel = result;
                        Program.configMGR.Save();
                        ScreenModal = true;
                        while (true)
                        {
                            WriteEntry(new LogMessage(LogSeverity.Info, "Console", "Saved logging level. Changes will take place when bot is restarted. Do you want to restart now? [Y/N]"),null,true,true,true);
                            ConsoleKeyInfo k = Console.ReadKey();
                            if (k.Key == ConsoleKey.Y)
                            {

                                discordNET.Stop(ref ShutdownCalled);
                                RestartRequested = true;
                                Thread.Sleep(1000);
                                return Task.Delay(1);
                            }
                            if (k.Key == ConsoleKey.N)
                            {
                                break;
                            }
                        }
                        ScreenModal = false;

                    }

                    else
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", $"Invalid parameter. Try a log severity level: {string.Join(", ", Enum.GetNames(typeof(LogSeverity)))}"));
                }

                if (input.ToLower().StartsWith("config.setupdates"))
                {
                    if (input.Split(' ').Length > 2)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too many arguments!"));
                        continue;
                    }
                    if (input.Split(' ').Length < 2)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too few arguments!"));
                        continue;
                    }
                    input = input.Remove(0, 18).Trim();
                    if (bool.TryParse(input, out bool result))
                    {
                        Program.configMGR.CurrentConfig.CheckForUpdates = result;
                        string pr = result ? "will" : "will not";
                        Program.configMGR.Save();
                        WriteEntry(new LogMessage(LogSeverity.Info, "Console", $"Program {pr} check for updates on startup."),null, true, false, true);
                    }
                    else
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Unexpected argument."));
                        continue;
                    }
                }

                if (input.ToLower().StartsWith("config.update.prerelease"))
                {
                    if (input.Split(' ').Length > 1)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too many arguments!"));
                        continue;
                    }
                    if (input.Split(' ').Length < 1)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too few arguments!"));
                        continue;
                    }
                    input = input.Remove(0, 25).Trim();
                    if (bool.TryParse(input, out bool result))
                    {
                        Program.configMGR.CurrentConfig.UsePreReleaseChannel = result;
                        Program.configMGR.Save();
                        WriteEntry(new LogMessage(LogSeverity.Info, "Console", "You've switched update channels."), null, true, false, true);
                    }
                    else
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Unexpected argument."));
                        continue;
                    }
                }

                if (input.ToLower().StartsWith("config.setlogo"))
                {
                    string PRV_TITLE = currentTitle;
                    List<LogEntry> v = new List<LogEntry>();

                    ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Logo", 5, 6, ConsoleColor.Green);
                    
                    SetLogo_Choices();
                    ConsoleKeyInfo k;
                    string path = "";
                    ScreenModal = true;
                    while (true)
                    {
                        WriteEntry("\u2502 Please enter a choice below...", ConsoleColor.DarkBlue, true);
                        Console.Write("\u2502 > ");
                        k = Console.ReadKey();
                        if (k.KeyChar == '1')
                        {
                            path = "NONE";
                            break;
                        }
                        if (k.KeyChar == '2')
                        {
                            path = "INTERNAL";
                            WriteEntry("\u2502 Previewing action... One second please...");
                            Thread.Sleep(600);
                            ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome", 79, 45);
                            Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                            Thread.Sleep(800);
                            ConsoleWriteImage(Properties.Resources.RMSoftwareICO);
                            Thread.Sleep(3000);
                            ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Logo", 5, 6, ConsoleColor.Green);
                            break;
                        }
                        if (k.KeyChar == '3')
                        {
                            WriteEntry("\u2502 Please enter the path to a valid image file...", ConsoleColor.DarkBlue);
                            Console.Write("\u2502 > ");
                            path = Console.ReadLine();
                            WriteEntry("\u2502 Previewing action... One second please...");
                            Thread.Sleep(600);
                            ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome", 79, 45);
                            Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                            Thread.Sleep(800);
                            try
                            {
                                ConsoleWriteImage(new System.Drawing.Bitmap(path.Replace("\"","")));
                            }
                            catch (Exception ex)
                            {

                                WriteEntry("\u2502 Something went wrong. Make sure you specified a valid image.", ConsoleColor.Red);
                                WriteEntry("\u2502 " + ex.Message, ConsoleColor.Red);
                                WriteEntry("\u2502");
                                SetLogo_Choices();
                                continue;
                            }
                            Thread.Sleep(3000);
                            break;
                        }
                    }

                    Program.configMGR.CurrentConfig.LogoPath = path.Replace("\"", "");
                    Program.configMGR.Save();
                    ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, PRV_TITLE);
                    ScreenModal = false;
                    v.AddRange(LogEntries);
                    LogEntries.Clear();//clear buffer.
                    //output previous logEntry.
                    foreach (var item in v)
                    {
                        WriteEntry(item.LogMessage, item.EntryColor);
                    }
                    WriteEntry(new LogMessage(LogSeverity.Info, "Config", "Startup logo saved successfully!"), null, true, false, true);
                    v = null;
                }

                if(input.ToLower().StartsWith("config.setcolors"))
                {
                    string PRV_TITLE = currentTitle;
                    List<LogEntry> v = new List<LogEntry>();

                    #region Background Color
                    ScreenModal = true;
                    ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Console Colors", 1, 2, ConsoleColor.Green);
                    WriteEntry("\u2502 Please select a background color.");
                    WriteEntry("\u2502");
                    for (int i = 0; i < 16; i++)
                    {
                        WriteEntry($"\u2502\u2005\u2005\u2005 {i.ToString("X")}. {((ConsoleColor)i).ToString()}", (ConsoleColor)i);
                    }
                    WriteEntry("\u2502");
                    ConsoleKeyInfo k;
                    ScreenModal = true;
                    while (true)
                    {
                        WriteEntry("\u2502 Please enter a choice below...", ConsoleColor.DarkBlue, true);
                        Console.Write("\u2502 > ");
                        k = Console.ReadKey();
                        Thread.Sleep(100);
                        char c = k.KeyChar;
                        if (int.TryParse(c.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int i))
                        {
                            Program.configMGR.CurrentConfig.ConsoleBackgroundColor = (ConsoleColor)i;
                            break;
                        }
                    }
                    #endregion

                    #region Foreground Color
                    ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Console Colors", 2, 2, ConsoleColor.Green);
                    WriteEntry("\u2502 Please select a foreground color.");
                    WriteEntry("\u2502");
                    for (int i = 0; i < 16; i++)
                    {
                        WriteEntry($"\u2502\u2005\u2005\u2005 {i.ToString("X")}. {((ConsoleColor)i).ToString()}", (ConsoleColor)i);
                    }
                    WriteEntry("\u2502");
                    ConsoleKeyInfo k1;
                    ScreenModal = true;
                    while (true)
                    {
                        WriteEntry("\u2502 Please enter a choice below...", ConsoleColor.DarkBlue, true);
                        Console.Write("\u2502 > ");
                        k1 = Console.ReadKey();
                        Thread.Sleep(100);
                        char c = k1.KeyChar;
                        if (int.TryParse(c.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int ie))
                        {
                            Program.configMGR.CurrentConfig.ConsoleForegroundColor = (ConsoleColor)ie;
                            break;
                        }
                    }
                    #endregion

                    Program.configMGR.Save();
                    ConsoleGUIReset(Program.configMGR.CurrentConfig.ConsoleForegroundColor, 
                        Program.configMGR.CurrentConfig.ConsoleBackgroundColor, PRV_TITLE);
                    ScreenModal = false;
                    v.AddRange(LogEntries);
                    LogEntries.Clear();//clear buffer.
                    //output previous logEntry.
                    foreach (var item in v)
                    {
                        WriteEntry(item.LogMessage, item.EntryColor);
                    }
                    WriteEntry(new LogMessage(LogSeverity.Info, "Config", "Console colors were changed successfully."), null, true, false, true);
                    v = null;
                }

                if(input.ToLower().StartsWith("guilds"))
                {
                    string PRV_TITLE = currentTitle;
                    List<LogEntry> v = new List<LogEntry>();
                    //---------------start modal---------------
                    ListGuilds(ref discordNET);
                    //----------------End modal----------------
                    ConsoleGUIReset(Program.configMGR.CurrentConfig.ConsoleForegroundColor,
                        Program.configMGR.CurrentConfig.ConsoleBackgroundColor, PRV_TITLE);
                    ScreenModal = false;
                    v.AddRange(LogEntries);
                    LogEntries.Clear();//clear buffer.
                    //output previous logEntry.
                    foreach (var item in v)
                    {
                        WriteEntry(item.LogMessage, item.EntryColor);
                    }
                }

                if (input.ToLower().StartsWith("users"))
                {
                    string page = "1";
                
                    if (input.Split(' ').Length > 3)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too many arguments!"));
                        continue;
                    }
                    if (input.Split(' ').Length < 2)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too few arguments!"));
                        continue;
                    }
                    if(input.Split(' ').Length <3)
                    {
                        input = input.Remove(0, 6).Trim();
                    }
                    if(input.Split(' ').Length == 3)
                    {
                        page = input.Split(' ')[2];
                        input = input.Split(' ')[1];
                    }
                    if(!short.TryParse(page,out short numpage))
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "List Users", "Invalid Page number"));
                        continue;
                    }
                    if (!ulong.TryParse(input, out ulong id))
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "List Users", "Invalid Guild ID format"));
                        continue;
                    }

                    string PRV_TITLE = currentTitle;
                    List<LogEntry> v = new List<LogEntry>();
                    //---------------start modal---------------
                    bool ModalResult = ListUsers(ref discordNET, id,numpage);
                    if (!ModalResult)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "List Users", "The guild was not found..."));
                        
                        continue;
                    }
                    //----------------End modal----------------
                    if(ModalResult)
                    {

                        ConsoleGUIReset(Program.configMGR.CurrentConfig.ConsoleForegroundColor,
                            Program.configMGR.CurrentConfig.ConsoleBackgroundColor, PRV_TITLE);
                        
                        v.AddRange(LogEntries);
                        LogEntries.Clear();//clear buffer.
                                           //output previous logEntry.
                        foreach (var item in v)
                        {
                            WriteEntry(item.LogMessage, item.EntryColor);
                        }
                    }
                }

                if (input.ToLower().StartsWith("search"))
                {
                    input = input.Remove(0, 7);
                    if (input.Split(' ').Length < 2)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too few arguments!"));
                        continue;
                    }

                    string rl = input.Split(' ')[0];
                   
                    if (!ulong.TryParse(rl, out ulong guild))
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "SEARCH", "Invalid guild ID"));
                        continue;
                    }
                    string query = input.Remove(0, rl.Length + 1);//guildID length + space
                    string PRV_TITLE = currentTitle;
                    List<LogEntry> v = new List<LogEntry>();
                    //---------------start modal---------------
                    bool ModalResult = ListUsers(ref discordNET, guild, query);
                    if (!ModalResult)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "SEARCH", "The guild was not found..."));

                        continue;
                    }
                    //----------------End modal----------------
                    if (ModalResult)
                    {

                        ConsoleGUIReset(Program.configMGR.CurrentConfig.ConsoleForegroundColor,
                            Program.configMGR.CurrentConfig.ConsoleBackgroundColor, PRV_TITLE);

                        v.AddRange(LogEntries);
                        LogEntries.Clear();//clear buffer.
                                           //output previous logEntry.
                        foreach (var item in v)
                        {
                            WriteEntry(item.LogMessage, item.EntryColor);
                        }
                    }
                }

                if (input.ToLower().StartsWith("channels"))
                {
                    string page = "1";

                    if (input.Split(' ').Length > 3)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too many arguments!"));
                        continue;
                    }
                    if (input.Split(' ').Length < 2)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too few arguments!"));
                        continue;
                    }
                    if (input.Split(' ').Length < 3)
                    {
                        input = input.Remove(0, 9).Trim();
                    }
                    if (input.Split(' ').Length == 3)
                    {
                        page = input.Split(' ')[2];
                        input = input.Split(' ')[1];
                    }
                    if (!short.TryParse(page, out short numpage))
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "List Channels", "Invalid Page number"));
                        continue;
                    }
                    if (!ulong.TryParse(input, out ulong id))
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "List Channels", "Invalid Guild ID format"));
                        continue;
                    }

                    string PRV_TITLE = currentTitle;
                    List<LogEntry> v = new List<LogEntry>();
                    //---------------start modal---------------
                    bool ModalResult = ListChannels(ref discordNET, id, numpage);
                    if (!ModalResult)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Critical, "List Channels", "The guild was not found..."));

                        continue;
                    }
                    //----------------End modal----------------
                    if (ModalResult)
                    {

                        ConsoleGUIReset(Program.configMGR.CurrentConfig.ConsoleForegroundColor,
                            Program.configMGR.CurrentConfig.ConsoleBackgroundColor, PRV_TITLE);

                        v.AddRange(LogEntries);
                        LogEntries.Clear();//clear buffer.
                                           //output previous logEntry.
                        foreach (var item in v)
                        {
                            WriteEntry(item.LogMessage, item.EntryColor);
                        }
                    }
                }

                else
                {
                    if (!ScreenBusy && !ScreenModal)
                    {
                        WriteEntry(new LogMessage(LogSeverity.Info, "Console", unproc), null, true, false, true);
                    }
                }
            }
            return Task.Delay(1);
        }

        private void SetLogo_Choices()
        {
            WriteEntry("\u2502 Have you ever seen those old DOS programs that have the fancy ASCII art @ startup?");
            WriteEntry("\u2502 Yea? Well great! This bot can do that! Why? (You may be asking) WHY NOT?!");
            WriteEntry("\u2502\u2005");
            WriteEntry("\u2502\u2005\u2005 Options:", ConsoleColor.DarkGreen);
            WriteEntry("\u2502\u2005\u2005\u2005 1. No logo", ConsoleColor.DarkGreen);
            WriteEntry("\u2502\u2005\u2005\u2005 2. Default logo", ConsoleColor.DarkGreen);
            WriteEntry("\u2502\u2005\u2005\u2005 3. Custom logo", ConsoleColor.DarkGreen);
            WriteEntry("\u2502\u2005");
        }

        private void ListGuilds(ref DiscordNET discord)
        {
            short page = 1;
            
            short max = (short)(Math.Ceiling((double)(discord.Client.Guilds.Count / 24))+1);
            int index = 0;
            ScreenModal = true;
            
            while (true)
            {
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Guilds", page, max, ConsoleColor.White);
                for (int i = index; i < 24 * page; i++)//28 results per page.
                {
                    if (index >= discord.Client.Guilds.Count)
                    {
                        break;
                    }
                    string name = discord.Client.Guilds.ElementAt(i).Name;
                    if(name.Length > 35)
                    {
                        name = name.Remove(35) + "...";
                    }
                    WriteEntry($"\u2502\u2005\u2005\u2005 - {name.PadRight(38,'\u2005')} [{discord.Client.Guilds.ElementAt(i).Id}]",ConsoleColor.DarkGreen);
                    
                    index++;
                }
                WriteEntry($"\u2502");
                if (page > 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page == max)
                {
                    WriteEntry($"\u2502 E: Exit list", ConsoleColor.White);
                }
                if (page > 1 && page == max)
                {
                    WriteEntry($"\u2502 P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                ConsoleKeyInfo s = Console.ReadKey();
                if(s.Key == ConsoleKey.P)
                {
                    if(page>1)
                    {
                        page--;
                        index = (page * 24) - 24;//0 page 1 = 0; page 2 = 20; etc.
                        //continue;
                    }
                    index = (page * 24) - 24;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
                if (s.Key == ConsoleKey.E)
                {
                    break;
                }
                if (s.Key == ConsoleKey.N)
                {
                    if (page < max)
                    {
                        page++;
                    }

                    index = (page * 24) - 24;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }

                else
                {
                    index = (page * 24) - 24;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
            }

            ScreenModal = false;

        }

        private bool ListUsers(ref DiscordNET discord,ulong guildID, short page=1)
        {
            SocketGuild g = discord.Client.GetGuild(guildID);
            if(g == null)
            {
                return false;
            }
            g.DownloadUsersAsync();
            List<SocketGuildUser> guildusers = g.Users.ToList().OrderByDescending(x => (int)(x.Hierarchy)).ToList();
            string name = g.Name.Length > 17 ? g.Name.Remove(17) : g.Name;
            

            short max = (short)(Math.Ceiling((double)(guildusers.Count / 22))+1);
            if(page > max)
            {
                page = max;
            }
            if (page < 1)
            {
                page = 1;
            }
            int index = (page * 22) - 22;
            ScreenModal = true;
            

            while (true)
            {
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Listing all users for Guild: {name}", page, max, ConsoleColor.White);
                WriteEntry($"\u2502\u2005\u2005\u2005 - {"Discord User".PadRight(39, '\u2005')} {"Snowflake ID".PadRight(22, '\u2005')} {"Access Level".PadRight(18, '\u2005')}", ConsoleColor.Blue);
                WriteEntry($"\u2502\u2005\u2005\u2005 \u2500 {"".PadRight(39, '\u2500')} {"".PadLeft(22, '\u2500')} {"".PadLeft(18, '\u2500')}", ConsoleColor.Blue);
                for (int i = index; i < 22 * page; i++)//22 results per page.
                {
                    if (index >= guildusers.Count)
                    {
                        break;
                    }
                    string userinput = guildusers.ElementAt(i).Username;
                    string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(userinput))).Replace(' ','\u2005').Replace("??","?");
                    string p = $"{o}#{guildusers.ElementAt(i).Discriminator}".PadRight(39, '\u2005');
                    WriteEntry($"\u2502\u2005\u2005\u2005 - {p} [{guildusers.ElementAt(i).Id.ToString().PadLeft(20,'0')}] {discord.PermissionManager.GetAccessLevel(guildusers.ElementAt(i)).ToString().PadRight(18,'\u2005')}", ConsoleColor.DarkGreen);
                    index++;
                }
                WriteEntry($"\u2502");
                if (page > 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page == max)
                {
                    WriteEntry($"\u2502 E: Exit list", ConsoleColor.White);
                }
                if (page > 1 && page == max)
                {
                    WriteEntry($"\u2502 P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                ConsoleKeyInfo s = Console.ReadKey();
                if (s.Key == ConsoleKey.P)
                {
                    if (page > 1)
                    {
                        page--;
                        index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                        //continue;
                    }
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
                if (s.Key == ConsoleKey.E)
                {
                    break;
                }
                if (s.Key == ConsoleKey.N)
                {
                    if (page < max)
                    {
                        page++;
                    }

                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }

                else
                {
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
            }

            ScreenModal = false;
            return true;

        }

        private bool ListUsers(ref DiscordNET discord, ulong guildID, string query)
        {
            short page = 1;
            string[] array = query.Split('#');
            string userquery = "";
            string discrimquery = "";
            if (array.Length > 1)
            {
                userquery = array[0];
                discrimquery = array[1];
            }
            SocketGuild g = discord.Client.GetGuild(guildID);
            if (g == null)
            {
                return false;
            }
            g.DownloadUsersAsync();
            List<SocketGuildUser> guildusers = g.Users.ToList().OrderByDescending(x => (int)(x.Hierarchy)).ToList();
            if(array.Length > 1)
            {
                guildusers = guildusers.FindAll(x => (x.Username.ToLower() + "#" + x.Discriminator).Contains(userquery.ToLower() + "#" + discrimquery));
                //output all results containing the query
            }
            if(array.Length == 1)
            {
                guildusers = guildusers.FindAll(x => x.Username.ToLower().Contains(query.ToLower()));
            }
            
            string name = g.Name.Length > 17 ? g.Name.Remove(17) : g.Name;


            short max = (short)(Math.Ceiling((double)(guildusers.Count / 22)) + 1);
            if (page > max)
            {
                page = max;
            }
            if (page < 1)
            {
                page = 1;
            }
            int index = (page * 22) - 22;
            ScreenModal = true;


            while (true)
            {
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Searching for '{query}' in Guild: {name}", page, max, ConsoleColor.White);
                WriteEntry($"\u2502\u2005\u2005\u2005 - {"Discord User".PadRight(39, '\u2005')} {"Snowflake ID".PadRight(22, '\u2005')} {"Access Level".PadRight(18, '\u2005')}", ConsoleColor.Blue);
                WriteEntry($"\u2502\u2005\u2005\u2005 \u2500 {"".PadRight(39, '\u2500')} {"".PadLeft(22, '\u2500')} {"".PadLeft(18, '\u2500')}", ConsoleColor.Blue);
                if (guildusers.Count == 0)
                {
                    WriteEntry($"\u2502\u2005\u2005\u2005 - No users found... :(", ConsoleColor.DarkRed);
                }
                for (int i = index; i < 22 * page; i++)//22 results per page.
                {
                    if (index >= guildusers.Count)
                    {
                        break;
                    }
                    string userinput = guildusers.ElementAt(i).Username;
                    string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(userinput))).Replace(' ', '\u2005').Replace("??", "?");
                    string p = $"{o}#{guildusers.ElementAt(i).Discriminator}".PadRight(39, '\u2005');
                    WriteEntry($"\u2502\u2005\u2005\u2005 - {p} [{guildusers.ElementAt(i).Id.ToString().PadLeft(20, '0')}] {discord.PermissionManager.GetAccessLevel(guildusers.ElementAt(i)).ToString().PadRight(18, '\u2005')}", ConsoleColor.DarkGreen);
                    index++;
                }
                WriteEntry($"\u2502");
                if (page > 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page == max)
                {
                    WriteEntry($"\u2502 E: Exit list", ConsoleColor.White);
                }
                if (page > 1 && page == max)
                {
                    WriteEntry($"\u2502 P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                ConsoleKeyInfo s = Console.ReadKey();
                if (s.Key == ConsoleKey.P)
                {
                    if (page > 1)
                    {
                        page--;
                        index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                        //continue;
                    }
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
                if (s.Key == ConsoleKey.E)
                {
                    break;
                }
                if (s.Key == ConsoleKey.N)
                {
                    if (page < max)
                    {
                        page++;
                    }

                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }

                else
                {
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
            }

            ScreenModal = false;
            return true;

        }

        private bool ListChannels(ref DiscordNET discord, ulong guildID, short page = 1)
        {
            SocketGuild g = discord.Client.GetGuild(guildID);
            if (g == null)
            {
                return false;
            }
            string name = g.Name.Length > 17 ? g.Name.Remove(17) : g.Name;


            short max = (short)(Math.Ceiling((double)(g.Channels.Count / 24)) + 1);
            if (page > max)
            {
                page = max;
            }
            if (page < 1)
            {
                page = 1;
            }
            int index = (page * 24) - 24;
            ScreenModal = true;


            while (true)
            {
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Channels for Guild: {name}", page, max, ConsoleColor.White);
                WriteEntry($"\u2502\u2005\u2005\u2005 - {"Channel Name".PadRight(39, '\u2005')} {"Snowflake ID".PadRight(22, '\u2005')} {"Channel Type".PadLeft(12, '\u2005')}", ConsoleColor.Blue);
                WriteEntry($"\u2502\u2005\u2005\u2005 \u2500 {"".PadRight(39, '\u2500')} {"".PadLeft(22, '\u2500')} {"".PadLeft(12, '\u2500')}", ConsoleColor.Blue);
                for (int i = index; i < 22 * page; i++)//22 results per page.
                {
                    if (index >= g.Channels.Count)
                    {
                        break;
                    }
                    string channelin = g.Channels.ElementAt(i).Name;
                    string chtype = g.Channels.ElementAt(i).GetType().ToString();
                    string chltype = chtype.Remove(0, chtype.LastIndexOf('.') + 1).Replace("Socket", "").Replace("Channel", "");
                    string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(channelin))).Replace(' ', '\u2005').Replace("??", "?");
                    string p = $"{o}".PadRight(39, '\u2005');
                    WriteEntry($"\u2502\u2005\u2005\u2005 - {p} [{g.Channels.ElementAt(i).Id.ToString().PadLeft(20, '0')}] {chltype.PadRight(12,'\u2005')}", ConsoleColor.DarkGreen);
                    index++;
                }
                WriteEntry($"\u2502");
                if (page > 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page == max)
                {
                    WriteEntry($"\u2502 E: Exit list", ConsoleColor.White);
                }
                if (page > 1 && page == max)
                {
                    WriteEntry($"\u2502 P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                ConsoleKeyInfo s = Console.ReadKey();
                if (s.Key == ConsoleKey.P)
                {
                    if (page > 1)
                    {
                        page--;
                        index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                        //continue;
                    }
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
                if (s.Key == ConsoleKey.E)
                {
                    break;
                }
                if (s.Key == ConsoleKey.N)
                {
                    if (page < max)
                    {
                        page++;
                    }

                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }

                else
                {
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
            }

            ScreenModal = false;
            return true;

        }

        #region Console pixel art - Slight modification of https://stackoverflow.com/a/33715138/4655190
        int[] cColors = { 0x000000, 0x000080, 0x008000, 0x008080, 0x800000, 0x800080, 0x808000, 0xC0C0C0, 0x808080, 0x0000FF, 0x00FF00, 0x00FFFF, 0xFF0000, 0xFF00FF, 0xFFFF00, 0xFFFFFF };

        public void ConsoleWritePixel(System.Drawing.Color cValue)
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

        public void ConsoleWriteImage(System.Drawing.Bitmap source)
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



        #region Screen buffer
        public class LogEntry
        {
            public LogMessage LogMessage { get; set; }

            public ConsoleColor? EntryColor { get; set; }

            public bool BypassFilter { get; set; }

            public bool BypassScreenLock { get; set; }

            public bool ShowCursor { get; set; }

            public LogEntry(LogMessage msg, ConsoleColor? color, bool bypassfilt = false, bool bypassScreen = false,bool showCursor = true)
            {
                LogMessage = msg;
                EntryColor = color;
                BypassFilter = bypassfilt;
                BypassScreenLock = bypassScreen;
                ShowCursor = showCursor;
            }
        }
        #endregion
    }
}