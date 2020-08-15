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

        private void OSS_RenderOptions(string option1, string option2, string option3, string option4, int selectionindex, int cl, int ct) //Sub-screen options...
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

        #region SCREENS

        #region User Listing

        internal bool ListUsers(ref DiscordNET discord, ulong guildID, string query, bool FromModal=false)
        {
            int selectionIndex = 0;
            int CursorOffset = 1;
            int countOnPage = 0;
            int ppg = 0;
            short page = 1;
            string[] array = query.Split('#');
            string userquery = "";
            string discrimquery = "";
            if (array.Length > 1)
            {
                userquery = array[0];
                discrimquery = array[1];
            }
            SocketGuild g = discord.Client.GetGuild(guildID);
            if (g == null)
            {
                return false;
            }
            g.DownloadUsersAsync();
            List<SocketGuildUser> guildusers = g.Users.ToList().OrderByDescending(x => (int)(x.Hierarchy)).ToList();
            if (array.Length > 1)
            {
                guildusers = guildusers.FindAll(x => (x.Username.ToLower() + "#" + x.Discriminator).Contains(userquery.ToLower() + "#" + discrimquery));
                //output all results containing the query
            }
            if (array.Length == 1)
            {
                guildusers = guildusers.FindAll(x => x.Username.ToLower().Contains(query.ToLower()));
            }

            string name = g.Name.Length > 17 ? g.Name.Remove(17) : g.Name;


            short max = (short)(Math.Ceiling((double)(guildusers.Count / 22)) + 1);
            if (page > max)
            {
                page = max;
            }
            if (page < 1)
            {
                page = 1;
            }
            int index = (page * 22) - 22;
            if (!FromModal) ScreenModal = true;


            while (true)
            {
                if (ppg != page)//is page changing?
                {
                    CursorOffset = ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Searching for '{query}' in {name}", page, max, ConsoleColor.White);
                    ppg = page;
                }

                countOnPage = 0;
                if (ppg == page)
                {
                    Console.SetCursorPosition(0, 5);
                }

                WriteEntry($"\u2502\u2005\u2005\u2005 - {"Discord User".PadRight(39, '\u2005')} {"Entity ID".PadRight(22, '\u2005')} {"Access Level".PadRight(18, '\u2005')} {"G.Admin".PadRight(7, '\u2005')} {"G.Owner".PadRight(7, '\u2005')}", ConsoleColor.Blue);
                WriteEntry($"\u2502\u2005\u2005\u2005 \u2500 {"".PadRight(39, '\u2500')} {"".PadLeft(22, '\u2500')} {"".PadLeft(18, '\u2500')} {"".PadLeft(7, '\u2500')} {"".PadLeft(7, '\u2500')}", ConsoleColor.Blue);
                if (guildusers.Count == 0)
                {
                    WriteEntry($"\u2502\u2005\u2005\u2005 - No users found... :(", ConsoleColor.DarkRed);
                }
                for (int i = index; i < 22 * page; i++)//22 results per page.
                {

                    if (index >= guildusers.Count)
                    {
                        break;
                    }
                    countOnPage++;
                    WriteUser(discord, selectionIndex, countOnPage, guildusers, i);
                    index++;
                }
                WriteEntry($"\u2502");
                string UDPROMPT = "| UP/DOWN: Move Selection | ENTER: Properties...";
                if (guildusers.Count == 0)
                {
                    UDPROMPT = "| NO SELECTION AVAILABLE";
                }
                //string UDPROMPT_T = discord.PermissionManager.DefaultAdmin.EntityID == guildusers[selectionIndex + index].Id ? "| UP/DOWN: Move Selection" : UDPROMPT;
                if (page > 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | P: Previous Page | E: Exit list {UDPROMPT}", ConsoleColor.White);
                }
                if (page == 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | E: Exit list {UDPROMPT}", ConsoleColor.White);
                }
                if (page == 1 && page == max)
                {
                    WriteEntry($"\u2502 E: Exit list {UDPROMPT}", ConsoleColor.White);
                }
                if (page > 1 && page == max)
                {
                    WriteEntry($"\u2502 P: Previous Page | E: Exit list {UDPROMPT}", ConsoleColor.White);
                }
                ConsoleKeyInfo s = Console.ReadKey(true);
                if (s.Key == ConsoleKey.P)
                {
                    ppg = page;
                    if (page > 1)
                    {
                        page--;
                        index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                        //continue;
                    }
                    selectionIndex = 0;
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
                if (s.Key == ConsoleKey.E)
                {
                    break;
                }
                if (s.Key == ConsoleKey.N)
                {
                    ppg = page;
                    if (page < max)
                    {

                        page++;
                    }
                    selectionIndex = 0;
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }

                if (s.Key == ConsoleKey.UpArrow)
                {

                    selectionIndex--;
                    if (selectionIndex < 0)
                    {
                        selectionIndex = countOnPage - 1;
                    }

                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.

                    continue;
                }
                if (s.Key == ConsoleKey.DownArrow)
                {

                    selectionIndex++;
                    if (selectionIndex > countOnPage - 1)
                    {
                        selectionIndex = 0;
                    }


                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }

                if (s.Key == ConsoleKey.Enter)
                {

                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.

                    if (discord.PermissionManager.DefaultAdmin.EntityID == guildusers[selectionIndex + index].Id)
                    {
                        continue;
                    }
                    #region SubScreen
                    string username = SafeName(guildusers, index + selectionIndex);
                    //int left = 71-20;
                    //int top = 16 - 7;
                    //Console.CursorLeft = left;
                    //Console.CursorTop = top;
                    ConsoleColor PRVBG = Console.BackgroundColor;
                    ConsoleColor PRVFG = Console.ForegroundColor;
                    int rr = -1;
                    rr = ShowOptionSubScreen($"Editing: {username}", "Please select a new access level", "BlackListed", "Normal", "Command Manager", "Administrator");

                    switch (rr)
                    {
                        case (1):
                            discord.PermissionManager.RegisterEntity(guildusers[selectionIndex + index], AccessLevels.Blacklisted);
                            break;
                        case (2):
                            if (discord.PermissionManager.IsEntityRegistered(guildusers[selectionIndex + index]))
                            {
                                discord.PermissionManager.DeleteEntity(guildusers[selectionIndex + index]);
                            }
                            break;
                        case (3):
                            discord.PermissionManager.RegisterEntity(guildusers[selectionIndex + index], AccessLevels.CommandManager);
                            break;
                        case (4):
                            discord.PermissionManager.RegisterEntity(guildusers[selectionIndex + index], AccessLevels.Administrator);
                            break;
                        default:
                            break;
                    }
                    #endregion


                    Console.ForegroundColor = PRVFG;
                    Console.BackgroundColor = PRVBG;
                    continue;
                }

                else
                {
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
            }

            if (!FromModal) ScreenModal = false;
            return true;

        }  //User Search Screen

        private void WriteUser(DiscordNET discord, int selectionIndex, int countOnPage, List<SocketGuildUser> guildusers, int i)
        {
            string userinput = guildusers.ElementAt(i).Username;
            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(userinput))).Replace(' ', '\u2005').Replace("??", "?");
            string p = $"{o}#{guildusers.ElementAt(i).Discriminator}".PadRight(39, '\u2005');
            string igo = guildusers.ElementAt(i).Id == guildusers.ElementAt(i).Guild.OwnerId ? "X" : "";
            string iga = guildusers.ElementAt(i).GuildPermissions.Has(GuildPermission.Administrator) ? "X" : "";
            WriteEntry($"\u2502\u2005\u2005 - {p} [{guildusers.ElementAt(i).Id.ToString().PadLeft(20, '0')}] {discord.PermissionManager.GetAccessLevel(guildusers.ElementAt(i)).ToString().PadRight(18, '\u2005')} {iga.PadRight(7, '\u2005')} {igo.PadRight(7, '\u2005')}", (countOnPage - 1) == selectionIndex, ConsoleColor.DarkGreen);
        }

        private string SafeName(List<SocketGuildUser> guildusers, int i)
        {
            string userinput = guildusers.ElementAt(i).Username;
            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(userinput))).Replace(' ', '\u2005').Replace("??", "?");
            string p = $"{o}#{guildusers.ElementAt(i).Discriminator}";
            return p;
        }

        #endregion

        //TODO: Make ALL screens with selection mode

        #region Channel Listing
        internal bool ListChannels(ref DiscordNET discord, ulong guildID, short page=1,bool FromModal=false)
        {
            SocketGuild g = discord.Client.GetGuild(guildID);
            if (g == null)
            {
                return false;
            }
            List<SocketGuildChannel> channels = g.Channels.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();//ignore unknown/invalid channels.
            short max = (short)(Math.Ceiling((double)(channels.Count / 22)) + 1);
            int index = 0;
            int selectionIndex = 0;
            int countOnPage = 0;
            int ppg = 0;
            if (!FromModal) ScreenModal = true;
            string name = g.Name.Length > 17 ? g.Name.Remove(17) : g.Name;

            if (page > max)
            {
                page = max;
            }
            if (page < 1)
            {
                page = 1;
            }
            index = (page * 22) - 22;

            while (true)
            {
                countOnPage = PopulateChannelList(page, selectionIndex, ref ppg, channels, name, max, ref index);
                ConsoleKeyInfo s = Console.ReadKey(true);
                if (s.Key == ConsoleKey.P)
                {
                    ppg = page;
                    if (page > 1)
                    {
                        page--;
                        index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                        //continue;
                    }
                    selectionIndex = 0;
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
                if (s.Key == ConsoleKey.E)
                {
                    break;
                }
                if (s.Key == ConsoleKey.N)
                {
                    ppg = page;
                    if (page < max)
                    {

                        page++;
                    }
                    selectionIndex = 0;
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }

                if (s.Key == ConsoleKey.UpArrow)
                {

                    selectionIndex--;
                    if (selectionIndex < 0)
                    {
                        selectionIndex = countOnPage - 1;
                    }

                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.

                    continue;
                }
                if (s.Key == ConsoleKey.DownArrow)
                {

                    selectionIndex++;
                    if (selectionIndex > countOnPage - 1)
                    {
                        selectionIndex = 0;
                    }


                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }

                if (s.Key == ConsoleKey.Enter)
                {

                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 22; etc.

                   
                    #region SubScreen
                    string channelname = SafeName(channels, index + selectionIndex);

                    ConsoleColor PRVBG = Console.BackgroundColor;
                    ConsoleColor PRVFG = Console.ForegroundColor;
                    int rr = -1;

                    string opt_Delete = g.CurrentUser.GetPermissions(channels[index + selectionIndex]).Has(ChannelPermission.ManageChannels) ? "Delete" : "-";
                    rr = ShowOptionSubScreen($"Manage: {channelname}", "Select an action", "Copy ID", "-", "-", opt_Delete);

                    switch (rr)
                    {
                        case (1):

                            Thread thread = new Thread(() => Clipboard.SetText(channels[selectionIndex + index].Id.ToString()));
                            thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                            thread.Start();
                            thread.Join(); //Wait for the thread to end
                            ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Listing all channels for Guild: {name}", page, max, ConsoleColor.White);

                            break;
                        case (2):
                            //TODO: View messages
                            ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Listing all channels for Guild: {name}", page, max, ConsoleColor.White);

                            break;
                        case (3):
                            //TODO: SEND Channel message
                            ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Listing all channels for Guild: {name}", page, max, ConsoleColor.White);

                            break;
                        case (4):

                            countOnPage = PopulateChannelList(page,selectionIndex,ref ppg,channels,name,max,ref index);
                            index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                            int confirmdel = ShowOptionSubScreen($"Delete: {channelname}", "Are you sure?", "-", "NO", "YES", "-", ConsoleColor.Red);
                            if (confirmdel == 3) { g.GetChannel(channels[index + selectionIndex].Id).DeleteAsync(); channels.Remove(channels[selectionIndex + index]); }
                            //countOnPage = PopulateChannelList(discord, page, selectionIndex, ref ppg, channels, name, max, ref index);
                            ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Listing all channels for Guild: {name}", page, max, ConsoleColor.White);

                            break;
                        default:
                            ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Listing all channels for Guild: {name}", page, max, ConsoleColor.White);

                            break;
                    }
                    #endregion


                    Console.ForegroundColor = PRVFG;
                    Console.BackgroundColor = PRVBG;
                    continue;
                }

                else
                {
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
            }

            if (!FromModal) ScreenModal = false;
            return true;


        }

        private int PopulateChannelList(short page, int selectionIndex, ref int ppg, List<SocketGuildChannel> channels, string name, short max, ref int index)
        {
            int countOnPage;
            if (ppg != page)//is page changing?
            {
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Listing all channels for Guild: {name}", page, max, ConsoleColor.White);
                ppg = page;
            }

            countOnPage = 0;
            if (ppg == page)
            {
                Console.SetCursorPosition(0, 5);
            }

            WriteEntry($"\u2502\u2005\u2005\u2005 - {"Channel".PadRight(39, '\u2005')} {"Entity ID".PadRight(22, '\u2005')} {"Channel Type".PadRight(24, '\u2005')}", ConsoleColor.Blue);
            WriteEntry($"\u2502\u2005\u2005\u2005 \u2500 {"".PadRight(39, '\u2500')} {"".PadLeft(22, '\u2500')} {"".PadLeft(24, '\u2500')}", ConsoleColor.Blue);
            for (int i = index; i < 22 * page; i++)//22 results per page.
            {

                if (index >= channels.Count)
                {
                    break;
                }
                countOnPage++;
                WriteChannel(selectionIndex, countOnPage, channels, i);
                index++;
            }
            WriteEntry($"\u2502");
            string UDPROMPT = "| UP/DOWN: Move Selection | ENTER: Properties...";
            //string UDPROMPT_T = discord.PermissionManager.DefaultAdmin.EntityID == guildusers[selectionIndex + index].Id ? "| UP/DOWN: Move Selection" : UDPROMPT;
            if (page > 1 && page < max)
            {
                WriteEntry($"\u2502 N: Next Page | P: Previous Page | E: Exit list {UDPROMPT}", ConsoleColor.White);
            }
            if (page == 1 && page < max)
            {
                WriteEntry($"\u2502 N: Next Page | E: Exit list {UDPROMPT}", ConsoleColor.White);
            }
            if (page == 1 && page == max)
            {
                WriteEntry($"\u2502 E: Exit list {UDPROMPT}", ConsoleColor.White);
            }
            if (page > 1 && page == max)
            {
                WriteEntry($"\u2502 P: Previous Page | E: Exit list {UDPROMPT}", ConsoleColor.White);
            }

            return countOnPage;
        }

        private void WriteChannel(int selectionIndex, int countOnPage, List<SocketGuildChannel> Channels, int i)
        {
            
            string channelin = Channels.ElementAt(i).Name ?? "Unsupported channel";
            string chtype = Channels.ElementAt(i).GetType().ToString();
            string chltype = chtype.Remove(0, chtype.LastIndexOf('.') + 1).Replace("Socket", "").Replace("Channel", "");
            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(channelin))).Replace(' ', '\u2005').Replace("??", "?");
            if (o.Length > 39)
            {
                o = o.Remove(35) + "...";
            }
            string p = $"{o}".PadRight(39, '\u2005');
            WriteEntry($"\u2502\u2005\u2005 - {p} [{Channels.ElementAt(i).Id.ToString().PadLeft(20, '0')}] {chltype.PadRight(22, '\u2005')}", (countOnPage - 1) == selectionIndex, ConsoleColor.DarkGreen);
        }

        private string SafeName(List<SocketGuildChannel> channels, int i)
        {
            string channelname = channels.ElementAt(i).Name ?? "Unsupported Channel";

            string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(channelname))).Replace(' ', '\u2005').Replace("??", "?");
            if(o.Length > 39)
            {
                o = o.Remove(35) + "...";
            }
            string p = $"{o}";
            return p;
        }

        #endregion

        internal bool ListCURoles(ref DiscordNET discord, ulong guildID, short page = 1, bool FromModal=false)
        {
            SocketGuild g = discord.Client.GetGuild(guildID);
            if (g == null)
            {
                return false;
            }
            string name = g.Name.Length > 17 ? g.Name.Remove(17) : g.Name;


            short max = (short)(Math.Ceiling((double)(g.CurrentUser.Roles.Count / 24)) + 1);
            if (page > max)
            {
                page = max;
            }
            if (page < 1)
            {
                page = 1;
            }
            int index = (page * 24) - 24;
            if (!FromModal) ScreenModal = true;


            while (true)
            {
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Bot's assigned Roles for Guild: {name}", page, max, ConsoleColor.White);
                WriteEntry($"\u2502\u2005\u2005\u2005 - {"Role Name".PadRight(39, '\u2005')} {"Snowflake ID".PadRight(22, '\u2005')}", ConsoleColor.Blue);
                WriteEntry($"\u2502\u2005\u2005\u2005 \u2500 {"".PadRight(39, '\u2500')} {"".PadLeft(22, '\u2500')}", ConsoleColor.Blue);
                for (int i = index; i < 22 * page; i++)//22 results per page.
                {
                    if (index >= g.CurrentUser.Roles.Count)
                    {
                        break;
                    }
                    string channelin = g.CurrentUser.Roles.ElementAt(i).Name;
                    string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(channelin))).Replace(' ', '\u2005').Replace("??", "?");
                    string p = $"{o}".PadRight(39, '\u2005');
                    WriteEntry($"\u2502\u2005\u2005\u2005 - {p} [{g.CurrentUser.Roles.ElementAt(i).Id.ToString().PadLeft(20, '0')}]", ConsoleColor.DarkGreen);
                    index++;
                }
                WriteEntry($"\u2502");
                if (page > 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page == max)
                {
                    WriteEntry($"\u2502 E: Exit list", ConsoleColor.White);
                }
                if (page > 1 && page == max)
                {
                    WriteEntry($"\u2502 P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                ConsoleKeyInfo s = Console.ReadKey();
                if (s.Key == ConsoleKey.P)
                {
                    if (page > 1)
                    {
                        page--;
                        //continue;
                    }
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
                if (s.Key == ConsoleKey.E)
                {
                    break;
                }
                if (s.Key == ConsoleKey.N)
                {
                    if (page < max)
                    {
                        page++;
                    }

                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }

                else
                {
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
            }

            if (!FromModal) ScreenModal = false;
            return true;

        }

        internal bool ListRoles(ref DiscordNET discord, ulong guildID, short page = 1, bool FromModal=false)
        {
            SocketGuild g = discord.Client.GetGuild(guildID);
            if (g == null)
            {
                return false;
            }
            string name = g.Name.Length > 17 ? g.Name.Remove(17) : g.Name;


            short max = (short)(Math.Ceiling((double)(g.Roles.Count / 24)) + 1);
            if (page > max)
            {
                page = max;
            }
            if (page < 1)
            {
                page = 1;
            }
            int index = (page * 24) - 24;
            if (!FromModal) ScreenModal = true;


            while (true)
            {
                ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, $"Bot's assigned Roles for Guild: {name}", page, max, ConsoleColor.White);
                WriteEntry($"\u2502\u2005\u2005\u2005 - {"Role Name".PadRight(39, '\u2005')} {"Snowflake ID".PadRight(22, '\u2005')}", ConsoleColor.Blue);
                WriteEntry($"\u2502\u2005\u2005\u2005 \u2500 {"".PadRight(39, '\u2500')} {"".PadLeft(22, '\u2500')}", ConsoleColor.Blue);
                for (int i = index; i < 22 * page; i++)//22 results per page.
                {
                    if (index >= g.Roles.Count)
                    {
                        break;
                    }
                    string channelin = g.Roles.ElementAt(i).Name;
                    string o = Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback("?"), new DecoderExceptionFallback()), Encoding.Unicode.GetBytes(channelin))).Replace(' ', '\u2005').Replace("??", "?");
                    string p = $"{o}".PadRight(39, '\u2005');
                    WriteEntry($"\u2502\u2005\u2005\u2005 - {p} [{g.Roles.ElementAt(i).Id.ToString().PadLeft(20, '0')}]", ConsoleColor.DarkGreen);
                    index++;
                }
                WriteEntry($"\u2502");
                if (page > 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page < max)
                {
                    WriteEntry($"\u2502 N: Next Page | E: Exit list", ConsoleColor.White);
                }
                if (page == 1 && page == max)
                {
                    WriteEntry($"\u2502 E: Exit list", ConsoleColor.White);
                }
                if (page > 1 && page == max)
                {
                    WriteEntry($"\u2502 P: Previous Page | E: Exit list", ConsoleColor.White);
                }
                ConsoleKeyInfo s = Console.ReadKey();
                if (s.Key == ConsoleKey.P)
                {
                    if (page > 1)
                    {
                        page--;
                    }
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
                if (s.Key == ConsoleKey.E)
                {
                    break;
                }
                if (s.Key == ConsoleKey.N)
                {
                    if (page < max)
                    {
                        page++;
                    }

                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }

                else
                {
                    index = (page * 22) - 22;//0 page 1 = 0; page 2 = 20; etc.
                    continue;
                }
            }

            if (!FromModal) ScreenModal = false;
            return true;

        }

        #endregion

        #endregion

        #region INTERNAL Methods

        internal void ProcessQueue()
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
                SpinWait.SpinUntil(() => Backlog.Count > 0);
                if (ScreenBusy) { continue; }                       //If the screen's busy (Resetting), DO NOT DQ!
                if (Writing) { continue; }                          //If the console is in the middle of writing, DO NOT DQ!

                LogEntry qitem = Backlog.Dequeue();                 //DQ the item and process it as qitem.
                LatestEntry = qitem;
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

                if (ScreenModal && !bypassScreenLock)
                {
                    continue;                                       //Do not output
                }
                Writing = true;
                PrvTop = Console.CursorTop;
                Console.SetCursorPosition(0, Console.CursorTop);    //Reset line position.
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

        internal Task GetConsoleInput(ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET) //TODO: Re-write for "Snap-in" commands. (additional Console commands from modules)
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
                string input = Console.ReadLine();
                string unproc = input;
                if (InputCanceled)
                {
                    return Task.Delay(0);
                }
                Console.CursorTop = CurTop;

                #region Console Command Statements

                ConsoleCommand cm = ConsoleCommands.FirstOrDefault(x => x.CommandName == input.Split(' ')[0]);
                if(cm == null)
                {
                    WriteEntry(new LogMessage(LogSeverity.Info, "Console", "unknown command"), null, true, false, true);
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

            ScreenBusy = true;
            if (title.Length > 72)
            {
                title = title.Remove(71) + "...";
            }
            ConsoleTitle = title;
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
            Console.SetWindowSize(144, 32);//Seems to be a reasonable console size.
            Console.SetBufferSize(144, 512);//Extra buffer room just because why not.
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
        public Task<bool> ShowKillScreen(string title, string message, bool autorestart, ref bool ProgramShutdownFlag, ref bool ProgramRestartFlag, int timeout = 5, Exception ex = null)
        {
            ScreenModal = true;
            ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkRed, title);
            WriteEntry(new LogMessage(LogSeverity.Critical, "MAIN", "The program encountered a problem, and was terminated. Details below."), null, true, true);
            LogMessage m = new LogMessage(LogSeverity.Critical, "CRITICAL", message);
            WriteEntry(m, null, true, true, false);

            WriteEntry(new LogMessage(LogSeverity.Info, "MAIN", "writing error report to CRASH.LOG"), null, true, true, false);
            CreateCrashLog(ex, m);
            WriteEntry(new LogMessage(LogSeverity.Info, "MAIN", "Writing additional information to ERRORS.LOG"), null, true, true, false);
            WriteErrorsLog(ex);

            if (!autorestart)
            {
                WriteEntry(new LogMessage(LogSeverity.Info, "MAIN", "Press any key to terminate..."), null, true, true, false);
                Console.ReadKey();
            }
            else
            {
                //prompt for autorestart.
                for (int i = 0; i < timeout; i++)
                {
                    int l = Console.CursorLeft;
                    int t = Console.CursorTop;

                    WriteEntry(new LogMessage(LogSeverity.Info, "MAIN", $"Restarting in {timeout - i} second(s)..."), null, false, true, false);

                    Console.CursorLeft = l;
                    Console.CursorTop = t;//reset.
                    Thread.Sleep(1000);
                }

                if (!Program.AppArguments.Contains("-crashed"))
                {
                    Program.AppArguments.Add("-crashed");
                }

            }

            ScreenModal = false;
            ProgramShutdownFlag = true;
            ProgramRestartFlag = autorestart;//redundancy
            //ScreenBusy = false;
            return Task.FromResult(autorestart);//redundancy

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

        /// <summary>
        /// Write exception to errors.log
        /// </summary>
        /// <param name="ex"></param>
        public void WriteErrorsLog(Exception ex)
        {
            SpinWait.SpinUntil(() => !errorLogWrite);
            errorLogWrite = true;
            using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + ex.ToString());
                    sw.Flush();
                    sw.Close();
                }
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
                    sw.Close();
                    Thread.Sleep(150);
                }
            }
        }

        public string GetStringPrompt(string message, string title, bool IsPW, ConsoleColor FG, ConsoleColor BG, short Step, short MaxStep)
        {

            string ct = ConsoleTitle;
            ScreenModal = true;
            PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_RETURN, 0);//pause input
            ConsoleGUIReset(FG, BG, title, Step, MaxStep, FG);
            WriteEntry(message);
            List<LogEntry> v = new List<LogEntry>();
            string auth = "";

            if (IsPW)
            {
                Console.Write("\u2502 > ");
                while (true)
                {
                    string pass = "";
                    do
                    {
                        ConsoleKeyInfo key = Console.ReadKey(true);
                        // Backspace Should Not Work

                        if (!char.IsControl(key.KeyChar))
                        {
                            pass += key.KeyChar;
                            Console.Write("*");
                        }
                        else
                        {
                            if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                            {
                                pass = pass.Substring(0, (pass.Length - 1));
                                Console.Write("\b \b");
                            }
                            else if (key.Key == ConsoleKey.Enter)
                            {
                                break;
                            }
                        }
                    } while (true);
                    auth = pass;

                    if (string.IsNullOrWhiteSpace(auth))
                    {
                        WriteEntry("You cannot leave this blank. Try again.", ConsoleColor.DarkRed);
                        Console.Write("\u2502 > ");
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (!IsPW)
            {
                Console.Write("\u2502 > ");
                while (true)
                {
                    string pass = "";
                    do
                    {
                        ConsoleKeyInfo key = Console.ReadKey(true);
                        // Backspace Should Not Work
                        if (!char.IsControl(key.KeyChar))
                        {
                            pass += key.KeyChar;
                            Console.Write(key.KeyChar.ToString());
                        }
                        else
                        {
                            if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                            {
                                pass = pass.Substring(0, (pass.Length - 1));
                                Console.Write("\b \b");
                            }
                            else if (key.Key == ConsoleKey.Enter)
                            {
                                break;
                            }
                        }
                    } while (true);
                    auth = pass;

                    if (string.IsNullOrWhiteSpace(auth))
                    {
                        WriteEntry("You cannot leave this blank. Try again.", ConsoleColor.DarkRed);
                        Console.Write("\u2502 > ");
                    }
                    else
                    {
                        break;
                    }
                }
            }

            ConsoleGUIReset(Program.configMGR.CurrentConfig.ConsoleForegroundColor,
                        Program.configMGR.CurrentConfig.ConsoleBackgroundColor, ct);

            ScreenModal = false;

            v.AddRange(LogEntries);
            LogEntries.Clear();//clear buffer.
                               //output previous logEntry.
            foreach (var item in v)
            {
                WriteEntry(item.LogMessage, item.EntryColor);
            }

            return auth;
        }

        public int ShowOptionSubScreen(string title, string prompt, string Option1, string Option2, string Option3, string Option4, ConsoleColor SBG = ConsoleColor.DarkBlue, ConsoleColor SFG = ConsoleColor.White)
        {
            Console.CursorVisible = false;
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

            if(string.IsNullOrWhiteSpace(Option1) || string.IsNullOrWhiteSpace(Option2)|| string.IsNullOrWhiteSpace(Option3)|| string.IsNullOrWhiteSpace(Option4))
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
            Console.CursorLeft = left;
            Console.CursorTop = top + 4;
            OSS_RenderOptions(Option1, Option2, Option3, Option4, SelectableIndicies[SelIndex], left, top + 4);
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

            int result;
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
                    OSS_RenderOptions(Option1, Option2, Option3, Option4, SelectableIndicies[SelIndex], left, top + 4);

                }

                if (c.Key == ConsoleKey.DownArrow || c.Key == ConsoleKey.PageDown)
                {

                    SelIndex++;
                    if (SelIndex >= SelectableIndicies.Count)
                    {
                        SelIndex = 0;
                    }
                    OSS_RenderOptions(Option1, Option2, Option3, Option4, SelectableIndicies[SelIndex], left, top + 4);

                }
                if (c.Key == ConsoleKey.Enter)
                {
                    result = SelectableIndicies[SelIndex] + 1;
                    for (int i = mintop; i < maxtop + 1; i++)
                    {
                        Console.BackgroundColor = PRVBG;
                        Console.ForegroundColor = PRVFG;
                        Console.CursorLeft = minLeft - 1;
                        Console.Write("".PadRight(maxleft + 2));
                        Console.CursorTop = i;
                    }
                    break;
                }
                if (c.Key == ConsoleKey.Escape)
                {
                    result = 0;//NON-SEL
                    for (int i = mintop; i < maxtop + 1; i++)
                    {
                        Console.BackgroundColor = PRVBG;
                        Console.ForegroundColor = PRVFG;
                        Console.CursorLeft = minLeft - 1;
                        Console.Write("".PadRight(maxleft + 2));
                        Console.CursorTop = i;
                    }
                    break;
                }

            }
            #endregion

            return result;
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