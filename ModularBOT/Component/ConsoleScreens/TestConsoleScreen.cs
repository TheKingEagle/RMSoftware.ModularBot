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
    public class TestConsoleScreen: ConsoleScreen
    {
        public TestConsoleScreen()
        {
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            TitlesBackColor = ConsoleColor.Black;
            TitlesFontColor = ConsoleColor.White;
            ProgressColor   = ConsoleColor.Green;

            Title           = $"Test Console Screen | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            Meta            = "This is a fucking test.";
            ShowProgressBar = true;
            ShowMeta        = true;

            ProgressVal     = 1;
            ProgressMax     = 2;
            BufferHeight    = 36;
            WindowHeight    = 35;
            
        }

        public override bool ProcessInput(ConsoleKeyInfo keyinfo)
        {
            return base.ProcessInput(keyinfo);
        }

        protected override void RenderContents()
        {
            SpinWait.SpinUntil(() => !LayoutUpdating);
            ScreenFontColor = ConsoleColor.DarkBlue;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Blue;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkRed;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Red;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkYellow;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Yellow;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkGreen;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Green;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkMagenta;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Magenta;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkGray;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Gray;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.White;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkCyan;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Cyan;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);

            ScreenBackColor = ConsoleColor.DarkBlue;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.Blue;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.DarkRed;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.Red;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.DarkYellow;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.Yellow;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.DarkGreen;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.Green;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.DarkMagenta;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.Magenta;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.DarkGray;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.Gray;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.White;
            WriteEntry("This is a fucking test!", ConsoleColor.Red, false);
            Console.CursorTop = 0;
        }
    }
}
