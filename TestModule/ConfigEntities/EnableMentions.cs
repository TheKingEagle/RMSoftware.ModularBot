using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using ModularBOT.Component;
using ModularBOT.Entity;
using Microsoft.Extensions.DependencyInjection;
using Discord;
namespace TestModule.ConfigEntities
{
    public class EnableMentions : ConfigEntity
    {
        public EnableMentions()
        {
            ReadOnly = false;
            ConfigIdentifier = "TMS_EnableMentions";
        }
        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            var cfg = TestModuleService.WelcomeBindings.FirstOrDefault(x => x.GuildId == Context.Guild.Id);
            return base.ExecuteView(_DiscordNet, Context, (cfg?.EnableMentions ?? false).ToString());
        }

        public override async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _discordNET, ICommandContext Context, string value)
        {
            if(!bool.TryParse(value, out bool res))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(_discordNET.serviceProvider.GetRequiredService<ConsoleIO>(), Context,
                    "Invalid Value", "Expected `True` or `False` boolean values", Color.DarkRed));
                return;
            }
            var cfg = TestModuleService.WelcomeBindings.FirstOrDefault(x => x.GuildId == Context.Guild.Id);
            if(cfg == null)
            {
                cfg = new WelcomeConfig()
                {
                    EnableMentions = res,
                    GuildId = Context.Guild.Id,
                    WelcomeChannel = 0,
                    WelcomeMessage = "Enjoy your stay!",
                    WelcomeRole = 0
                };
                //create it if not existing;
                TestModuleService.WelcomeBindings.Add(cfg);
            }
            else
            {
                //update the root binding.
                TestModuleService.WelcomeBindings[TestModuleService.WelcomeBindings.IndexOf(cfg)].EnableMentions = res;
            }

            //save config. 
            WelcomeConfig.SaveConfig(_discordNET.serviceProvider.GetRequiredService<ConsoleIO>(),
                TestModuleService.WelcomeBindings, TestModuleService.WelcomeBindingsConfig);
            await base.ExecuteSet(Client, _discordNET, Context, value);
        }
    }
}
