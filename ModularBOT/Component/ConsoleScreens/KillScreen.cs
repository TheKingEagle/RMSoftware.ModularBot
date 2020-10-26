using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Entity;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;
using System.Net;
using System.IO;
using System.Runtime.CompilerServices;
using System.IO.Ports;

namespace ModularBOT.Component.ConsoleScreens
{
    public class KillScreen : ConsoleScreen
    {


        string ErrorDeet = "";
        string EVSource = "";
        int TimeOut = 5;
        bool AutoReboot = false;
        Exception exception = null;
        bool timedout = false;
        public KillScreen(Exception ex, bool autorestart, string source, string title, string message, int timeout=5)
        {
            ScreenFontColor = ConsoleColor.White;
            ScreenBackColor = ConsoleColor.Black;
            TitlesBackColor = ConsoleColor.Black;
            TitlesFontColor = ConsoleColor.White;
            ProgressColor = ConsoleColor.Green;
            MetaFontColor = ConsoleColor.Red;
            Title = $"{title} | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version}";

            Meta = $"Something went wrong...";
            ShowProgressBar = false;
            ShowMeta = true;
            ProgressVal = 1;
            ProgressMax = 1;
            BufferHeight = 34;
            WindowHeight = 32;

            ErrorDeet = message;
            EVSource = source;
            TimeOut = timeout;
            AutoReboot = autorestart;
            exception = ex;
        }
        #region P/Invoke
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();
        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        internal static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        #endregion
        public override bool ProcessInput(ConsoleKeyInfo keyinfo)
        {
            
            if(keyinfo.Key == ConsoleKey.Escape)
            {
                return true;
            }
            if(AutoReboot)
            {

                if(keyinfo.Key == ConsoleKey.Enter && timedout)
                {
                    return true;
                }
            }
            return base.ProcessInput(keyinfo);
        }

        protected override void RenderContents()
        {
            
            SpinWait.SpinUntil(() => !LayoutUpdating);
            int width = 140;
            Console.CursorLeft = ((140 / 2) - (width / 2)) + 2;
            Console.ForegroundColor = ConsoleColor.White;
            ScreenBackColor = ConsoleColor.Black;
            ScreenFontColor = ConsoleColor.White;

            #region fill
            for (int i = 4; i < 31; i++)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                ScreenBackColor = ConsoleColor.DarkRed;
                ScreenFontColor = ConsoleColor.White;
                Console.CursorLeft = ((140 / 2) - (width / 2)) + 2;
                Console.CursorTop = i;
                Console.Write("".PadLeft(width + 2, '\u2005'));//+2 to include spacing
            }
            #endregion

            #region Text
            
            string stack = "";
            int count = 0;
            if(exception.StackTrace != null)
            {
                string[] stacksp = exception.StackTrace.Split('\n');
                foreach (string item in stacksp)
                {
                    count++;
                    stack += $"{item}\n";
                    if (count > 7)
                    {
                        break;
                    }
                }
            }
            else
            {
                stack = "Not available...";
            }
            UpdateScreen_WriteTitleLine(140, "The program has encountered a problem...");
            UpdateScreen_WriteBody(140,$"{ErrorDeet}\r\n\r\n" +
                $"Dev Stack (partial):\r\n"+
                $"{"".PadLeft(140-16, '\u2500')}\r\n" +
                $"{stack}\r\n" +
                $"{"".PadLeft(140-16, '\u2500')}\r\n" +
                $"If this happens frequently, please submit a bug report to the ModularBOT GitHub repository. ",
                "Something went wrong...", ConsoleColor.Red);
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorTop = 28;
            Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
            Console.Write("".PadLeft(width - 4, '\u2500'));//bottom line
            WriteFooter("[ESC] Terminate Application...");
            #endregion
            
            //Thread.Sleep(1000);
            //Meta = "Downloading Software Update...";
            //UpdateMeta(ShowProgressBar ? 3 : 2);
            Console.CursorTop = 0;

            if (AutoReboot)
            {
                Task.Run(() => CountDown());
            }
        }

        private void UpdateScreen_WriteBody(int width, string Message, string meta, ConsoleColor MetaColor = ConsoleColor.Green)
        {
            MetaFontColor = MetaColor;
            Meta = meta;

            int coffset = 0;
            UpdateMeta(ShowProgressBar ? 3 : 2);

            string[] summlines = WordWrap($"{Message}", 16).Split('\n');
            Console.BackgroundColor = ConsoleColor.DarkRed;
            ScreenBackColor = ConsoleColor.DarkRed;
            foreach (string line in summlines)
            {


                Console.CursorTop = 8 + coffset;
                Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
                Console.Write(line);
                coffset++;
            }
            Console.CursorTop = 10 + coffset;
            Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"STOP CODE: {EVSource}");
            Console.ForegroundColor = ConsoleColor.White;

        }

        private void UpdateScreen_WriteTitleLine(int width, string Title)
        {
            Console.CursorTop = 5;
            Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;

            Console.Write($"{Title}");
            Console.CursorTop = 6;
            Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
            Console.Write("".PadLeft(width - 4, '\u2500'));
            Console.CursorTop = 8;
            Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
        }

        
        private void WriteFooter(string footer, ConsoleColor BackColor = ConsoleColor.Gray, ConsoleColor ForeColor = ConsoleColor.Black)
        {
            SpinWait.SpinUntil(() => !LayoutUpdating);
            LayoutUpdating = true;
            ScreenBackColor = BackColor;
            ScreenFontColor = ForeColor;
            int CT = Console.CursorTop;
            Console.CursorTop = 31;

            WriteEntry($"\u2502 {footer.Trim()} \u2502".PadRight(141, '\u2005') + "\u2502", BackColor, false, BackColor, null, BackColor);
            
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            Console.CursorTop = 0;
            if (CT < Console.BufferHeight)
            {
                Console.CursorTop = CT;
            }
            LayoutUpdating = false;
        }

        private void CountDown()
        {
            for (int i = 0; i < TimeOut; i++)
            {
                WriteFooter($"Auto Restart in [{TimeOut-i}] second(s)...");
                Thread.Sleep(1000);
            }
            timedout = true;
            PostMessage(GetConsoleWindow(), ConsoleIO.WM_KEYDOWN, ConsoleIO.VK_RETURN,0);
        }

    }
}