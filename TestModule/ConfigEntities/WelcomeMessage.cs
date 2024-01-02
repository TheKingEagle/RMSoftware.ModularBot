using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using ModularBOT.Component;
using ModularBOT.Entity;

namespace TestModule.ConfigEntities
{
    class WelcomeMessage:ConfigEntity
    {
        public WelcomeMessage()
        {
            ReadOnly = false;
            ConfigIdentifier = "TMS_WelcomeMessage";

        }

        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            var cfg = TestModuleService.WelcomeBindings.FirstOrDefault(x => x.GuildId == Context.Guild.Id);
            return base.ExecuteView(_DiscordNet, Context, (cfg?.WelcomeMessage ?? "Not Configured").ToString());
        }

        public override async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _discordNET, ICommandContext Context, string value)
        {
            
            var cfg = TestModuleService.WelcomeBindings.FirstOrDefault(x => x.GuildId == Context.Guild.Id);
            if (cfg == null)
            {
                cfg = new WelcomeConfig()
                {
                    EnableMentions = false,
                    GuildId = Context.Guild.Id,
                    WelcomeChannel = 0,
                    WelcomeMessage = value,
                    WelcomeRole = 0
                };
                //create it if not existing;
                TestModuleService.WelcomeBindings.Add(cfg);
            }
            else
            {
                //update the root binding.
                TestModuleService.WelcomeBindings[TestModuleService.WelcomeBindings.IndexOf(cfg)].WelcomeMessage = value;
            }

            //save config. 
            WelcomeConfig.SaveConfig(_discordNET._serviceProvider.GetRequiredService<ConsoleIO>(),
                TestModuleService.WelcomeBindings, TestModuleService.WelcomeBindingsConfig);
            await base.ExecuteSet(Client, _discordNET, Context, value);
        }
    }
}
