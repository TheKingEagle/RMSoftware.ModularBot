using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Entity;
using System.Reflection;
using System.Threading;
namespace ModularBOT.Component.ConsoleScreens
{
    public class PromptScreen : ConsoleScreen
    {
        string tx = "";
        public PromptScreen(string title, string text)
        {
            ScreenFontColor = ConsoleColor.White;
            ScreenBackColor = ConsoleColor.DarkBlue;
            TitlesBackColor = ConsoleColor.Black;
            TitlesFontColor = ConsoleColor.White;
            ProgressColor = ConsoleColor.Green;

            Title = $"{title} | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version}";
            Meta = "Please confirm your choice.";
            ShowProgressBar = false;
            ShowMeta = true;

            ProgressVal = 1;
            ProgressMax = 2;
            BufferHeight = 34;
            WindowHeight = 32;
            tx = text;
        }

        public override bool ProcessInput(ConsoleKeyInfo keyinfo)
        {
            return base.ProcessInput(keyinfo);
        }
        public int Show(string title, string text, ConsoleColor PromptBackColor, ConsoleColor PromptForeColor)
        {
            return ShowOptionSubScreen(title, text, "-", "Yes", "No", "-", PromptBackColor, PromptForeColor);
        }
        protected override void RenderContents()
        {
            //SpinWait.SpinUntil(() => !LayoutUpdating);
            WriteEntry($"{tx}", ConsoleColor.DarkRed, false,ConsoleColor.White,null,ScreenBackColor);
            //ScreenBackColor = ConsoleColor.Gray;
            //ScreenFontColor = ConsoleColor.Black;
            //Console.CursorTop = 0;
            WriteFooter("[ENTER] Confirm selection... \u2502 [ESC] Cancel");
            
        }

        private void WriteFooter(string footer)
        {
            LayoutUpdating = true;
            ScreenBackColor = ConsoleColor.Gray;
            ScreenFontColor = ConsoleColor.Black;
            int CT = Console.CursorTop;
            Console.CursorTop = 31;
            WriteEntry($"\u2502 {footer} \u2502".PadRight(141, '\u2005') + "\u2502", ConsoleColor.Gray, false, ConsoleColor.Gray, null, ConsoleColor.Gray);
            Console.CursorTop = 0;
            Console.CursorTop = CT;
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            LayoutUpdating = false;
        }
    }
}
