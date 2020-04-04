﻿using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ModularBOT.Entity
{
    public class ConsoleScreen
    {
        #region Private Values
        private int _prgVal = 0;
        private int CurTop;
        private bool Writing;
        private int PrvTop;
        private bool QueueProcessStarted;

        #endregion

        #region Public Properties

        public string Title { get; set; } = "Untitled Screen";
        public string Meta { get; set; } = "Default Meta";

        public int ProgressMax { get; set; } = 100;
        public int ProgressVal
        {
            get
            {
                return _prgVal;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("ProgressVal", value, "Your value must be at least zero.");
                if (value > ProgressMax) throw new ArgumentOutOfRangeException("ProgressVal", value, "Your value must be not exceed ProgressMax.");
                _prgVal = value;
            }
        }

        public int WindowWidth { get; set; } = 144;
        public int WindowHeight { get; set; } = 32;

        public int BufferWidth { get; set; } = 144;
        public int BufferHeight { get; set; } = 32;

        protected int ContentTop { get; private set; }

        public ConsoleColor ScreenFontColor { get; set; } = ConsoleColor.Cyan;
        public ConsoleColor ScreenBackColor { get; set; } = ConsoleColor.Black;

        public ConsoleColor TitlesFontColor { get; set; } = ConsoleColor.Cyan;
        public ConsoleColor TitlesBackColor { get; set; } = ConsoleColor.Black;

        public ConsoleColor ProgressColor { get; set; } = ConsoleColor.Green;

        public ConsoleColor MetaFontColor { get; set; } = ConsoleColor.Yellow;

        /// <summary>
        /// Entry Queue for log entry writing. This buffer only applies if you use derived screen as a log
        /// </summary>
        public Queue<LogEntry> Backlog { get; set; } = new Queue<LogEntry>();
        /// <summary>
        /// Entry Back Buffer for log entries to be re-written 
        /// </summary>
        public List<LogEntry> LogEntries { get; set; } = new List<LogEntry>();
        /// <summary>
        /// Is the screen currently being written to the console?
        /// </summary>
        public bool ActiveScreen { get; set; }

        /// <summary>
        /// Is the screen currently outputting entries from the buffer?
        /// </summary>
        public bool WritingContents { get; private set; }

        /// <summary>
        /// Are screen components being updated? [Progress bars, titles, meta, borders, etc.]
        /// </summary>
        public bool LayoutUpdating { get; protected set; }

        public bool ActivePrompt { get; protected set; }

        public bool ShowProgressBar { get; set; }

        public bool ShowMeta { get; set; }

        public bool IsLogScreen { get; set; } = false;

        #endregion

        #region internal/private/protected methods

        #region Bordering and formatting

        private void DecorateTop()
        {
            for (int i = 0; i < WindowWidth; i++)
            {
                if (i == 0)
                {
                    Console.Write("\u2554");
                    continue;
                }

                if (i == WindowWidth - 1)
                {
                    Console.Write("\u2557");
                    break;
                }
                Console.Write("\u2550");
            }
        }

        private void DecorateBottom()
        {
            for (int i = 0; i < WindowWidth; i++)
            {
                if (i == 0)
                {
                    Console.Write("\u255A");
                    continue;
                }
                if (i == 1)
                {
                    Console.Write("\u2566");
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

        private string WordWrap(string paragraph, int consoleoffset = 24)
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

        private ConsoleColor GetInvertedColor(ConsoleColor Color) //I realize this is not accurate.
        {
            return (ConsoleColor)(Math.Abs((Color - ConsoleColor.White)));
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
        protected void WriteEntry(LogMessage message, ConsoleColor? Entrycolor = null, bool showCursor = true, bool bypassScreenLock = false, bool bypassFilter = false)
        {
            if (message.Severity > Program.configMGR.CurrentConfig.DiscordEventLogLevel)
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
        protected void WriteEntry(string message, ConsoleColor Entrycolor = ConsoleColor.Black, bool showCursor = true, ConsoleColor BorderColor = ConsoleColor.White, ConsoleColor? SEntrycolor = null)
        {
            SpinWait.SpinUntil(() => !Writing);//This will help prevent the console from being sent into a mess of garbled words.
            Writing = true;
            PrvTop = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);//Reset line position.
            string[] lines = WordWrap(message, 1).Split('\n');
            ConsoleColor bglast = ScreenBackColor;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length == 0)
                {
                    continue;
                }
                ConsoleColor bg = ConsoleColor.Black;
                ConsoleColor fg = ConsoleColor.Black;
                bg = Entrycolor;
                fg = SEntrycolor ?? Entrycolor;
                Console.BackgroundColor = bg;
                Console.ForegroundColor = fg;

                Console.Write((char)9617);//Write the colored space.
                Console.BackgroundColor = bglast;//restore previous color.
                Console.ForegroundColor = BorderColor;//white UI border.
                Console.Write("\u2551");
                Console.ForegroundColor = ScreenFontColor;//reset font.

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

            Console.BackgroundColor = ScreenBackColor;
            Console.ForegroundColor = ScreenFontColor;
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
        protected void WriteEntry(string message, bool SELECTED, ConsoleColor Entrycolor = ConsoleColor.Black, bool showCursor = true, ConsoleColor BorderColor = ConsoleColor.White, ConsoleColor? SEntrycolor = null)
        {
            SpinWait.SpinUntil(() => !Writing);//This will help prevent the console from being sent into a mess of garbled words.
            Writing = true;
            PrvTop = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);//Reset line position.

            string[] lines = WordWrap(message, 1).Split('\n');
            ConsoleColor bglast = ScreenBackColor;

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
                fg = SEntrycolor ?? Entrycolor;
                Console.BackgroundColor = bg;
                Console.ForegroundColor = fg;
                //Thread.Sleep(1);//safe.
                Console.Write((char)9617);//Write the colored space.
                Console.BackgroundColor = bglast;                   //restore previous color.
                Console.ForegroundColor = BorderColor;   //previous FG.
                Console.Write("\u2551");                            //uileft-double ║
                Console.ForegroundColor = ScreenFontColor;   //previous FG.

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
                        Console.ForegroundColor = ScreenFontColor;   //previous FG.
                    }
                    Console.CursorTop = Console.CursorTop - 1;
                }
                if (i > 0)
                {
                    //write current line in queue, padded by 21 enQuads to preserve line format.
                    Console.WriteLine(lines[i].PadLeft(lines[i].Length, '\u2000').PadRight(Console.BufferWidth - 2));
                    Console.CursorTop = Console.CursorTop - 1;
                }

            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            if (showCursor)
            {
                Console.Write(">");//Write the input indicator.

            }

            Console.BackgroundColor = ScreenBackColor;
            Console.ForegroundColor = ScreenFontColor;
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
        protected void WriteEntry(string message, bool SELECTED, bool Disabled, ConsoleColor Entrycolor = ConsoleColor.Black, bool showCursor = true, ConsoleColor BorderColor = ConsoleColor.White, ConsoleColor? SEntrycolor = null)
        {
            SpinWait.SpinUntil(() => !Writing);//This will help prevent the console from being sent into a mess of garbled words.
            Writing = true;
            PrvTop = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);//Reset line position.

            string[] lines = WordWrap(message, 1).Split('\n');
            ConsoleColor bglast = ScreenBackColor;

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
                fg = SEntrycolor ?? Entrycolor;
                Console.BackgroundColor = bg;
                Console.ForegroundColor = fg;
                //Thread.Sleep(1);//safe.
                Console.Write((char)9617);//Write the colored space.
                Console.BackgroundColor = bglast;                   //restore previous color.
                Console.ForegroundColor = BorderColor;   //previous FG.
                Console.Write("\u2551");                            //uileft-double ║
                Console.ForegroundColor = ScreenFontColor;   //previous FG.

                if (i == 0)
                {
                    if (SELECTED)
                    {
                        Console.BackgroundColor = GetInvertedColor(Console.BackgroundColor);
                        Console.ForegroundColor = GetInvertedColor(Console.ForegroundColor);

                    }
                    if (Disabled) Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(lines[i].PadRight(Console.BufferWidth - 2, '\u2000'));
                    if (Disabled) Console.ForegroundColor = ScreenFontColor;

                    if (SELECTED)
                    {
                        Console.BackgroundColor = bglast;           //restore previous color.
                        Console.ForegroundColor = ScreenFontColor;  //previous FG.
                    }
                    Console.CursorTop = Console.CursorTop - 1;
                }
                if (i > 0)
                {
                    //write current line in queue, padded by 21 enQuads to preserve line format.
                    Console.WriteLine(lines[i].PadLeft(lines[i].Length, '\u2000').PadRight(Console.BufferWidth - 2));
                    Console.CursorTop = Console.CursorTop - 1;
                }

            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            if (showCursor)
            {
                Console.Write(">");//INPUT PROMPT

            }

            Console.BackgroundColor = ScreenBackColor;
            Console.ForegroundColor = ScreenFontColor;
            Console.CursorVisible = showCursor;
            CurTop = Console.CursorTop;
            Writing = false;
        }

        #endregion

        protected void ProcessQueue()
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
                SpinWait.SpinUntil(() => !Writing);
                SpinWait.SpinUntil(() => ActiveScreen);
                SpinWait.SpinUntil(() => Backlog.Count > 0);
                if (LayoutUpdating) { continue; }                   //If the screen's busy (Resetting), DO NOT DQ!
                if (Writing) { continue; }                          //If the console is in the middle of writing, DO NOT DQ!
                if(!ActiveScreen) { continue; }                     //If the screen is not the active screen, DO NOT DQ!

                LogEntry qitem = Backlog.Dequeue();                 //DQ the item and process it as qitem.
                LogMessage message = qitem.LogMessage;              //Entry's log message data.
                ConsoleColor? Entrycolor = qitem.EntryColor;        //left margin color

                bool bypassFilter = qitem.BypassFilter;             //will this entry obey application log level?
                bool bypassScreenLock = qitem.BypassScreenLock;     //will this entry show up through a modal screen?
                bool showCursor = qitem.ShowCursor;                 //will this entry output and show the console cursor?

                LogEntries.Add(new LogEntry(message, Entrycolor));  //Add the entry to buffer. Ignore screen modal, for outputting when modal is closed.

                if (LogEntries.Count > Console.BufferHeight - 3)
                {
                    LogEntries.Remove(LogEntries.First());          //keep the buffer tidy. (509 MAX)
                }

               
                Writing = true;
                PrvTop = Console.CursorTop;
                Console.SetCursorPosition(0, Console.CursorTop);    //Reset line position.
                LogMessage l = new LogMessage(message.Severity,
                    message.Source.PadRight(11, '\u2000'),
                    message.Message, message.Exception);
                string[] lines = WordWrap(l.ToString()).Split('\n');
                ConsoleColor bglast = ScreenBackColor;
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
                    Console.ForegroundColor = ScreenFontColor;          //previous FG.
                    Console.Write("\u2551");                            //uileft ║

                    if (i == 0)
                    {
                        Console.WriteLine(lines[i].PadRight(Console.BufferWidth - 2, '\u2000')); //write current line in queue.
                        Console.CursorTop = Console.CursorTop - 1;
                    }
                    if (i > 0)
                    {
                        //write current line in queue, padded by 21 enQuads to preserve line format.
                        Console.WriteLine(lines[i].PadLeft(lines[i].Length + 21, '\u2000').PadRight(Console.BufferWidth - 2));
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
                Console.BackgroundColor = ScreenBackColor;
                Console.ForegroundColor = ScreenFontColor;
                Console.CursorVisible = showCursor;
                if (showCursor)
                {
                    Console.Write("\u2551");
                }

                CurTop = Console.CursorTop;
                Writing = false;
            }

        }

        protected int UpdateProgressBar(int linecount)
        {
            linecount = 2;
            if (ShowProgressBar)
            {
                Console.ForegroundColor = TitlesFontColor;
                Console.BackgroundColor = TitlesBackColor;
                Console.Write("\u2551{0}\u2551", "".PadLeft(142));

                linecount++;//3
                string progressBAR = "";
                float f = (float)(ProgressVal / (float)ProgressMax);

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
                progressBAR += $" PAGE {ProgressVal} OF {ProgressMax}";
                string pbar = progressBAR.PadLeft(71 + progressBAR.Length / 2);
                pbar += "".PadRight(71 - progressBAR.Length / 2);
                Console.Write("\u2551");
                Console.ForegroundColor = ProgressColor;
                Console.Write("{0}", pbar);
                Console.ForegroundColor = TitlesFontColor;
                Console.Write("\u2551");

                linecount++;//4
            }

            return linecount;
        }
        #endregion

        #region overridden methods.

        protected virtual void RenderContents()
        {
            //Derive and Override.
            Console.WriteLine("test");
        }
        private void _OSS_RenderOptions(string option1, string option2, string option3, string option4, int selectionindex, int cl, int ct) //Sub-screen options...
        {
            #region Option Trimming
            if (option1.Length > 37)
            {
                option1 = option1.Remove(34) + "...";
            }
            if (option2.Length > 37)
            {
                option2 = option2.Remove(34) + "...";
            }
            if (option3.Length > 37)
            {
                option3 = option3.Remove(34) + "...";
            }
            if (option4.Length > 37)
            {
                option4 = option4.Remove(34) + "...";
            }
            #endregion
            string[] options = { "- " + option1, "- " + option2, "- " + option3, "- " + option4 };
            int curleft = cl;
            int curtop = ct;

            for (int i = 0; i < 4; i++)
            {

                Console.CursorLeft = curleft;
                Console.CursorTop = curtop + i;
                ConsoleColor bg = Console.BackgroundColor;
                ConsoleColor fg = Console.ForegroundColor;
                Console.Write("\u2502 ");

                if (i == selectionindex)
                {

                    Console.BackgroundColor = fg;
                    Console.ForegroundColor = bg;
                }
                if (options[i] == "- -")
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("\u2550".PadRight(38, '\u2550').PadRight(38));
                    Console.ForegroundColor = fg;
                }
                //if (options[i] == "-")
                //{
                //    Console.Write("\u2005".PadRight(38, '\u2005').PadRight(38));
                //}
                else
                {
                    Console.Write(options[i].PadRight(38));
                }

                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;
                Console.Write(" \u2502");

            }
            Console.CursorTop = curtop;
            Console.CursorLeft = curleft;

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Console Manager will render screen.
        /// </summary>
        public void RenderScreen()
        {
            
            LayoutUpdating = true;
            int linecount = 0;

            #region Primary Clear
            Console.Clear();
            Console.SetBufferSize(BufferWidth, BufferHeight);

            #region Window Height buffering
            if (WindowWidth > Console.LargestWindowWidth)
            {
                WindowWidth = Console.LargestWindowWidth;
            }
            if (WindowHeight > Console.LargestWindowHeight)
            {
                WindowHeight = Console.LargestWindowHeight;
            }
            #endregion

            Console.SetWindowSize(WindowWidth, WindowHeight);

            Console.BackgroundColor = ScreenBackColor;
            Console.ForegroundColor = ScreenFontColor;

            Console.Clear();

            #endregion

            #region Title Segment
            Console.BackgroundColor = TitlesBackColor;
            Console.ForegroundColor = TitlesFontColor;

            DecorateTop();

            linecount++;//1

            string WTitle = Title;
            string pTitle = WTitle.PadLeft(71 + WTitle.Length / 2);
            pTitle += "".PadRight(71 - WTitle.Length / 2);
            Console.Write("\u2551{0}\u2551", pTitle);
            linecount++;//2

            linecount = UpdateProgressBar(linecount);

            if (ShowMeta && !string.IsNullOrWhiteSpace(Meta))
            {
                string fmeta = Meta.PadLeft(71 + Meta.Length / 2);
                fmeta += "".PadRight(71 - Meta.Length / 2);
                if (Meta.Length > 120)
                {
                    throw new ArgumentException("Your meta caption can't be over 120 characters.");
                }
                Console.ForegroundColor = TitlesFontColor;
                Console.Write("\u2551");
                Console.ForegroundColor = MetaFontColor;
                Console.Write("{0}", fmeta);
                Console.ForegroundColor = TitlesFontColor;
                Console.Write("\u2551");
                linecount++;//5
            }

            DecorateBottom();
            linecount++;//6
            ContentTop = linecount;
            Console.ForegroundColor = ScreenFontColor;

            #endregion

            #region Sidebar
            int ct = linecount;
            for (int i = ct; i < BufferHeight; i++)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Black;
                //Thread.Sleep(1);
                Console.Write((char)9617);//Write the colored space.
                                          //Thread.Sleep(1);
                Console.BackgroundColor = ScreenBackColor;//restore previous color.
                Console.ForegroundColor = ConsoleColor.White;
                //Thread.Sleep(1);
                Console.Write("\u2551");//uileft
                                        //Thread.Sleep(1);
                Console.CursorTop = i;
                Console.CursorLeft = 0;
            }
            Console.CursorTop = 0;

            #endregion

            Console.CursorTop = ct;
            Console.ForegroundColor = ScreenFontColor;

            LayoutUpdating = false;
            RenderContents();
        }


        public virtual bool ProcessInput(ConsoleKeyInfo keyinfo)
        {
            if (keyinfo.Key == ConsoleKey.Escape)
            {
                ActiveScreen = false;
            }
            return keyinfo.Key == ConsoleKey.Escape;
            //Derive and Override.
        }

        public int ShowOptionSubScreen(string title, string prompt, string Option1, string Option2, string Option3, string Option4, ConsoleColor SBG = ConsoleColor.DarkBlue, ConsoleColor SFG = ConsoleColor.White)
        {
            ActivePrompt = true;
            Console.CursorVisible = false;
            int result = -1;
            int SelIndex = 0;
            int left = 71 - 20;
            int top = 16 - 7;

            List<int> SelectableIndicies = new List<int>();
            Console.CursorLeft = left;
            Console.CursorTop = top;
            ConsoleColor PRVBG = Console.BackgroundColor;
            ConsoleColor PRVFG = Console.ForegroundColor;
            Console.BackgroundColor = SBG;
            Console.ForegroundColor = SFG;

            if (string.IsNullOrWhiteSpace(Option1) || string.IsNullOrWhiteSpace(Option2) || string.IsNullOrWhiteSpace(Option3) || string.IsNullOrWhiteSpace(Option4))
            {
                throw (new ArgumentException("We cannot support empty text at this time."));
            }

            #region TOP
            if (title.Length > 35)
            {
                title = title.Remove(32) + "...";
            }

            string WTitle = " " + title + " ";
            string pTitle = WTitle.PadLeft(((40 / 2)) + WTitle.Length / 2, '\u2550');
            pTitle += "".PadRight(((40 / 2)) - WTitle.Length / 2, '\u2550');
            Console.Write("\u2552{0}\u2555", pTitle);
            #endregion

            #region Option Filtering
            if (Option1 != "-" && Option1 != "")
            {
                SelectableIndicies.Add(0);
            }
            if (Option2 != "-" && Option2 != "")
            {
                SelectableIndicies.Add(1);
            }
            if (Option3 != "-" && Option3 != "")
            {
                SelectableIndicies.Add(2);
            }
            if (Option4 != "-" && Option4 != "")
            {
                SelectableIndicies.Add(3);
            }
            if (SelectableIndicies.Count < 1)
            {
                throw (new ArgumentException("You must have at least ONE selectable option"));
            }
            #endregion

            #region Prompt
            Console.CursorLeft = left;
            Console.CursorTop = top + 1;
            if (prompt.Length > 40)
            {
                prompt = prompt.Remove(36) + "...";
            }

            Console.Write("\u2502 " + "".PadRight(39) + "\u2502");
            Console.CursorLeft = left;
            Console.CursorTop = top + 2;
            Console.Write("\u2502 " + prompt.PadRight(39) + "\u2502");
            Console.CursorLeft = left;
            Console.CursorTop = top + 3;
            Console.Write("\u2502 " + "".PadRight(39) + "\u2502");

            #endregion

            #region Options and status
            Console.CursorLeft = left;
            Console.CursorTop = top + 4;
            _OSS_RenderOptions(Option1, Option2, Option3, Option4, SelectableIndicies[SelIndex], left, top + 4);
            Console.CursorLeft = left;
            Console.CursorTop = top + 8;
            Console.Write("\u2502 " + "".PadRight(39) + "\u2502");
            Console.CursorLeft = left;
            Console.CursorTop = top + 9;
            Console.Write("\u2502 " + "[UP/DOWN]: Move Selection".PadRight(39) + "\u2502");
            Console.CursorLeft = left;
            Console.CursorTop = top + 10;
            Console.Write("\u2502 " + "[ENTER]: Confirm | [ESC]: Cancel".PadRight(39) + "\u2502");
            Console.CursorLeft = left;
            Console.CursorTop = top + 11;
            Console.Write("\u2502 " + "".PadRight(39) + "\u2502");
            Console.CursorLeft = left;
            Console.CursorTop = top + 12;
            Console.Write("\u2514" + "".PadRight(40, '\u2500') + "\u2518");
            int minLeft = left;
            int maxleft = Console.CursorLeft;
            int mintop = top;
            int maxtop = top + 13;
            #endregion

            #region Control handler
            while (true)
            {
                ConsoleKeyInfo c = Console.ReadKey(true);


                if (c.Key == ConsoleKey.UpArrow || c.Key == ConsoleKey.PageUp)
                {

                    SelIndex--;
                    if (SelIndex < 0)
                    {
                        SelIndex = SelectableIndicies.Count - 1;
                    }
                    _OSS_RenderOptions(Option1, Option2, Option3, Option4, SelectableIndicies[SelIndex], left, top + 4);

                }

                if (c.Key == ConsoleKey.DownArrow || c.Key == ConsoleKey.PageDown)
                {

                    SelIndex++;
                    if (SelIndex >= SelectableIndicies.Count)
                    {
                        SelIndex = 0;
                    }
                    _OSS_RenderOptions(Option1, Option2, Option3, Option4, SelectableIndicies[SelIndex], left, top + 4);

                }
                if (c.Key == ConsoleKey.Enter)
                {
                    result = SelectableIndicies[SelIndex] + 1;

                    //RenderScreen();
                    break;
                }
                if (c.Key == ConsoleKey.Escape)
                {
                    result = 0;//NON-SEL
                    //RenderScreen();
                    break;
                }

            }
            #endregion

            ActivePrompt = false;
            RenderScreen();
            return result;
        }


        #endregion

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
    }
}
