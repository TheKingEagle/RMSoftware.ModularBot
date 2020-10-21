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
    public class CSFRoleDel:CSFunction
    {
        
        public CSFRoleDel()
        {
            Name = "ROLE_DEL";
        }

        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = "";
            if (!client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult()
                                        .GetCurrentUserAsync(CacheMode.AllowDownload).GetAwaiter().GetResult()
                                        .GuildPermissions.Has(GuildPermission.ManageRoles))
            {
                errorEmbed.WithDescription($"Function error: I don't have permission to manage roles.");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            engine.OutputCount++;
            if (engine.OutputCount > 4)
            {
                errorEmbed.WithDescription($"`ROLE_DEL` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            output = line.Remove(0, Name.Length).Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            string[] arguments1 = output.Split(' ');
            if (string.IsNullOrWhiteSpace(output) || arguments1.Length < 2)
            {
                errorEmbed.WithDescription($"Syntax is not correct ```{line}```");
                errorEmbed.AddField("Usage", "`ROLE_ADD <ulong roleID> <string message>`");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd, true);
                return false;
            }
            string arg01 = arguments1[0];
            string arg02 = output.Remove(0, arg01.Length).Trim();
            if (ulong.TryParse(arg01, out ulong ulo1))
            {
                IRole role = (await client.GetGuildAsync(gobj.ID)).GetRole(ulo1);
                if (message.Author is SocketGuildUser sgu)
                {

                    await sgu.RemoveRoleAsync(role);
                    await Task.Delay(100);
                    if (sgu.Roles.FirstOrDefault(rf => rf.Id == role.Id) == null)
                    {
                        EmbedBuilder bz = new EmbedBuilder();
                        bz.WithTitle("Role Removed!");
                        bz.WithAuthor(client.CurrentUser);
                        bz.WithColor(Color.LightOrange);
                        bz.WithDescription($"{arg02}");
                        await message.Channel.SendMessageAsync("", false, bz.Build());
                        return true;
                    }
                    else
                    {
                        errorEmbed.WithDescription($"The role could not be removed ```{line}```");
                        errorEmbed.AddField("Line", LineInScript, true);
                        errorEmbed.AddField("Execution Context", cmd, true);
                        return false;
                    }
                }
            }
            else
            {
                errorEmbed.WithDescription($"A ulong ID was expected. ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd, true);
                return false;
            }

            return true;
        }
    }
}
