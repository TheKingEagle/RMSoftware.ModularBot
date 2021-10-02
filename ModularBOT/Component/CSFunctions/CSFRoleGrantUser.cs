using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ModularBOT.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ModularBOT.Component.CSFunctions
{
    public class CSFRoleGrantUser:CSFunction
    {
        
        public CSFRoleGrantUser()
        {
            Name = "ROLE_GRANT_USER";
        }


        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = "";
            if (cmd.CommandAccessLevel < AccessLevels.CommandManager || !cmd.RequirePermission)
            {
                EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { IsInline = false, Name = "Minimum AccessLevel", Value = "`CommandManager`" } };
                return ScriptError("Command has insufficient AccessLevel requirement.", cmd, errorEmbed, LineInScript, line, fields);
            }
            if (!client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult()
                .GetCurrentUserAsync(CacheMode.AllowDownload).GetAwaiter().GetResult()
                .GuildPermissions.Has(GuildPermission.ManageRoles))
            {
                EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { Name = "Missing Permission", Value = "`Manage Roles`", IsInline = false } };
                return ScriptError("This function requires additional permissions!", cmd, errorEmbed, LineInScript, line, fields);
            }
            engine.OutputCount++;
            if (engine.OutputCount > 4)
            {
                return ScriptError("Rate limit triggered! Add waits between executions.", cmd, errorEmbed, LineInScript, line);
            }
            output = line.Remove(0, Name.Length).Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            string[] aarguments = output.Split(' ');
            if (string.IsNullOrWhiteSpace(output) || aarguments.Length < 3)
            {
                return ScriptError("Syntax is not correct.",
                    "<ulong roleID> <Mentionable User> <string RevokeMessage>", cmd, errorEmbed, LineInScript, line);
            }
            string aarg1 = aarguments[0];
            string aarg2 = aarguments[1];
            string aarg3 = output.Remove(0, $"{aarg1} {aarg2}".Length).Trim();
            UserTypeReader<SocketGuildUser> SF = new UserTypeReader<SocketGuildUser>();
            CommandContext cde = new CommandContext(client, (IUserMessage)message);
            TypeReaderResult s = SF.ReadAsync(cde, aarg2, engine.Services).GetAwaiter().GetResult();
            if (!ulong.TryParse(aarg1, out ulong aulo))
            {
                return ScriptError("Syntax is not correct. Expected Argument 1 to be role ID",
                    "<ulong roleID> <Mentionable User> <string RevokeMessage>", cmd, errorEmbed, LineInScript, line);
            }
            if (!s.IsSuccess)
            {
                return ScriptError("Syntax is not correct. Expected Argument 2 to be user mention",
                    "<ulong roleID> <Mentionable User> <string RevokeMessage>", cmd, errorEmbed, LineInScript, line);
            }
            if (string.IsNullOrWhiteSpace(aarg3))
            {
                return ScriptError("Syntax is not correct. Expected Argument 3 to have a value",
                    "<ulong roleID> <Mentionable User> <string RevokeMessage>", cmd, errorEmbed, LineInScript, line);
            }
            IRole arole = (await client.GetGuildAsync(gobj.ID)).GetRole(aulo);
            if (s.BestMatch is SocketGuildUser asgu)
            {

                await asgu.AddRoleAsync(arole);
                await Task.Delay(100);
                if (asgu.Roles.FirstOrDefault(rf => rf.Id == arole.Id) != null)
                {
                    EmbedBuilder bz = new EmbedBuilder();
                    bz.WithTitle("Role Assigned!");
                    bz.WithAuthor(client.CurrentUser);
                    bz.WithColor(Color.Green);
                    bz.WithDescription($"{aarg3}");
                    await message.Channel.SendMessageAsync("", false, bz.Build());
                    return true;
                }
                else
                {
                    return ScriptError("Could not grant this role. Ensure it exists and accessible (Hierarchy)", cmd, errorEmbed, LineInScript, line);

                }
            }

            return true;
        }
    }
}
