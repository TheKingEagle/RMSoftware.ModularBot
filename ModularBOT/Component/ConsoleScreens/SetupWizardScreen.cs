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
        private int SetupStep = 1;
        public SetupWizardScreen(int startingStep = 1)
        {
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            TitlesBackColor = ConsoleColor.Black;
            TitlesFontColor = ConsoleColor.White;
            ProgressColor = ConsoleColor.Green;
            MetaFontColor = ConsoleColor.Yellow;

            Title = $"Setup Wizard | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            Meta = "Welcome -- Getting Started";
            ShowProgressBar = true;
            ShowMeta = true;

            ProgressVal = 1;
            ProgressMax = 8;
            BufferHeight = 34;
            WindowHeight = 32;

        }

        public override bool ProcessInput(ConsoleKeyInfo keyinfo)
        {
            if (keyinfo.Key == ConsoleKey.Enter)
            {
                if (SetupStep < 8)
                {
                    SetupStep++;
                    ProgressVal = SetupStep;
                    ClearContents();
                    RenderContents();
                }
            }
            switch (SetupStep)
            {
                case (1): Input_Step_1(keyinfo); break;
                case (2): Input_Step_2(keyinfo); break;
                case (3): Input_Step_3(keyinfo); break;
                case (4): Input_Step_4(keyinfo); break;
                case (5): Input_Step_5(keyinfo); break;
                case (6): Input_Step_6(keyinfo); break;
                case (7): Input_Step_7(keyinfo); break;
                case (8): Input_Step_8(keyinfo); break;
                default:
                    break;
            }
            
            return base.ProcessInput(keyinfo);
        }

        protected override void RenderContents()
        {
            SpinWait.SpinUntil(() => !LayoutUpdating);
            switch (SetupStep)
            {
                case (1): Render_Step_1(); break;
                case (2): Render_Step_2(); break;
                case (3): Render_Step_3(); break;
                case (4): Render_Step_4(); break;
                case (5): Render_Step_5(); break;
                case (6): Render_Step_6(); break;
                case (7): Render_Step_7(); break;
                case (8): Render_Step_8(); break;
                default:
                    break;
            }

            WriteFooter("Press [ENTER] to Continue.");
        }

        #region Input
        private bool Input_Step_1(ConsoleKeyInfo keyinfo)
        {

            return false;
        }
        private bool Input_Step_2(ConsoleKeyInfo keyinfo)
        {
            //TODO: Show pressed chars as *
            return false;
        }
        private bool Input_Step_3(ConsoleKeyInfo keyinfo)
        {
            //TODO: Show pressed chars.
            return false;
        }
        private bool Input_Step_4(ConsoleKeyInfo keyinfo)
        {
            //TODO: Show pressed chars.
            return false;
        }
        private bool Input_Step_5(ConsoleKeyInfo keyinfo)
        {
            //TODO: Show options and allow selection
            return false;
        }
        private bool Input_Step_6(ConsoleKeyInfo keyinfo)
        {
            
            //TODO: Show options and allow selection
            //TODO: Show entered path chars
            return false;
        }
        private bool Input_Step_7(ConsoleKeyInfo keyinfo)
        {
            //TODO: Show options and allow selection
            return false;
        }
        private bool Input_Step_8(ConsoleKeyInfo keyinfo)
        {
            return true;
        }
        #endregion

        #region Render
        private void Render_Step_1()
        {
            Meta = "Initial Setup: Welcome & Privacy";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);
            
            ClearContents();
            WriteEntry("|\u2005\u2005\u2005 - TODO: step 1.", ConsoleColor.Blue, false);

        }
        private void Render_Step_2()
        {
            Meta = "Discord Setup: Authorization Token";

            UpdateProgressBar();

            UpdateMeta(ShowProgressBar ? 4 : 3);

            ClearContents();
            WriteEntry("|\u2005\u2005\u2005 - TODO: step 2.", ConsoleColor.Blue, false);
        }
        private void Render_Step_3()
        {
            Meta = "Discord Setup: Startup Channel";

            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);

            ClearContents();
            WriteEntry("|\u2005\u2005\u2005 - TODO: step 3.", ConsoleColor.Blue, false);
        }
        private void Render_Step_4()
        {
            Meta = "Application Setup: Global Command Prefix";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);



            ClearContents();
            WriteEntry("|\u2005\u2005\u2005 - TODO: step 4.", ConsoleColor.Blue, false);
        }
        private void Render_Step_5()
        {
            Meta = "Application Setup: Automatic Permission Assignment";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);


            ClearContents();
            WriteEntry("|\u2005\u2005\u2005 - TODO: step 5.", ConsoleColor.Blue, false);
        }
        private void Render_Step_6()
        {
            Meta = "Application Setup: ASCII Startup Logo (CONSOLE)";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);


            ClearContents();
            WriteEntry("|\u2005\u2005\u2005 - TODO: step 6.", ConsoleColor.Blue, false);
        }
        private void Render_Step_7()
        {
            Meta = "Application Setup: Software Update Preferences";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);
            ClearContents();
            WriteEntry("|\u2005\u2005\u2005 - TODO: step 7.", ConsoleColor.Blue, false);
        }
        private void Render_Step_8()
        {
            Meta = "Setup Complete!";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);
            ClearContents();
            WriteEntry("|\u2005\u2005\u2005 - TODO: step 8.", ConsoleColor.Blue, false);
        }
        #endregion

        private void WriteFooter(string footer, ConsoleColor BackColor = ConsoleColor.Gray, ConsoleColor ForeColor = ConsoleColor.Black)
        {
            LayoutUpdating = true;
            ScreenBackColor = BackColor;
            ScreenFontColor = ForeColor;
            int CT = Console.CursorTop;
            Console.CursorTop = 31;
            WriteEntry($"\u2502 {footer} \u2502".PadRight(141, '\u2005') + "\u2502", BackColor, false, BackColor, null, BackColor);
            Console.CursorTop = 0;
            Console.CursorTop = CT;
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            LayoutUpdating = false;
        }

    }
}
