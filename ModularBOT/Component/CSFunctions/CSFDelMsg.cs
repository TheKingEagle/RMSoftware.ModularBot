using Discord;
using Discord.WebSocket;
using ModularBOT.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Component.CSFunctions
{
    public class CSFDelMsg : CSFunction
    {
        public CSFDelMsg()
        {
            Name = "DELMSG";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            if (message.Channel is SocketTextChannel)
            {
                SocketTextChannel msgsoc = message.Channel as SocketTextChannel;
                if (msgsoc.Guild != null)
                {
                    if ((await (await client.GetGuildAsync(msgsoc.Guild.Id)).GetCurrentUserAsync(CacheMode.AllowDownload)).GuildPermissions.Has(GuildPermission.ManageMessages))
                    {
                        await message.DeleteAsync();
                    }
                }
            }
            return await Task.FromResult(true);
        }
    }
}
