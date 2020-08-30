using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModularBOT.Entity;
using System.Reflection;
using System.Threading;
using Discord.WebSocket;
using System.Windows;

namespace ModularBOT.Component.ConsoleScreens
{
    public class GuildsScreen : ConsoleScreen
    {
        #region --- DECLARE ---
        private short page = 1;
        private readonly short max = 1;
        private int index = 0;
        private int selectionIndex = 0;
        private int countOnPage = 0;
        private int ppg = 0;
        private readonly DiscordNET DNet;
        private List<SocketGuild> Guildlist = new List<SocketGuild>();

        #endregion

        public GuildsScreen(List<SocketGuild> guilds, DiscordNET discord, short startpage=1)
        {
            DNet = discord;
            Guildlist = guilds.OrderBy(x=>x.Name).ToList();
            max = (short)Math.Ceiling((double)discord.Client.Guilds.Count / 22);
            if(max == 0) { max = 1; }
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
            ProgressColor = ConsoleColor.Cyan;

            Title = $"Guilds | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version}";
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

            if(ActivePrompt) return false;

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
                    countOnPage = PopulateGuildList(page, max, ref index, selectionIndex, ref ppg, ref Guildlist);
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
                    countOnPage = PopulateGuildList(page, max, ref index, selectionIndex, ref ppg, ref Guildlist);
                }

                ppg = page;
            }

            if (keyinfo.Key == ConsoleKey.UpArrow)
            {
                selectionIndex--;

                if (selectionIndex < 0) selectionIndex = countOnPage - 1;

                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                countOnPage = PopulateGuildList(page, max, ref index, selectionIndex, ref ppg, ref Guildlist);
            }

            if (keyinfo.Key == ConsoleKey.DownArrow)
            {
                selectionIndex++;

                if (selectionIndex > countOnPage - 1) selectionIndex = 0;

                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                countOnPage = PopulateGuildList(page, max, ref index, selectionIndex, ref ppg, ref Guildlist);
            }

