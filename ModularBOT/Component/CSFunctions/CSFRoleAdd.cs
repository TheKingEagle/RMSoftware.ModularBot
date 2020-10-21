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
    public class CSFRoleAdd:CSFunction
    {
        
        public CSFRoleAdd()
        {
            Name = "ROLE_ADD";
        }


        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = "";
            if (!client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult()
                                        .GetCurrentUserAsync(CacheMode.AllowDownload).GetAwaiter().GetResult()
                                        .GuildPermissions.Has(GuildPermission.ManageRoles))
            {
                errorEmbed.WithDescription($"Function error: I Don't have the proper permissions to assign roles.");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            engine.OutputCount++;
            if (engine.OutputCount > 4)
            {
                
                errorEmbed.WithDescription($"`ROLE_ADD` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            output = line.Remove(0, Name.Length).Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            string[] arguments = output.Split(' ');
            if (string.IsNullOrWhiteSpace(output) || arguments.Length < 2)
            {
                errorEmbed.WithDescription($"Syntax is not correct ```{line}```");
                errorEmbed.AddField("Usage", "`ROLE_ADD <ulong roleID> <string message>`");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd, true);
                return false;
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
                        errorEmbed.WithDescription($"The role was not added. Please make sure bot has proper permission to add the role. ```{line}```");
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
