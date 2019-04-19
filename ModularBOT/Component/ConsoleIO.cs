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
namespace ModularBOT.Component
{
    public class ConsoleIO
    {
        public bool Busy { get; private set; }
        public List<string> ARGS { get; private set; }

        public int CurTop { get; private set; }
        public int PrvTop { get; private set; }

        private string currentTitle = "";
        private ConsoleColor ConsoleForegroundColor = ConsoleColor.Gray;
        private ConsoleColor ConsoleBackgroundColor = ConsoleColor.Black;

        public ConsoleIO(List<string> ProgramArgs)
        {
            Busy = false;
            ARGS = ProgramArgs;
        }

        /// <summary>
        /// Reset the console layout using specified values
        /// </summary>
        /// <param name="fore">Text color</param>
        /// <param name="back">Background color</param>
        /// <param name="title">Console's header title (not window title)</param>
        public void ConsoleGUIReset(ConsoleColor fore, ConsoleColor back, string title)
        {


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

            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;

            ConsoleBackgroundColor = back;
            ConsoleForegroundColor = fore;
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
            progressBAR += $" STEP {ProgressValue} OF {ProgressMax}";
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
        /// Write a color cordinated log message to console. Function is intended for full mode.
        /// </summary>
        /// <param name="message">The Discord.NET Log message</param>
        /// <param name="Entrycolor">An optional entry color. If none (or black), the message.LogSeverity is used for color instead.</param>
        public void WriteEntry(LogMessage message, ConsoleColor Entrycolor = ConsoleColor.Black, bool showCursor = true)
        {

            if (Busy)
            {
                SpinWait.SpinUntil(() => !Busy);//This will help prevent the console from being sent into a mess of garbled words.
            }
            Busy = true;
            PrvTop = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);//Reset line position.
            LogMessage l = new LogMessage(message.Severity, message.Source.PadRight(11, '\u2000'), message.Message, message.Exception);
            string[] lines = WordWrap(l.ToString()).Split('\n');
            ConsoleColor bglast = ConsoleBackgroundColor;




            for (int i = 0; i < lines.Length; i++)
            {

                if (lines[i].Length == 0)
                {
                    continue;
                }
                ConsoleColor bg = ConsoleColor.Black;
                ConsoleColor fg = ConsoleColor.Black;
                #region setup entry color.
                if (Entrycolor == ConsoleColor.Black)
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
                    bg = Entrycolor;
                    fg = Entrycolor;
                }
                #endregion

                Console.BackgroundColor = bg;
                Console.ForegroundColor = fg;
                Console.Write((char)9617);//Write the colored space.
                Console.BackgroundColor = bglast;//restore previous color.
                Console.ForegroundColor = ConsoleForegroundColor;
                Console.Write("\u2551");//uileft
                Thread.Sleep(1);//safe.
                if (i == 0)
                {
                    Console.WriteLine(lines[i]);//write current line in queue.
                }
                if (i > 0)
                {
                    Console.WriteLine(lines[i].PadLeft(lines[i].Length + 21, '\u2000'));//write current line in queue, padded by 21 enQuads to preserve line format.
                }

            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            if (showCursor)
            {
                Console.Write(">");//Write the input indicator.

            }
            //Program.CursorPTop = Console.CursorTop;//Set the cursor position, this will delete ALL displayed input from console when it is eventually reset.
            Thread.Sleep(1);//safe.
            Console.BackgroundColor = ConsoleBackgroundColor;
            Console.ForegroundColor = ConsoleForegroundColor;
            Console.CursorVisible = showCursor;
            if (showCursor)
            {
                Console.Write("\u2551");
            }

            Busy = false;
            CurTop = Console.CursorTop;
        }

