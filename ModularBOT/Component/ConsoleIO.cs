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
using System.Windows;
using ModularBOT.Entity;
using ModularBOT.Component.ConsoleScreens;

namespace ModularBOT.Component
{
    public class ConsoleIO
    {
        #region PRIVATE | INTERNAL Fields

        internal const int VK_RETURN = 0x0D;
        internal const int WM_KEYDOWN = 0x100;
        internal ulong chID = 0;

        private bool errorLogWrite = false;
        internal ConsoleColor ConsoleForegroundColor = ConsoleColor.Gray;
        internal ConsoleColor ConsoleBackgroundColor = ConsoleColor.Black;

        private readonly int[] cColors =
            {
            0x000000, 0x000080, 0x008000, 0x008080, 0x800000, 0x800080, 0x808000, 0xC0C0C0,
            0x808080, 0x0000FF, 0x00FF00, 0x00FFFF, 0xFF0000, 0xFF00FF, 0xFFFF00, 0xFFFFFF
        };

        internal List<LogEntry> LogEntries { get; set; } = new List<LogEntry>();
        private StringBuilder debugdump = new StringBuilder();

        #endregion

        #region PUBLIC Properties
        public static bool Writing { get; set; } = false;
        public static bool QueueProcessStarted { get; set; } = false;
        public static bool ScreenBusy { get; set; }//If console is resetting or rendering new ui.
        public static bool ScreenModal { get; set; }//If there is a screen showing above discord logs
        public IReadOnlyCollection<LogEntry> LogEntriesBuffer { get { return LogEntries.AsReadOnly(); } }
        public static Queue<LogEntry> Backlog { get; set; } = new Queue<LogEntry>();
        public int CurTop { get; set; }
        public int PrvTop { get; set; }
        public string ConsoleTitle { get; set; } = "";
        public static ConsoleScreen ActiveScreen { get; set; }
        public List<ConsoleCommand> ConsoleCommands { get; internal set; } = new List<ConsoleCommand>();

        public LogEntry LatestEntry { get; set; }
        #endregion

        #region PRIVATE Methods

        #region Misc Component

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

        private void ConsoleWritePixel(System.Drawing.Color cValue) //SRC: Modified from https://stackoverflow.com/a/33715138/4655190
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

        private ConsoleColor GetInvertedColor(ConsoleColor Color) //I realize this is not accurate.
        {
            return (ConsoleColor)(Math.Abs((Color - ConsoleColor.White)));
        }
        #endregion

        #region SETUP WIZARD
        internal void SetLogo_Choices()
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

        #endregion

        #endregion

        #region INTERNAL Methods

        internal void ProcessQueue()
        {
            try
            {
                if (QueueProcessStarted)
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
                    debugdump.Clear();
                    SpinWait.SpinUntil(() => Backlog.Count > 0);
                    if (ScreenBusy) { continue; }                       //If the screen's busy (Resetting), DO NOT DQ!
                    if (Writing) { continue; }                          //If the console is in the middle of writing, DO NOT DQ!
                    if(Program.ShutdownCalled)
                    {
                        return;
                    }
                    
                    debugdump.AppendLine($"backlog: {Backlog?.ToString() ?? "null"}");
                    debugdump.AppendLine($"backlog count: {Backlog?.Count}");
                    LogEntry qitem = Backlog.Dequeue();                 //DQ the item and process it as qitem.
                    if(qitem == null) { continue; }
                    debugdump.AppendLine($"qitem: {qitem?.ToString() ?? "null"}");
                    if (qitem.LogMessage.Source == "██▒")
                    {
                        throw new Exception($"SYSTEM STOP -- Source-triggered crash. Msg: {qitem.LogMessage.Message}");
                    }
                    LatestEntry = qitem;
                    LogMessage message = qitem.LogMessage;              //Entry's log message data.
                    
                    ConsoleColor? Entrycolor = qitem.EntryColor;        //left margin color

                    bool bypassFilter = qitem.BypassFilter;             //will this entry obey application log level?
                    bool bypassScreenLock = qitem.BypassScreenLock;     //will this entry show up through a modal screen?
                    bool showCursor = qitem.ShowCursor;                 //will this entry output and show the console cursor?
                    
                    debugdump.AppendLine($"LogEntries: {LogEntries?.ToString() ?? "null"}");
                    LogEntries.Add(new LogEntry(message, Entrycolor));  //Add the entry to buffer. Ignore screen modal, for outputting when modal is closed.

                    if (LogEntries.Count > Console.BufferHeight - 3)
                    {
                        LogEntries.Remove(LogEntries.First());          //keep the buffer tidy. (509 MAX)
                    }

                    if (ScreenModal && !bypassScreenLock)
                    {
                        continue;                                       //Do not output
                    }
                    Writing = true;
                    PrvTop = Console.CursorTop;
                    Console.SetCursorPosition(0, Console.CursorTop);    //Reset line position.
                    debugdump.AppendLine($"message.exception null?: {(message.Exception ==null ? "YES" : "NO")}");
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
                            Console.WriteLine(lines[i].PadRight(Console.BufferWidth - 2, '\u2000')); //write current line in queue.
                            Console.CursorTop -= 1;
                        }
                        if (i > 0)
                        {
                            //write current line in queue, padded by 21 enQuads to preserve line format.
                            Console.WriteLine(lines[i].PadLeft(lines[i].Length + 21, '\u2000').PadRight(Console.BufferWidth - 2));
                            Console.CursorTop -= 1;
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
            catch (Exception ex)
            {
                PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_RETURN, 0);
                string dump = debugdump.ToString();
                Program.RestartRequested = ShowKillScreen("Queue Failure", $"The ConsoleIO Queue process crashed. {ex.Message}\r\n\r\n{"".PadRight(64, '─')}\r\nOBJ DUMP:\r\n\r\n{dump}\r\n\r\n{"".PadRight(64, '─')}", false,
                    ref Program.ShutdownCalled, ref Program.RestartRequested, 5, ex, "CIO_QUEUE_EXCEPTION").GetAwaiter().GetResult();
                
                CurTop = 0;
                Program.ShutdownCalled = true;
                Program.ImmediateTerm = true;
            }
            

        }

