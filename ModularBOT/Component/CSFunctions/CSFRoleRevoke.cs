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
    public class CSFRoleRevoke:CSFunction
    {
        
        public CSFRoleRevoke()
        {
            Name = "ROLE_REVOKE";
        }

        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = "";
            if (!client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult()
                                        .GetCurrentUserAsync(CacheMode.AllowDownload).GetAwaiter().GetResult()
                                        .GuildPermissions.Has(GuildPermission.ManageRoles))
            {
                EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { Name = "Missing Permission", Value = "`Manage Roles`", IsInline = false } };
                return ScriptError("This function requires additional permissions!", cmd, errorEmbed, LineInScript, line, fields);
            }
            engine.OutputCount++;
            if (engine.OutputCount > 6)
            {
                return ScriptError("Rate limit triggered! Add waits between executions.", cmd, errorEmbed, LineInScript, line);
            }
            output = line.Remove(0, Name.Length).Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            string[] arguments1 = output.Split(' ');
            if (string.IsNullOrWhiteSpace(output) || arguments1.Length < 2)
            {
                return ScriptError("Syntax is not correct.",
                    "<ulong roleID> <string RevokeMessage>", cmd, errorEmbed, LineInScript, line);
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
                        bz.WithTitle("Role Revoked!");
                        bz.WithAuthor(client.CurrentUser);
                        bz.WithColor(Color.LightOrange);
                        bz.WithDescription($"{arg02}");
                        await message.Channel.SendMessageAsync("", false, bz.Build());
                        return true;
                    }
                    else
                    {
                        return ScriptError("Could not revoke role. Ensure it exists and accessible (Hierarchy)", cmd, errorEmbed, LineInScript, line);

                    }
                }
            }
            else
            {
                return ScriptError("Expected a ulong formatted role ID.", cmd, errorEmbed, LineInScript, line);

            }

            return true;
        }
    }
}
