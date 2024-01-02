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
    class WelcomeRole : ConfigEntity
    {
        public WelcomeRole()
        {
            ReadOnly = false;
            ConfigIdentifier = "TMS_WelcomeRole";
        }

        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            var cfg = TestModuleService.WelcomeBindings.FirstOrDefault(x => x.GuildId == Context.Guild.Id);
            return base.ExecuteView(_DiscordNet, Context, (cfg?.WelcomeRole ?? 0).ToString());
        }

        public override async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _discordNET, ICommandContext Context, string value)
        {
            if (!ulong.TryParse(value, out ulong res))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(_discordNET._serviceProvider.GetRequiredService<ConsoleIO>(), Context,
                    "Invalid Value", "Expected a valid `ULONG` value", Color.DarkRed));
                return;
            }
            var cfg = TestModuleService.WelcomeBindings.FirstOrDefault(x => x.GuildId == Context.Guild.Id);
            if (cfg == null)
            {
                cfg = new WelcomeConfig()
                {
                    EnableMentions = false,
                    GuildId = Context.Guild.Id,
                    WelcomeChannel = 0,
                    WelcomeMessage = "Enjoy your stay!",
                    WelcomeRole = res
                };
                //create it if not existing;
                TestModuleService.WelcomeBindings.Add(cfg);
            }
            else
            {
                //update the root binding.
                TestModuleService.WelcomeBindings[TestModuleService.WelcomeBindings.IndexOf(cfg)].WelcomeRole = res;
            }

            //save config. 
            WelcomeConfig.SaveConfig(_discordNET._serviceProvider.GetRequiredService<ConsoleIO>(),
                TestModuleService.WelcomeBindings, TestModuleService.WelcomeBindingsConfig);
            await base.ExecuteSet(Client, _discordNET, Context, value);
        }
    }
}
