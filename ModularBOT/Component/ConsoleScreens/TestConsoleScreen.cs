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
            Meta            = "This is a test.";
            ShowProgressBar = true;
            ShowMeta        = true;

            ProgressVal     = 1;
            ProgressMax     = 2;
            BufferHeight    = 34;
            WindowHeight    = 32;
            
        }

        public override bool ProcessInput(ConsoleKeyInfo keyinfo)
        {
            if(keyinfo.Key == ConsoleKey.F3)
            {
                string val = ShowStringPrompt("Test StringPrompt", "Please enter some text below.");
                ScreenFontColor = ConsoleColor.Cyan;
                ScreenBackColor = ConsoleColor.Black;
                TitlesBackColor = ConsoleColor.Black;
                TitlesFontColor = ConsoleColor.White;
                ProgressColor = ConsoleColor.Green;
                RenderScreen();//reset;
                if(val !=null)
                {
                    WriteEntry($"Prompt response: {val}", ConsoleColor.Red, false);
                }
            }
            return base.ProcessInput(keyinfo);
        }

        protected override void RenderContents()
        {
            SpinWait.SpinUntil(() => !LayoutUpdating);
            ScreenFontColor = ConsoleColor.DarkBlue;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Blue;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkRed;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Red;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkYellow;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Yellow;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkGreen;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Green;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkMagenta;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Magenta;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkGray;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Gray;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.White;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.DarkCyan;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenFontColor = ConsoleColor.Cyan;
            WriteEntry("this is a test!", ConsoleColor.Red, false);

            ScreenBackColor = ConsoleColor.DarkBlue;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.Blue;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.DarkRed;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.Red;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.DarkYellow;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.Yellow;
            WriteEntry("this is a test!", ConsoleColor.Red, false);
            ScreenBackColor = ConsoleColor.DarkGreen;
        }
    }
}
