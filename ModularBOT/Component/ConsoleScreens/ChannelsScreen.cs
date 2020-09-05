using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Entity;
using System.Reflection;
using System.Threading;
using Discord.WebSocket;
using System.Windows;

namespace ModularBOT.Component.ConsoleScreens
{
    public class ChannelsScreen : ConsoleScreen
    {

        #region --- DECLARE ---
        private short page = 1;
        private readonly short max = 1;
        private int index = 0;
        private int selectionIndex = 0;
        private int countOnPage = 0;
        private int ppg = 0;
        private List<SocketGuildChannel> Channels = new List<SocketGuildChannel>();
        private readonly SocketGuild currentguild;
        #endregion

        public ChannelsScreen(SocketGuild Guild, List<SocketGuildChannel> ChannelList, string title = "Channel List", short startpage = 1)
        {
            currentguild = Guild;
            Channels = ChannelList.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();//ignore noname channels.
            max = (short)Math.Ceiling((double)Channels.Count / 22);
            if (max == 0) { max = 1; }
            page = startpage;
            selectionIndex = 0;
            countOnPage = 0;
            if (page > max)
            {
                page = max;
            }
            ppg = 0;
            index = (page * 22) - 22;

            ProgressMax = max;
            ProgressVal = page;

            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            TitlesBackColor = ConsoleColor.Black;
            TitlesFontColor = ConsoleColor.White;
            ProgressColor = ConsoleColor.Green;

            Title = $"{title} for {GetSafeName(Guild)} | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version}";
            RefreshMeta();
            ShowProgressBar = true;
            ShowMeta = true;

            BufferHeight = 34;
            WindowHeight = 32;
        }

        #region Screen Methods

        public override bool ProcessInput(ConsoleKeyInfo keyinfo)
        {
            Console.CursorVisible = false;

            if (ActivePrompt) return false;

            if (keyinfo.Key == ConsoleKey.P || keyinfo.Key == ConsoleKey.LeftArrow)
            {
                if (page > 1) page--;

                selectionIndex = 0;
                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.

                if (ppg != page)
                {
                    ProgressVal = page;
                    RefreshMeta();
                    UpdateProgressBar();
                    ClearContents();
                    countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref Channels);
                }

                ppg = page;
            }

            if (keyinfo.Key == ConsoleKey.N || keyinfo.Key == ConsoleKey.RightArrow)
            {
                if (page < max) page++;

                selectionIndex = 0;
                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.

                if (ppg != page)
                {
                    ProgressVal = page;
                    RefreshMeta();
                    UpdateProgressBar();
                    ClearContents();
                    countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref Channels);
                }

                ppg = page;
            }

