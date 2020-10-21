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
    public class CSFSet : CSFunction
    {
        public CSFSet()
        {
            Name = "SET";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            if (line.Split(' ')[1].StartsWith("/"))
            {
                bool hidden = line.Split(' ')[1].ToUpper().Contains('H');
                if (line.Split(' ')[1].Length > 4)
                {
                    errorEmbed.WithDescription($"Syntax Error: ```Too many flags.```");
                    errorEmbed.AddField("details", $"```Expected format: SET /UPH VarName=PromptOrValue.\r\n\r\nFlags can be any combination of U P and H.\r\n\r\nMaximum flag supported: 3```");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                    return false;
                }
                if (line.Split(' ')[1].Length == 1)
                {
                    errorEmbed.WithDescription($"Syntax Error: ```No Flags Specified.```");
                    errorEmbed.AddField("details", $"```Expected format: SET /UPH VarName=PromptOrValue.\r\n\r\nFlags can be any combination of U P and H.\r\n\r\nMaximum flag supported: 3.```");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                    return false;
                }
                if (line.Split(' ')[1].ToUpper().Contains('U'))
                {
                    if (line.Split(' ')[1].ToUpper().Contains('P'))
                    {
                        return CaseSetUserVarPrompt(engine,line, ref errorEmbed, ref LineInScript, ref cmd, ref gobj, ref client, ref message, ChannelTarget, contextToDM, hidden);
                        
                    }
                    return CaseSetUserVar(engine,line, ref errorEmbed, ref LineInScript, ref cmd, ref gobj, ref client, ref message, hidden);
                    
                }
                if (line.Split(' ')[1].ToUpper().Contains('P'))
                {
                    return CaseSetVarPrompt(engine, line, ref errorEmbed, ref LineInScript, ref cmd, ref gobj, ref client, ref message, ChannelTarget, contextToDM, hidden);
                   
                }
                if (line.Split(' ')[1].ToUpper().Contains('H'))
                {
                    return CaseSetVar(engine,line, ref errorEmbed, ref LineInScript, ref cmd, ref gobj, ref client, ref message, hidden);
                    
                }
                else if (!line.Split(' ')[1].ToUpper().Contains('H'))
                {
                    errorEmbed.WithDescription($"Syntax Error: ```Unrecognized Flag```");
                    errorEmbed.AddField("details", $"```Expected format: SET /UPH VarName=PromptOrValue.\r\n\r\nFlags can be any combination of U P and H.\r\n\r\nMaximum flag supported: 3.```");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                    return false;
                }
            }

            return CaseSetVar(engine, line,ref errorEmbed,ref LineInScript,ref cmd,ref gobj,ref client,ref message);

        }

        private bool CaseSetVar(CoreScript engine, string line, ref EmbedBuilder errorEmbed, ref int LineInScript, ref GuildCommand cmd, ref GuildObject gobj, ref IDiscordClient client, ref IMessage message, bool hidden = false)
        {
            string output = line;
            if (output.Split(' ').Length < 2)
            {
                
                errorEmbed.WithDescription($"The Syntax of this function is incorrect. ```{line}```");
                errorEmbed.AddField("Function", line.Split(' ')[0]);
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            if (hidden)
            {
                output = line.Remove(0, 7).Trim();// SET /H 
            }
            if (!hidden)
            {
                output = line.Remove(0, 4).Trim(); //SET 
            }
            string varname = output.Split('=')[0];
            output = output.Split('=')[1];
            output = output.Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            try
            {
                engine.Set(varname, output, hidden);
            }
            catch (ArgumentException ex)
            {
                errorEmbed.WithDescription($"{ex.Message}\r\n");
                errorEmbed.AddField("Function", "```" + line.Split(' ')[0] + "```", true);
                errorEmbed.AddField("Variable Name", "```" + varname + "```", true);
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            return true;
        }

        private bool CaseSetUserVar(CoreScript engine, string line, ref EmbedBuilder errorEmbed, ref int LineInScript, ref GuildCommand cmd, ref GuildObject gobj, ref IDiscordClient client, ref IMessage message, bool hidden = false)
        {
            string output = line;
            if (output.Split(' ').Length < 2)
            {
                errorEmbed.WithDescription($"The Syntax of this function is incorrect. ```{line}```");
                errorEmbed.AddField("Function", line.Split(' ')[0]);
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            if (hidden)
            {
                output = line.Remove(0, 8).Trim();//SET /UH 
            }
            if (!hidden)
            {
                output = line.Remove(0, 7).Trim();//SET /U 
            }
            string varname = output.Split('=')[0];
            output = output.Split('=')[1];
            output = output.Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            try
            {
                engine.SetUserVar(message.Author.Id, varname, output, hidden);
            }
            catch (ArgumentException ex)
            {
                errorEmbed.WithDescription($"{ex.Message}\r\n");
                errorEmbed.AddField("Function", "```" + line.Split(' ')[0] + "```", true);
                errorEmbed.AddField("Variable Name", "```" + varname + "```", true);

                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            return true;
        }

        private bool CaseSetVarPrompt(CoreScript engine, string line, ref EmbedBuilder errorEmbed, ref int LineInScript,
            ref GuildCommand cmd, ref GuildObject gobj, ref IDiscordClient client, ref IMessage message,
            ulong channelTarget, bool contextToDM, bool hidden = false)
        {
            string output = line;
            if (output.Split(' ').Length < 2)
            {
                errorEmbed.WithDescription($"The Syntax of this function is incorrect. ```{line}```");
                errorEmbed.AddField("Function", line.Split(' ')[0]);
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            if (hidden)
            {
                output = line.Remove(0, 8).Trim();//SET /HP 
            }
            if (!hidden)
            {

                output = line.Remove(0, 7).Trim();//SET /P 
            }
            string varname = output.Split('=')[0];
            output = output.Split('=')[1];
            output = output.Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (contextToDM)
            {
                message.Author.SendMessageAsync(engine.ProcessVariableString(gobj, output, cmd, client, message), false);
            }
            else
            {
                if (channelTarget == 0)
                {
                    message.Channel.SendMessageAsync(engine.ProcessVariableString(gobj, output, cmd, client, message), false);
                }
                else
                {
                    SocketTextChannel channelfromid = client.GetChannelAsync(channelTarget).GetAwaiter().GetResult() as SocketTextChannel;
                    channelfromid.SendMessageAsync(engine.ProcessVariableString(gobj, output, cmd, client, message), false);
                }
            }

            ulong iprompter = message.Author.Id;
            if (!engine.ActivePrompts.TryGetValue(iprompter, out (IMessage invoker, ulong channelid, string reply) Prompt))
            {
                engine.ActivePrompts.Add(iprompter, (message, message.Channel.Id, ""));
            }
            else
            {
                return true;//One prompt per user. sorrynotsorry
            }
            System.Threading.SpinWait.SpinUntil(() => !string.IsNullOrWhiteSpace(engine.ActivePrompts[iprompter].PromptReply));
            try
            {
                engine.Set(varname, engine.ActivePrompts[iprompter].PromptReply, hidden);
                engine.ActivePrompts.Remove(iprompter);
            }
            catch (ArgumentException ex)
            {
                errorEmbed.WithDescription($"{ex.Message}\r\n");
                errorEmbed.AddField("Function", "```" + line.Split(' ')[0] + "```", true);
                errorEmbed.AddField("Variable Name", "```" + varname + "```", true);

                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            return true;
        }

        private bool CaseSetUserVarPrompt(CoreScript engine, string line, ref EmbedBuilder errorEmbed, ref int LineInScript,
            ref GuildCommand cmd, ref GuildObject gobj, ref IDiscordClient client, ref IMessage message,
            ulong channelTarget, bool contextToDM, bool hidden = false)
        {
            string output = line;
            if (output.Split(' ').Length < 2)
            {
                errorEmbed.WithDescription($"The Syntax of this function is incorrect. ```{line}```");
                errorEmbed.AddField("Function", line.Split(' ')[0]);
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            if (hidden)
            {
                output = line.Remove(0, 9).Trim();//SET /UPH 
            }
            if (!hidden)
            {

                output = line.Remove(0, 8).Trim();//SET /UP 
            }
            string varname = output.Split('=')[0];
            output = output.Split('=')[1];
            output = output.Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (contextToDM)
            {
                message.Author.SendMessageAsync(engine.ProcessVariableString(gobj, output, cmd, client, message), false);
            }
            else
            {
                if (channelTarget == 0)
                {
                    message.Channel.SendMessageAsync(engine.ProcessVariableString(gobj, output, cmd, client, message), false);
                }
                else
                {
                    SocketTextChannel channelfromid = client.GetChannelAsync(channelTarget).GetAwaiter().GetResult() as SocketTextChannel;
                    channelfromid.SendMessageAsync(engine.ProcessVariableString(gobj, output, cmd, client, message), false);
                }
            }

            ulong iprompter = message.Author.Id;
            if (!engine.ActivePrompts.TryGetValue(iprompter, out (IMessage invoker, ulong channelid, string reply) Prompt))
            {
                engine.ActivePrompts.Add(iprompter, (message, message.Channel.Id, ""));
            }
            else
            {
                return true;//One prompt per user. sorrynotsorry
            }
            System.Threading.SpinWait.SpinUntil(() => !string.IsNullOrWhiteSpace(engine.ActivePrompts[iprompter].PromptReply));
            try
            {
                engine.SetUserVar(iprompter, varname, engine.ActivePrompts[iprompter].PromptReply, hidden);
                engine.ActivePrompts.Remove(iprompter);
            }
            catch (ArgumentException ex)
            {
                errorEmbed.WithDescription($"{ex.Message}\r\n");
                errorEmbed.AddField("Function", "```" + line.Split(' ')[0] + "```", true);
                errorEmbed.AddField("Variable Name", "```" + varname + "```", true);

                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            return true;
        }

    }
}
