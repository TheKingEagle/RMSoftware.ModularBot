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
        public SetupWizard(ref ConsoleIO ioHelper)
        {
            ConsoleIOHelper = ioHelper;
        }

        public void StartSetupWizard(Configuration appConfig)
        {
            bool firstrun = appConfig == null;
            if (firstrun)
            {
                appConfig = new Configuration();
            }
            if(!appConfig.DebugWizard)
            {
                if (appConfig.LogChannel != 0 && !appConfig.CheckForUpdates.HasValue && !string.IsNullOrWhiteSpace(appConfig.CommandPrefix) 
                    && !string.IsNullOrWhiteSpace(appConfig.AuthToken) && !string.IsNullOrWhiteSpace(appConfig.LogoPath))
                {
                    return;//if every critical thing is set... continue.
                }
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

            #region PAGE 2 - Auth token
            if(string.IsNullOrWhiteSpace(appConfig.AuthToken))
            {
                ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Authorization Token", 2, 6, ConsoleColor.Green);


                
                ConsoleIOHelper.WriteEntry("\u2502 This bot requires a way to authenticate with the Discord API. You will need an Authorization Token (Auth Token) in order to use this bot.");
                ConsoleIOHelper.WriteEntry("\u2502 For more information on how to get started, see https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/master/doc/setup.md");
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005 Special notes:");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- This is a required configuration step!", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- If your token is invalid, the program will not function.", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- This token should be treated as a very secure password.", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- DO NOT share it with anyone, since they'll be able authenticate as your app's bot user!", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- If your token is leaked, you should IMEDIATELY have it reset.", ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502 Please paste (or painfully type in) your auth token below.", ConsoleColor.DarkBlue, true);
                
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
                ConsoleIOHelper.WriteEntry("\u2502 The auth token has been set! Excellent. Press any key to continue...");
                Console.Write("\u2502 > ");
                Console.ReadKey();


            }
            #endregion

            #region PAGE 3 - ALog channel
            if (appConfig.LogChannel == 0)
            {
                ConsoleIOHelper.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Channel", 2, 6, ConsoleColor.Green);



                ConsoleIOHelper.WriteEntry("\u2502 When you start the program, the will execute several commands in a script called onStart.CORE.");
                ConsoleIOHelper.WriteEntry("\u2502 They need to output in a guild channel. In order to do this, you need to enter a channel id.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005 To get your guild channel:");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Open your settings in discord");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- navigate to Appearence then scroll down to Advanced settings.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Enable Developer mode. save and close settings. ");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Right click the channel you want to use as the bot's startup channel.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- Click Copy ID, then paste it in below.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- If you cannot paste it below, you can paste elsewhere, and painfully type it in manually.");
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005 NOTE:");
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- This is a required configuration step!",ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005\u2005\u2005- If an invalid channel id is used, the program will not function.",ConsoleColor.Red);
                ConsoleIOHelper.WriteEntry("\u2502\u2005");
                ConsoleIOHelper.WriteEntry("\u2502 Please paste (or painfully type in) your channel id below.", ConsoleColor.DarkBlue, true);

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
                ConsoleIOHelper.WriteEntry("\u2502 The auth token has been set! Excellent. Press any key to continue...");
                Console.Write("\u2502 > ");
                Console.ReadKey();


            }
            #endregion
        }
    }
}
