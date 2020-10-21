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
    public class CSFRoleAssign:CSFunction
    {
        
        public CSFRoleAssign()
        {
            Name = "ROLE_ASSIGN";
        }


        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = "";
            if (cmd.CommandAccessLevel < AccessLevels.CommandManager || !cmd.RequirePermission)
            {
                errorEmbed.WithDescription($"Function error: This requires `AccessLevels.CommandManager`");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
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
                errorEmbed.WithDescription($"`ROLE_ASSIGN` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            output = line.Remove(0, Name.Length).Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            string[] aarguments = output.Split(' ');
            if (string.IsNullOrWhiteSpace(output) || aarguments.Length < 3)
            {
                errorEmbed.WithDescription($"Syntax is not correct ```{line}```");
                errorEmbed.AddField("Usage", "`ROLE_ASSIGN <ulong roleID> <User Mention> <string message>`");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd, true);
                return false;
            }
            string aarg1 = aarguments[0];
            string aarg2 = aarguments[1];
            string aarg3 = output.Remove(0, $"{aarg1} {aarg2}".Length).Trim();
            UserTypeReader<SocketGuildUser> SF = new UserTypeReader<SocketGuildUser>();
            CommandContext cde = new CommandContext(client, (IUserMessage)message);
            TypeReaderResult s = SF.ReadAsync(cde, aarg2, engine.Services).GetAwaiter().GetResult();
            if (!ulong.TryParse(aarg1, out ulong aulo))
            {
                errorEmbed.WithDescription($"A ulong ID was expected for Argument 1. ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd.Name, true);
                return false;
            }
            if (!s.IsSuccess)
            {
                errorEmbed.WithDescription($"A Guild User was expected in Argument 2 ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd, true);
                return false;
            }
            if (string.IsNullOrWhiteSpace(aarg3))
            {
                errorEmbed.WithDescription($"Argument 3 cannot be empty. Please specify a message ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd, true);
                return false;
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
                    errorEmbed.WithDescription($"The role was not added. Please make sure bot has proper permission to add the role. ```{line}```");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", cmd, true);
                    return false;
                }
            }

            return true;
        }
    }
}