            if (keyinfo.Key == ConsoleKey.Enter)
            {
                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                
                string guildname = GetSafeName(Guildlist, index + selectionIndex);
                
                if (!(Guildlist[index + selectionIndex].IsConnected)) return false;

                ConsoleColor PRVBG = Console.BackgroundColor;
                ConsoleColor PRVFG = Console.ForegroundColor;

                #region -------------- [Guild Manager Sub-Screen] --------------
                UpdateFooter(page, max, true);          //Prompt footer

                int rr = ShowOptionSubScreen($"Manage: {guildname}", "What do you want to do?", 
                    "View Users...", "View Channels...", "View Roles...", "More Actions...");

                switch (rr)
                {
                    case (1):
                        SS_ViewUserScreen();
                        break;
                    case (2):
                        SS_ViewChannelsScreen();
                        break;
                    case (3):
                        SS_ViewRolesScreen(false);
                        break;
                    case (4):
                        SS_MoreActions(guildname);
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
            countOnPage = PopulateGuildList(page, max, ref index, selectionIndex, ref ppg, ref Guildlist);
        }

        private void RefreshMeta()
        {
            Meta = $"Present in {Guildlist.Count} guild(s). Connected to {Guildlist.Where(x => x.IsConnected).LongCount()} guild(s)";
            UpdateMeta();
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
            WriteEntry($"\u2502 {footer} \u2502".PadRight(141, '\u2005') + "\u2502", ConsoleColor.Gray, false, ConsoleColor.Gray);
            Console.CursorTop = 0;
            Console.CursorTop = CT;
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            LayoutUpdating = false;
        }

        #endregion

        #region Guild Manager Sub-Screen Methods
        private void SS_MoreActions(string guildname)
        {
            countOnPage = PopulateGuildList(page, max, ref index, selectionIndex, ref ppg, ref Guildlist);
            index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
            UpdateFooter(page, max, true);            //PROMPT FOOTER

            int ss = ShowOptionSubScreen($"Manage: {guildname}", "What do you want to do?", "View My Roles...", "Copy ID...", "-", "Leave Guild...");

            switch (ss)
            {
                case (1):
                    SS_ViewRolesScreen(true);
                    break;
                case (2):
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                    Thread thread = new Thread(() => Clipboard.SetText(Guildlist[selectionIndex + index].Id.ToString()));
                    thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                    thread.Start();
                    thread.Join(); //Wait for the thread to end
                                   //TODO: If success or fail, Display a message prompt
                    break;
                case (4):
                    UpdateFooter(page, max, true);     //PROMPT FOOTER

                    Console.Beep(440, 50);
                    Console.Beep(880, 50);
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                    int confirmprompt = ShowOptionSubScreen($"Leave {guildname}?", $"Are you sure you want to leave?", "-", "NO", "YES", "-", ConsoleColor.DarkRed);
                    if (confirmprompt == 3)
                    {
                        index = (page * 22) - 22;
                        Guildlist[selectionIndex + index].LeaveAsync();
                        Guildlist.Remove(Guildlist[selectionIndex + index]);
                        RefreshMeta();
                        RenderScreen();
                    }
                    
                    break;
            }

            UpdateFooter(page, max);                    //RESTORE FOOTER
        }

        private void SS_ViewUserScreen()
        {
            index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.

            //---------------start modal---------------
            var NGScreen = new UsersScreen(Guildlist[index + selectionIndex], DNet)
            {
                ActiveScreen = true
            };
            NGScreen.RenderScreen();
            while (true)
            {
                if (NGScreen.ProcessInput(Console.ReadKey(true)))
                {
                    break;
                }
            }
            NGScreen.ActiveScreen = false;
            //----------------End modal----------------
            index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
            RefreshMeta();
            RenderScreen();
            countOnPage = PopulateGuildList(page, max, ref index, selectionIndex, ref ppg, ref Guildlist);
        }

        private void SS_ViewRolesScreen(bool BotRoles)
        {
            index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.

            //---------------start modal---------------
            var RoleList = BotRoles ? Guildlist[index + selectionIndex].CurrentUser.Roles.ToList() : Guildlist[index + selectionIndex].Roles.ToList();
            string title = BotRoles ? $"Listing roles for {Guildlist[index + selectionIndex].CurrentUser.Username}#" +
                $"{Guildlist[index + selectionIndex].CurrentUser.Discriminator}" : "Listing all roles";
            var NGScreen = new RolesScreen(DNet,Guildlist[index + selectionIndex], RoleList, title);
            {
                ActiveScreen = true;
            };
            NGScreen.RenderScreen();
            while (true)
            {
                if (NGScreen.ProcessInput(Console.ReadKey(true)))
                {
                    break;
                }
            }
            NGScreen.ActiveScreen = false;
            //----------------End modal----------------
            index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
            RefreshMeta();
            RenderScreen();
            countOnPage = PopulateGuildList(page, max, ref index, selectionIndex, ref ppg, ref Guildlist);
        }

        private void SS_ViewChannelsScreen()
        {
            index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.

            //---------------start modal---------------
            var NGScreen = new ChannelsScreen(Guildlist[index + selectionIndex], Guildlist[index + selectionIndex].Channels.ToList())
            {
                ActiveScreen = true
            };
            NGScreen.RenderScreen();
            while (true)
            {
                if (NGScreen.ProcessInput(Console.ReadKey(true)))
                {
                    break;
                }
            }
            NGScreen.ActiveScreen = false;
            //----------------End modal----------------
            index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
            RefreshMeta();
            RenderScreen();
            countOnPage = PopulateGuildList(page, max, ref index, selectionIndex, ref ppg, ref Guildlist);
        }

        #endregion

        #region GuildList Methods

        private int PopulateGuildList(short page, short max, ref int index, int selectionIndex, ref int ppg, ref List<SocketGuild> guilds)
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

            WriteEntry($"\u2502\u2005\u2005\u2005 - {"Guild Name".PadRight(39, '\u2005')} {"Guild ID".PadRight(22, '\u2005')} {"G. Admin".PadLeft(10, '\u2005')}", ConsoleColor.Blue,false);
            WriteEntry($"\u2502\u2005\u2005\u2005 \u2500 {"".PadRight(39, '\u2500')} {"".PadLeft(22, '\u2500')} {"".PadLeft(10, '\u2500')}", ConsoleColor.Blue,false);
            
            for (int i = index; i < 22 * page; i++)//22 results per page.
            {
                if (i >= guilds.Count)
                {
                    break;
                }
                countOnPage++;
                WriteGuild(selectionIndex, countOnPage, guilds, i);
            }

            Console.SetCursorPosition(0, 0);
            return countOnPage;
        }

        private void WriteGuild(int selectionIndex, int countOnPage, List<SocketGuild> guilds, int i)
        {
            if (guilds[i].IsConnected)
            {
                string name = guilds[i]?.Name ?? "[Pending...]";

                if (name.Length > 36)
                {
                    name = name.Remove(36) + "...";
                }
                string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(name))).Replace(' ', '\u2005').Replace("??", "?");
                string p = $"{o}".PadRight(39, '\u2005');
                string admin = guilds[i].CurrentUser.GuildPermissions.Administrator ? "Yes".PadLeft(10, '\u2005') : "No".PadLeft(10, '\u2005');
                WriteEntry($"\u2502\u2005\u2005\u2005 - {p} [{guilds.ElementAt(i).Id.ToString().PadLeft(20, '0')}] {admin}", (countOnPage - 1) == selectionIndex, ConsoleColor.DarkGreen,false);
            }
            else
            {
                string name = "[Guild Unavailable]";

                if (name.Length > 36)
                {
                    name = name.Remove(36) + "...";
                }
                string p = $"{name}".PadRight(39, '\u2005');
                WriteEntry($"\u2502\u2005\u2005\u2005 - {p} [{guilds.ElementAt(i).Id.ToString().PadLeft(20, '0')}]", (countOnPage - 1) == selectionIndex, true, ConsoleColor.DarkGreen,false);

            }
        }

        private string GetSafeName(List<SocketGuild> guilds, int i)
        {
            string userinput = guilds.ElementAt(i).Name ?? "[Pending...]";
            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(userinput))).Replace(' ', '\u2005').Replace("??", "?");
            
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
