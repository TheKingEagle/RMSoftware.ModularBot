using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Entity;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

namespace ModularBOT.Component.ConsoleScreens
{
    public class SetupWizardScreen : ConsoleScreen
    {
        private List<int> Steps = new List<int>()
        {
            1,2,3,4,5,6,7,8
        };
        private bool Debug = false;
        private bool InitialRun = false;
        private int StepIndex = 0;
        private int CLeft = 0;
        bool APAoptionState = false;
        bool AUAoptionState = false;
        bool AUCoptionState = false;


        public bool Completed { get; private set; }

        public Configuration NewConfig { get; private set; }
        
        public SetupWizardScreen(ref Configuration appConfig)
        {
            
            ScreenFontColor = ConsoleColor.Cyan;
            ScreenBackColor = ConsoleColor.Black;
            TitlesBackColor = ConsoleColor.Black;
            TitlesFontColor = ConsoleColor.White;
            ProgressColor = ConsoleColor.Green;
            MetaFontColor = ConsoleColor.Yellow;
            Title = $"Setup Wizard | ModularBOT v{Assembly.GetExecutingAssembly().GetName().Version}";
            Meta = "Welcome -- Getting Started";
            ShowProgressBar = true;
            ShowMeta = true;

            ProgressVal = 1;
            ProgressMax = Steps.Count;
            BufferHeight = 36;
            WindowHeight = 34;

            NewConfig = appConfig;

        }

        public bool NeedsConfig(ref Configuration appConfig)
        {
            InitialRun = appConfig == null;
            if (!InitialRun)
            {
                Debug = appConfig.DebugWizard;
                if (!Debug)
                {
                    if (appConfig.LogChannel != 0 && appConfig.CheckForUpdates.HasValue && appConfig.UseInDevChannel.HasValue && !string.IsNullOrWhiteSpace(appConfig.CommandPrefix)
                            && !appConfig.CommandPrefix.Contains('`') && !string.IsNullOrWhiteSpace(appConfig.AuthToken) && !string.IsNullOrWhiteSpace(appConfig.LogoPath) && appConfig.RegisterManagementOnJoin.HasValue)
                    {
                        return false;//if every critical thing is set... continue.
                    }
                    else
                    {
                        //add missing steps
                        Steps = new List<int>();
                        Steps.Add(1);//intro;
                        if (string.IsNullOrWhiteSpace(appConfig.AuthToken)) Steps.Add(2);
                        if (appConfig.LogChannel == 0) Steps.Add(3);
                        if (string.IsNullOrWhiteSpace(appConfig.CommandPrefix) || appConfig.CommandPrefix.Contains('`')) Steps.Add(4);
                        if (!appConfig.RegisterManagementOnJoin.HasValue) Steps.Add(5);
                        if (string.IsNullOrWhiteSpace(appConfig.LogoPath)) Steps.Add(6);
                        if (!appConfig.CheckForUpdates.HasValue || !appConfig.UseInDevChannel.HasValue) Steps.Add(7);
                        Steps.Add(8);//outro;
                        ProgressMax = Steps.Count;
                        return true;
                    }
                }
                
            }

            if (InitialRun)
            {
                NewConfig = new Configuration();//set for later.
            }
            return Debug || InitialRun;// or debug


        }

        public override bool ProcessInput(ConsoleKeyInfo keyinfo)
        {
            
            switch (Steps[StepIndex])
            {
                case (1): return Input_Step_1(keyinfo);
                case (2): return Input_Step_2(keyinfo); 
                case (3): return Input_Step_3(keyinfo); 
                case (4): return Input_Step_4(keyinfo); 
                case (5): return Input_Step_5(keyinfo);
                case (6): return Input_Step_6(keyinfo);
                case (7): return Input_Step_7(keyinfo); 
                case (8): return Input_Step_8(keyinfo);
                default:
                    return Input_Step_8(keyinfo);
            }
            
            //return base.ProcessInput(keyinfo);
        }

        protected override void RenderContents()
        {
            
            SpinWait.SpinUntil(() => !LayoutUpdating);
            switch (Steps[StepIndex])
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
            
            if (Steps[StepIndex] != 8)
            {
                RenderFooter("Press [ENTER] to Continue.");
            }
            if (Steps[StepIndex] == 1)
            {
                RenderFooter("Press [ENTER] to Begin Setup.");
            }
            if(Steps[StepIndex] == 8)
            {
                RenderFooter("Press [ENTER] to Start Application.");
            }
        }


        #region Input
        private bool Input_Step_1(ConsoleKeyInfo keyinfo)
        {
            CheckForEnter(keyinfo);

            return false;
        }

        private bool Input_Step_2(ConsoleKeyInfo keyinfo)
        {
            string auth = "";


            while (true)
            {
                int CT = Console.CursorTop;
                int CL = CLeft + 2;
                RenderFooter("INPUT Requested value then press [ENTER]");
                Console.CursorTop = CT;
                Console.CursorLeft = CL;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("> ");
                Console.CursorVisible = true;
                bool errorShown = false;
                string pass = "";
                do
                {

                    ConsoleKeyInfo key = Console.ReadKey(true);
                    // Backspace Should Not Work
                    if (!char.IsControl(key.KeyChar))
                    {
                        if (pass.Length < 96)
                        {
                            pass += key.KeyChar;
                            Console.Write("*");
                            if (errorShown)
                            {
                                Console.CursorLeft = CL + 2;
                                Console.CursorTop++;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("".PadRight(84, '\u2005'));
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.CursorLeft = pass.Length + CL + 2;
                                Console.CursorTop--;
                                errorShown = false;
                            }
                        }
                        else
                        {
                            Console.CursorLeft = CL + 2;
                            Console.CursorTop++;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("Maximum length reached!");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.CursorLeft = pass.Length + CL + 2;
                            Console.CursorTop--;
                            errorShown = true;
                        }
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                        {
                            pass = pass.Substring(0, (pass.Length - 1));
                            Console.Write("\b \b");
                            if (errorShown)
                            {
                                Console.CursorLeft = CL + 2;
                                Console.CursorTop++;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("".PadRight(84, '\u2005'));
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.CursorLeft = pass.Length + CL + 2;
                                Console.CursorTop--;
                                errorShown = false;
                            }
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            if (errorShown)
                            {
                                Console.CursorLeft = CL + 2;
                                Console.CursorTop++;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("".PadRight(84, '\u2005'));
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.CursorLeft = pass.Length + CL + 2;
                                Console.CursorTop--;
                                errorShown = false;
                            }
                            break;
                        }
                    }
                } while (true);
                auth = pass;

                if (string.IsNullOrWhiteSpace(auth))
                {
                    Console.CursorLeft = CL + 2;
                    Console.CursorVisible = false;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("You cannot leave this blank. Try again.");
                    Thread.Sleep(1000);
                    ClearContents();
                    RenderContents();
                    PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_ESCAPE, 0);
                }
                else
                {
                    if (!Debug) NewConfig.AuthToken = auth;

                    break;
                }

            } 
            
