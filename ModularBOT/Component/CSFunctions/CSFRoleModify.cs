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
    public class CSFRoleModify : CSFunction
    {
        
        public CSFRoleModify()
        {
            Name = "ROLE_MODIFY";
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
            if (string.IsNullOrWhiteSpace(output) || arguments.Length < 3)
            {
                errorEmbed.WithDescription($"Syntax is not correct ```{line}```");
                errorEmbed.AddField("Usage", "`ROLE_MODIFY <ulong roleID> <string color> <string text>`");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd, true);
                return false;
            }
            string arg1 = arguments[0]; //<ulong id>
            string arg2 = arguments[1]; //<string color>
            string arg3 = output.Remove(0, arg1.Length + arg2.Length + 1).Trim();//<string name>
            if (ulong.TryParse(arg1, out ulong ulo))
            {
                IRole role = (await client.GetGuildAsync(gobj.ID)).GetRole(ulo);
                System.Drawing.Color c = System.Drawing.Color.Black;
                try
                {
                    c = System.Drawing.ColorTranslator.FromHtml(arg2);
                }
                catch (Exception ex)
                {
                    errorEmbed.WithDescription($"A valid hex color was expected. (# and 3 or 6 digits [0-9, A-F] Example: #10F7E3) ```{line}```");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", cmd, true);
                    errorEmbed.AddField("Internal Exception", "```\r\n"+ex.Message+"\r\n```", false);
                    return false;
                }
                
                
                if(arg3.Length > 40)
                {
                    errorEmbed.WithDescription($"Role name too long. ```{line}```");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", cmd, true);
                    return false;
                }
                if (role != null)
                {
                    await role.ModifyAsync(x =>
                   {
                       x.Color = new Color(c.R,c.G,c.B);
                       x.Name = arg3;
                   });
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
