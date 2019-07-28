using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using System.IO;
using ModularBOT.Component;
using ModularBOT.Component.ConfigEntities;
using ModularBOT.Entity;
namespace ModularBOT
{
    /// <summary>
    /// Configuration data structure
    /// </summary>
    public class Configuration
    {
        public string AuthToken { get; set; }

        public int ShardCount { get; set; }

        public string CommandPrefix { get; set; }

        public ulong LogChannel { get; set; }

        public string LogoPath { get; set; }

        public ActivityType ReadyActivity { get; set; }

        public UserStatus ReadyStatus { get; set; }

        public string ReadyText { get; set; }

        public bool DebugWizard { get; set; }

        public bool LoadCoreModule { get; set; }

        public bool? CheckForUpdates { get; set; }

        public bool? UsePreReleaseChannel { get; set; }

        public LogSeverity DiscordEventLogLevel { get; set; }

        public ConsoleColor ConsoleBackgroundColor { get; set; }

        public ConsoleColor ConsoleForegroundColor { get; set; }

        public bool? RegisterManagementOnJoin { get; set; }

        public Configuration()
        {
            //welcome                              //step 1
            AuthToken = null;                      //step 2
            LogChannel = 0;                        //step 3
            CommandPrefix = null;                  //step 4
            LogoPath = null;                       //step 5
            RegisterManagementOnJoin = null;       //step 6
            ReadyActivity = ActivityType.Playing;
            ReadyStatus = UserStatus.Online;
            LoadCoreModule = true;
            CheckForUpdates = null;                //step 7 - 1
            UsePreReleaseChannel = null;           //step 7 - 2
            DebugWizard = false;
            ReadyText = "Ready!";
            ShardCount = 1;                        //TODO: Figure out proper implementation to automatically set this as bot is added to more guilds.
            
            DiscordEventLogLevel = LogSeverity.Verbose;
            ConsoleForegroundColor = ConsoleColor.White;
            ConsoleBackgroundColor = ConsoleColor.DarkBlue;
        }

        public void SaveConfig(string jsonFilename)
        {
            using (StreamWriter sw = new StreamWriter(jsonFilename))
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                sw.WriteLine(json);
            }
        }
    }

    /// <summary>
    /// Configuration system. implementing JSON
    /// </summary>
    public class ConfigurationManager
    {
        public Configuration CurrentConfig;
        internal SetupWizard setup;

        private List<ConfigEntity> _GuildConfigEntities = new List<ConfigEntity>();
        private List<ConfigEntity> _ModularCnfgEntities = new List<ConfigEntity>();

        public IReadOnlyCollection<ConfigEntity> GuildConfigEntities { get { return _GuildConfigEntities.AsReadOnly(); } }
        public IReadOnlyCollection<ConfigEntity> ModularCnfgEntities { get { return _ModularCnfgEntities.AsReadOnly(); } }

        public void RegisterGuildConfigEntity(ConfigEntity entity)
        {
            if(!_GuildConfigEntities.Contains(entity))
            {
                _GuildConfigEntities.Add(entity);
            }
        }

        public void RegisterModularCnfgEntity(ConfigEntity entity)
        {
            if (!_ModularCnfgEntities.Contains(entity))
            {
                _ModularCnfgEntities.Add(entity);
            }
        }

        string FileName = "";
        public ConfigurationManager(string jsonFilename, ref ConsoleIO consoleIO)
        {
            FileName = jsonFilename;
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
                CurrentConfig.SaveConfig(jsonFilename);
            }

            _GuildConfigEntities.Add(new GuildPrefix());
            _GuildConfigEntities.Add(new LockPrefix());
            _ModularCnfgEntities.Add(new CheckForUpdates());
            _ModularCnfgEntities.Add(new UsePreReleaseChannel());
            _ModularCnfgEntities.Add(new GlobalInitChannel());
            _ModularCnfgEntities.Add(new GlobalCommandPrefix());
            _ModularCnfgEntities.Add(new ShardCount());
            _ModularCnfgEntities.Add(new StartLogoPath());
            _ModularCnfgEntities.Add(new EventLogLevel());
            _ModularCnfgEntities.Add(new MassDeploymentMode());
            _ModularCnfgEntities.Add(new LoadCoreModule());

        }
        public void Save()
        {
            CurrentConfig.SaveConfig(FileName);
        }
        
    }
}