            Console.CursorLeft = CLeft+4;
            Console.CursorTop++;
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Authorization token has been set! Excellent. Moving on...");
            Thread.Sleep(1000);
            CheckForEnter(new ConsoleKeyInfo('\n',ConsoleKey.Enter,false,false,false));//fake key check
            return false;
        }

        private bool Input_Step_3(ConsoleKeyInfo keyinfo)
        {
            string chId = "";
            while (true)
            {
                int CT = Console.CursorTop;
                int CL = CLeft + 2;
                RenderFooter("INPUT Requested value then press [ENTER]");
                Console.CursorTop = CT;
                Console.CursorLeft = CL;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("> ");
                Console.CursorVisible = true;
                bool errorShown = false;
                string id = "";
                do
                {

                    ConsoleKeyInfo key = Console.ReadKey(true);
                    // Backspace Should Not Work
                    if (!char.IsControl(key.KeyChar))
                    {
                        if (id.Length < 32)
                        {
                            id += key.KeyChar;
                            Console.Write(key.KeyChar);
                            if (errorShown)
                            {
                                Console.CursorLeft = CL + 2;
                                Console.CursorTop++;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("".PadRight(84, '\u2005'));
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.CursorLeft = id.Length + CL + 2;
                                Console.CursorTop--;
                                errorShown = false;
                            }
                        }
                        else
                        {
                            Console.CursorLeft = CL + 2;
                            Console.CursorTop++;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("Maximum length reached!");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.CursorLeft = id.Length + CL + 2;
                            Console.CursorTop--;
                            errorShown = true;
                        }
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && id.Length > 0)
                        {
                            id = id.Substring(0, (id.Length - 1));
                            Console.Write("\b \b");
                            if (errorShown)
                            {
                                Console.CursorLeft = CL + 2;
                                Console.CursorTop++;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("".PadRight(84, '\u2005'));
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.CursorLeft = id.Length + CL + 2;
                                Console.CursorTop--;
                                errorShown = false;
                            }
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            if (errorShown)
                            {
                                Console.CursorLeft = CL + 2;
                                Console.CursorTop++;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("".PadRight(84, '\u2005'));
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.CursorLeft = id.Length + CL + 2;
                                Console.CursorTop--;
                                errorShown = false;
                            }
                            break;
                        }
                    }
                } while (true);
                chId = id;

                if (string.IsNullOrWhiteSpace(chId))
                {
                    Console.CursorLeft = CL + 2;
                    Console.CursorVisible = false;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("You cannot leave this blank. Try again.");
                    Thread.Sleep(1000);
                    ClearContents();
                    RenderContents();
                    PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_ESCAPE, 0);
                }
                else
                {
                    if (!ulong.TryParse(chId,out ulong result))
                    {
                        Console.CursorLeft = CL + 2;
                        Console.CursorVisible = false;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("The value must be a non-negative number. Try again.");
                        Thread.Sleep(1000);
                        ClearContents();
                        RenderContents();
                        PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_ESCAPE, 0);
                    }
                    else
                    {
                        if (!Debug) NewConfig.LogChannel = result;
                        break;
                    }
                }

            }
            
            Console.CursorLeft = CLeft + 4;
            Console.CursorTop++;
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("The startup channel has been set. Excellent! Moving on...");
            Thread.Sleep(1000);
            CheckForEnter(new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false));//fake key check
            return false;
        }

        private bool Input_Step_4(ConsoleKeyInfo keyinfo)
        {
            string prefix = "";
            while (true)
            {
                int CT = Console.CursorTop;
                int CL = CLeft + 2;
                RenderFooter("INPUT Requested value then press [ENTER]");
                Console.CursorTop = CT;
                Console.CursorLeft = CL;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("> ");
                Console.CursorVisible = true;
                bool errorShown = false;
                string pf = "";
                do
                {

                    ConsoleKeyInfo key = Console.ReadKey(true);
                    // Backspace Should Not Work
                    if (!char.IsControl(key.KeyChar))
                    {
                        if (pf.Length < 8)
                        {
                            pf += key.KeyChar;
                            Console.Write(key.KeyChar);
                            if (errorShown)
                            {
                                Console.CursorLeft = CL + 2;
                                Console.CursorTop++;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("".PadRight(84, '\u2005'));
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.CursorLeft = pf.Length + CL + 2;
                                Console.CursorTop--;
                                errorShown = false;
                            }
                        }
                        else
                        {
                            Console.CursorLeft = CL + 2;
                            Console.CursorTop++;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("Maximum length reached!");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.CursorLeft = pf.Length + CL + 2;
                            Console.CursorTop--;
                            errorShown = true;
                        }
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && pf.Length > 0)
                        {
                            pf = pf.Substring(0, (pf.Length - 1));
                            Console.Write("\b \b");
                            if (errorShown)
                            {
                                Console.CursorLeft = CL + 2;
                                Console.CursorTop++;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("".PadRight(84, '\u2005'));
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.CursorLeft = pf.Length + CL + 2;
                                Console.CursorTop--;
                                errorShown = false;
                            }
                        }
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            if (errorShown)
                            {
                                Console.CursorLeft = CL + 2;
                                Console.CursorTop++;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("".PadRight(84, '\u2005'));
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.CursorLeft = pf.Length + CL + 2;
                                Console.CursorTop--;
                                errorShown = false;
                            }
                            break;
                        }
                    }
                } while (true);
                prefix = pf;

                if (string.IsNullOrWhiteSpace(prefix))
                {
                    Console.CursorLeft = CL + 2;
                    Console.CursorVisible = false;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("You cannot leave this blank. Try again.");
                    Thread.Sleep(1000);
                    ClearContents();
                    RenderContents();
                    PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_ESCAPE, 0);
                }
                else
                {
                    if (prefix.Contains('`'))
                    {
                        Console.CursorLeft = CL + 2;
                        Console.CursorVisible = false;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("Invalid Characters.");
                        Thread.Sleep(1000);
                        ClearContents();
                        RenderContents();
                        PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_ESCAPE, 0);
                    }
                    else
                    {
                        if (!Debug) NewConfig.CommandPrefix = prefix;
                        break;
                    }
                }

            }

            Console.CursorLeft = CLeft + 4;
            Console.CursorTop++;
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("The prefix has been set. Excellent! Moving on...");
            Thread.Sleep(1000);
            CheckForEnter(new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false));//fake key check

            return false;
        }

        private bool Input_Step_5(ConsoleKeyInfo keyinfo)
        {
            int CL = CLeft;
            
            RenderFooter("[UP/DOWN/LEFT/RIGHT]: EDIT SELECTION \u2502 [ENTER]: CONFIRM SELECTION");
            if(keyinfo.Key == ConsoleKey.UpArrow || 
                keyinfo.Key == ConsoleKey.DownArrow || 
                keyinfo.Key == ConsoleKey.LeftArrow || 
                keyinfo.Key == ConsoleKey.RightArrow)
            {
                APAoptionState = !APAoptionState;
            }
            Console.CursorTop -= 2;
            Console.CursorLeft = CL;
            string yebtn = APAoptionState ? "\u25C4 YES \u25BA" : "\u2005 YES \u2005";
            string nobtn = !APAoptionState ? "\u25C4 NO \u25BA" : "\u2005 NO \u2005";

            ConsoleColor TextColorY = APAoptionState ? ConsoleColor.Yellow : ConsoleColor.White;
            ConsoleColor TextColorN = !APAoptionState ? ConsoleColor.Yellow : ConsoleColor.White;
            
            int ct = Console.CursorTop;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            int p1 = (144 / 2) - ((yebtn.Length / 2) + 12);
            int p2 = (144 / 2) + ((-nobtn.Length / 2) + (yebtn.Length / 2)) + 12;
            RenderButton(yebtn, ct, p1-3, ConsoleColor.DarkGray, TextColorY);
            RenderButton(nobtn, ct, p2-3, ConsoleColor.DarkGray, TextColorN);
            Console.CursorLeft = CL;
            Console.CursorTop += 2;

            if(keyinfo.Key == ConsoleKey.Enter)
            {
                if (!Debug) NewConfig.RegisterManagementOnJoin = APAoptionState;
                Console.CursorTop -=1;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                string OState = APAoptionState ? "Feature Enabled! " : "Feature Disabled! ";
                Console.CursorLeft = (144 / 2) - (("Configuration updated! - " + OState + "Moving on...").Length / 2);
                Console.Write("Configuration updated! - ");
                Console.ForegroundColor = APAoptionState ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write(OState);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Moving on...");

                Thread.Sleep(1000);
            }
            CheckForEnter(keyinfo);

            return false;
        }

        private bool Input_Step_6(ConsoleKeyInfo keyinfo)
        {

            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    ClearContents();
                    if (Debug) WriteEntry("DEBUG: This setting will not be saved", ConsoleColor.Yellow, false);
                    int LogoOption = ShowOptionSubScreen("ASCII Logo", "Choose a startup logo type.", "No Startup Logo", "Default Startup Logo", "Choose Custom Image", "-");

                    if (LogoOption == 1) 
                    { 
                        NewConfig.LogoPath = "NONE";
                        break; 
                    }
                    if (LogoOption == 2)
                    {
                        NewConfig.LogoPath = "INTERNAL";
                        break;
                    }
                    if (LogoOption == 3)
                    {
                        Render_Step_6A();
                        string imagepath = "";
                        while (true)
                        {
                            int CT = Console.CursorTop;
                            int CL = CLeft + 2;
                            RenderFooter("INPUT Requested value then press [ENTER]");
                            Console.CursorTop = CT;
                            Console.CursorLeft = CL;
                            Console.BackgroundColor = ConsoleColor.DarkBlue;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("> ");
                            Console.CursorVisible = true;
                            bool errorShown = false;
                            string path = "";
                            do
                            {

                                ConsoleKeyInfo key = Console.ReadKey(true);
                                // Backspace Should Not Work
                                if (!char.IsControl(key.KeyChar))
                                {
                                    if (path.Length < 96)
                                    {
                                        path += key.KeyChar;
                                        Console.Write(key.KeyChar);
                                        if (errorShown)
                                        {
                                            Console.CursorLeft = CL + 2;
                                            Console.CursorTop++;
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("".PadRight(84, '\u2005'));
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.CursorLeft = path.Length + CL + 2;
                                            Console.CursorTop--;
                                            errorShown = false;
                                        }
                                    }
                                    else
                                    {
                                        Console.CursorLeft = CL + 2;
                                        Console.CursorTop++;
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.Write("Maximum length reached!");
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.CursorLeft = path.Length + CL + 2;
                                        Console.CursorTop--;
                                        errorShown = true;
                                    }
                                }
                                else
                                {
                                    if (key.Key == ConsoleKey.Backspace && path.Length > 0)
                                    {
                                        path = path.Substring(0, (path.Length - 1));
                                        Console.Write("\b \b");
                                        if (errorShown)
                                        {
                                            Console.CursorLeft = CL + 2;
                                            Console.CursorTop++;
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("".PadRight(84, '\u2005'));
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.CursorLeft = path.Length + CL + 2;
                                            Console.CursorTop--;
                                            errorShown = false;
                                        }
                                    }
                                    else if (key.Key == ConsoleKey.Enter)
                                    {
                                        if (errorShown)
                                        {
                                            Console.CursorLeft = CL + 2;
                                            Console.CursorTop++;
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("".PadRight(84, '\u2005'));
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.CursorLeft = path.Length + CL + 2;
                                            Console.CursorTop--;
                                            errorShown = false;
                                        }
                                        break;
                                    }
                                }
                            } while (true);
                            imagepath = path;

                            if (string.IsNullOrWhiteSpace(imagepath))
                            {
                                Console.CursorLeft = CL + 2;
                                Console.CursorVisible = false;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("You cannot leave this blank. Try again.");
                                Thread.Sleep(1000);
                                ClearContents();
                                RenderContents();
                                PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_ESCAPE, 0);
                            }
                            else
                            {
                                if (imagepath.Contains('`'))
                                {
                                    Console.CursorLeft = CL + 2;
                                    Console.CursorVisible = false;
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("Invalid Characters.");
                                    Thread.Sleep(1000);
                                    ClearContents();
                                    RenderContents();
                                    PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_ESCAPE, 0);
                                }
                                else
                                {
                                    if (!Debug) NewConfig.LogoPath = imagepath.Replace("\"","");
                                    break;
                                }
                            }

                        }

                        break;
                        //TODO: PREVIEW.

                    }
                }
            }
            Console.CursorLeft = CLeft + 4;
            Console.CursorTop++;
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("The startup logo has been set. Excellent! Moving on...");
            Thread.Sleep(1000);
            CheckForEnter(new ConsoleKeyInfo('\n',ConsoleKey.Enter,false,false,false));//fake it till you make it
            return false;
        }

        private bool Input_Step_7(ConsoleKeyInfo keyinfo)
        {
            int CL = CLeft;

            RenderFooter("[UP/DOWN/LEFT/RIGHT]: EDIT SELECTION \u2502 [ENTER]: CONFIRM SELECTION");
            if (keyinfo.Key == ConsoleKey.UpArrow ||
                keyinfo.Key == ConsoleKey.DownArrow ||
                keyinfo.Key == ConsoleKey.LeftArrow ||
                keyinfo.Key == ConsoleKey.RightArrow)
            {
                AUAoptionState = !AUAoptionState;
            }
            Console.CursorTop -= 2;
            Console.CursorLeft = CL;
            string yebtn = AUAoptionState ? "\u25C4 YES \u25BA" : "\u2005 YES \u2005";
            string nobtn = !AUAoptionState ? "\u25C4 NO \u25BA" : "\u2005 NO \u2005";

            ConsoleColor TextColorY = AUAoptionState ? ConsoleColor.Yellow : ConsoleColor.White;
            ConsoleColor TextColorN = !AUAoptionState ? ConsoleColor.Yellow : ConsoleColor.White;

            int ct = Console.CursorTop;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            int p1 = (144 / 2) - ((yebtn.Length / 2) + 12);
            int p2 = (144 / 2) + ((-nobtn.Length / 2) + (yebtn.Length / 2)) + 12;
            RenderButton(yebtn, ct, p1 - 3, ConsoleColor.DarkGray, TextColorY);
            RenderButton(nobtn, ct, p2 - 3, ConsoleColor.DarkGray, TextColorN);
            Console.CursorLeft = CL;
            Console.CursorTop += 2;

            if (keyinfo.Key == ConsoleKey.Enter)
            {
                if (!Debug) NewConfig.CheckForUpdates = AUAoptionState;
                Console.CursorTop -= 1;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                string OState = AUAoptionState ? "Automatic update check Enabled! " : "Automatic update check Disabled! ";
                Console.CursorLeft = (144 / 2) - (("Configuration updated! - " + OState + "Moving on...").Length / 2);
                Console.Write("Configuration updated! - ");
                Console.ForegroundColor = AUAoptionState ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write(OState);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Moving on...");

                Thread.Sleep(1000);

                if (AUAoptionState)
                {
                    PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_ESCAPE, 0);
                    ClearContents();
                    Render_Step_7A();
                    while (true)
                    {
                        if(Input_Step_7A(Console.ReadKey(true))) {
                            break;
                        }
                    }
                }
            }
            CheckForEnter(keyinfo);

            return false;
        }

        private bool Input_Step_7A(ConsoleKeyInfo keyinfo)
        {
            int CL = (144 / 2) - (("[  INDEV]".Length + "".PadRight(32, '\u2005').Length + "[  RELEASE]".Length) / 2);

            RenderFooter("[UP/DOWN/LEFT/RIGHT]: EDIT SELECTION \u2502 [ENTER]: CONFIRM SELECTION");
            if (keyinfo.Key == ConsoleKey.UpArrow ||
                keyinfo.Key == ConsoleKey.DownArrow ||
                keyinfo.Key == ConsoleKey.LeftArrow ||
                keyinfo.Key == ConsoleKey.RightArrow)
            {
                AUCoptionState = !AUCoptionState;
            }
            Console.CursorTop -= 2;
            Console.CursorLeft = CL;
            string yebtn = AUCoptionState ? "\u25C4 INDEV \u25BA" : "\u2005 INDEV \u2005";
            string nobtn = !AUCoptionState ? "\u25C4 RELEASE \u25BA" : "\u2005 RELEASE \u2005";

            ConsoleColor TextColorY = AUCoptionState ? ConsoleColor.Yellow : ConsoleColor.White;
            ConsoleColor TextColorN = !AUCoptionState ? ConsoleColor.Yellow : ConsoleColor.White;

            int ct = Console.CursorTop;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            int p1 = (144 / 2) - ((yebtn.Length / 2) + 12);
            int p2 = (144 / 2) + ((-nobtn.Length / 2) + (yebtn.Length / 2)) + 12;
            RenderButton(yebtn, ct, p1 - 3, ConsoleColor.DarkGray, TextColorY);
            RenderButton(nobtn, ct, p2 - 3, ConsoleColor.DarkGray, TextColorN);
            Console.CursorLeft = CL;
            Console.CursorTop += 2;

            if (keyinfo.Key == ConsoleKey.Enter)
            {
                if (!Debug) NewConfig.UseInDevChannel = !AUCoptionState;
                Console.CursorTop -= 1;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                string OState = !AUCoptionState ? "Update Channel Set as RELEASE " : "Update Channel Set as INDEV  ";
                Console.CursorLeft = (144 / 2) - (("Configuration updated! - " + OState + "Moving on...").Length / 2);
                Console.Write("Configuration updated! - ");
                Console.ForegroundColor = !AUCoptionState ? ConsoleColor.Green : ConsoleColor.Yellow;
                Console.Write(OState);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Moving on...");

                Thread.Sleep(1000);
            }

            return keyinfo.Key == ConsoleKey.Enter;
        }

        private bool Input_Step_8(ConsoleKeyInfo keyinfo)
        {
            CheckForEnter(keyinfo);
            if(keyinfo.Key == ConsoleKey.Enter)
            {
                Completed = !Debug;
                ActiveScreen = false;

                return true;
            }
            return false;
        }

        private void CheckForEnter(ConsoleKeyInfo keyinfo)
        {
            if (keyinfo.Key == ConsoleKey.Enter)
            {
                StepIndex++;

                if (StepIndex < Steps.Count)
                {
                    
                    ProgressVal = StepIndex+1;
                    ProgressMax = Steps.Count;
                    ClearContents();
                    RenderContents();
                    PostMessage(GetConsoleWindow(), WM_KEYDOWN, VK_ESCAPE, 0);//Let's Jank: fake a key press to properly enter next stage's input

                }
            }
        }
        #endregion

        #region Render
        private void Render_Step_1()
        {
            Meta = "Initial Setup: Welcome & Privacy";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);
            
            ClearContents();
            if (Debug) WriteEntry("NOTICE: This wizard was started in debug mode.", ConsoleColor.Yellow,false);
            else if (!InitialRun) WriteEntry("WARNING: One or more items were not properly configured.", ConsoleColor.Red,false);

            string[] contentBlock =
            {
                "Welcome to the initial setup wizard for your new modular discord bot!",
                "Here we will go through some basic configuration steps to get your new bot up and running.",
                "\u2005",
                "".PadRight(44,'\u2500')+" BEFORE YOU BEGIN "+"".PadRight(44,'\u2500'),
                "\u2005",
                "\u2005\u2005\u2005- This wizard will automatically determine what needs to be set up, and will prompt you when needed.",
                "\u2005\u2005\u2005- You will need to access the Discord Developer Portal.",
                "\u2005\u2005\u2005- You must have the ability to create a discord API application via the Discord Developer Portal.",
                "\u2005\u2005\u2005- You will need to enable developer mode in discord",
                "\u2005",
                "".PadRight(45,'\u2500')+" PRIVACY NOTICE "+"".PadRight(45,'\u2500'),
                "\u2005",
                "\u2005\u2005\u2005- This application will output any message that mentions the bot",
                "\u2005\u2005\u2005\u2005\u2005 or messages that start with the command prefix, to the console.",
                "\u2005\u2005\u2005- By default, messages between other users in any given channel WILL NOT show up in the console.",
                "\u2005\u2005\u2005- Any data written to the console will ONLY appear in the console. (With the exception of errors.)",
                "\u2005\u2005\u2005- Otherwise, that would be extremely creepy. This isn't what we want.",
                "\u2005",

            };
            int maxSize = contentBlock.OrderByDescending(s => s.Length).First().Length + 2;

            int CL = (WindowWidth / 2) - (maxSize / 2);
            CLeft = CL;
            Console.CursorTop++;
            if(InitialRun && !Debug) Console.CursorTop++;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorLeft = CL;
            Console.Write("\u250c{0}\u2510", "".PadRight(maxSize, '\u2500'));//TOP UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            for (int i = 0; i < contentBlock.Length; i++)
            {
                if(i <10)
                {
                    Console.Write("\u2502 {0} \u2502", contentBlock[i].PadRight(maxSize-2, '\u2005'));//CONTENT FILL UI
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\u2502 ");
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(contentBlock[i].PadRight(maxSize-2, '\u2005'));
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" \u2502");
                }
                Console.CursorLeft = CL;
                Console.CursorTop++;
            }
            Console.Write("\u2514{0}\u2518", "".PadRight(maxSize, '\u2500'));//BOTTOM UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("{0}", "".PadRight(maxSize+2, '\u2005'));//Button Bar1 (Fill)
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("{0}", "".PadRight(maxSize+2, '\u2005'));//Button Bar2 (Button text)
            int ct = Console.CursorTop;
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("{0}", "".PadRight(maxSize+2, '\u2005'));//Button Bar3 (Fill)
            string tx = "\u25c4 BEGIN SETUP \u25ba";
            RenderButton(tx, ct, (144 / 2) - (tx.Length / 2),ConsoleColor.DarkGray,ConsoleColor.Yellow);


        }
        
        private void Render_Step_2()
        {
            
            Meta = "Discord Setup: Authorization Token";

            UpdateProgressBar();

            UpdateMeta(ShowProgressBar ? 4 : 3);

            ClearContents();
            if(Debug) WriteEntry("DEBUG: This value will not be saved.", ConsoleColor.Yellow, false);

            string[] contentBlock = 
            {
                "This software requires a way to authenticate with the Discord API. You will need an authorization token in order to use it.",
                "For more information on how to get started, see https://github.com/TheKingEagle/RMSoftware.ModularBot/blob/v2/doc/setup.md",
                "\u2005",
                "\u2005\u2005 NOTE:",
                "\u2005\u2005\u2005- This is a required configuration step!",
                "\u2005\u2005\u2005- The program will fail to start if the token is invalid.",
                "\u2005\u2005\u2005- In this case, you will be prompted with this configuration step again on next run.",
                "\u2005\u2005\u2005- This token should be treated as a very secure password.",
                "\u2005\u2005\u2005- DO NOT share it with anyone, since they'll be able authenticate as your bot!",
                "\u2005\u2005\u2005- If your token is leaked, IMEDIATELY have it regenerated.",
                "\u2005\u2005\u2005- See https://discord.com/developers for more info on authorization tokens.",
                "\u2005",
                "Please paste (or painfully type in) your token below.",
                "\u2005"
            };

            int maxSize = contentBlock.OrderByDescending(s => s.Length).First().Length+4;

            int CL = (WindowWidth / 2) - (maxSize / 2);
            CLeft = CL;
            Console.CursorTop++;
            if(!Debug) Console.CursorTop++;//do it again to keep it padded by two
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorLeft = CL;
            Console.Write("\u250c{0}\u2510", "".PadRight(maxSize-2, '\u2500'));//TOP UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            for (int i = 0; i < contentBlock.Length; i++)
            {
                if (i < 4)
                {
                    Console.Write("\u2502 {0} \u2502", contentBlock[i].PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\u2502 ");
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    
                    if(i > 3 && i < 7)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else if (i > 6 && i < 11)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    } 
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write(contentBlock[i].PadRight(maxSize - 4, '\u2005'));
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" \u2502");
                }
                Console.CursorLeft = CL;
                Console.CursorTop++;
            }
            
            Console.Write("\u2502 {0} \u2502", "".PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2502 {0} \u2502", "".PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2514{0}\u2518", "".PadRight(maxSize - 2, '\u2500'));//BOTTOM UI
            Console.CursorLeft = CL;
            Console.CursorTop-=2;

        }
        
        private void Render_Step_3()
        {
            Meta = "Discord Setup: Startup Channel";

            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);

            ClearContents();
            if (Debug) WriteEntry("DEBUG: This value will not be saved.", ConsoleColor.Yellow, false);
            string[] contentBlock =
            {
                "When you start the program, the bot will execute several commands in a script called onStart.CORE.",
                "These will need to output their results to a guild channel. Here, you will need to enter the ID of the channel you wish to use.",
                "\u2005",
                "\u2005\u2005 To get your guild channel ID:",
                "\u2005\u2005\u2005- Open your settings in discord",
                "\u2005\u2005\u2005- Under App Settings, click the 'Advanced' tab.",
                "\u2005\u2005\u2005- Enable Developer mode. save and close settings. ",
                "\u2005\u2005\u2005- Right click the channel you want to use as the bot's startup channel.",
                "\u2005\u2005\u2005- Click Copy ID.",
                "\u2005\u2005\u2005- If you cannot paste it into this console, you can paste elsewhere, and painfully type it in manually.",
                "\u2005",
                "\u2005\u2005 NOTE:",
                "\u2005\u2005\u2005- This is a required configuration step!",
                "\u2005\u2005\u2005- If an invalid channel id is used, the program will not function.",
                "\u2005\u2005\u2005- In this case, you will be prompted with this configuration step again on next run.",
                "\u2005",
                "Please paste (or painfully type in) your channel id below.",
            };

            int maxSize = contentBlock.OrderByDescending(s => s.Length).First().Length + 4;

            int CL = (WindowWidth / 2) - (maxSize / 2);
            CLeft = CL;
            Console.CursorTop++;
            if (!Debug) Console.CursorTop++;//do it again to keep it padded by two
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorLeft = CL;
            Console.Write("\u250c{0}\u2510", "".PadRight(maxSize - 2, '\u2500'));//TOP UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            for (int i = 0; i < contentBlock.Length; i++)
            {
                if (i < 3)
                {
                    Console.Write("\u2502 {0} \u2502", contentBlock[i].PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\u2502 ");
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    if (i > 11 && i < 15)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write(contentBlock[i].PadRight(maxSize - 4, '\u2005'));
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" \u2502");
                }
                Console.CursorLeft = CL;
                Console.CursorTop++;
            }

            Console.Write("\u2502 {0} \u2502", "".PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2502 {0} \u2502", "".PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2514{0}\u2518", "".PadRight(maxSize - 2, '\u2500'));//BOTTOM UI
            Console.CursorLeft = CL;
            Console.CursorTop -= 2;
        }
        
        private void Render_Step_4()
        {
            Meta = "Application Setup: Global Command Prefix";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);



            ClearContents();
            if (Debug) WriteEntry("DEBUG: This value will not be saved.", ConsoleColor.Yellow, false);
            string[] contentBlock =
            {
                "Now we need to set a desired global command prefix. This prefix will be used when one has not been defined in a guild.",
                "A Typical command prefix is usually a symbol or group of symbols you type before the name of the command.",
                "\u2005",
                "\u2005\u2005 Common examples:",
                "\u2005\u2005\u2005- !",
                "\u2005\u2005\u2005- /",
                "\u2005\u2005\u2005- .",
                "\u2005",
                "\u2005\u2005 NOTE:",
                "\u2005\u2005\u2005- This is a required configuration step!",
                "\u2005\u2005\u2005- A prefix cannot contain backticks (`) or be pure whitespace.",
                "\u2005\u2005\u2005- If your prefix is invalid, the program will not function.",
                "\u2005\u2005\u2005- In this case, you will be prompted with this configuration step again on next run.",
                "\u2005",
                "Please specify a prefix below. You can use multiple characters.",
            };

            int maxSize = contentBlock.OrderByDescending(s => s.Length).First().Length + 4;

            int CL = (WindowWidth / 2) - (maxSize / 2);
            CLeft = CL;
            Console.CursorTop++;
            if (!Debug) Console.CursorTop++;//do it again to keep it padded by two
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorLeft = CL;
            Console.Write("\u250c{0}\u2510", "".PadRight(maxSize - 2, '\u2500'));//TOP UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            for (int i = 0; i < contentBlock.Length; i++)
            {
                if (i < 3)
                {
                    Console.Write("\u2502 {0} \u2502", contentBlock[i].PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\u2502 ");
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    if (i > 8 && i < 13)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write(contentBlock[i].PadRight(maxSize - 4, '\u2005'));
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" \u2502");
                }
                Console.CursorLeft = CL;
                Console.CursorTop++;
            }

            Console.Write("\u2502 {0} \u2502", "".PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2502 {0} \u2502", "".PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2514{0}\u2518", "".PadRight(maxSize - 2, '\u2500'));//BOTTOM UI
            Console.CursorLeft = CL;
            Console.CursorTop -= 2;
        }
        
        private void Render_Step_5()
        {
            Meta = "Application Setup: Automatic Permission Assignment";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);


            ClearContents();
            if (Debug) WriteEntry("DEBUG: This value will not be saved.", ConsoleColor.Yellow, false);
            string[] contentBlock =
            {
                "Would you like to automatically grant some users permission to manage bot commands? ",
                "This is useful if you plan on making your bot public.",
                "\u2005",
                "\u2005\u2005 What this will do:",
                "\u2005\u2005\u2005- Attempt to cache all users in the guild the bot joins",
                "\u2005\u2005\u2005- Check all users' permission levels in said guild (Specifically for 'Manage Guild' and Admin permissions) ",
                "\u2005\u2005\u2005- Adds the role which grants the required guild permissions to the bot's whitelist.",
                "\u2005",
                "\u2005\u2005 NOTE:",
                "\u2005\u2005\u2005- This will require extra memory usage. Plan accordingly.",
                "\u2005\u2005\u2005- If the user does not have any roles, they will not be automatically added. (Except for guild owners)",
                "\u2005\u2005\u2005- The larger the guild member list, the longer this process will take",
                "\u2005\u2005\u2005- YOU WILL NEED PRIVILEGED INTENTS ENABLED IN YOUR BOT APPLICATION, VIA THE DISCORD DEVELOPER PORTAL.",
                "\u2005",
            };

            int maxSize = contentBlock.OrderByDescending(s => s.Length).First().Length + 4;

            int CL = (WindowWidth / 2) - (maxSize / 2);
            CLeft = CL;
            Console.CursorTop++;
            if (!Debug) Console.CursorTop++;//do it again to keep it padded by two
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorLeft = CL;
            Console.Write("\u250c{0}\u2510", "".PadRight(maxSize - 2, '\u2500'));//TOP UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            for (int i = 0; i < contentBlock.Length; i++)
            {
                if (i < 3)
                {
                    Console.Write("\u2502 {0} \u2502", contentBlock[i].PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\u2502 ");
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    if (i > 8 && i < 12)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    } 
                    else if (i > 11)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write(contentBlock[i].PadRight(maxSize - 4, '\u2005'));
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" \u2502");
                }
                Console.CursorLeft = CL;
                Console.CursorTop++;
            }

            Console.Write("\u2514{0}\u2518", "".PadRight(maxSize - 2, '\u2500'));//BOTTOM UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2005 {0} \u2005", "".PadRight(maxSize - 4, '\u2005'));//button fill
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2005 {0} \u2005", "".PadRight(maxSize - 4, '\u2005'));//button fill
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2005 {0} \u2005", "".PadRight(maxSize - 4, '\u2005'));//button fill
            Console.CursorLeft = CL;
            Console.CursorTop++;
            
        }
        
        private void Render_Step_6()
        {
            Meta = "Application Setup: ASCII Startup Logo (CONSOLE)";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);


            ClearContents();
            if (Debug) WriteEntry("DEBUG: This setting will not be saved", ConsoleColor.Yellow, false);
            string[] contentBlock =
            {
                "Remember those old DOS style programs that would output fancy ascii art on startup?",
                "Great! this program has that ability, but with actual image pixel data!",
                "Why? Because, Why not.",
                "\u2005",
                "\u2005\u2005 You have three options",
                "\u2005\u2005\u2005- No startup logo (useful for limited screen resolutions, but less fun...)",
                "\u2005\u2005\u2005- The Default startup logo (The RMSoftware Development emblem)",
                "\u2005\u2005\u2005- a path to your custom image of choice**",
                "\u2005",
                "Press [ENTER] to continue.",
                "\u2005"
            };

            int maxSize = contentBlock.OrderByDescending(s => s.Length).First().Length + 4;

            int CL = (WindowWidth / 2) - (maxSize / 2);
            CLeft = CL;
            Console.CursorTop++;
            if (!Debug) Console.CursorTop++;//do it again to keep it padded by two
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorLeft = CL;
            Console.Write("\u250c{0}\u2510", "".PadRight(maxSize - 2, '\u2500'));//TOP UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            for (int i = 0; i < contentBlock.Length; i++)
            {
                Console.Write("\u2502 {0} \u2502", contentBlock[i].PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
                Console.CursorLeft = CL;
                Console.CursorTop++;
            }
            Console.Write("\u2514{0}\u2518", "".PadRight(maxSize - 2, '\u2500'));//BOTTOM UI
            Console.CursorLeft = CL;
            Console.CursorTop -= 2;
        }

        private void Render_Step_6A()
        {
            //UpdateProgressBar();
            //UpdateMeta(ShowProgressBar ? 4 : 3);
            ClearContents();
            if (Debug) WriteEntry("DEBUG: This setting will not be saved", ConsoleColor.Yellow, false);
            string[] contentBlock =
            {
                "You've decided to use a custom image. What would you like to Pixelize?".PadRight(100,'\u2005'),
                "\u2005",
                "\u2005\u2005 NOTE on custom images:",
                "\u2005\u2005\u2005- Keep the image path short (96 or fewer characters)",
                "\u2005\u2005\u2005- A square image is best. Try something under 512 pixels width and height",
                "\u2005\u2005\u2005- Don't expect magic. The image won't look as pretty as the real thing due to limitations.",
                "\u2005\u2005\u2005- If your image doesn't render, it might not be a supported format.",
                "\u2005",
                "Please enter the path to your custom image below!",
            };

            int maxSize = contentBlock.OrderByDescending(s => s.Length).First().Length + 4;

            int CL = (WindowWidth / 2) - (maxSize / 2);
            CLeft = CL;
            Console.CursorTop++;
            if (!Debug) Console.CursorTop++;//do it again to keep it padded by two
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorLeft = CL;
            Console.Write("\u250c{0}\u2510", "".PadRight(maxSize - 2, '\u2500'));//TOP UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            for (int i = 0; i < contentBlock.Length; i++)
            {
                if (i < 1)
                {
                    Console.Write("\u2502 {0} \u2502", contentBlock[i].PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\u2502 ");
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    if (i > 2 && i < 8)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write(contentBlock[i].PadRight(maxSize - 4, '\u2005'));
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" \u2502");
                }
                Console.CursorLeft = CL;
                Console.CursorTop++;
            }

            Console.Write("\u2502 {0} \u2502", "".PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2502 {0} \u2502", "".PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2514{0}\u2518", "".PadRight(maxSize - 2, '\u2500'));//BOTTOM UI
            Console.CursorLeft = CL;
            Console.CursorTop -= 2;
        }

        private void Render_Step_7()
        {
            Meta = "Application Setup: Software Update Preferences";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);
            ClearContents();
            if (Debug) WriteEntry("DEBUG: This value will not be saved.", ConsoleColor.Yellow, false);
            string[] contentBlock =
            {
                "Would you like to automatically check for software updates on startup? ",
                "Keeping the software updated will ensure it maintains functionality as discord evolves over time.",
                "\u2005",
                "\u2005\u2005 Perks of updating:",
                "\u2005\u2005\u2005- New features -- Command updates, and functionality added to the core.",
                "\u2005\u2005\u2005- Bug fixes -- Stability is everything in a discord bot. longer uptime, better performance, and less frustration.",
                "\u2005\u2005\u2005- Security -- Keeping updated will ensure the latest discord API is used, keeping the application secure.",
                "\u2005",
                "\u2005\u2005 Information:",
                "\u2005\u2005\u2005- This will prompt you to download and run the update right from the console.",
                "\u2005\u2005\u2005- Once installed, the new version will start automatically.",
                "\u2005\u2005\u2005- Regardless, you can still manually check for the most resent stable version via the update command.",
                "\u2005",
            };

            int maxSize = contentBlock.OrderByDescending(s => s.Length).First().Length + 4;

            int CL = (WindowWidth / 2) - (maxSize / 2);
            CLeft = CL;
            Console.CursorTop++;
            if (!Debug) Console.CursorTop++;//do it again to keep it padded by two
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorLeft = CL;
            Console.Write("\u250c{0}\u2510", "".PadRight(maxSize - 2, '\u2500'));//TOP UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            for (int i = 0; i < contentBlock.Length; i++)
            {
                if (i < 3)
                {
                    Console.Write("\u2502 {0} \u2502", contentBlock[i].PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\u2502 ");
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    if (i > 8 && i < 12)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else if (i > 11)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write(contentBlock[i].PadRight(maxSize - 4, '\u2005'));
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" \u2502");
                }
                Console.CursorLeft = CL;
                Console.CursorTop++;
            }

            Console.Write("\u2514{0}\u2518", "".PadRight(maxSize - 2, '\u2500'));//BOTTOM UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2005 {0} \u2005", "".PadRight(maxSize - 4, '\u2005'));//button fill
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2005 {0} \u2005", "".PadRight(maxSize - 4, '\u2005'));//button fill
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2005 {0} \u2005", "".PadRight(maxSize - 4, '\u2005'));//button fill
            Console.CursorLeft = CL;
            Console.CursorTop++;
        }

        private void Render_Step_7A()
        {
            
            ClearContents();
            if (Debug) WriteEntry("DEBUG: This value will not be saved.", ConsoleColor.Yellow, false);
            string[] contentBlock =
            {
                "Which channel for software updates would you like to use?",
                "There are two different channels you can subscribe to, when automatically checking for updates. Below are descriptions of each.",
                "\u2005",
                "\u2005\u2005 RELEASE:",
                "\u2005\u2005\u2005- Less frequent updates",
                "\u2005\u2005\u2005- More stable/polished",
                "\u2005\u2005\u2005- Production environment ready",
                "\u2005",
                "\u2005\u2005 INDEV:",
                "\u2005\u2005\u2005- Frequent updates",
                "\u2005\u2005\u2005- First to see new features",
                "\u2005\u2005\u2005- Not entirely stable, less polished, more bugs.",
                "\u2005\u2005\u2005- Not entirely production ready.",
                "\u2005",
            };

            int maxSize = contentBlock.OrderByDescending(s => s.Length).First().Length + 4;

            int CL = (WindowWidth / 2) - (maxSize / 2);
            CLeft = CL;
            Console.CursorTop++;
            if (!Debug) Console.CursorTop++;//do it again to keep it padded by two
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorLeft = CL;
            Console.Write("\u250c{0}\u2510", "".PadRight(maxSize - 2, '\u2500'));//TOP UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            for (int i = 0; i < contentBlock.Length; i++)
            {
                if (i < 3)
                {
                    Console.Write("\u2502 {0} \u2502", contentBlock[i].PadRight(maxSize - 4, '\u2005'));//CONTENT FILL UI
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\u2502 ");
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    if (i > 8 && i < 12)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else if (i > 11)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write(contentBlock[i].PadRight(maxSize - 4, '\u2005'));
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" \u2502");
                }
                Console.CursorLeft = CL;
                Console.CursorTop++;
            }

            Console.Write("\u2514{0}\u2518", "".PadRight(maxSize - 2, '\u2500'));//BOTTOM UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2005 {0} \u2005", "".PadRight(maxSize - 4, '\u2005'));//button fill
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2005 {0} \u2005", "".PadRight(maxSize - 4, '\u2005'));//button fill
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("\u2005 {0} \u2005", "".PadRight(maxSize - 4, '\u2005'));//button fill
            Console.CursorLeft = CL;
            Console.CursorTop++;
        }

        private void Render_Step_8()
        {
            Meta = "Setup Complete!";
            UpdateProgressBar();
            UpdateMeta(ShowProgressBar ? 4 : 3);
            ClearContents();
            if (Debug) WriteEntry("DEBUG: No configuration was actually affected during this run.", ConsoleColor.Yellow, false);

            string[] contentBlock =
            {
                "That is all the configuration for right now! Here are a few more things to know:",
                "\u2005\u2005\u2005- If you want to run this configuration wizard again, delete the 'modbot-config.cnf' file in the program's directory -OR-",
                "\u2005\u2005\u2005   you can use the config.reset command.",
                "\u2005\u2005\u2005- The documentation, and links to the source code are available at https://rmsoftware.org/modularbot",
                "\u2005",
                "".PadRight(30,'\u2500')+" Key Console Commands "+"".PadRight(30,'\u2500'),
                "\u2005",
                "\u2005\u2005\u2005- cls: Clear console",
                "\u2005\u2005\u2005- stopbot: Gracefully shutdown the bot",
                "\u2005\u2005\u2005- update: Check for software updates",
                "\u2005\u2005\u2005- leave <guildId>: make the bot leave a guild",
                "\u2005\u2005\u2005- mbotdata: Open the program's install directory",
                "\u2005\u2005\u2005- about: Display info about the ConsoleIO system",
                "\u2005\u2005\u2005- guilds: list all the guilds the bot is in.",
                "\u2005",

            };
            int maxSize = contentBlock.OrderByDescending(s => s.Length).First().Length + 2;

            int CL = (WindowWidth / 2) - (maxSize / 2);
            CLeft = CL;
            Console.CursorTop++;
            if (!Debug) Console.CursorTop++;
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorLeft = CL;
            Console.Write("\u250c{0}\u2510", "".PadRight(maxSize, '\u2500'));//TOP UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            for (int i = 0; i < contentBlock.Length; i++)
            {
                if (i < 5)
                {
                    Console.Write("\u2502 {0} \u2502", contentBlock[i].PadRight(maxSize - 2, '\u2005'));//CONTENT FILL UI
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\u2502 ");
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(contentBlock[i].PadRight(maxSize - 2, '\u2005'));
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" \u2502");
                }
                Console.CursorLeft = CL;
                Console.CursorTop++;
            }
            Console.Write("\u2514{0}\u2518", "".PadRight(maxSize, '\u2500'));//BOTTOM UI
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("{0}", "".PadRight(maxSize + 2, '\u2005'));//Button Bar1 (Fill)
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("{0}", "".PadRight(maxSize + 2, '\u2005'));//Button Bar2 (Button text)
            int ct = Console.CursorTop;
            Console.CursorLeft = CL;
            Console.CursorTop++;
            Console.Write("{0}", "".PadRight(maxSize + 2, '\u2005'));//Button Bar3 (Fill)
            string tx = "\u25c4 FINISH SETUP & LAUNCH \u25ba";
            RenderButton(tx, ct, (144 / 2) - (tx.Length / 2), ConsoleColor.DarkGray, ConsoleColor.Yellow);


        }

        private void RenderFooter(string footer, ConsoleColor BackColor = ConsoleColor.Gray, ConsoleColor ForeColor = ConsoleColor.Black)
        {
            ConsoleColor prvBG = ScreenBackColor;
            ConsoleColor prvFG = ScreenFontColor;

            LayoutUpdating = true;
            ScreenBackColor = BackColor;
            ScreenFontColor = ForeColor;
            int CT = Console.CursorTop;
            Console.CursorTop = WindowHeight - 1;
            WriteEntry($"\u2502 {footer} \u2502".PadRight(141, '\u2005') + "\u2502", BackColor, false, BackColor, null, BackColor);
            Console.CursorTop = 0;
            Console.CursorTop = CT;
            ScreenFontColor = prvFG;
            ScreenBackColor = prvBG;
            //hard set
            Console.ForegroundColor = ScreenFontColor;
            Console.BackgroundColor = ScreenBackColor;
            LayoutUpdating = false;
        }

        #endregion

    }
}
