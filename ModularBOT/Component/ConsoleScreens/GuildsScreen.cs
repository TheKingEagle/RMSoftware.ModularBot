using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Entity;
using System.Reflection;
using System.Threading;
using Discord.WebSocket;

namespace ModularBOT.Component.ConsoleScreens
{
    public class GuildsScreen : ConsoleScreen
    {
        private List<SocketGuild> guildlist { get; set; } = new List<SocketGuild>();
        List<SocketGuildUser> agregateUserList { get; set; } = new List<SocketGuildUser>();

        public GuildsScreen(List<SocketGuild> guilds, DiscordShardedClient client)
        {
            guildlist = guilds;
            foreach (SocketGuild item in guildlist)
            {
                item.DownloadUsersAsync();
                agregateUserList.AddRange(item.Users);
            }
            agregateUserList = agregateUserList.Distinct().ToList();//remove duplicates.
            long usercount = agregateUserList.LongCount();
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            TitlesBackColor = ConsoleColor.Black;
            TitlesFontColor = ConsoleColor.White;
            ProgressColor = ConsoleColor.Cyan;

            Title = $"Guilds | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            Meta = $"There are {usercount} players in {guildlist.Count} guilds.";
            ShowProgressBar = true;
            ShowMeta = true;

            ProgressVal = 15;
            ProgressMax = 102;
            BufferHeight = 34;
            WindowHeight = 32;

        }
        
        public override bool ProcessInput(ConsoleKeyInfo keyinfo)
        {
            //TODO: Custom input handling: NOTE -- Base adds exit handler [E] key.
           
            return base.ProcessInput(keyinfo);
        }

        protected override void RenderContents()
        {
            SpinWait.SpinUntil(() => !LayoutUpdating);
            WriteEntry("\u2502\u2005\u2005\u2005 - TODO: List guilds, write guilds, add keybindings for guilds", ConsoleColor.Blue, false);
            WriteEntry("\u2502\u2005\u2005\u2005 - TEST: [DEBG] This is the DEBUG color", ConsoleColor.Gray, false, ConsoleColor.White, ConsoleColor.Gray);
            WriteEntry("\u2502\u2005\u2005\u2005 - TEST: [VERB] This is the VERBOSE color", ConsoleColor.Cyan, false,ConsoleColor.White,ConsoleColor.Magenta);
            WriteEntry("\u2502\u2005\u2005\u2005 - TEST: [INFO] This is the INFO color", ConsoleColor.Black, false);
            WriteEntry("\u2502\u2005\u2005\u2005 - TEST: [WARN] This is the WARNING color", ConsoleColor.Yellow, false);
            WriteEntry("\u2502\u2005\u2005\u2005 - TEST: [ERRO] This is the ERROR color", ConsoleColor.DarkRed, false);
            WriteEntry("\u2502\u2005\u2005\u2005 - TEST: [CRIT] This is the CRITICAL color", ConsoleColor.Red, false);
            WriteEntry("\u2502\u2005\u2005\u2005 - TEST: [RNDM] This is the Light Blue", ConsoleColor.Blue, false, ConsoleColor.White, ConsoleColor.White);
            ScreenBackColor = ConsoleColor.Gray;
            ScreenFontColor = ConsoleColor.Black;
            int CT = Console.CursorTop;
            Console.CursorTop = 31;
            WriteEntry("\u2502 [ESC] Exit \u2502 [N] Next Page \u2502 [P] Previous Page \u2502 [UP/DOWN] Select \u2502 [ENTER] Properties... \u2502".PadRight(141,'\u2005')+ "\u2502", ConsoleColor.DarkGray, false,ConsoleColor.Gray);
            Console.CursorTop = 0;
            Console.CursorTop = CT;
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
        }

    }
}