            if (keyinfo.Key == ConsoleKey.UpArrow)
            {
                selectionIndex--;

                if (selectionIndex < 0) selectionIndex = countOnPage - 1;

                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref Channels);
            }

            if (keyinfo.Key == ConsoleKey.DownArrow)
            {
                selectionIndex++;

                if (selectionIndex > countOnPage - 1) selectionIndex = 0;

                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref Channels);
            }

            if (keyinfo.Key == ConsoleKey.Enter)
            {
                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.

                string channame = GetSafeName(Channels, index + selectionIndex);

                ConsoleColor PRVBG = Console.BackgroundColor;
                ConsoleColor PRVFG = Console.ForegroundColor;

                #region -------------- [Channel Properties Sub-Screen] --------------
                UpdateFooter(page, max, true);          //Prompt footer

                int rr = ShowOptionSubScreen($"Properties: {channame}", "What do you want to do?",
                    "Copy ID", "-", "-", "-");

                switch (rr)
                {
                    case (1):
                        index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                        Thread thread = new Thread(() => Clipboard.SetText(Channels[selectionIndex + index].Id.ToString()));
                        thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                        thread.Start();
                        thread.Join(); //Wait for the thread to end
                                       //TODO: If success or fail, Display a message prompt
                        break;
                    default:
                        break;
                }

                UpdateFooter(page, max);                //Restore footer 
                #endregion

                Console.ForegroundColor = PRVFG;
                Console.BackgroundColor = PRVBG;
            }

            return base.ProcessInput(keyinfo);
        }

        protected override void RenderContents()
        {
            SpinWait.SpinUntil(() => !LayoutUpdating);
            SpinWait.SpinUntil(() => !ActivePrompt);
            countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref Channels);
            Console.CursorTop = 0;
        }

        private void RefreshMeta()
        {
            Meta = $"Listing {Channels.Count} channels(s) in {GetSafeName(currentguild)}";
            UpdateMeta(ShowProgressBar ? 2:4);
        }

        private void UpdateFooter(short page, short max, bool prompt = false)
        {
            LayoutUpdating = true;

            if (page > 1 && page < max)
            {
                WriteFooter("[ESC] Exit \u2502 [N/RIGHT] Next Page \u2502 [P/LEFT] Previous Page \u2502 [UP/DOWN] Select \u2502 [ENTER] Properties...");
            }
            if (page == 1 && page < max)
            {
                WriteFooter("[ESC] Exit \u2502 [N/RIGHT] Next Page \u2502 [UP/DOWN] Select \u2502 [ENTER] Properties...");
            }
            if (page == 1 && page == max)
            {
                WriteFooter("[ESC] Exit \u2502 [UP/DOWN] Select \u2502 [ENTER] Properties...");
            }
            if (page > 1 && page == max)
            {
                WriteFooter("[ESC] Exit \u2502 [P/LEFT] Previous Page \u2502 [UP/DOWN] Select \u2502 [ENTER] Properties...");
            }
            if (prompt)
            {
                WriteFooter("[Prompt] Please follow on-prompt instruction");
            }

            LayoutUpdating = false;
        }

        private void WriteFooter(string footer)
        {
            LayoutUpdating = true;
            ScreenBackColor = ConsoleColor.Gray;
            ScreenFontColor = ConsoleColor.Black;
            int CT = Console.CursorTop;
            Console.CursorTop = 31;
            WriteEntry($"\u2502 {footer} \u2502".PadRight(141, '\u2005') + "\u2502", ConsoleColor.Gray, false, ConsoleColor.Gray,null,ConsoleColor.Gray);
            Console.CursorTop = 0;
            Console.CursorTop = CT;
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            LayoutUpdating = false;
        }
        #endregion

        #region Channel Listing Methods.

        private int PopulateList(short page, short max, ref int index, int selectionIndex, ref int ppg, ref List<SocketGuildChannel> channels)
        {
            int countOnPage;

            if (ppg != page)//is page changing?
            {
                LayoutUpdating = true;
                _ = Console.CursorTop;
                Console.CursorTop = 2;
                UpdateProgressBar();
                Console.CursorTop = 2;
                ppg = page;

                LayoutUpdating = false;
                UpdateFooter(page, max);
            }

            countOnPage = 0;

            if (ppg == page)
            {
                Console.SetCursorPosition(0, ContentTop);
            }

            WriteEntry($"\u2502\u2005\u2005\u2005 - {"Channel Name".PadRight(39, '\u2005')} {"Channel ID".PadRight(22, '\u2005')} {"Type".PadLeft(10, '\u2005')}", ConsoleColor.Blue, false);
            WriteEntry($"\u2502\u2005\u2005\u2005 \u2500 {"".PadRight(39, '\u2500')} {"".PadRight(22, '\u2500')} {"".PadLeft(10, '\u2500')}", ConsoleColor.Blue, false);

            for (int i = index; i < 22 * page; i++)//22 results per page.
            {
                if (i >= channels.Count)
                {
                    break;
                }
                countOnPage++;
                WriteChannel(selectionIndex, countOnPage, channels, i);
            }

            Console.SetCursorPosition(0, 0);
            return countOnPage;
        }

        private void WriteChannel(int selectionIndex, int countOnPage, List<SocketGuildChannel> guildChannels, int i)
        {
            string name = guildChannels[i]?.Name ?? "[Pending...]";

            if (name.Length > 36)
            {
                name = name.Remove(36) + "...";
            }
            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(name))).Replace(' ', '\u2005').Replace("??", "?");
            string p = $"{o}".PadRight(39, '\u2005');
            string chtype = Channels.ElementAt(i).GetType().ToString();
            string chltype = chtype.Remove(0, chtype.LastIndexOf('.') + 1).Replace("Socket", "").Replace("Channel", "");
            WriteEntry($"\u2502\u2005\u2005\u2005 - {p} [{guildChannels.ElementAt(i).Id.ToString().PadLeft(20, '0')}] {chltype.PadLeft(10,'\u2005')}", (countOnPage - 1) == selectionIndex, ConsoleColor.DarkGreen, false);
        }

        private string GetSafeName(SocketGuild guild)
        {
            string userinput = guild.Name ?? "[Pending...]";
            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(userinput))).Replace(' ', '\u2005').Replace("??", "?");

            if (o.Length > 17)
            {
                o = o.Remove(13) + "...";
            }

            string p = $"{o}";

            return p;
        }

        private string GetSafeName(List<SocketGuildChannel> Channels, int i)
        {
            string chname = Channels.ElementAt(i).Name ?? "[Pending...]";
            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, 
                Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"),
                new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(chname))).Replace(' ', '\u2005').Replace("??", "?");

            if (o.Length > 17)
            {
                o = o.Remove(13) + "...";
            }

            string p = $"{o}";

            return p;
        }

        #endregion
    }
}