        /// <summary>
        /// Write a color cordinated log message to console. Function is intended for full mode.
        /// </summary>
        /// <param name="message">The Discord.NET Log message</param>
        /// <param name="Entrycolor">An optional entry color. If none (or black), the message.LogSeverity is used for color instead.</param>
        public void WriteEntry(string message, ConsoleColor Entrycolor = ConsoleColor.Black, bool showCursor = true)
        {

            if (Busy)
            {
                SpinWait.SpinUntil(() => !Busy);//This will help prevent the console from being sent into a mess of garbled words.
            }
            PrvTop = Console.CursorTop;
            Busy = true;

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
                Console.Write((char)9617);//Write the colored space.
                Console.BackgroundColor = bglast;//restore previous color.
                Console.ForegroundColor = ConsoleForegroundColor;
                Thread.Sleep(1);//safe.
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
            if (showCursor)
            {
                Console.Write(">");//Write the input indicator.

            }
            //Program.CursorPTop = Console.CursorTop;//Set the cursor position, this will delete ALL displayed input from console when it is eventually reset.
            Thread.Sleep(1);//safe.
            Console.BackgroundColor = ConsoleBackgroundColor;
            Console.ForegroundColor = ConsoleForegroundColor;
            Console.CursorVisible = showCursor;
            Busy = false;
            CurTop = Console.CursorTop;
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
        public Task<bool> ShowKillScreen(string title, string message, bool autorestart, ref bool ShutdownCalled, int timeout = 5, Exception ex = null)
        {
            ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, title);
            WriteEntry(new LogMessage(LogSeverity.Critical, "MAIN", "The program encountered a problem, and was terminated. Details below."));
            LogMessage m = new LogMessage(LogSeverity.Critical, "CRITICAL", message);
            WriteEntry(m);

            WriteEntry(new LogMessage(LogSeverity.Info, "MAIN", "writing error report to CRASH.LOG"));
            CreateCrashLog(ex, m);
            WriteEntry(new LogMessage(LogSeverity.Info, "MAIN", "Writing additional information to ERRORS.LOG"));
            WriteErrorsLog(ex);

            if (!autorestart)
            {
                WriteEntry(new LogMessage(LogSeverity.Info, "MAIN", "Press any key to terminate..."), ConsoleColor.Black, true);
                Console.ReadKey();
            }
            else
            {
                //prompt for autorestart.
                for (int i = 0; i < timeout; i++)
                {
                    int l = Console.CursorLeft;
                    int t = Console.CursorTop;

                    WriteEntry(new LogMessage(LogSeverity.Critical, "MAIN", $"Restarting in {timeout - i} second(s)..."), ConsoleColor.Black, false);

                    Console.CursorLeft = l;
                    Console.CursorTop = t;//reset.
                    Thread.Sleep(1000);
                }
                List<string> restart_args = new List<string>();
                restart_args.AddRange(ARGS);
                if (!restart_args.Contains("-crashed"))
                {
                    restart_args.Add("-crashed");
                }
                ARGS = restart_args;

            }
            ShutdownCalled = true;
            return Task.FromResult(autorestart);

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

                }
            }
        }

        /// <summary>
        /// Process console commands
        /// </summary>
        internal void GetConsoleInput(ref bool ShutdownCalled, ref bool RestartRequested, ref DiscordNET discordNET)
        {
            ulong chID = 0;

            while (true)
            {
                string input = Console.ReadLine();
                Console.CursorTop = CurTop;
                WriteEntry(new LogMessage(LogSeverity.Info, "Console", input));
                if (input.ToLower() == "stopbot")
                {
                    WriteEntry(new LogMessage(LogSeverity.Critical, "MAIN", "Console session called STOPBOT."));

                    discordNET.Stop(ref ShutdownCalled);
                    RestartRequested = false;
                    break;
                }
                if (input.ToLower() == "rskill")
                {

                    RestartRequested = ShowKillScreen("Test KS", "The program was instructed to run a test killscreen. This will auto restart the program.", true, ref ShutdownCalled, 5, new ApplicationException("Command rskill triggered kill screen. USER INITIATED CRASH SCREEN.")).GetAwaiter().GetResult();
                    break;
                }
                if (input.ToLower() == "cls" || input.ToLower() == "clear")
                {
                    ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, currentTitle);//default
                    WriteEntry(new LogMessage(LogSeverity.Info, "Console", "Console cleared!!"));
                }
                if (input.ToLower() == "tskill")
                {
                    RestartRequested = ShowKillScreen("Test KS", "The program was instructed to run a test killscreen. This will prompt you to terminate the program.", false, ref ShutdownCalled, 5, new ApplicationException("Command rskill triggered kill screen. USER INITIATED CRASH SCREEN.")).GetAwaiter().GetResult();
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
                    discordNET.Client.SetGameAsync("for commands!",null,ActivityType.Watching);
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
                if (input.StartsWith("setgch"))
                {
                    input = input.Remove(0, 6).Trim();
                    if (!ulong.TryParse(input, out chID))
                    {
                        WriteEntry(new LogMessage(LogSeverity.Error, "Console", "Invalid ULONG."));
                        continue;
                    }
                    WriteEntry(new LogMessage(LogSeverity.Error, "Console", "Set guild channel id."));

                }
                if (input.StartsWith("conmsg"))
                {
                    input = input.Remove(0, 6).Trim();
                    if (!(discordNET.Client.GetChannel(chID) is SocketTextChannel Channel))
                    {
                        WriteEntry(new LogMessage(LogSeverity.Error, "Console", "Invalid channel."));
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
                    discordNET.customCMDMgr.coreScript.Set(varname, input);
                }

            }

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
    }
}