using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModularBOT.Entity;
using System.Reflection;
using System.Threading;
using Discord.WebSocket;

namespace ModularBOT.Component.ConsoleScreens
{
    public class UsersScreen : ConsoleScreen
    {
        private short page = 1;
        private short max = 1;
        private int index = 0;
        private int selectionIndex = 0;
        private int countOnPage = 0;
        private int ppg = 0;
        private bool errorfooter = false;
        private readonly DiscordNET DNet;
        private List<SocketGuildUser> UserList = new List<SocketGuildUser>();
        private readonly SocketGuild guild;

        public UsersScreen(SocketGuild _guild, DiscordNET discord, short startpage=1)
        {
            DNet = discord;
            guild = _guild;
            guild.DownloadUsersAsync();
            UserList = guild.Users.ToList().OrderByDescending(x => (int)(x.Hierarchy)).ToList();

            max = (short)(Math.Ceiling((double)(UserList.Count / 22)) + 1);

            page = startpage;
            selectionIndex = 0;
            countOnPage = 0;
            if(page > max)
            {
                page = max;
            }
            ppg = 0;
            index = (page * 22) - 22;

            ProgressMax = max;
            ProgressVal = page;

            ShowProgressBar = true;
            ShowMeta = true;

            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            TitlesBackColor = ConsoleColor.Black;
            TitlesFontColor = ConsoleColor.White;
            ProgressColor = ConsoleColor.Cyan;
            string shortname = guild.Name.Length > 24 ? $"{guild.Name.Remove(24)}..." : $"{guild.Name}";
            Title = $"Listing users for {shortname} | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version}";
            RefreshMeta();
            BufferHeight = 34;
            WindowHeight = 32;

        }

        private void RefreshMeta()
        {
            Meta = $"Users: {UserList.Where(x=>!x.IsBot).ToList().Count()} | Bots: {UserList.Where(x => x.IsBot).ToList().Count()} | Total: {UserList.Count}";
        }

        public override bool ProcessInput(ConsoleKeyInfo keyinfo)
        {
            Console.CursorVisible = false;
            //TODO: Custom input handling: NOTE -- Base adds exit handler [E] key.
            if(ActivePrompt) { return false; }

            if (keyinfo.Key == ConsoleKey.P || keyinfo.Key == ConsoleKey.LeftArrow)
            {
                if (page > 1)
                {
                    page--;
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                }
                selectionIndex = 0;
                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.

                if (ppg != page)
                {
                    ProgressVal = page;
                    RefreshMeta();
                    UpdateMeta();
                    UpdateProgressBar();
                    ClearContents();
                    //RenderContents();
                    countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref UserList);
                }
                    
                ppg = page;

            }
            
            if (keyinfo.Key == ConsoleKey.N || keyinfo.Key == ConsoleKey.RightArrow)
            {
                if (page < max)
                {

                    page++;
                }
                selectionIndex = 0;
                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                if (ppg != page)
                {
                    ProgressVal = page;
                    UpdateMeta();
                    UpdateProgressBar();
                    ClearContents();
                    //RenderContents();
                    countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref UserList);
                }
                ppg = page;

            }
            
