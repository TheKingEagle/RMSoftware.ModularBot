using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace ModularBOT.Component
{
    internal class SetupWizard
    {
        Configuration backup;

        internal bool StartSetupWizard(ref ConsoleIO consoleIO, ref Configuration appConfig)
        {

            bool firstrun = appConfig == null;
            if (firstrun)
            {
                appConfig = new Configuration();
            }

            if (!appConfig.DebugWizard)
            {
                if (appConfig.LogChannel != 0 && appConfig.CheckForUpdates.HasValue && appConfig.UsePreReleaseChannel.HasValue && !string.IsNullOrWhiteSpace(appConfig.CommandPrefix)
                    && !appConfig.CommandPrefix.Contains('`') && !string.IsNullOrWhiteSpace(appConfig.AuthToken) && !string.IsNullOrWhiteSpace(appConfig.LogoPath) && appConfig.RegisterManagementOnJoin.HasValue)
                {
                    return false;//if every critical thing is set... continue.
                }
            }
            if (appConfig.DebugWizard)
            {
                backup = appConfig;//just to be safe
                appConfig = new Configuration
                {
                    DebugWizard = true
                };
            }
            #region PAGE 1 - Introduction
            consoleIO.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Welcome", 1, 8, ConsoleColor.Green);

            #region Welcome Check - Additional messages
            if (appConfig.DebugWizard)
            {
                consoleIO.WriteEntry("\u2502 NOTICE: This wizard was started in debug mode.", ConsoleColor.Yellow);
            }
            else
            {
                if (!firstrun && (appConfig.LogChannel != 0 || !appConfig.CheckForUpdates.HasValue || !string.IsNullOrWhiteSpace(appConfig.CommandPrefix) || appConfig.CommandPrefix.Contains('`') ||
                    !string.IsNullOrWhiteSpace(appConfig.AuthToken) || !string.IsNullOrWhiteSpace(appConfig.LogoPath)))
                {
                    consoleIO.WriteEntry("\u2502 One or more items were not configured correctly.", ConsoleColor.Yellow);
                }
            }
            #endregion

            consoleIO.WriteEntry("\u2502 Welcome to the initial setup wizard for your new modular discord bot!");
            consoleIO.WriteEntry("\u2502 Here we will go through some basic configuration steps to get your new bot up and running.");
            consoleIO.WriteEntry("\u2502\u2005");
            consoleIO.WriteEntry("\u2502 Some things to note before getting started:");
            consoleIO.WriteEntry("\u2502\u2005");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- This wizard will automatically determine what needs to be set up, and will prompt you when needed.");
            consoleIO.WriteEntry("\u2502\u2005");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- PRIVACY Notice: This application will output any message that mentions the bot", ConsoleColor.Red);
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005\u2005\u2005 or messages that start with the command prefix, to the console.", ConsoleColor.Red);
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- Messages between other users in any given channel WILL NOT show up in the console.", ConsoleColor.Red);
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- Any data written to the console will ONLY appear in the console.", ConsoleColor.Red);
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- Otherwise, that would be extremely creepy. This isn't what we want.", ConsoleColor.Red);
            consoleIO.WriteEntry("\u2502\u2005");
            consoleIO.WriteEntry("\u2502 Press any key to continue.", ConsoleColor.DarkBlue, true);
            Console.Write("\u2502 > ");
            Console.ReadKey();
            #endregion

            #region PAGE 2 - Authorization token
            if (string.IsNullOrWhiteSpace(appConfig.AuthToken))
            {
                consoleIO.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Authorization Token", 2, 8, ConsoleColor.Green);



                consoleIO.WriteEntry("\u2502 This bot requires a way to authenticate with the Discord API. You will need an Authorization Token in order to use this bot.");
                consoleIO.WriteEntry("\u2502 For more information on how to get started, see https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/master/doc/setup.md");
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502\u2005\u2005 NOTE:", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- This is a required configuration step!", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- If your token is invalid, the program will not function.", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- This token should be treated as a very secure password.", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- DO NOT share it with anyone, since they'll be able authenticate as your bot user!", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- If your token is leaked, you should IMEDIATELY have it reset.", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502 Please paste (or painfully type in) your token below.", ConsoleColor.DarkBlue, true);

                if (appConfig.DebugWizard)
                {
                    consoleIO.WriteEntry("\u2502 DEBUG MODE: (This value WILL NOT BE SAVED)", ConsoleColor.Yellow, true);

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
                        consoleIO.WriteEntry("\u2502 You cannot leave this blank. Try again.", ConsoleColor.DarkRed);
                        Console.Write("\u2502 > ");
                    }
                    else
                    {
                        break;
                    }

                }
                if (!appConfig.DebugWizard) appConfig.AuthToken = auth;
                Console.CursorTop = Console.CursorTop + 1;
                consoleIO.WriteEntry("\u2502 The auth token has been set! Excellent. Press any key to continue...");
                Console.Write("\u2502 > ");
                Console.ReadKey();


            }
            #endregion

            #region PAGE 3 - Log channel
            if (appConfig.LogChannel == 0)
            {
                consoleIO.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Channel", 3, 8, ConsoleColor.Green);



                consoleIO.WriteEntry("\u2502 When you start the program, the bot will execute several commands in a script called onStart.CORE.");
                consoleIO.WriteEntry("\u2502 They will need to display results in a guild channel. In order to do this, you need to enter a channel id.");
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502\u2005\u2005 To get your guild channel:");
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- Open your settings in discord");
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- navigate to Appearence then scroll down to Advanced settings.");
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- Enable Developer mode. save and close settings. ");
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- Right click the channel you want to use as the bot's startup channel.");
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- Click Copy ID, then paste it in below.");
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- If you cannot paste it below, you can paste elsewhere, and painfully type it in manually.");
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502\u2005\u2005 NOTE:", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- This is a required configuration step!", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- If an invalid channel id is used, the program will not function.", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502 Please paste (or painfully type in) your channel id below.", ConsoleColor.DarkBlue, true);

                if (appConfig.DebugWizard)
                {
                    consoleIO.WriteEntry("\u2502 DEBUG MODE: (This value WILL NOT BE SAVED)", ConsoleColor.Yellow, true);

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

                    if (!ulong.TryParse(id, out uid) && uid <= 0)
                    {
                        consoleIO.WriteEntry("\u2502 Invalid format. Any number greater than 0 expected. Try again.", ConsoleColor.DarkRed);
                        Console.Write("\u2502 > ");
                    }
                    else
                    {
                        break;
                    }

                }
                if (!appConfig.DebugWizard) appConfig.LogChannel = uid;
                consoleIO.WriteEntry("\u2502 Startup channel has been set! Excellent. Press any key to continue...");
                Console.Write("\u2502 > ");
                Console.ReadKey();


            }
            #endregion

            #region PAGE 4 - Prefix
            if (string.IsNullOrWhiteSpace(appConfig.CommandPrefix) || appConfig.CommandPrefix.Contains('`'))
            {
                consoleIO.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Command Prefix", 4, 8, ConsoleColor.Green);



                consoleIO.WriteEntry("\u2502 Now we need to set a desired global command prefix. This prefix will be used when one has not been defined in a guild.");
                consoleIO.WriteEntry("\u2502 A Typical command prefix is usually a symbol or group of symbols you type before the name of the command.");
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502\u2005\u2005 Examples:", ConsoleColor.DarkGreen);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- !help", ConsoleColor.DarkGreen);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- /help", ConsoleColor.DarkGreen);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- ->help", ConsoleColor.DarkGreen);
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502\u2005\u2005 NOTE:", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- This is a required configuration step!", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- Prefix may not be purely whitespace.", ConsoleColor.Red);
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502 Please enter a command prefix below..", ConsoleColor.DarkBlue, true);

                if (appConfig.DebugWizard)
                {
                    consoleIO.WriteEntry("\u2502 DEBUG MODE: (This value WILL NOT BE SAVED)", ConsoleColor.Yellow, true);
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

                    if (string.IsNullOrWhiteSpace(prefix) || prefix.Contains('`'))
                    {
                        consoleIO.WriteEntry("\u2502 Invalid prefix. Must be letters, numbers, or most symbols. Try again.", ConsoleColor.DarkRed);
                        Console.Write("\u2502 > ");
                    }
                    else
                    {
                        break;
                    }

                }
                if (!appConfig.DebugWizard) appConfig.CommandPrefix = prefix;
                consoleIO.WriteEntry("\u2502 Command Prefix has been set! Excellent. Press any key to continue...");
                Console.Write("\u2502 > ");
                Console.ReadKey();


            }
            #endregion

            #region PAGE 5 - Logo startup
            if (string.IsNullOrWhiteSpace(appConfig.LogoPath))
            {
                WritePage5BODY(ref consoleIO);

                if (appConfig.DebugWizard)
                {
                    consoleIO.WriteEntry("\u2502 DEBUG MODE: (This value WILL NOT BE SAVED)", ConsoleColor.Yellow, true);
                }

                ConsoleKeyInfo k;
                string path = "";
                while (true)
                {
                    consoleIO.WriteEntry("\u2502 Please enter a choice below...", ConsoleColor.DarkBlue, true);
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
                        consoleIO.WriteEntry("\u2502 Previewing action... One second please...");
                        Thread.Sleep(600);
                        consoleIO.ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome", 79, 45);
                        Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                        Thread.Sleep(800);
                        consoleIO.ConsoleWriteImage(Properties.Resources.RMSoftwareICO);
                        Thread.Sleep(3000);
                        consoleIO.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Logo", 5, 7, ConsoleColor.Green);
                        break;
                    }
                    if (k.KeyChar == '3')
                    {
                        consoleIO.WriteEntry("\u2502 Please enter the path to a valid image file...", ConsoleColor.DarkBlue);
                        Console.Write("\u2502 > ");
                        path = Console.ReadLine();
                        consoleIO.WriteEntry("\u2502 Previewing action... One second please...");
                        Thread.Sleep(600);
                        consoleIO.ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome", 79, 45);
                        Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                        Thread.Sleep(800);
                        try
                        {
                            consoleIO.ConsoleWriteImage(new System.Drawing.Bitmap(path));
                        }
                        catch (Exception ex)
                        {
                            WritePage5BODY(ref consoleIO);
                            consoleIO.WriteEntry("\u2502 Something went wrong. Make sure you specified a valid image.", ConsoleColor.Red);
                            consoleIO.WriteEntry("\u2502 " + ex.Message, ConsoleColor.Red);
                            consoleIO.WriteEntry("\u2502");
                            continue;
                        }
                        Thread.Sleep(3000);
                        consoleIO.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Logo", 5, 6, ConsoleColor.Green);
                        break;
                    }

                }
                if (!appConfig.DebugWizard) appConfig.LogoPath = path;
                consoleIO.WriteEntry("\u2502 NOTE: You may change this setting via the config.setlogo command.");
                consoleIO.WriteEntry("\u2502 Startup logo has been set! Excellent. Press any key to continue...");
                Console.Write("\u2502 > ");
                Console.ReadKey();


            }
            #endregion

            #region PAGE 6 - Register Command Managers
            if(!appConfig.RegisterManagementOnJoin.HasValue)
            {
                consoleIO.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Mass-deployment Mode", 6, 8, ConsoleColor.Green);
                if (appConfig.DebugWizard)
                {
                    consoleIO.WriteEntry("\u2502 DEBUG MODE: (This value WILL NOT BE SAVED)", ConsoleColor.Yellow, true);
                }
                consoleIO.WriteEntry("\u2502 Would you like to enable large-scale permission assignment? This is useful if you plan on making your bot public.");
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502 WHAT IT DOES:");
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005 - Enables bot to download all users when it joins a guild");
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005 - Searches for all users who can manage the guild");
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005 - Registers all found guild managers to your bot's permission system as 'CommandManager'...");
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502 This may cause increased memory use. Please plan accordingly.", ConsoleColor.Yellow);
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502\u2005");
                bool md = false;
                while (true)
                {
                    consoleIO.WriteEntry("\u2502 Please enter a choice below... (Y/N)", ConsoleColor.DarkBlue, true);
                    Console.Write("\u2502 > ");
                    ConsoleKeyInfo kz = Console.ReadKey();
                    
                    if (kz.Key == ConsoleKey.Y )
                    {
                        md = true;
                        break;
                    }
                    if (kz.Key == ConsoleKey.N)
                    {
                        md = false;
                        break;
                    }

                }
                if(!appConfig.DebugWizard)
                {
                    appConfig.RegisterManagementOnJoin = md;
                }
            }
            #endregion

            #region PAGE 7 - Updates
            if (!appConfig.CheckForUpdates.HasValue || !appConfig.UsePreReleaseChannel.HasValue)
            {
                consoleIO.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Check for updates", 7, 8, ConsoleColor.Green);

                if (appConfig.DebugWizard)
                {
                    consoleIO.WriteEntry("\u2502 DEBUG MODE: (This value WILL NOT BE SAVED)", ConsoleColor.Yellow, true);
                }

                consoleIO.WriteEntry("\u2502 RMSoftware Development may occasionally push live updates, with feature improvements and important bug fixes.");
                consoleIO.WriteEntry("\u2502 While enabling update notifications is purely optional, it is highly recommended.");
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502 Notice:", ConsoleColor.Yellow);
                consoleIO.WriteEntry("\u2502\u2005", ConsoleColor.Yellow);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005 - This will prompt you to open a browser URL to manually download and install updates", ConsoleColor.Yellow);
                consoleIO.WriteEntry("\u2502\u2005\u2005\u2005 - If you elect to install the updates, you must close any running instance of this program.", ConsoleColor.Yellow);
                consoleIO.WriteEntry("\u2502\u2005");
                consoleIO.WriteEntry("\u2502\u2005");


                if(!appConfig.CheckForUpdates.HasValue)
                {
                    while (true)
                    {
                        consoleIO.WriteEntry("\u2502 Do you wish to enable update checks on start up? [Y/N]", ConsoleColor.DarkGreen);
                        Console.Write("\u2502 > ");
                        var k = Console.ReadKey();
                        if (k.KeyChar == 'n')
                        {
                            if (!appConfig.DebugWizard)
                            {
                                appConfig.CheckForUpdates = false;
                            }
                            consoleIO.WriteEntry("\u2502 OK. You will not receive application update notifications.");
                            consoleIO.WriteEntry("\u2502 You can change this later via command: `config.setupdates true`");
                            
                            break;
                        }
                        if (k.KeyChar == 'y')
                        {
                            if(!appConfig.DebugWizard)
                            {
                                appConfig.CheckForUpdates = true;
                            }
                            consoleIO.WriteEntry("\u2502 OK. You will receive application update notifications.");
                            consoleIO.WriteEntry("\u2502 You can change this later via command: `config.setupdates false`");
                            consoleIO.WriteEntry("\u2502\u2005");
                            
                            break;
                        }
                    }
                }
                if (!appConfig.CheckForUpdates.Value)
                {
                    if (!appConfig.DebugWizard)
                    {
                        appConfig.UsePreReleaseChannel = false;//set to use stable by default so wizard will stop bothering us.
                    }
                }
                if (!appConfig.UsePreReleaseChannel.HasValue && appConfig.CheckForUpdates.Value)
                {
                    while (true)
                    {
                        consoleIO.WriteEntry("\u2502 Please choose which update channel you'd like to use.",ConsoleColor.DarkGreen);
                        consoleIO.WriteEntry("\u2502\u2005");
                        consoleIO.WriteEntry("\u2502\u2005");
                        consoleIO.WriteEntry("\u2502\u2005\u2005\u2005 1. STABLE");
                        consoleIO.WriteEntry("\u2502\u2005\u2005\u2005 2. PRE-RELEASE");
                        Console.Write("\u2502 > ");
                        var k = Console.ReadKey();
                        if (k.KeyChar == '1')
                        {
                            if(!appConfig.DebugWizard)
                            {
                                appConfig.UsePreReleaseChannel = false;
                            }
                            consoleIO.WriteEntry("\u2502 You've subscribed to the STABLE updates channel.");
                            consoleIO.WriteEntry("\u2502 You can change this later via command: `config.update.prerelease true`");

                            break;
                        }
                        if (k.KeyChar == '2')
                        {
                            if (!appConfig.DebugWizard)
                            {
                                appConfig.UsePreReleaseChannel = true;
                            }
                            consoleIO.WriteEntry("\u2502  You've subscribed to the PRE-RELEASE updates channel.");
                            consoleIO.WriteEntry("\u2502 You can change this later via command: `config.update.prerelease false`");
                            break;
                        }
                    }
                }

            }

            #endregion

            #region Final page
            consoleIO.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Complete", 8, 8, ConsoleColor.Green);
            consoleIO.WriteEntry("\u2502\u2005That is all the configuration for right now! Here are a few more things to know:");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- If you want to run this configuration wizard again, delete the 'modbot.-config.cnf' file in the program's directory.");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- The documentation, and links to the source code are available at https://rmsoftware.org/modularbot");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- Version history can be found here https://rmsoftware.org/modularbot/history");
            consoleIO.WriteEntry("\u2502\u2005");
            consoleIO.WriteEntry("\u2502\u2005Available Console Commands");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- cls or clear: clear the console output");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- conmsg <message>: sends a message to the guild channel you set with setgch first");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- disablecmd: disables message and command processing");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- enablecmd: enables message and command processing");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- mbotdata: Opens modularBOT's installation directory.");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- rskill: Cause the program to crash (and restart)");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- setgch <id>: sets conmsg guild channel");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- setvar <var name> <value>: sets a temporary variable.");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- stopbot: stops the bot");
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005- tskill: Cause the program to crash (and prompt for termination)");
            consoleIO.WriteEntry("\u2502\u2005");
            consoleIO.WriteEntry("\u2502\u2005");
            consoleIO.WriteEntry("\u2502 The setup wizard is complete! Press any key to start the bot.", ConsoleColor.Green);
            Console.Write("\u2502 > ");
            Console.ReadKey();
            if (appConfig.DebugWizard)
            {
                consoleIO.WriteEntry("\u2502 Wizard debug complete. Restoring settings to previous value", ConsoleColor.Yellow);
                appConfig.DebugWizard = false;
                appConfig = backup;//revert running config, and proceed.
                return false;
            }
            return true;
            #endregion
        }

        internal void WritePage5BODY(ref ConsoleIO consoleIO)
        {
            consoleIO.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Logo", 5, 8, ConsoleColor.Green);

            consoleIO.WriteEntry("\u2502 Have you ever seen those old DOS programs that have the fancy ASCII art @ startup?");
            consoleIO.WriteEntry("\u2502 Yea? Well great! This bot can do that! Why? (You may be asking) WHY NOT?!");
            consoleIO.WriteEntry("\u2502\u2005");
            consoleIO.WriteEntry("\u2502\u2005\u2005 Options:", ConsoleColor.DarkGreen);
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005 1. No logo", ConsoleColor.DarkGreen);
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005 2. Default logo", ConsoleColor.DarkGreen);
            consoleIO.WriteEntry("\u2502\u2005\u2005\u2005 3. Custom logo", ConsoleColor.DarkGreen);
            consoleIO.WriteEntry("\u2502\u2005");
        }
    }
}
