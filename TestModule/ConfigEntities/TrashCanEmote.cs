using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using ModularBOT.Component;
using ModularBOT.Entity;
namespace TestModule.ConfigEntities
{
    internal class TrashCanEmote : ConfigEntity
    {
        
        public TrashCanEmote()
        {
            ReadOnly = true;
            ConfigIdentifier = "TrashCanEmote";
        }

        public override string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            if (TestModuleService.Trashcans.TryGetValue(Context.Guild.Id, out string trashcan))
            {
                return base.ExecuteView(_DiscordNet, Context, trashcan);
            }
            return base.ExecuteView(_DiscordNet, Context, "Not Configured");
        }
    }
}
