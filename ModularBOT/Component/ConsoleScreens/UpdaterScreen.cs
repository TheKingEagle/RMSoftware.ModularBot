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
    public class UpdaterScreen : ConsoleScreen
    {

        bool DownloadFinished = false;
        public bool InstallUpdate { get; private set; }
        string UpdateVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        string UpdateTitle = "System Update";
        public string UPDATERLOC = "";
        public string UPDATEDVER = "";
        bool UpdateAvailable = false;
        bool pr = false;
        string ErrorDeet = "";
        DiscordNET disnet;
        UpdateInfo u = null;
        bool isAuto = false;
        public UpdaterScreen(ref Configuration currentconfig, ref DiscordNET dnet, bool auto=false)
        {
            isAuto = auto;
            disnet = dnet;
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            TitlesBackColor = ConsoleColor.Black;
            TitlesFontColor = ConsoleColor.White;
            ProgressColor = ConsoleColor.Green;
            MetaFontColor = ConsoleColor.Green;
            Title = $"Software Updater | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version}";
            if (currentconfig.UseInDevChannel.HasValue)
            {
                pr = currentconfig.UseInDevChannel.Value;
            }
            Meta = $"Checking for updates...";
            ShowProgressBar = false;
            ShowMeta = true;
            ProgressVal = 1;
            ProgressMax = 1;
            BufferHeight = 34;
            WindowHeight = 32;
            try
            {
                if (currentconfig.UseInDevChannel.HasValue)
                {
                    UpdateAvailable = dnet.Updater.CheckUpdate(true).GetAwaiter().GetResult();

                    if (UpdateAvailable)
                    {
                        u = dnet.Updater.UpdateInfo;

                        UpdateMeta(ShowProgressBar ? 3 : 2);
                    }
                    if (u != null)
                    {
                        UpdateVersion = currentconfig.UseInDevChannel.Value ? u.PREVERS : u.VERSION;
                        Meta = "Software Update Available!";
                        MetaFontColor = ConsoleColor.Green;
                        UpdateVersion = currentconfig.UseInDevChannel.Value ? u.PREVERS : u.VERSION;
                        UpdateTitle = currentconfig.UseInDevChannel.Value ? u.BTITLE : u.ATITLE;
                    }
                }
            }
            catch(Exception ex)
            {
                ErrorDeet = ex.Message;
                updateStep = -1;
            }

        }
        #region P/Invoke
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();
        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        internal static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        #endregion
        public override bool ProcessInput(ConsoleKeyInfo keyinfo)
        {

            if (UpdateAvailable)
            {
                if (updateStep == 0)
                {
                    if (keyinfo.Key == ConsoleKey.Enter)
                    {
                        //run thread
                        WriteFooter("Downloading... Please wait");
                        Task.Run(() => DownloadUpdate(pr ? u.PREPAKG : u.PACKAGE, $"updater-{(pr ? u.PREVERS : u.VERSION)}.exe"));
                        SpinWait.SpinUntil(() => DownloadFinished);
                        if(updateStep != -1)
                        {
                            updateStep = 1;
                            RenderScreen();
                            UPDATERLOC = $"updater-{ (pr ? u.PREVERS : u.VERSION)}.exe";
                            UPDATEDVER = pr ? u.PREVERS : u.VERSION;
                            Thread.Sleep(2500);
                        }
                    }


                }
                if (updateStep == 1)
                {
                    if (keyinfo.Key == ConsoleKey.Enter)
                    {
                        InstallUpdate = true;
                        return true;
                    }
                }
            }
            return base.ProcessInput(keyinfo);
        }

        int updateStep = 0;
        bool progupdating = false;
        protected override void RenderContents()
        {
            SpinWait.SpinUntil(() => !LayoutUpdating);
            int width = 140;
            Console.CursorLeft = ((140/2) - (width/2))+2;
            Console.ForegroundColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.DarkBlue;
            ScreenFontColor = ConsoleColor.Cyan;
            
            if(updateStep == 0)
            {

                #region fill
                for (int i = 4; i < 31; i++)
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    ScreenBackColor = ConsoleColor.DarkBlue;
                    ScreenFontColor = ConsoleColor.Cyan;
                    Console.CursorLeft = ((140 / 2) - (width / 2)) + 2;
                    Console.CursorTop = i;
                    Console.Write("".PadLeft(width + 2, '\u2005'));//+2 to include spacing
                }
                #endregion

                #region Text
                
                UpdateScreen_WriteTitleLine(width,$"{ UpdateTitle} (v{ UpdateVersion})");
                
                if (!UpdateAvailable)
                {
                    UpdateScreen_WriteBody(width,"You are already running the latest version. Check https://rms0.org?a=mbchanges for the change log.","Update Check Finished");

                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.CursorTop = 28;
                    Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
                    Console.Write("".PadLeft(width - 4, '\u2500'));//bottom line
                    WriteFooter("[ESC] Close...");
                    if(isAuto)
                    {
                        Thread.Sleep(1000);
                        
                        PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_ESCAPE, 0);
                    }
                }
                else
                {
                    string summ = pr ? u.BSUMMARY : u.ASUMMARY;
                    int coffset = 0;
                    if (summ.Length > 240)
                    {
                        summ = summ.Remove(237) + "...";
                    }
                    string[] summlines = WordWrap($"Update Summary: {summ}", 16).Split('\n');
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    ScreenBackColor = ConsoleColor.DarkBlue;
                    foreach (string line in summlines)
                    {


                        Console.CursorTop = 8 + coffset;
                        Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
                        Console.Write(line);
                        coffset++;
                    }
                    Console.CursorTop = 8 + coffset;
                    Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
                    Console.Write($"Key changes:");
                    List<string> changes = pr ? u.BCHANGES : u.ACHANGES;
                    for (int i = 0; i < 10; i++)
                    {
                        if (i > changes.Count - 1)
                        {
                            break;
                        }

                        coffset++;
                        Console.CursorTop = 8 + coffset;
                        Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($"- {changes[i]}");
                    }
                    if (changes.Count > 10)
                    {
                        Console.CursorTop = 8 + coffset;
                        Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"- full change list: ");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("https://rms0.org?a=mbchanges");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    }
                    Console.CursorTop = 11 + coffset;
                    Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($"Update Channel: {(pr ? "INDEV" : "RELEASE")}");

                    #region Progress Bar

                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.CursorTop = 28;
                    Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
                    Console.Write("".PadLeft(width - 4, '\u2500'));//bottom line

                    RenderProgress(width, -1, 100);
                    WriteFooter("[Enter] Begin update... \u2502 [ESC] Cancel...");
                    #endregion

                    if (isAuto)
                    {
                        Thread.Sleep(1000);
                        PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_RETURN, 0);
                    }
                }
                #endregion

            }

            if (updateStep == 1)
            {

                #region fill
                for (int i = 4; i < 31; i++)
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    ScreenBackColor = ConsoleColor.DarkBlue;
                    ScreenFontColor = ConsoleColor.Cyan;
                    Console.CursorLeft = ((140 / 2) - (width / 2)) + 2;
                    Console.CursorTop = i;
                    Console.Write("".PadLeft(width + 2, '\u2005'));//+2 to include spacing
                }
                #endregion

                #region Text
                Console.CursorTop = 5;
                Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;

                Console.Write($"{UpdateTitle} (v{UpdateVersion})");
                Console.CursorTop = 6;
                Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
                Console.Write("".PadLeft(width - 4, '\u2500'));
                Console.CursorTop = 8;
                Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
               
                Meta = "Ready to install";

                int coffset = 0;
                UpdateMeta(ShowProgressBar ? 3 : 2);
                string[] summlines = WordWrap($"The update has finished downloading. The installation will start in just a second...", 16).Split('\n');
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                ScreenBackColor = ConsoleColor.DarkBlue;
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
                Console.Write($"Update Channel: {(pr ? "INDEV" : "RELEASE")}");
                Console.ForegroundColor = ConsoleColor.Cyan;

                #endregion

                WriteFooter("Please Wait just a moment...");
            }

            if(updateStep == -1)
            {
                for (int i = 4; i < 31; i++)
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    ScreenBackColor = ConsoleColor.DarkBlue;
                    ScreenFontColor = ConsoleColor.Cyan;
                    Console.CursorLeft = ((140 / 2) - (140 / 2)) + 2;
                    Console.CursorTop = i;
                    Console.Write("".PadLeft(140 + 2, '\u2005'));//+2 to include spacing
                }
                UpdateScreen_WriteTitleLine(140, "The program has encountered a problem...");
                UpdateScreen_WriteBody(140, $"Unable to update due to an error.\r\n" +
                    $"ERROR DETAILS: {ErrorDeet}\r\n" +
                    $"{"".PadLeft(100,'\u2500')}\r\n" +
                    $"Try the following:\r\n" +
                    $"- Check your Internet connection\r\n" +
                    $"- Ensure you can reach api.rms0.org\r\n" +
                    $"- If all else fails, send error report to the ModularBOT repository.", "Update Failed", ConsoleColor.Red);
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.CursorTop = 28;
                Console.CursorLeft = ((140 / 2) - (width / 2)) + 5;
                Console.Write("".PadLeft(width - 4, '\u2500'));//bottom line
                WriteFooter("[ESC] Exit...");
            }
            //Thread.Sleep(1000);
            //Meta = "Downloading Software Update...";
            //UpdateMeta(ShowProgressBar ? 3 : 2);
            Console.CursorTop = 0;
        }

        private void UpdateScreen_WriteBody(int width, string Message, string meta, ConsoleColor MetaColor = ConsoleColor.Green)
        {
            MetaFontColor = MetaColor;
            Meta = meta;

            int coffset = 0;
            UpdateMeta(ShowProgressBar ? 3 : 2);

            string[] summlines = WordWrap($"{Message}", 16).Split('\n');
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            ScreenBackColor = ConsoleColor.DarkBlue;
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
            Console.Write($"Update Channel: {(pr ? "INDEV" : "RELEASE")}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            
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

        private void RenderProgress(int width, int val, int max)
        {
            if (progupdating) { return; }
            progupdating = true;
            if (val == -1)
            {
                Console.CursorTop = 29;
                Console.CursorLeft = 5;
                Console.BackgroundColor = ConsoleColor.DarkGray;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" \u25BA UPDATE \u25C4 "); //progress% or button 12 spaces
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\u2005\u2005");

            }
            string bar = "";
            if(val > max)
            {
                val = max;
            }

            double f = (double)(val / (double)max);
            int amt = (int)((width-19) * (double)f);
            int percent = (int)(f * 100);
            if (f < 0) f = 0;
            if (amt < 0) amt = 0;
            if (percent < 0) percent = 0;
            if (val >= 0)
            {
                Console.CursorTop = 29;
                Console.CursorLeft = 5;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{percent}% complete ".PadLeft(14,'\u2005')); //progress% or button
            }

            for (int i = 0; i < (width-19); i++)
            {
                if (i <= amt && amt > 0)
                {
                    bar += "\u2588";
                }
                else
                {
                    bar += "\u2591";
                }
            }

            Console.CursorTop = 29;
            Console.CursorLeft = 20;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(bar);//progress bar
            progupdating = false;
        }

        private void WriteFooter(string footer, ConsoleColor BackColor = ConsoleColor.Gray, ConsoleColor ForeColor = ConsoleColor.Black)
        {
            LayoutUpdating = true;
            ScreenBackColor = BackColor;
            ScreenFontColor = ForeColor;
            int CT = Console.CursorTop;
            Console.CursorTop = 31;

            WriteEntry($"\u2502 {footer.Trim()} \u2502".PadRight(141, '\u2005') + "\u2502", BackColor, false, BackColor, null,BackColor);
            Console.CursorTop = 0;
            Console.CursorTop = CT;
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            LayoutUpdating = false;
        }

        private void DownloadUpdate(string url, string FPATH)
        {
            WebClient wc = new WebClient();
            wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
            wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
            wc.DownloadFileAsync(new Uri(url), FPATH);
        }

        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if(e.Error != null)
            {
                ErrorDeet = e.Error.Message;
                updateStep = -1;
                RenderScreen();
            }
            DownloadFinished = true;
           
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if(progupdating)
            {
                return;
            }
            RenderProgress(140, e.ProgressPercentage, 100);
        }
    }
}
