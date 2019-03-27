using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using System.IO;
using ModularBOT.Component;
namespace ModularBOT
{
    /// <summary>
    /// Configuration data structure
    /// </summary>
    public class Configuration
    {
        public string AuthToken { get; set; }

        public string CommandPrefix { get; set; }

        public ulong LogChannel { get; set; }

        public string LogoPath { get; set; }

        public ActivityType ReadyActivity { get; set; }

        public UserStatus ReadyStatus { get; set; }

        public string ReadyText { get; set; }

        public bool DebugWizard { get; set; }

        public bool LoadCoreModule { get; set; }

        public bool? CheckForUpdates { get; set; }

        public Configuration()
        {
            //welcome                              //step 1
            AuthToken = null;                      //step 2
            LogChannel = 0;                        //step 3
            CommandPrefix = null;                  //step 4
            LogoPath = null;                       //step 5
            ReadyActivity = ActivityType.Playing;
            ReadyStatus = UserStatus.Online;
            LoadCoreModule = true;
            CheckForUpdates = null;                //step 6
            DebugWizard = false;
            ReadyText = "Ready!";
        }
    }

    /// <summary>
    /// Configuration system. implementing JSON
    /// </summary>
    public class ConfigurationManager
    {
        public Configuration CurrentConfig;
        public SetupWizard setup;
        public ConfigurationManager(string jsonFilename, ref ConsoleIO consoleIO)
        {
            setup = new SetupWizard();
            if(File.Exists(jsonFilename))
            {
                using (StreamReader sr = new StreamReader(jsonFilename))
                {
                    try
                    {
                        CurrentConfig = JsonConvert.DeserializeObject<Configuration>(sr.ReadToEnd());
                        if(CurrentConfig == null)
                        {
                            CurrentConfig = new Configuration();
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Config", ex.Message, ex));
                    }
                }
            }
            if (setup.StartSetupWizard(ref consoleIO, ref CurrentConfig))
            {
                using (StreamWriter sw = new StreamWriter(jsonFilename))
                {
                    string json = JsonConvert.SerializeObject(CurrentConfig, Formatting.Indented);
                    sw.WriteLine(json);
                }
            }
        }
    }
}
