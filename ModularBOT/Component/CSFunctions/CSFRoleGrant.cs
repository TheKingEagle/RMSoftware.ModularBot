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
    public class CSFRoleGrant:CSFunction
    {
        
        public CSFRoleGrant()
        {
            Name = "ROLE_GRANT";
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
            string[] arguments = output.Split(' ');
            if (string.IsNullOrWhiteSpace(output) || arguments.Length < 2)
            {
                return ScriptError("Syntax is not correct.", 
                    "<ulong roleID> <string SuccessMessage>",cmd,errorEmbed,LineInScript,line);
            }
            string arg1 = arguments[0];
            string arg2 = output.Remove(0, arg1.Length).Trim();
            if (ulong.TryParse(arg1, out ulong ulo))
            {
                IRole role = (await client.GetGuildAsync(gobj.ID)).GetRole(ulo);
                if (message.Author is SocketGuildUser sgu)
                {

                    await sgu.AddRoleAsync(role);
                    await Task.Delay(100);
                    if (sgu.Roles.FirstOrDefault(rf => rf.Id == role.Id) != null)
                    {
                        EmbedBuilder bz = new EmbedBuilder();
                        bz.WithTitle("Role Added!");
                        bz.WithAuthor(client.CurrentUser);
                        bz.WithColor(Color.Green);
                        bz.WithDescription($"{arg2}");
                        await message.Channel.SendMessageAsync("", false, bz.Build());
                        return true;
                    }
                    else
                    {
                        return ScriptError("Role could not be added. Check to ensure role exists or is within reach (hierarchy)", cmd, errorEmbed, LineInScript, line);
                    }
                }
            }
            else
            {
                return ScriptError("A ulong format role ID was expected.", cmd, errorEmbed, LineInScript, line);
            }


            return true;
        }
    }
}
