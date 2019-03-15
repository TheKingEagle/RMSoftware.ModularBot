// Pastebin #SELLOUT Links [LOL]
//https://rms0.org?a=r1 - Rewards1 paid surveys.
//https://twitch.tv/TheKingEagle - Twitch streaming sometimes.
//https://rmsoftware.org - The entire reason I exist on this planet.

using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RMSoftware.ModularBot
{
    public class ConsoleLogWriter
    {
        public bool Busy { get; private set; }

        public ConsoleLogWriter()
        {
            Busy = false;
        }

        //Heavily tweaked from: https://stackoverflow.com/questions/20534318/make-console-writeline-wrap-words-instead-of-letters
        //Fixed a bug where wrap would fail if no spaces & even if space, characters longer than console width would break)
        public static string WordWrap(string paragraph)
        {
            paragraph = new Regex(@" {2,}").Replace(paragraph.Trim(), @" ");
            //paragraph = new Regex(@"\r\n{2,}").Replace(paragraph.Trim(), @" ");
            //paragraph = new Regex(@"\r{2,}").Replace(paragraph.Trim(), @" ");
            var lines = new List<string>();
            string returnstring = "";
            int i = 0;
            while (paragraph.Length > 0)
            {
                lines.Add(paragraph.Substring(0, Math.Min(Console.WindowWidth-23, paragraph.Length)));
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

                if (length == -1 && lines[i].Length > Console.WindowWidth - 23) //23 (█00:00:00 MsgSource00)
                {
                    int l = Console.WindowWidth - 23;
                    lines[i] = lines[i].Remove(l);
                    //lines[i] = lines[i].Remove(l).PadRight(Console.WindowWidth-2,'\u2000');
                }
                if (length > 20 && paragraph.Length > Console.WindowWidth - 23)
                {
                    lines[i] = lines[i].Remove(length);

                    //lines[i] = lines[i].Remove(length).PadRight(Console.WindowWidth - 2, '\u2000');
                }
                paragraph = paragraph.Substring(Math.Min(lines[i].Length, paragraph.Length));
                returnstring += (lines[i].Trim())+"\n";
                i++;
            }
            if (lines.Count > 1)
            {

                returnstring += "\u00a0";
            }
            return returnstring;
        }

        /// <summary>
        /// Write a color cordinated log message to console. Function is intended for full mode. Not '-log_only'.
        /// </summary>
        /// <param name="message">The Discord.NET Log message</param>
        /// <param name="Entrycolor">An optional entry color. If none (or black), the message.LogSeverity is used for color instead.</param>
        public void WriteEntry(LogMessage message,ConsoleColor Entrycolor=ConsoleColor.Black, bool showGT= true)
        {
            if(Program.LOG_ONLY_MODE)
            {
                Console.WriteLine(JsonConvert.SerializeObject(message));
                return;
            }
            if (Busy)
            {
                SpinWait.SpinUntil(() => !Busy);//This will help prevent the console from being sent into a mess of garbled words.
            }
            Busy = true;

            Console.SetCursorPosition(0, Console.CursorTop);//Reset line position.
            LogMessage l = new LogMessage(message.Severity, message.Source.PadRight(11, '\u2000'), message.Message, message.Exception);
            string[] lines = WordWrap(l.ToString()).Split('\n');
            ConsoleColor bglast = Program.ConsoleBackgroundColor;



            
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
                            fg = ConsoleColor.DarkGray;
                            bg = ConsoleColor.DarkGray;
                            break;
                        case LogSeverity.Debug:
                            fg = ConsoleColor.DarkMagenta;
                            bg = ConsoleColor.DarkMagenta;
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
                Console.ForegroundColor = Program.ConsoleForegroundColor;
                Thread.Sleep(1);//safe.
                if(i==0)
                {
                    Console.WriteLine(lines[i]);//write current line in queue.
                }
                if (i > 0)
                {
                    Console.WriteLine(lines[i].PadLeft(lines[i].Length+21,'\u2000'));//write current line in queue, padded by 21 enQuads to preserve line format.
                }

            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            if(showGT)
            {
                Console.Write(">");//Write the input indicator.
            }
            //Program.CursorPTop = Console.CursorTop;//Set the cursor position, this will delete ALL displayed input from console when it is eventually reset.
            Thread.Sleep(1);//safe.
            Console.BackgroundColor = Program.ConsoleBackgroundColor;
            Console.ForegroundColor = Program.ConsoleForegroundColor;
            Busy = false;
        }

        
    }
}
