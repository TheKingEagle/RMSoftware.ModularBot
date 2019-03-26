using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace ModularBOT.Component
{
    public class SetupWizard
    {
        ConsoleIO ConsoleIOHelper;
        Configuration backup;
        public SetupWizard(ref ConsoleIO ioHelper)
        {
            ConsoleIOHelper = ioHelper;
        }

        public bool StartSetupWizard(ref Configuration appConfig)
        {
            
            bool firstrun = appConfig == null;
            if (firstrun)
            {
                appConfig = new Configuration();
            }
            
            if(!appConfig.DebugWizard)
            {
                if (appConfig.LogChannel != 0 && appConfig.CheckForUpdates.HasValue && !string.IsNullOrWhiteSpace(appConfig.CommandPrefix) 
                    && !string.IsNullOrWhiteSpace(appConfig.AuthToken) && !string.IsNullOrWhiteSpace(appConfig.LogoPath))
                {
                    return false;//if every critical thing is set... continue.
                }
            }
            if (appConfig.DebugWizard)
            {
                backup = appConfig;//just to be safe
                appConfig = new Configuration();
                appConfig.DebugWizard = true;
            }
            #region PAGE 1 - Introduction
            ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Welcome", 1, 6, ConsoleColor.Green);

            #region Welcome Check - Additional messages
            if (appConfig.DebugWizard)
            {
                ConsoleIOHelper.WriteEntry("\u2502 NOTICE: This wizard was started in debug mode.",ConsoleColor.Yellow);
            }
            else
            {
                if (!firstrun && (appConfig.LogChannel != 0 || !appConfig.CheckForUpdates.HasValue || !string.IsNullOrWhiteSpace(appConfig.CommandPrefix) || 
                    !string.IsNullOrWhiteSpace(appConfig.AuthToken) || !string.IsNullOrWhiteSpace(appConfig.LogoPath)))
                {
                    ConsoleIOHelper.WriteEntry("\u2502 One or more items were not configured correctly.", ConsoleColor.Yellow);
                }
            }
            #endregion

            ConsoleIOHelper.WriteEntry("\u2502 Welcome to the initial setup wizard for your new modular discord bot!");
            ConsoleIOHelper.WriteEntry("\u2502 Here we will go through some basic configuration steps to get your new bot up and running. Some things to note before getting started:");
            ConsoleIOHelper.WriteEntry("\u2502\u2005");
            ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- This wizard will automatically determine what needs to be set up, and will prompt you when needed.");
            ConsoleIOHelper.WriteEntry("\u2502\u2005");
            ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- PRIVACY Notice: This application will output any message that mentions the bot, or messages that start with ! to the console.", ConsoleColor.Red);
            ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Messages between other users in any given channel WILL NOT show up in the console.", ConsoleColor.Red);
            ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Any data written to the console will ONLY appear in the console.", ConsoleColor.Red);
            ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Otherwise, that would be extremely creepy. This isn't what we want.", ConsoleColor.Red);
            ConsoleIOHelper.WriteEntry("\u2502\u2005");
            ConsoleIOHelper.WriteEntry("\u2502 Press any key to continue.", ConsoleColor.DarkBlue,true);
            Console.Write("\u2502 > ");
            Console.ReadKey();
            #endregion

            #region PAGE 2 - Authorization token
            if(string.IsNullOrWhiteSpace(appConfig.AuthToken))
            {
                ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Authorization Token", 2, 6, ConsoleColor.Green);


                
                ConsoleIOHelper.WriteEntry("\u2502 This bot requires a way to authenticate with the Discord API. You will need an Authorization Token in order to use this bot.");
                ConsoleIOHelper.WriteEntry("\u2502 For more information on how to get started, see https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/master/doc/setup.md");
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005 NOTE:", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- This is a required configuration step!", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- If your token is invalid, the program will not function.", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- This token should be treated as a very secure password.", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- DO NOT share it with anyone, since they'll be able authenticate as your bot user!", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- If your token is leaked, you should IMEDIATELY have it reset.", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502 Please paste (or painfully type in) your token below.", ConsoleColor.DarkBlue, true);
                
                if (appConfig.DebugWizard)
                {
                    ConsoleIOHelper.WriteEntry("\u2502 DEBUG MODE: (This value WILL NOT BE SAVED)", ConsoleColor.Yellow, true);

                }
                Console.Write("\u2502 > ");
                string auth = "";
                while (true)
                {
                    //ConsoleColor b = Console.BackgroundColor;
                    //ConsoleColor f = Console.ForegroundColor;
                    //Console.BackgroundColor = ConsoleColor.Black;
                    //Console.ForegroundColor = ConsoleColor.Black;

                    //auth = Console.ReadLine().Replace("\r\n", "");
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
                   // Console.BackgroundColor = b;
                    //Console.ForegroundColor = f;
                    
                    if (string.IsNullOrWhiteSpace(auth))
                    {
                        ConsoleIOHelper.WriteEntry("\u2502 You cannot leave this blank. Try again.", ConsoleColor.DarkRed);
                        Console.Write("\u2502 > ");
                    }
                    else
                    {
                        break;
                    }
                    
                }
                if (!appConfig.DebugWizard) appConfig.AuthToken = auth;
                Console.CursorTop = Console.CursorTop + 1;
                ConsoleIOHelper.WriteEntry("\u2502 The auth token has been set! Excellent. Press any key to continue...");
                Console.Write("\u2502 > ");
                Console.ReadKey();


            }
            #endregion

            #region PAGE 3 - Log channel
            if (appConfig.LogChannel == 0)
            {
                ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Channel", 3, 6, ConsoleColor.Green);



                ConsoleIOHelper.WriteEntry("\u2502 When you start the program, the bot will execute several commands in a script called onStart.CORE.");
                ConsoleIOHelper.WriteEntry("\u2502 They will need to display results in a guild channel. In order to do this, you need to enter a channel id.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005 To get your guild channel:");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Open your settings in discord");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- navigate to Appearence then scroll down to Advanced settings.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Enable Developer mode. save and close settings. ");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Right click the channel you want to use as the bot's startup channel.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Click Copy ID, then paste it in below.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- If you cannot paste it below, you can paste elsewhere, and painfully type it in manually.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005 NOTE:", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- This is a required configuration step!",ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- If an invalid channel id is used, the program will not function.",ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502 Please paste (or painfully type in) your channel id below.", ConsoleColor.DarkBlue, true);

                if (appConfig.DebugWizard)
                {
                    ConsoleIOHelper.WriteEntry("\u2502 DEBUG MODE: (This value WILL NOT BE SAVED)", ConsoleColor.Yellow, true);

                }
                Console.Write("\u2502 > ");
                string id = "";
                ulong uid = 0;
                while (true)
                {
                    //ConsoleColor b = Console.BackgroundColor;
                    //ConsoleColor f = Console.ForegroundColor;
                    //Console.BackgroundColor = ConsoleColor.Black;
                    //Console.ForegroundColor = ConsoleColor.Black;

                    id = Console.ReadLine().Replace("\r\n", "");
                    //string pass = "";
                   
                    // Console.BackgroundColor = b;
                    //Console.ForegroundColor = f;

                    if (!ulong.TryParse(id,out uid) && uid <=0)
                    {
                        ConsoleIOHelper.WriteEntry("\u2502 Invalid format. Any number greater than 0 expected. Try again.", ConsoleColor.DarkRed);
                        Console.Write("\u2502 > ");
                    }
                    else
                    {
                        break;
                    }

                }
                if (!appConfig.DebugWizard) appConfig.LogChannel = uid;
                ConsoleIOHelper.WriteEntry("\u2502 Startup channel has been set! Excellent. Press any key to continue...");
                Console.Write("\u2502 > ");
                Console.ReadKey();


            }
            #endregion

            #region PAGE 4 - Prefix
            if (string.IsNullOrWhiteSpace(appConfig.CommandPrefix))
            {
                ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Command Prefix", 4, 6, ConsoleColor.Green);



                ConsoleIOHelper.WriteEntry("\u2502 Now we need to set a desired global command prefix. This prefix will be used when one has not been defined in a guild.");
                ConsoleIOHelper.WriteEntry("\u2502 A Typical command prefix is usually a symbol or group of symbols you type before the name of the command.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005 Examples:",ConsoleColor.DarkGreen);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- !help", ConsoleColor.DarkGreen);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- /help", ConsoleColor.DarkGreen);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- ->help", ConsoleColor.DarkGreen);
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005 NOTE:", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- This is a required configuration step!", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Prefix may not be purely whitespace.", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502 Please enter a command prefix below..", ConsoleColor.DarkBlue, true);

                if (appConfig.DebugWizard)
                {
                    ConsoleIOHelper.WriteEntry("\u2502 DEBUG MODE: (This value WILL NOT BE SAVED)", ConsoleColor.Yellow, true);

                }
                Console.Write("\u2502 > ");
                string prefix = "";
                while (true)
                {
                    //ConsoleColor b = Console.BackgroundColor;
                    //ConsoleColor f = Console.ForegroundColor;
                    //Console.BackgroundColor = ConsoleColor.Black;
                    //Console.ForegroundColor = ConsoleColor.Black;

                    prefix = Console.ReadLine().Replace("\r\n", "");
                    //string pass = "";

                    // Console.BackgroundColor = b;
                    //Console.ForegroundColor = f;

                    if (string.IsNullOrWhiteSpace(prefix))
                    {
                        ConsoleIOHelper.WriteEntry("\u2502 Invalid prefix. Must be letters, numbers, or symbols. Try again.", ConsoleColor.DarkRed);
                        Console.Write("\u2502 > ");
                    }
                    else
                    {
                        break;
                    }
                    
                }
                if (!appConfig.DebugWizard) appConfig.CommandPrefix = prefix;
                ConsoleIOHelper.WriteEntry("\u2502 Command Prefix has been set! Excellent. Press any key to continue...");
                Console.Write("\u2502 > ");
                Console.ReadKey();


            }
            #endregion

            #region PAGE 5 - Logo startup
            if (string.IsNullOrWhiteSpace(appConfig.LogoPath))
            {
                WritePage5BODY();

                if (appConfig.DebugWizard)
                {
                    ConsoleIOHelper.WriteEntry("\u2502 DEBUG MODE: (This value WILL NOT BE SAVED)", ConsoleColor.Yellow, true);

                }

                ConsoleKeyInfo k;
                string path = "";
                while (true)
                {
                    ConsoleIOHelper.WriteEntry("\u2502 Please enter a choice below...", ConsoleColor.DarkBlue, true);
                    Console.Write("\u2502 > ");
                    k = Console.ReadKey();
                    if (k.KeyChar == '1')
                    {
                        path = "NONE";
                        break;
                    }
                    if (k.KeyChar == '2')
                    {
                        path = "INTERNAL";
                        ConsoleIOHelper.WriteEntry("\u2502 Previewing action... One second please...");
                        Thread.Sleep(600);
                        ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome", 79, 45);
                        Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                        Thread.Sleep(800);
                        ConsoleIOHelper.ConsoleWriteImage(Properties.Resources.RMSoftwareICO);
                        Thread.Sleep(3000);
                        ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Logo", 5, 6, ConsoleColor.Green);
                        break;
                    }
                    if (k.KeyChar == '3')
                    {
                        ConsoleIOHelper.WriteEntry("\u2502 Please the path to a valid image file...",ConsoleColor.DarkBlue);
                        Console.Write("\u2502 > ");
                        path = Console.ReadLine();
                        ConsoleIOHelper.WriteEntry("\u2502 Previewing action... One second please...");
                        Thread.Sleep(600);
                        ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome", 79, 45);
                        Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                        Thread.Sleep(800);
                        try
                        {
                            ConsoleIOHelper.ConsoleWriteImage(new System.Drawing.Bitmap(path));
                        }
                        catch (Exception ex)
                        {
                            WritePage5BODY();
                            ConsoleIOHelper.WriteEntry("\u2502 Something went wrong. Make sure you specified a valid image.", ConsoleColor.Red);
                            ConsoleIOHelper.WriteEntry("\u2502 " + ex.Message,ConsoleColor.Red);
                            ConsoleIOHelper.WriteEntry("\u2502");
                            continue;
                        }
                        Thread.Sleep(3000);
                        ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Logo", 5, 6, ConsoleColor.Green);
                        break;
                    }

                }
                if (!appConfig.DebugWizard) appConfig.LogoPath = path;
                ConsoleIOHelper.WriteEntry("\u2502 NOTE: You may change this setting via the config.setlogo command.");
                ConsoleIOHelper.WriteEntry("\u2502 Startup logo has been set! Excellent. Press any key to continue...");
                Console.Write("\u2502 > ");
                Console.ReadKey();


            }
            #endregion

            #region PAGE 6 - Updates
            if(!appConfig.CheckForUpdates.HasValue)
            {
                ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Check for updates", 6, 6, ConsoleColor.Green);



                ConsoleIOHelper.WriteEntry("\u2502 RMSoftware Development may occasionally push live updates, with feature improvements and important bug fixes.");
                ConsoleIOHelper.WriteEntry("\u2502 While enabling update notifications is purely optional, it is highly recommended.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502 Notice:",ConsoleColor.Yellow);
                ConsoleIOHelper.WriteEntry("\u2502\u2005",ConsoleColor.Yellow);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005 - This will prompt you to open a browser URL to manually download and install updates", ConsoleColor.Yellow);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005 - If you elect to install the updates, you must close any running instance of this program.", ConsoleColor.Yellow);
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502\u2005");


                while (true)
                {
                    ConsoleIOHelper.WriteEntry("\u2502 Do you wish to enable update checks on start up? [Y/N]", ConsoleColor.DarkGreen);
                    Console.Write("\u2502 > ");
                    var k = Console.ReadKey();
                    if (k.KeyChar == 'n')
                    {
                        appConfig.CheckForUpdates = false;
                        ConsoleIOHelper.WriteEntry("\u2502 OK. You will not receive application update notifications.");
                        ConsoleIOHelper.WriteEntry("\u2502 You can change this later via command: `config.setupdates true`");

                        break;
                    }
                    if (k.KeyChar == 'y')
                    {
                        appConfig.CheckForUpdates = true;
                        ConsoleIOHelper.WriteEntry("\u2502 OK. You will receive application update notifications.");
                        ConsoleIOHelper.WriteEntry("\u2502 You can change this later via command: `config.setupdates false`");
                        break;
                    }
                }
            }

            #endregion
            ConsoleIOHelper.WriteEntry("\u2502\u2005");
            ConsoleIOHelper.WriteEntry("\u2502\u2005");
            ConsoleIOHelper.WriteEntry("\u2502\u2005");
            ConsoleIOHelper.WriteEntry("\u2502 The setup wizard is complete! Press any key to start the bot.", ConsoleColor.Green);
            Console.Write("\u2502 > ");
            Console.ReadKey();
            if (appConfig.DebugWizard)
            {
                ConsoleIOHelper.WriteEntry("\u2502 Wizard debug complete. Restoring settings to previous value", ConsoleColor.Yellow);
                appConfig.DebugWizard = false;
                appConfig = backup;//revert running config, and proceed.
                return false;
            }
            return true;
        }

        private void WritePage5BODY()
        {
            ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Logo", 5, 6, ConsoleColor.Green);



            ConsoleIOHelper.WriteEntry("\u2502 Have you ever seen those old DOS programs that have the fancy ASCII art @ startup?");
            ConsoleIOHelper.WriteEntry("\u2502 Yea? Well great! This bot can do that! Why? (You may be asking) WHY NOT?!");
            ConsoleIOHelper.WriteEntry("\u2502\u2005");
            ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005 Options:", ConsoleColor.DarkGreen);
            ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005 1. No logo", ConsoleColor.DarkGreen);
            ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005 2. Default logo", ConsoleColor.DarkGreen);
            ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005 3. Custom logo", ConsoleColor.DarkGreen);
            ConsoleIOHelper.WriteEntry("\u2502\u2005");
        }
    }
}