        //Heavily tweaked from: https://stackoverflow.com/questions/20534318/make-console-writeline-wrap-words-instead-of-letters
        //Fixed a bug where wrap would fail if no spaces & even if space, characters longer than console width would break)
        internal string WordWrap(string paragraph, int consoleoffset = 24)
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

                returnstring += "\u2005";
            }
            return returnstring;
        }

        #region P/Invoke
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();
        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        internal static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        #endregion

        internal Task GetConsoleInput(ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET)
        {
            //Process Loop
            while (true)
            {

                if (ScreenBusy)
                {
                    continue;
                }
                if (ScreenModal)
                {
                    continue;
                }

                if (InputCanceled)
                {
                    return Task.Delay(0);
                }
                if (ShutdownCalled)
                {
                    return Task.Delay(0);
                }
                string input = Console.ReadLine();
                string unproc = input;
                if(CurTop < Console.BufferHeight)
                {
                    Console.CursorTop = CurTop;
                }

                #region Console Command Statements

                ConsoleCommand cm = ConsoleCommands.FirstOrDefault(x => x.CommandName == input.Split(' ')[0]);
                if(cm == null)
                {
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        WriteEntry(new LogMessage(LogSeverity.Info, "Console", "unknown command"), null, true, false, true);
                    }
                    else
                    {
                        Console.CursorLeft = 2;
                    }
                    continue;
                }
                else
                {
                    ConsoleIO c = this;
                    bool r = cm.Execute(input, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET, ref c);
                    if(!r)
                    {
                        break;
                    }
                }

                #endregion

                if (!ScreenBusy && !ScreenModal)
                {
                    WriteEntry(new LogMessage(LogSeverity.Info, "Console", unproc), null, true, false, true);
                }
            }
            WriteEntry(new LogMessage(LogSeverity.Info, "Console", "ConsoleInput closed"), null, true, false, true);

            return Task.Delay(1);
        }

        internal void ConsoleWriteImage(System.Drawing.Bitmap source) //SRC: Modified from https://stackoverflow.com/a/33715138/4655190
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

        #region PUBLIC Methods

        #region GUI Reset

        /// <summary>
        /// Reset the console layout using specified values
        /// </summary>
        /// <param name="fore">Text color</param>
        /// <param name="back">Background color</param>
        /// <param name="title">Console's header title (not window title)</param>
        public void ConsoleGUIReset(ConsoleColor fore, ConsoleColor back, string title)
        {
            SpinWait.SpinUntil(() => !ScreenBusy);
            ScreenBusy = true;
            if (title.Length > 72)
            {
                title = title.Remove(71) + "...";
            }
            ConsoleTitle = title;
            Console.Clear();
            int w = 144 > Console.LargestWindowWidth ? Console.LargestWindowWidth : 144;
            int h = 32 > Console.LargestWindowHeight ? Console.LargestWindowHeight : 32;
            Console.SetWindowSize(w, h);//Seems to be a reasonable console size.
            Console.SetBufferSize(w, 512);//Extra buffer room just because why not.
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
        public int ConsoleGUIReset(ConsoleColor fore, ConsoleColor back, string title, short ProgressValue, short ProgressMax, ConsoleColor ProgressColor,string META="")
        {
            int linecount = 1;
            ScreenBusy = true;
            if (title.Length > 72)
            {
                title = title.Remove(71) + "...";
            }
            ConsoleTitle = title;
            Console.Clear();
            int w = 144 > Console.LargestWindowWidth ? Console.LargestWindowWidth : 144;
            int h = 32 > Console.LargestWindowHeight ? Console.LargestWindowHeight : 32;
            
            Console.SetWindowSize(w, h);//Seems to be a reasonable console size.
            Console.SetBufferSize(w, 512);//Extra buffer room just because why not.
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
            Console.Clear();

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            DecorateTop();
            linecount++;
            string WTitle = ("" + DateTime.Now.ToString("HH:mm:ss") + " " + title + " - RMSoftwareModularBot v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            string pTitle = WTitle.PadLeft(71 + WTitle.Length / 2);
            pTitle += "".PadRight(71 - WTitle.Length / 2);
            Console.Write("\u2551{0}\u2551", pTitle);
            Console.Write("\u2551{0}\u2551", "".PadLeft(142));
            string progressBAR = "";
            linecount++;
            
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
            linecount++;
            if (!string.IsNullOrWhiteSpace(META))
            {
                string fmeta = META.PadLeft(71 + META.Length / 2);
                fmeta += "".PadRight(71 - META.Length / 2);
                if (META.Length > 80)
                {
                    throw new ArgumentException("Your meta caption can't be over 80 characters.");
                }
                Console.Write("\u2551");
                Console.ForegroundColor = ProgressColor;
                Console.Write("{0}", fmeta);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\u2551");
                linecount++;
            }
            DecorateBottom();
            linecount++;


            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;

            ConsoleBackgroundColor = back;
            ConsoleForegroundColor = fore;
            Thread.Sleep(5);
            ScreenBusy = false;
            return linecount;
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
            ConsoleTitle = title;
            Console.Clear();
            int ww = w > Console.LargestWindowWidth ? Console.LargestWindowWidth : w;
            int hh = h > Console.LargestWindowHeight ? Console.LargestWindowHeight : h;
            Console.SetWindowSize(ww, hh);
            Console.SetBufferSize(ww, hh);
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

        #endregion

        #region Entry Writing

        /// <summary>
        /// Queues a Color-coordinated Discord.NET Log message for writing to the console.
        /// </summary>
        /// <param name="message">Discord.NET Log Message to parse.</param>
        /// <param name="Entrycolor">The Left-margin color. If none specified, LogMessage.LogSeverity will be used instead.</param>
        /// <param name="showCursor">If False, the '&gt;' will not be output and line position will not increment, allowing for the message to be overwritten.</param>
        /// <param name="bypassScreenLock">If true, the message will output even if a modal screen is showing (Kill Screen/list/etc.)</param>
        /// <param name="bypassFilter">If true, the message will be processed regardless of log level configuration</param>
        public void WriteEntry(LogMessage message, ConsoleColor? Entrycolor = null, bool showCursor = true, bool bypassScreenLock = false, bool bypassFilter = false)
        {
            if (message.Severity > Program.configMGR.CurrentConfig.DiscordEventLogLevel && !bypassFilter)
            {
                return; //Do Not Queue
            }
            Backlog.Enqueue(new LogEntry(message, Entrycolor, bypassFilter, bypassScreenLock, showCursor));
        }

        /// <summary>
        /// Write a color-coordinated text message to console.
        /// </summary>
        /// <param name="message">Text to write</param>
        /// <param name="Entrycolor">Left margin color.</param>
        /// <param name="showCursor">If false the '&gt;' will not be shown after the output.</param>
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
        /// Write a "Selectable" color-coordinated text message to console.
        /// </summary>
        /// <param name="message">Text to write</param>
        /// <param name="SELECTED">If true, the text/background colors will be "inverted"</param>
        /// <param name="Entrycolor">Left margin color.</param>
        /// <param name="showCursor">If false the '&gt;' will not be shown after the output.</param>
        public void WriteEntry(string message, bool SELECTED, ConsoleColor Entrycolor = ConsoleColor.Black, bool showCursor = true)
        {


            SpinWait.SpinUntil(() => !Writing);//This will help prevent the console from being sent into a mess of garbled words.


            Writing = true;
            PrvTop = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);//Reset line position.

            string[] lines = WordWrap(message, 1).Split('\n');
            ConsoleColor bglast = ConsoleBackgroundColor;

            Writing = true;
            PrvTop = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);    //Reset line position.
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
                Console.BackgroundColor = bglast;                   //restore previous color.
                Console.ForegroundColor = ConsoleForegroundColor;   //previous FG.
                Console.Write("\u2502");                            //uileft-single │

                if (i == 0)
                {
                    if (SELECTED)
                    {
                        Console.BackgroundColor = GetInvertedColor(Console.BackgroundColor);
                        Console.ForegroundColor = GetInvertedColor(Console.ForegroundColor);

                    }
                    Console.WriteLine(lines[i].PadRight(Console.BufferWidth - 2, '\u2000')); //write current line in queue.
                    if (SELECTED)
                    {
                        Console.BackgroundColor = bglast;                   //restore previous color.
                        Console.ForegroundColor = ConsoleForegroundColor;   //previous FG.
                    }
                    Console.CursorTop -= 1;
                }
                if (i > 0)
                {
                    //write current line in queue, padded by 21 enQuads to preserve line format.
                    Console.WriteLine(lines[i].PadLeft(lines[i].Length, '\u2000').PadRight(Console.BufferWidth - 2));
                    Console.CursorTop -= 1;
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
        /// Write a "Selectable" color-coordinated text message to console.
        /// </summary>
        /// <param name="message">Text to write</param>
        /// <param name="SELECTED">If true, the text/background colors will be "inverted"</param>
        /// <param name="Entrycolor">Left margin color.</param>
        /// <param name="showCursor">If false the '&gt;' will not be shown after the output.</param>
        public void WriteEntry(string message, bool SELECTED, bool Disabled, ConsoleColor Entrycolor = ConsoleColor.Black, bool showCursor = true)
        {


            SpinWait.SpinUntil(() => !Writing);//This will help prevent the console from being sent into a mess of garbled words.


            Writing = true;
            PrvTop = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);//Reset line position.

            string[] lines = WordWrap(message, 1).Split('\n');
            ConsoleColor bglast = ConsoleBackgroundColor;

            Writing = true;
            PrvTop = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);    //Reset line position.
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
                Console.BackgroundColor = bglast;                   //restore previous color.
                Console.ForegroundColor = ConsoleForegroundColor;   //previous FG.
                Console.Write("\u2502");                            //uileft-single │

                if (i == 0)
                {
                    if (SELECTED)
                    {
                        Console.BackgroundColor = GetInvertedColor(Console.BackgroundColor);
                        Console.ForegroundColor = GetInvertedColor(Console.ForegroundColor);

                    }
                    if (Disabled) Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(lines[i].PadRight(Console.BufferWidth - 2, '\u2000')); //write current line in queue.
                    if (Disabled) Console.ForegroundColor = ConsoleForegroundColor;

                    if (SELECTED)
                    {
                        Console.BackgroundColor = bglast;                   //restore previous color.
                        Console.ForegroundColor = ConsoleForegroundColor;   //previous FG.
                    }
                    Console.CursorTop -= 1;
                }
                if (i > 0)
                {
                    //write current line in queue, padded by 21 enQuads to preserve line format.
                    Console.WriteLine(lines[i].PadLeft(lines[i].Length, '\u2000').PadRight(Console.BufferWidth - 2));
                    Console.CursorTop -= 1;
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

        #endregion

        #region Miscellaneous Screens & logs

        /// <summary>
        /// Show 'CRASH' screen with custom title and call for program termination or restart.
        /// </summary>
        /// <param name="title">Title of kill screen</param>
        /// <param name="message">the point of the kill screen</param>
        /// <param name="autorestart">True: Prompt for auto restart in timeout period</param>
        /// <param name="timeout">auto restart timeout in seconds.</param>
        /// <param name="ex">The inner exception leading to the kill screen.</param>
        /// <returns></returns>
        public Task<bool> ShowKillScreen(string title, string message, bool autorestart, ref bool ProgramShutdownFlag, ref bool ProgramRestartFlag, int timeout = 5, Exception ex = null,string source = "Unknown Source")
        {
            ScreenModal = true;
            PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_RETURN, 0);
            var NGScreen = new KillScreen(ex ?? new Exception("Undefined exception"),autorestart,source,title,message,timeout)
            {
                ActiveScreen = true
            };
            ActiveScreen = NGScreen;
            NGScreen.RenderScreen();
            while (true)
            {
                if (NGScreen.ProcessInput(Console.ReadKey(true)))
                {
                    break;
                }
            }
            NGScreen.ActiveScreen = false; ConsoleIO.ActiveScreen = null;
            if(ex!=null)
            {
                CreateCrashLog(ex, new LogMessage(LogSeverity.Critical, source, message, ex));
                WriteErrorsLog(message,ex);
            }
            Program.AppArguments.Add("-crashed");
            Program.ImmediateTerm = true;
            ScreenModal = false;
            ProgramShutdownFlag = true;
            ProgramRestartFlag = autorestart;//redundancy
            PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_RETURN, 0);
            return Task.FromResult(autorestart);//redundancy

        }

        /// <summary>
        /// Create a new Crash.LOG file
        /// </summary>
        /// <param name="ex">Exception data</param>
        /// <param name="m">Log message data</param>
        public void CreateCrashLog(Exception ex, LogMessage m)
        {
            
                using (StreamWriter sw = new StreamWriter("CRASH.LOG",false))
                {
                    sw.WriteLine(m.ToString());
                    sw.WriteLine("If you continue to get this error, please report it to the developer, including the stack below.");
                    sw.WriteLine();
                    sw.WriteLine("Developer STACK:");
                    sw.WriteLine("=================================================================================================================================");
                    sw.WriteLine(ex.StackTrace??"No stack available.");
                    sw.Flush();
                }
        }

        /// <summary>
        /// Write exception to errors.log
        /// </summary>
        /// <param name="ex"></param>
        public void WriteErrorsLog(Exception ex)
        {
            SpinWait.SpinUntil(() => !errorLogWrite);
            errorLogWrite = true;
            using (StreamWriter sw = new StreamWriter("ERRORS.LOG", true))
            {
                
                sw.WriteLine($"[{DateTime.Now:MM/dd/yyyy}]: {ex}");
                sw.Flush();
            }
            errorLogWrite = false;
        }

        /// <summary>
        /// Write message to errors log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex">Optional exception</param>
        public void WriteErrorsLog(string message, Exception ex = null)
        {
            SpinWait.SpinUntil(() => !errorLogWrite);
            errorLogWrite = true;
            using (StreamWriter sw = new StreamWriter("ERRORS.LOG", true))
            {
                sw.WriteLine($"[{DateTime.Now:MM/dd/yyyy}]: {message}");
                if (ex != null)
                {
                    sw.WriteLine($"[{DateTime.Now:MM/dd/yyyy}]: {ex}");
                }
                sw.Flush();
            }
            errorLogWrite = false;
        }

        public void ShowConsoleScreen(ConsoleScreen NGScreen, bool FromLog, bool InterruptInput=false)
        {
            
            string PRV_TITLE = ConsoleTitle;
            ScreenModal = true;
            if(InterruptInput)
            {
                //Exit the running console input read if desired
                PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_RETURN, 0);
            }
            //---------------start modal---------------
            NGScreen.ActiveScreen = true;
            ActiveScreen = NGScreen;
            NGScreen.RenderScreen();
            while (true)
            {
                if (NGScreen.ProcessInput(Console.ReadKey(true)))
                {
                    break;
                }
            }
            NGScreen.ActiveScreen = false; ActiveScreen = null;
            //----------------End modal----------------
            if(FromLog)
            {
                List<LogEntry> v = new List<LogEntry>();
                ConsoleGUIReset(Program.configMGR.CurrentConfig.ConsoleForegroundColor,
                Program.configMGR.CurrentConfig.ConsoleBackgroundColor, PRV_TITLE);
                ScreenModal = false;
                SpinWait.SpinUntil(() => !ScreenBusy);
                v.AddRange(LogEntries);
                LogEntries.Clear();//clear buffer.
                                   //output previous logEntry.
                foreach (var item in v)
                {
                    WriteEntry(item.LogMessage, item.EntryColor);
                }
            }
            
        }

        #endregion

        #endregion

        #region SUBCLASS Screen buffer
        public class LogEntry
        {
            public LogMessage LogMessage { get; set; }

            public ConsoleColor? EntryColor { get; set; }

            public bool BypassFilter { get; set; }

            public bool BypassScreenLock { get; set; }

            public bool ShowCursor { get; set; }

            public LogEntry(LogMessage msg, ConsoleColor? color, bool bypassfilt = false, bool bypassScreen = false, bool showCursor = true)
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