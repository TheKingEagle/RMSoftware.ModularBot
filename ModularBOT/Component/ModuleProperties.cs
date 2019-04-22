﻿using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ModularBOT.Component
{
    public class ModulePropertyItem
    {
        public string ModuleName { get; set; }
        public string ServiceClass { get;  set; }
        public ulong GuildAvailable { get;  set; }
    }

    public class ModuleManager
    {
        List<ModulePropertyItem> _modules;
        public IReadOnlyCollection<ModulePropertyItem> Modules { get { return _modules.AsReadOnly(); } }

        public ModuleManager(ref CommandService cmdsvr,ref IServiceCollection serviceCollection, ref IServiceProvider serviceProvider, ref Configuration appConfig)
        {
            _modules = new List<ModulePropertyItem>();
            //LOAD MODULES AND SERVICES.
            foreach (string item in Directory.EnumerateFiles("modules", "*.dll", SearchOption.TopDirectoryOnly))
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Modules", $"Adding commands from module library: {item}"), ConsoleColor.DarkGreen);
                try
                {
                    Assembly asmb = Assembly.LoadFile(Path.GetFullPath(item));
                    string PiFN = $"modules\\{Path.GetFileNameWithoutExtension(item)}.mpi";
                    string servicefilePath = Path.GetFullPath(PiFN);
                    if (File.Exists(servicefilePath))
                    {
                        using (StreamReader sr = new StreamReader(servicefilePath))
                        {
                            string json = sr.ReadToEnd();
                            ModulePropertyItem propertyItem = JsonConvert.DeserializeObject<ModulePropertyItem>(json);
                            if(!string.IsNullOrWhiteSpace(propertyItem.ServiceClass))
                            {
                                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Modules", $"Service type specified! Injecting service: {propertyItem.ServiceClass} from {asmb.GetName().Name}"));
                                serviceCollection = serviceCollection.AddSingleton(asmb.GetType(propertyItem.ServiceClass));
                                serviceProvider = serviceCollection.BuildServiceProvider();
                            }

                            if(string.IsNullOrWhiteSpace(propertyItem.ModuleName))
                            {
                                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "Modules", $"Type name is required! Unable to add module from {asmb.GetName().Name}"));
                                continue;
                            }
                            cmdsvr.AddModuleAsync(asmb?.GetType(propertyItem.ModuleName), serviceProvider);
                            _modules.Add(propertyItem);
                        }
                    }
                    else
                    {
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "Modules", $"CRITICAL: No MPI found for the module. Cannot load module {asmb.GetName().Name}"));
                        continue;
                    }
                    
                }
                catch (Exception ex)
                {
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "Modules", $"A Critical error occurred. could not load module {Path.GetFileName(item)}"));
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "Modules", ex.Message, ex));

                    using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + ex.ToString());
                            sw.Flush();
                        }
                    }
                }
            }
        }
    }
}