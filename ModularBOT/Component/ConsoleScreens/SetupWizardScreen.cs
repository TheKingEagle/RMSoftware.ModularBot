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
    public class SetupWizardScreen : ConsoleScreen
    {
        public SetupWizardScreen()
        {
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            TitlesBackColor = ConsoleColor.Black;
            TitlesFontColor = ConsoleColor.White;
            ProgressColor = ConsoleColor.Green;

            Title = $"SetupWizardScreen | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            Meta = "Template Meta";
            ShowProgressBar = false;
            ShowMeta = false;

            ProgressVal = 1;
            ProgressMax = 2;
            BufferHeight = 36;
            WindowHeight = 35;

        }

        public override bool ProcessInput(ConsoleKeyInfo keyinfo)
        {
            //TODO: Custom input handling: NOTE -- Base adds exit handler [E] key.
            return base.ProcessInput(keyinfo);
        }

        protected override void RenderContents()
        {
            SpinWait.SpinUntil(() => !LayoutUpdating);
            WriteEntry("|\u2005\u2005\u2005 - TODO: do your custom implementation here.", ConsoleColor.Blue, false);
            ScreenBackColor = ConsoleColor.Gray;
            ScreenFontColor = ConsoleColor.Black;
            WriteEntry("PRESS [E] to Exit", ConsoleColor.Blue, false);
            Console.CursorTop = 0;
        }

    }
}
