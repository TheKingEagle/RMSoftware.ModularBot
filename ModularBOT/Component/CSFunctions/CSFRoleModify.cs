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
                EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { Name = "Missing Permission", Value = "`Manage Roles`", IsInline = false } };
                return ScriptError("This function requires me to have additional permissions!", cmd, errorEmbed, LineInScript, line,fields);
            }
            engine.OutputCount++;
            if (engine.OutputCount > 6)
            {
                return ScriptError("Rate limit triggered! Add waits between executions.", cmd, errorEmbed, LineInScript, line);
            }
            output = line.Remove(0, Name.Length).Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            string[] arguments = output.Split(' ');
            if (string.IsNullOrWhiteSpace(output) || arguments.Length < 3)
            {
                return ScriptError("Syntax is not correct.", 
                    "<ulong roleID> <string color> <string text>",cmd,errorEmbed,LineInScript,line);
            }
            string arg1 = arguments[0]; //<ulong id>
            string arg2 = arguments[1]; //<string color>
            string arg3 = output.Remove(0, arg1.Length + arg2.Length + 1).Trim();//<string name>
            if (ulong.TryParse(arg1, out ulong ulo))
            {
                IRole role = (await client.GetGuildAsync(gobj.ID)).GetRole(ulo);
                if(role == null)
                {
                    return ScriptError("Role with specified ID not found in guild.", cmd, errorEmbed, LineInScript, line);
                }
                if(role.Id == (await client.GetGuildAsync(gobj.ID)).EveryoneRole.Id)
                {
                    return ScriptError("Unable to modify this role.", cmd, errorEmbed, LineInScript, line);
                }
                System.Drawing.Color c = System.Drawing.Color.Black;
                if(arg2.ToLower() != "null")
                {
                    try
                    {
                        c = System.Drawing.ColorTranslator.FromHtml(arg2);
                    }
                    catch (Exception ex)
                    {
                        EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { Name = "Internal Exception", Value = $"```\r\n{ex.Message}\r\n```", IsInline = false } };
                        return ScriptError("Specify a valid hex number or color name was expected. (Example: #10F7E3, purple, null)", cmd, errorEmbed, LineInScript, line, fields);
                    }
                }
                if(arg3.Length > 40)
                {
                    return ScriptError("Role name too long.", cmd, errorEmbed, LineInScript, line);
                }

                await role.ModifyAsync(x =>
                {
                   x.Color = arg2.ToLower() != "null" ? new Color(c.R, c.G, c.B) : Optional.Create<Color>();
                   x.Name = arg3.ToLower() != "null" ? arg3 : Optional.Create<string>();
                });
            }
            else
            {
                return ScriptError("A ulong format role ID was expected.", cmd, errorEmbed, LineInScript, line);
            }


            return true;
        }

        
    }
}
