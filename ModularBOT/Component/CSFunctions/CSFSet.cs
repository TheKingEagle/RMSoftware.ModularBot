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
                EmbedFieldBuilder[] Flagfields = {
                        new EmbedFieldBuilder() { IsInline = false, Name = "Supported Flags", Value = "Any Combination of /`U``P``H`\r\n" +
                        "```\r\nU: Variable for user only\r\nP: Prompt sender to input value\r\nH: Hide variable from variable list\r\n```" },
                        new EmbedFieldBuilder() { IsInline = false, Name = "Example", Value = $"{Name} /UH Hello=World" }
                    };
                bool hidden = line.Split(' ')[1].ToUpper().Contains('H');
                if (line.Split(' ')[1].Length > 4)
                {
                    return ScriptError("Syntax Error: `Too many flags`","[/flags] <variableName>=<value>", cmd, errorEmbed, LineInScript, line, Flagfields);
                }
                if (line.Split(' ')[1].Length == 1)
                {
                    return ScriptError("Syntax Error: `No Flags Specified`", "[/flags] <variableName>=<value>", cmd, errorEmbed, LineInScript, line, Flagfields);
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
                    
                    return ScriptError("Syntax Error: `Too many flags`", "[/flags] <variableName>=<value>", cmd, errorEmbed, LineInScript, line, Flagfields);
                }
            }

            return CaseSetVar(engine, line,ref errorEmbed,ref LineInScript,ref cmd,ref gobj,ref client,ref message);

        }

        private bool CaseSetVar(CoreScript engine, string line, ref EmbedBuilder errorEmbed, ref int LineInScript, ref GuildCommand cmd, ref GuildObject gobj, ref IDiscordClient client, ref IMessage message, bool hidden = false)
        {
            string output = line;
            if (output.Split(' ').Length < 2)
            {

                return ScriptError("Syntax Error. ","[/flags] <variableName>=<value>", cmd, errorEmbed, LineInScript, line);

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
                EmbedFieldBuilder[] fields = {
                    new EmbedFieldBuilder() { Name = "Function", Value = $"```\r\n{line.Split(' ')[0]}\r\n```", IsInline = true },
                    new EmbedFieldBuilder() { Name = "Variable Name", Value = $"```\r\n{varname}\r\n```", IsInline = true },
                    new EmbedFieldBuilder() { Name = "Internal Exception", Value = $"```\r\n{ex.Message}\r\n```", IsInline = false } 
                };
                return ScriptError("Internal Exception thrown.", cmd, errorEmbed, LineInScript, line, fields);
            }
            return true;
        }

        private bool CaseSetUserVar(CoreScript engine, string line, ref EmbedBuilder errorEmbed, ref int LineInScript, ref GuildCommand cmd, ref GuildObject gobj, ref IDiscordClient client, ref IMessage message, bool hidden = false)
        {
            string output = line;
            if (output.Split(' ').Length < 2)
            {
                return ScriptError("Syntax Error. ", "[/flags] <variableName>=<value>", cmd, errorEmbed, LineInScript, line);
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
            if (string.IsNullOrWhiteSpace(varname))
            {
                return ScriptError("Syntax Error: Variable name must not be empty.", "[/flags] <variableName>=<value>", cmd, errorEmbed, LineInScript, line);
            }
            output = output.Split('=')[1];
            output = output.Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            try
            {
                engine.SetUserVar(message.Author.Id, varname, output, hidden);
            }
            catch (ArgumentException ex)
            {
                EmbedFieldBuilder[] fields = {
                    new EmbedFieldBuilder() { Name = "Function", Value = $"```\r\n{line.Split(' ')[0]}\r\n```", IsInline = true },
                    new EmbedFieldBuilder() { Name = "Variable Name", Value = $"```\r\n{varname}\r\n```", IsInline = true },
                    new EmbedFieldBuilder() { Name = "Internal Exception", Value = $"```\r\n{ex.Message}\r\n```", IsInline = false }
                };
                return ScriptError("Internal Exception thrown.", cmd, errorEmbed, LineInScript, line, fields);
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
                return ScriptError("Syntax Error. ", "[/flags] <variableName>=<PromptForUser>", cmd, errorEmbed, LineInScript, line);
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
            if (string.IsNullOrWhiteSpace(varname))
            {
                return ScriptError("Syntax Error: Variable name must not be empty.", "[/flags] <variableName>=<PromptForUser>", cmd, errorEmbed, LineInScript, line);
            }
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
                EmbedFieldBuilder[] fields = {
                    new EmbedFieldBuilder() { Name = "Function", Value = $"```\r\n{line.Split(' ')[0]}\r\n```", IsInline = true },
                    new EmbedFieldBuilder() { Name = "Variable Name", Value = $"```\r\n{varname}\r\n```", IsInline = true },
                    new EmbedFieldBuilder() { Name = "Internal Exception", Value = $"```\r\n{ex.Message}\r\n```", IsInline = false }
                };
                return ScriptError("Internal Exception thrown.", cmd, errorEmbed, LineInScript, line, fields);
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
                return ScriptError("Syntax Error. ", "[/flags] <variableName>=<PromptForUser>", cmd, errorEmbed, LineInScript, line);
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
            if (string.IsNullOrWhiteSpace(varname))
            {
                return ScriptError("Syntax Error: Variable name must not be empty.", "[/flags] <variableName>=<PromptForUser>", cmd, errorEmbed, LineInScript, line);
            }
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
                EmbedFieldBuilder[] fields = {
                    new EmbedFieldBuilder() { Name = "Function", Value = $"```\r\n{line.Split(' ')[0]}\r\n```", IsInline = true },
                    new EmbedFieldBuilder() { Name = "Variable Name", Value = $"```\r\n{varname}\r\n```", IsInline = true },
                    new EmbedFieldBuilder() { Name = "Internal Exception", Value = $"```\r\n{ex.Message}\r\n```", IsInline = false }
                };
                return ScriptError("Internal Exception thrown.", cmd, errorEmbed, LineInScript, line, fields);
            }
            return true;
        }

    }
}
