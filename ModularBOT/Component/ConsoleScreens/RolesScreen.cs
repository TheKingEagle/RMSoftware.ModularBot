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
    public class RolesScreen : ConsoleScreen
    {
        #region --- DECLARE ---
        private short page = 1;
        private readonly short max = 1;
        private int index = 0;
        private int selectionIndex = 0;
        private int countOnPage = 0;
        private int ppg = 0;
        private List<SocketRole> Roles = new List<SocketRole>();
        private readonly SocketGuild currentguild;
        private readonly DiscordNET DNet;

        #endregion

        public RolesScreen(DiscordNET discord, SocketGuild Guild, List<SocketRole> RoleList, string title = "Role List", short startpage = 1)
        {
            currentguild = Guild;
            Roles = RoleList;
            DNet = discord;
            max = (short)(Math.Ceiling((double)(RoleList.Count / 22)) + 1);
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

            ShowProgressBar = true;
            ShowMeta = true;

            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            TitlesBackColor = ConsoleColor.Black;
            TitlesFontColor = ConsoleColor.White;
            ProgressColor = ConsoleColor.Cyan;

            Title = $"{title} in {GetSafeName(Guild)} | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version}";
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
                    countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref Roles);
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
                    countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref Roles);
                }

                ppg = page;
            }

            if (keyinfo.Key == ConsoleKey.UpArrow)
            {
                selectionIndex--;

                if (selectionIndex < 0) selectionIndex = countOnPage - 1;

                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref Roles);
            }

            if (keyinfo.Key == ConsoleKey.DownArrow)
            {
                selectionIndex++;

                if (selectionIndex > countOnPage - 1) selectionIndex = 0;

                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref Roles);
            }

            if (keyinfo.Key == ConsoleKey.Enter)
            {
                index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                
                string RoleName = GetSafeName(Roles, index + selectionIndex);

                ConsoleColor PRVBG = Console.BackgroundColor;
                ConsoleColor PRVFG = Console.ForegroundColor;

                #region -------------- [Role Properties Sub-Screen] --------------
                UpdateFooter(page, max, true);          //Prompt footer

                int rr = ShowOptionSubScreen($"Properties: {RoleName}", "What do you want to do?", 
                    "-", "Copy ID", "Edit AccessLevel...", "-");

                switch (rr)
                {
                    case (2):
                        index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.
                        Thread thread = new Thread(() => Clipboard.SetText(Roles[selectionIndex + index].Id.ToString()));
                        thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                        thread.Start();
                        thread.Join(); //Wait for the thread to end
                                       //TODO: If success or fail, Display a message prompt
                        break;
                    case (3):
                        SS_EditAccessLevel();
                        RefreshMeta();
                        RenderScreen();
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
            countOnPage = PopulateList(page, max, ref index, selectionIndex, ref ppg, ref Roles);
        }

        private void RefreshMeta()
        {
            Meta = $"Listing {Roles.Count} role(s) in {GetSafeName(currentguild)}";
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

        private void WriteFooter(string footer, ConsoleColor BackColor = ConsoleColor.Gray, ConsoleColor ForeColor = ConsoleColor.Black)
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
        #endregion

        #region Role Properties Sub-Screen Methods
        //TODO: SS_EditAccessLevel();

        public bool SS_EditAccessLevel()
        {
            index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.

            string rolename = GetSafeName(Roles, index + selectionIndex);
            if (Roles[index+selectionIndex].Id == Roles[index + selectionIndex].Guild.Id)
            {
                WriteFooter("[ERROR] Operation Failed", ConsoleColor.DarkRed, ConsoleColor.White);
                ShowOptionSubScreen("Invalid Operation", $"Editing {rolename} is not permitted", "-", "Okay", "-", "-", ConsoleColor.DarkRed);
                
                return false;
            }
            ConsoleColor PRVBG = Console.BackgroundColor;
            ConsoleColor PRVFG = Console.ForegroundColor;

            #region ------------ [ACCESS LEVEL EDITOR] ------------

            UpdateFooter(page, max, true);          //prompt footer
            int rr = ShowOptionSubScreen($"Editing: {rolename}", "Please select a new access level",
                "BlackListed", "Normal", "Command Manager", "Administrator");

            switch (rr)
            {
                case (1):
                    WriteFooter("[ERROR] Operation Failed.", ConsoleColor.DarkRed, ConsoleColor.White);
                    ShowOptionSubScreen("Invalid Operation", "You cannot blacklist a role.", "-", "Okay", "-", "-", ConsoleColor.DarkRed);
                    break;
                case (2):
                    if (DNet.PermissionManager.IsEntityRegistered(Roles[index + selectionIndex]))
                    {
                        DNet.PermissionManager.DeleteEntity(Roles[index + selectionIndex]);
                    }
                    break;
                case (3):
                    DNet.PermissionManager.RegisterEntity(Roles[index + selectionIndex], AccessLevels.CommandManager);
                    break;
                case (4):
                    DNet.PermissionManager.RegisterEntity(Roles[index + selectionIndex], AccessLevels.Administrator);
                    break;
                default:
                    break;
            }

            UpdateFooter(page, max);                //restore footer

            #endregion

            Console.ForegroundColor = PRVFG;
            Console.BackgroundColor = PRVBG;
            return true;

        }
        #endregion

        #region Role Listing Methods

        private int PopulateList(short page, short max, ref int index, int selectionIndex, ref int ppg, ref List<SocketRole> roles)
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

            WriteEntry($"\u2502\u2005\u2005\u2005 - {"Role Name".PadRight(39, '\u2005')} {"Role ID".PadRight(22, '\u2005')} {"Access Level".PadRight(16, '\u2005')} {"Is Admin".PadLeft(10, '\u2005')}", ConsoleColor.Blue,false);
            WriteEntry($"\u2502\u2005\u2005\u2005 \u2500 {"".PadRight(39, '\u2500')} {"".PadLeft(22, '\u2500')} {"".PadRight(16, '\u2500')} {"".PadLeft(10, '\u2500')}", ConsoleColor.Blue,false);
            
            for (int i = index; i < 22 * page; i++)//22 results per page.
            {
                if (i >= roles.Count)
                {
                    break;
                }
                countOnPage++;
                WriteRole(selectionIndex, countOnPage, roles, i);
            }

            Console.SetCursorPosition(0, 0);
            return countOnPage;
        }

        private void WriteRole(int selectionIndex, int countOnPage, List<SocketRole> roles, int i)
        {
            string name = roles[i]?.Name ?? "[Pending...]";

            if (name.Length > 36)
            {
                name = name.Remove(36) + "...";
            }
            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(name))).Replace(' ', '\u2005').Replace("??", "?");
            string p = $"{o}".PadRight(39, '\u2005');
            string admin = roles[i].Permissions.Administrator ? "Yes".PadLeft(10, '\u2005') : "No".PadLeft(10, '\u2005');
            string accesslevel = DNet.PermissionManager.GetAccessLevel(roles.ElementAt(i)).ToString().PadRight(16, '\u2005');
            WriteEntry($"\u2502\u2005\u2005\u2005 - {p} [{roles.ElementAt(i).Id.ToString().PadLeft(20, '0')}] {accesslevel} {admin}", (countOnPage - 1) == selectionIndex, ConsoleColor.DarkGreen, false);


        }

        private string GetSafeName(List<SocketRole> roles, int i)
        {
            string userinput = roles.ElementAt(i).Name ?? "[Pending...]";
            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(userinput))).Replace(' ', '\u2005').Replace("??", "?");
            
            if (o.Length > 17)
            {
                o = o.Remove(13) + "...";
            }
            
            string p = $"{o}";
            
            return p;
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

        #endregion
    }
}