            if (keyinfo.Key == ConsoleKey.UpArrow)
            {
                selectionIndex--;
                if (selectionIndex < 0)
                {
                    selectionIndex = countOnPage - 1;
                }

                if (errorfooter)
                {
                    UpdateFooter(page, max);
                    errorfooter = false;
                }
                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref UserList);
            }

            if (keyinfo.Key == ConsoleKey.DownArrow)
            {
                selectionIndex++;
                if (selectionIndex > countOnPage - 1)
                {
                    selectionIndex = 0;
                }

                if (errorfooter)
                {
                    UpdateFooter(page, max);
                    errorfooter = false;
                }
                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref UserList);
            }

            if(keyinfo.Key == ConsoleKey.F3)
            {
                string SearchQuery = ShowStringPrompt($"Search {guild.Name}", "Enter a username and/or #tag.");
                string[] array = { SearchQuery } ;
                string userquery = "";
                string discrimquery = "";
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    array = SearchQuery.Split('#');
                    if (array.Length > 1)
                    {
                        userquery = array[0];
                        discrimquery = array[1];
                    }
                }
                //guild.DownloadUsersAsync();
                UserList = guild.Users.ToList().OrderByDescending(x => (int)(x.Hierarchy)).ToList();
                if (!string.IsNullOrWhiteSpace(SearchQuery) && (array.Length ==1))
                {
                    UserList = UserList.FindAll(x => x.Username.ToLower().Contains(SearchQuery.ToLower())).OrderByDescending(x => (int)(x.Hierarchy)).ToList();

                }

                if (!string.IsNullOrWhiteSpace(SearchQuery) && (array.Length >1))
                {
                    UserList = UserList.FindAll(x => (x.Username.ToLower() + "#" + x.Discriminator).Contains(userquery.ToLower() + "#" + discrimquery)).ToList().OrderByDescending(x => (int)(x.Hierarchy)).ToList();
                }
                max = (short)(Math.Ceiling((double)(UserList.Count / 22)) + 1);

                page = 1;
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
                RefreshMeta();
                RenderScreen();
            }
            if (keyinfo.Key == ConsoleKey.Enter)
            {
                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.

                string username = GetSafeName(UserList, index + selectionIndex);
                if ((DNet.PermissionManager.DefaultAdmin.EntityID == UserList[index+selectionIndex].Id))
                {
                    WriteFooter("[ERROR] You cannot modify this user.",ConsoleColor.DarkRed,ConsoleColor.White);
                    errorfooter = true;
                    return false;
                }
                ConsoleColor PRVBG = Console.BackgroundColor;
                ConsoleColor PRVFG = Console.ForegroundColor;

                #region ------------ [ACCESS LEVEL EDITOR] ------------

                UpdateFooter(page, max, true);          //prompt footer
                int rr = ShowOptionSubScreen($"Editing: {username}", "Please select a new access level",
                    "BlackListed", "Normal", "Command Manager", "Administrator");

                switch (rr)
                {
                    case (1):
                        DNet.PermissionManager.RegisterEntity(UserList[selectionIndex + index], AccessLevels.Blacklisted);
                        break;
                    case (2):
                        if (DNet.PermissionManager.IsEntityRegistered(UserList[selectionIndex + index]))
                        {
                            DNet.PermissionManager.DeleteEntity(UserList[selectionIndex + index]);
                        }
                        break;
                    case (3):
                        DNet.PermissionManager.RegisterEntity(UserList[selectionIndex + index], AccessLevels.CommandManager);
                        break;
                    case (4):
                        DNet.PermissionManager.RegisterEntity(UserList[selectionIndex + index], AccessLevels.Administrator);
                        break;
                    default:
                        break;
                }

                UpdateFooter(page, max);                //restore footer

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
            countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref UserList);
        }

        private void WriteFooter(string footer, ConsoleColor BackColor= ConsoleColor.Gray,ConsoleColor ForeColor = ConsoleColor.Black)
        {
            LayoutUpdating = true;
            ScreenBackColor = BackColor;
            ScreenFontColor = ForeColor;
            int CT = Console.CursorTop;
            Console.CursorTop = 31;
            WriteEntry($"\u2502 {footer} \u2502".PadRight(141, '\u2005') + "\u2502", BackColor, false, BackColor);
            Console.CursorTop = 0;
            Console.CursorTop = CT;
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            LayoutUpdating = false;
        }

        private int PopulateList(short page, short max, ref int index, int selectionIndex, ref int ppg, ref List<SocketGuildUser> users)
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
            
            WriteEntry($"\u2502\u2005\u2005\u2005 - {"Discord User".PadRight(39, '\u2005')} {"Entity ID".PadRight(22, '\u2005')} {"Access Level".PadRight(16, '\u2005')} {"G. Admin".PadLeft(10, '\u2005')} {"G. Owner".PadLeft(10, '\u2005')}", ConsoleColor.Blue,false);
            WriteEntry($"\u2502\u2005\u2005\u2005 \u2500 {"".PadRight(39, '\u2500')} {"".PadLeft(22, '\u2500')} {"".PadLeft(16, '\u2500')} {"".PadLeft(10, '\u2500')} {"".PadLeft(10, '\u2500')}", ConsoleColor.Blue,false);
            
            for (int i = index; i < 22 * page; i++)//22 results per page.
            {
                if (i >= users.Count)
                {
                    break;
                }
                countOnPage++;
                WriteListEntry(selectionIndex, countOnPage, users, i);

            }

            Console.SetCursorPosition(0, 0);
            return countOnPage;
        }

        private void UpdateFooter(short page, short max, bool prompt=false)
        {
            LayoutUpdating = true;

            if (page > 1 && page < max)
            {
                WriteFooter("[ESC] Exit \u2502 [N/RIGHT] Next Page \u2502 [P/LEFT] Previous Page \u2502 [UP/DOWN] Select \u2502 [ENTER] Properties... \u2502 [F3] Search...");
            }
            if (page == 1 && page < max)
            {
                WriteFooter("[ESC] Exit \u2502 [N/RIGHT] Next Page \u2502 [UP/DOWN] Select \u2502 [ENTER] Properties... \u2502 [F3] Search...");
            }
            if (page == 1 && page == max)
            {
                WriteFooter("[ESC] Exit \u2502 [UP/DOWN] Select \u2502 [ENTER] Properties... \u2502 [F3] Search...");
            }
            if (page > 1 && page == max)
            {
                WriteFooter("[ESC] Exit \u2502 [P/LEFT] Previous Page \u2502 [UP/DOWN] Select \u2502 [ENTER] Properties... \u2502 [F3] Search...");
            }
            if(prompt)
            {
                WriteFooter("[Prompt] Please follow on-prompt instruction");
            }

            LayoutUpdating = false;
        }

        private void WriteListEntry(int selectionIndex, int countOnPage, List<SocketGuildUser> users, int i)
        {
            string name = users[i]?.Username ?? "[Pending...]";

            if (name.Length > 36)
            {
                name = name.Remove(36) + "...";
            }
            
            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(name))).Replace(' ', '\u2005').Replace("??", "?");
            string p = $"{o}#{UserList.ElementAt(i).Discriminator}".PadRight(39, '\u2005');
            string admin = users[i].GuildPermissions.Administrator ? "Yes".PadLeft(10, '\u2005') : "No".PadLeft(10, '\u2005');
            string owner = users[i].Guild.OwnerId == users[i].Id ? "Yes".PadLeft(10, '\u2005') : "No".PadLeft(10, '\u2005');
            string accesslevel = DNet.PermissionManager.GetAccessLevel(users.ElementAt(i)).ToString().PadRight(16,'\u2005');
            WriteEntry($"\u2502\u2005\u2005\u2005 - {p} [{users.ElementAt(i).Id.ToString().PadLeft(20, '0')}] {accesslevel} {admin} {owner} ", (countOnPage - 1) == selectionIndex, ConsoleColor.DarkGreen, false);
        }

        private string GetSafeName(List<SocketGuildUser> users, int i)
        {
            string userinput = users.ElementAt(i).Username ?? "[Pending...]";
            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(userinput))).Replace(' ', '\u2005').Replace("??", "?");
            
            if (o.Length > 17)
            {
                o = o.Remove(13) + "...";
            }

            string p = $"{o}";
            
            return p;
        }
    }
}
