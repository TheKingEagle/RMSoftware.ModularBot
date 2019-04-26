using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Discord.Net;
using Discord;
using Discord.Commands;
using Discord.Webhook;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
namespace ModularBOT.Component
{
    public class UpdateManager
    {
        private UpdateInfo _upd = null;
        public UpdateInfo UpdateInfo
        {
            get
            {
                if (_upd == null)
                {
                    throw new InvalidOperationException("You must check for updates first.");
                }
                else
                {
                    return _upd ;
                }
            }
            private set
            {
                _upd = value;
            }
        }
        IServiceProvider ServiceProvider;
        public UpdateManager(IServiceProvider _serviceProvider)
        {
            ServiceProvider = _serviceProvider;
        }

        public async Task<bool> CheckUpdate(bool PrereleaseChannel)
        {
            //download json
            try
            {
                WebRequest wrq = WebRequest.Create("https://api.rms0.org/mbot");
                WebResponse wrr = await wrq.GetResponseAsync();
                
                if (wrr.ContentType == "application/json")
                {
                    using (StreamReader sr = new StreamReader(wrr.GetResponseStream()))
                    {
                        string json = sr.ReadToEnd();
                        UpdateInfo = JsonConvert.DeserializeObject<UpdateInfo>(json);
                    }
                }
                if (UpdateInfo == null)
                {
                    throw new JsonException("Retrieved result was invalid.");
                }
                else
                {
                    ServiceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "UPDATE", $"Project (v{Assembly.GetExecutingAssembly().GetName().Version.ToString(2)} build): {Assembly.GetExecutingAssembly().GetName().Version.Build}"));
                    ServiceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "UPDATE", $"Revision (Total Builds since 1.0): {Assembly.GetExecutingAssembly().GetName().Version.Revision}"));

                    if (!PrereleaseChannel && Assembly.GetExecutingAssembly().GetName().Version.Revision < UpdateInfo.RELEASE)
                    {
                       return true;
                    }
                    if (PrereleaseChannel && Assembly.GetExecutingAssembly().GetName().Version.Revision < UpdateInfo.PRERELE)
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex )
            {

                throw ex;
            }

        }
    }

    public class UpdateInfo
    {
        public string PACKAGE { get; set; }
        public string PREPAKG { get; set; }
        public string VERSION { get; set; }
        public string PREVERS { get; set; }
        public int RELEASE { get; set; }
        public int PRERELE { get; set; }
    }
}
