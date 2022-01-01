using Discord;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using ModularBOT.Component;
using System;

namespace TestModule
{
    public class WelcomeConfig
    {
        public ulong GuildId { get; set; }
        public ulong WelcomeChannel { get; set; }
        public ulong WelcomeRole { get; set; }
        public string WelcomeMessage { get; set; }
        public bool EnableMentions { get; set; }

        public static List<WelcomeConfig> LoadConfig(ref ConsoleIO consoleIO, string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                consoleIO.WriteEntry(new LogMessage(LogSeverity.Warning, "TMSWelcome", "The WelcomeConfig file did not exist. Creating new one."));
                return new List<WelcomeConfig>();
            }
            using (StreamReader sr = new StreamReader(jsonPath))
            {
                List<WelcomeConfig> loaded = JsonConvert.DeserializeObject<List<WelcomeConfig>>(sr.ReadToEnd());
                if (loaded == null) consoleIO.WriteEntry(new LogMessage(LogSeverity.Warning, "TMSWelcome", "The WelcomeConfig could not be loaded. returned a new list"));
                return loaded ?? new List<WelcomeConfig>();
            }
        }

        public static void SaveConfig(ConsoleIO consoleIO,List<WelcomeConfig> data, string jsonPath)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(jsonPath))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "TMSWelcome", ex.Message, ex));
            }
            
        }
    }
}
