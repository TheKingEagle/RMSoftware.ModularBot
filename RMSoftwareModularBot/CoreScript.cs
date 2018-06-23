using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using RMSoftware.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RMSoftware.ModularBot
{
    public class CoreScript
    {
        bool LOG_ONLY_MODE = false;
        ConsoleLogWriter _writer;
        CustomCommandManager ccmgr;
        char CommandPrefix;
        public CoreScript(bool LogOnlyMode,char CmdPrefix, ConsoleLogWriter writer, CustomCommandManager ccmgr,
            ref IServiceProvider _services, ref CommandService _cmdsvr, Dictionary<string, object> dict = null)
        {
            _writer = writer;
            cmdsvr = _cmdsvr;
            services = _services;
            this.ccmgr = ccmgr;
            CommandPrefix = CmdPrefix;
            if (dict == null)
            {
                Variables = new Dictionary<string, object>();
            }
            else
            {
                Variables = dict;
            }
        }
        private Dictionary<string, object> Variables { get; set; }
        private CommandService cmdsvr;
        private IServiceProvider services;
        /// <summary>
        /// These are variable names that are defined by the custom commands class.
        /// They are not managed by the CoreScript in any way.
        /// </summary>
        private readonly string[] SystemVars = { "counter", "invoker", "self", "version", "pf", "prefix" };

        #region Public Methods
        public void Set(string var, object value)
        {
            object v = null;
            if (SystemVars.Contains(var))
            {
                throw (new ArgumentException("This variable cannot be modified."));
            }
            bool result = Variables.TryGetValue(var, out v);
            if (!result)
            {
                //add the new variable.
                Variables.Add(var, value);
                return;
            }
            else
            {
                Variables.Remove(var);//remove the old value.
                Variables.Add(var, value);//add the new value.
                return;
            }
        }

        public object Get(string var)
        {
            object v = null;
            bool result = Variables.TryGetValue(var, out v);
            if (!result)
            {
                return null;
            }
            else
            {
                return v;
            }

        }

        public string ProcessVariableString(string response, INIFile CmdDB, string cmd, IDiscordClient client, IMessage message)
        {

            if (response.Contains("%counter%"))
            {
                int counter = CmdDB.GetCategoryByName(cmd).GetEntryByName("counter").GetAsInteger() + 1;
                CmdDB.GetCategoryByName(cmd).GetEntryByName("counter").SetValue(counter);
                CmdDB.SaveConfiguration();
                response = response.Replace("%counter%", counter.ToString());
            }
            if (response.Contains("%self%"))
            {

                response = response.Replace("%self%", client.CurrentUser.Mention);
            }
            if (response.Contains("%prefix%"))
            {

                response = response.Replace("%prefix%", Program.CommandPrefix.ToString());
            }
            if (response.Contains("%pf%"))
            {

                response = response.Replace("%pf%", Program.CommandPrefix.ToString());
            }
            if (response.Contains("%invoker%"))
            {
                response = response.Replace("%invoker%", message.Author.Mention);
            }
            if (response.Contains("%version%"))
            {
                response = response.Replace("%version%", Assembly.GetCallingAssembly().GetName().Version.ToString(4));
            }
            //Check for use of Custom defined variables.

            foreach (Match item in Regex.Matches(response, @"%[^%]*%", RegexOptions.ExplicitCapture))
            {
                string vname = item.Value.Replace("%", "");
                if (Get(vname) != null)
                {
                    response = response.Replace(item.Value.ToString(), Get(vname).ToString());
                }
            }
            //Final variable.
            return response;
        }

        public async Task EvaluateScript(string response, INIFile CmdDB, string cmd, IDiscordClient client, IMessage message)
        {
            int LineInScript = 0;
            bool error = false;
           
            EmbedBuilder errorEmbed = new EmbedBuilder();

            errorEmbed.WithAuthor(client.CurrentUser);
            errorEmbed.WithTitle("CoreScript Error");
            errorEmbed.WithColor(Color.Red);
            errorEmbed.WithFooter("CoreScript Engine • RMSoftware.ModularBOT");
            bool terminated = false;
            //For the sake of in-chat scripts, they should be smaller.
            //otherwise they will be saved as a file.
            using (StringReader sr = new StringReader(response))
            {
                while (!error)
                {

                    if (sr.Peek() == -1)
                    {
                        if (!terminated)
                        {
                            error = true;
                            //errorMessage = $"SCRIPT ERROR:```The codeblock was not closed.\r\nCoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                            errorEmbed.WithDescription("The codeblock was not closed.");
                            errorEmbed.AddInlineField("Line", LineInScript);
                            errorEmbed.AddInlineField("Command", cmd);
                            break;
                        }
                    }
                    string line = await sr.ReadLineAsync();
            
                    if (line == ("```"))
                    {
                        terminated = true;
                        LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "End of script!"), ConsoleColor.Green);
                        break;
                    }
                    if (LineInScript == 0)
                    {
                        if (line == "```DOS")
                        {
                            LineInScript = 1;
                            continue;
                        }
                        else
                        {
                            error = true;
                            //errorMessage = $"SCRIPT ERROR:```\r\nUnexpected header:``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```\r\nAdditional info: Multi-line formatting required.";
                            errorEmbed.WithDescription($"Unexpected Header: ```{line}```");
                            errorEmbed.AddField("Additional Information", "You must type your script in multiple lines. Header should look like ```\r\n```DOS\r\n```");
                            errorEmbed.AddInlineField("Line", LineInScript);
                            errorEmbed.AddInlineField("Command", cmd);
                            break;
                        }
                    }
                    if (LineInScript >= 1)
                    {

                        if (line == ("```"))
                        {
                            terminated = true;
                            break;
                        }
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            LineInScript++;//ignore blank lines.
                            continue;
                        }
                        if (line.ToUpper().StartsWith("::") || line.ToUpper().StartsWith("REM") || line.ToUpper().StartsWith("//"))
                        {
                            //comment line.
                            LineInScript++;
                            continue;
                        }

                        if (line == "```DOS")
                        {
                            error = true;
                            //errorMessage = $"SCRIPT ERROR:```\r\nDuplicate header:``` ```{line.Split(' ')[0]}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                            errorEmbed.WithDescription($"Duplicate Header: ```{line.Split(' ')[0]}```");
                            errorEmbed.AddInlineField("Line", LineInScript);
                            errorEmbed.AddInlineField("Command", cmd);
                            break;
                        }
                        //GET COMMAND PART.
                        string output = "";
                        switch (line.Split(' ')[0].ToUpper())
                        {
                            case ("ECHO"):

                                //Get the line removing echo.
                                output = line.Remove(0, 5);
                                if (string.IsNullOrWhiteSpace(ProcessVariableString(output, CmdDB, cmd, client, message)))
                                {
                                    error = true;
                                    //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                    errorEmbed.WithDescription($"Output string cannot be empty. ```{line}```");
                                    errorEmbed.AddInlineField("Line", LineInScript);
                                    errorEmbed.AddInlineField("Command", cmd);
                                    break;
                                }
                                await message.Channel.SendMessageAsync(ProcessVariableString(output, CmdDB, cmd, client, message), false);

                                break;
                            case ("ECHOTTS"):
                                //Get the line removing echo.
                                output = line.Remove(0, 8);
                                if (string.IsNullOrWhiteSpace(ProcessVariableString(output, CmdDB, cmd, client, message)))
                                {
                                    error = true;
                                    errorEmbed.WithDescription($"Output string cannot be empty. ```{line}```");
                                    errorEmbed.AddInlineField("Line", LineInScript);
                                    errorEmbed.AddInlineField("Command", cmd);
                                    break;
                                }
                                await message.Channel.SendMessageAsync(ProcessVariableString(output, CmdDB, cmd, client, message), true);

                                break;
                            case ("SETVAR"):
                                caseSetVar(line, ref error, ref errorEmbed, ref LineInScript, ref cmd);

                                break;
                            case ("CMD"):
                                //SocketMessage m = message as SocketMessage;
                                caseExecCmd(ProcessVariableString(line, CmdDB, cmd, client, message),ccmgr,CommandPrefix, ref error, ref errorEmbed, ref LineInScript,ref client, ref cmd, ref message);

                                break;
                            case ("BOTSTATUS"):
                                await ((DiscordSocketClient)client).SetGameAsync(ProcessVariableString(line.Remove(0, 10), CmdDB, cmd, client, message));
                                break;
                            case ("STATUSORB"):
                                string cond = line.Remove(0, 10).ToUpper();
                                switch (cond)
                                {
                                    case ("ONLINE"):
                                        await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.Online);
                                        break;
                                    case ("AWAY"):
                                        await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.Idle);
                                        break;
                                    case ("AFK"):
                                        await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.AFK);
                                        break;
                                    case ("BUSY"):
                                        await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.DoNotDisturb);
                                        break;
                                    case ("OFFLINE"):
                                        await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.Offline);
                                        break;
                                    case ("INVISIBLE"):
                                        await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.Invisible);
                                        break;
                                    default:
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nUnexpected Argument: {cond}. Try either ONLINE, BUSY, AWAY, AFK, INVISIBLE, OFFLINE.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error. Unexpected argument: {cond}.\r\nTry either ONLINE, BUSY, AWAY, AFK, INVISIBLE, OFFLINE.");
                                        errorEmbed.AddInlineField("Line", LineInScript);
                                        errorEmbed.AddInlineField("Command", cmd);
                                        break;
                                }

                                break;
                            case ("BOTGOLIVE"):
                                string linevar = ProcessVariableString(line, CmdDB, cmd, client, message);
                                string[] data = linevar.Remove(0, 10).Split(' ');
                                if (data.Length < 2)
                                {
                                    error = true;
                                    //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                    errorEmbed.WithDescription($"Function error: Expected format ```BOTGOLIVE <ChannelName> <status text>.```");
                                    errorEmbed.AddInlineField("Line", LineInScript);
                                    errorEmbed.AddInlineField("Command", cmd);
                                    break;
                                }
                                string statusText = line.Remove(0, 10 + data[0].Length + 1).Trim();

                                await ((DiscordSocketClient)client).SetGameAsync(statusText, $"https://twitch.tv/{data[0]}", StreamType.Twitch);
                                break;
                            case ("WAIT"):
                                int v = 1;
                                if (!int.TryParse(ProcessVariableString(line.Remove(0, 5), CmdDB, cmd, client, message), out v))
                                {
                                    error = true;
                                    //errorMessage = $"SCRIPT ERROR:```\r\nA number was expected here. You gave: {line.Remove(0, 5)}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                    errorEmbed.WithDescription($"Function error: Expected a valid number greater than zero & below the maximum value supported by the system. You gave: `{line.Remove(0, 5)}`");
                                    errorEmbed.AddInlineField("Line", LineInScript);
                                    errorEmbed.AddInlineField("Command", cmd);
                                    break;
                                }
                                if(v < 1)
                                {
                                    //errorMessage = $"SCRIPT ERROR:```\r\nA number was expected here. You gave: {line.Remove(0, 5)}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                    errorEmbed.WithDescription($"Function error: Expected a valid number greater than zero & below the maximum value supported by the system. You gave: `{line.Remove(0, 5)}`");
                                    errorEmbed.AddInlineField("Line", LineInScript);
                                    errorEmbed.AddInlineField("Command", cmd);
                                    error = true;
                                    break;
                                }
                                await Task.Delay(v);
                                break;
                            default:
                                error = true;
                                //errorMessage = $"SCRIPT ERROR:```\r\nUnexpected core function: {line.Split(' ')[0]}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                errorEmbed.WithDescription($"Unexpected function: ```{line.Split(' ')[0]}```");
                                errorEmbed.AddInlineField("Line", LineInScript);
                                errorEmbed.AddInlineField("Command", cmd);
                                break;
                        }

                    }
                    await Task.Delay(20);
                    LineInScript++;
                }
            }
            if (error)
            {
                await message.Channel.SendMessageAsync("",false,errorEmbed.Build());
                return;
            }
        }

        private void caseExecCmd(string v, CustomCommandManager ccmgr, char commandPrefix, ref bool error, ref EmbedBuilder errorEmbed, ref int lineInScript, ref string cmd, ref IMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task EvaluateScriptFile(string filename, INIFile CmdDB, string cmd, IDiscordClient client, IMessage message)
        {
            int LineInScript = 1;
            bool error = false;
            EmbedBuilder errorEmbed = new EmbedBuilder();

            errorEmbed.WithAuthor(client.CurrentUser);
            errorEmbed.WithTitle("CoreScript Error");
            errorEmbed.WithColor(Color.Red);
            errorEmbed.WithFooter("CoreScript Engine • RMSoftware.ModularBOT");
            //This method supports reading a script from a file. This script does not require ```DOS header.
            using (FileStream fs = File.OpenRead(filename))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    while (!error)
                    {

                        if (sr.Peek() == -1)
                        {
                            LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "End of script!"), ConsoleColor.Green);
                            break;
                        }
                        string line = await sr.ReadLineAsync();

                        if (LineInScript >= 1)
                        {
                            if (line.ToUpper().StartsWith("::") || line.ToUpper().StartsWith("REM") || line.ToUpper().StartsWith("//"))
                            {
                                //comment line.
                                LineInScript++;
                                continue;
                            }


                            //GET COMMAND PART.
                            string output = "";
                            switch (line.Split(' ')[0].ToUpper())
                            {
                                case ("ECHO"):

                                    //Get the line removing echo.
                                    output = line.Remove(0, 5);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(output, CmdDB, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Output string cannot be empty. ```{line}```");
                                        errorEmbed.AddInlineField("Line", LineInScript);
                                        errorEmbed.AddInlineField("Command", cmd);
                                        break;
                                    }
                                    await message.Channel.SendMessageAsync(ProcessVariableString(output, CmdDB, cmd, client, message), false);

                                    break;
                                case ("ECHOTTS"):
                                    //Get the line removing echo.
                                    output = line.Remove(0, 8);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(output, CmdDB, cmd, client, message)))
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"Output string cannot be empty. ```{line}```");
                                        errorEmbed.AddInlineField("Line", LineInScript);
                                        errorEmbed.AddInlineField("Command", cmd);
                                        break;
                                    }
                                    await message.Channel.SendMessageAsync(ProcessVariableString(output, CmdDB, cmd, client, message), true);

                                    break;
                                case ("SETVAR"):
                                    caseSetVar(line, ref error, ref errorEmbed, ref LineInScript, ref cmd);

                                    break;
                                case ("CMD"):
                                    //SocketMessage m = message as SocketMessage;
                                    caseExecCmd(ProcessVariableString(line, CmdDB, cmd, client, message),ccmgr,CommandPrefix, ref error, ref errorEmbed, ref LineInScript,ref client, ref cmd, ref message);

                                    break;
                                case ("BOTSTATUS"):
                                    await ((DiscordSocketClient)client).SetGameAsync(ProcessVariableString(line.Remove(0, 10), CmdDB, cmd, client, message));
                                    break;
                                case ("STATUSORB"):
                                    string cond = line.Remove(0, 10).ToUpper();
                                    switch (cond)
                                    {
                                        case ("ONLINE"):
                                            await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.Online);
                                            break;
                                        case ("AWAY"):
                                            await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.Idle);
                                            break;
                                        case ("AFK"):
                                            await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.AFK);
                                            break;
                                        case ("BUSY"):
                                            await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.DoNotDisturb);
                                            break;
                                        case ("OFFLINE"):
                                            await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.Offline);
                                            break;
                                        case ("INVISIBLE"):
                                            await ((DiscordSocketClient)client).SetStatusAsync(UserStatus.Invisible);
                                            break;
                                        default:
                                            error = true;
                                            //errorMessage = $"SCRIPT ERROR:```\r\nUnexpected Argument: {cond}. Try either ONLINE, BUSY, AWAY, AFK, INVISIBLE, OFFLINE.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                            break;
                                    }

                                    break;
                                case ("BOTGOLIVE"):
                                    string linevar = ProcessVariableString(line, CmdDB, cmd, client, message);
                                    string[] data = linevar.Remove(0, 10).Split(' ');
                                    if (data.Length < 2)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: Expected format ```BOTGOLIVE <ChannelName> <status text>.```");
                                        errorEmbed.AddInlineField("Line", LineInScript);
                                        errorEmbed.AddInlineField("Command", cmd);
                                        break;
                                    }
                                    string statusText = line.Remove(0, 10 + data[0].Length + 1).Trim();

                                    await ((DiscordSocketClient)client).SetGameAsync(statusText, $"https://twitch.tv/{data[0]}", StreamType.Twitch);
                                    break;
                                case ("WAIT"):
                                    int v = 1;
                                    if (!int.TryParse(ProcessVariableString(line.Remove(0, 5), CmdDB, cmd, client, message), out v))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nA number was expected here. You gave: {line.Remove(0, 5)}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: Expected a valid number greater than zero & below the maximum value supported by the system. You gave: `{line.Remove(0, 5)}`");
                                        errorEmbed.AddInlineField("Line", LineInScript);
                                        errorEmbed.AddInlineField("Command", cmd);
                                        break;
                                    }
                                    if (v < 1)
                                    {
                                        //errorMessage = $"SCRIPT ERROR:```\r\nA number was expected here. You gave: {line.Remove(0, 5)}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: Expected a valid number greater than zero & below the maximum value supported by the system. You gave: `{line.Remove(0, 5)}`");
                                        errorEmbed.AddInlineField("Line", LineInScript);
                                        errorEmbed.AddInlineField("Command", cmd);
                                        error = true;
                                        break;
                                    }
                                    await Task.Delay(v);
                                    break;
                                default:
                                    error = true;
                                    //errorMessage = $"SCRIPT ERROR:```\r\nUnexpected core function: {line.Split(' ')[0]}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                    errorEmbed.WithDescription($"Unexpected function: ```{line.Split(' ')[0]}```");
                                    errorEmbed.AddInlineField("Line", LineInScript);
                                    errorEmbed.AddInlineField("Command", cmd);
                                    break;
                            }

                        }
                        await Task.Delay(20);
                        LineInScript++;
                    }
                }
                if (error)
                {
                    await message.Channel.SendMessageAsync("",false,errorEmbed);
                    return;
                }
            }
        }
        #endregion

        private void caseSetVar(string line, ref bool error, ref EmbedBuilder errorEmbed, ref int LineInScript, ref string cmd)
        {
            string output = line;
            if (output.Split(' ').Length < 3)
            {
                error = true;
                //errorMessage = $"SCRIPT ERROR:```\r\nThe syntax of this function is incorrect.```"
                //+ $" ```Function {line.Split(' ')[0]}```" +
                //$"```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                errorEmbed.WithDescription($"The Syntax of this function is incorrect. ```{line}```");
                errorEmbed.AddField("Function", line.Split(' ')[0]);
                errorEmbed.AddInlineField("Line", LineInScript);
                errorEmbed.AddInlineField("Command", cmd);
                return;
            }
            output = line.Remove(0, 6).Trim();
            string varname = output.Split(' ')[0];
            output = output.Remove(0, varname.Length);
            output = output.Trim();
            try
            {
                Set(varname, output);
            }
            catch (ArgumentException ex)
            {
                error = true;
                
                errorEmbed.WithDescription($"{ex.Message}\r\n");
                errorEmbed.AddField("Function", "```"+line.Split(' ')[0]+"```",true);
                errorEmbed.AddField("Variable Name", "```"+varname+"```",true);
                
                errorEmbed.AddField("Line", LineInScript);
                errorEmbed.AddField("Command", cmd);
                return;
            }

        }

        private void caseExecCmd(string line, CustomCommandManager ccmg, char CommandPrefix, ref bool error, ref EmbedBuilder errorEmbed, ref int LineInScript,
            ref IDiscordClient client, ref string cmd, ref IMessage ArgumentMessage)
        {
            string ecmd = line.Remove(0, 4);
            if (ccmg.Process(new PsuedoMessage(CommandPrefix.ToString() + ecmd, ArgumentMessage.Author as SocketUser, 
                (ArgumentMessage.Channel as IGuildChannel), MessageSource.Bot)).GetAwaiter().GetResult())
            {
                LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
                LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "CustomCMD Success..."));
                return;
            }
            //Damn, I can't be sassy here... If it was a command, but not a ccmg command, then try the context for modules. If THAT didn't work
            //Then it will output the result of the context.
            var context = new CommandContext(client, new PsuedoMessage(CommandPrefix.ToString() + ecmd, ArgumentMessage.Author as SocketUser, (ArgumentMessage.Channel as IGuildChannel), MessageSource.Bot));
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = cmdsvr.ExecuteAsync(context, 1, services);
            LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
            LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", result.Result.ToString()));
            if (!result.Result.IsSuccess)
            {
                error = true;
                errorEmbed.WithDescription($"CMD Function Error: The command context returned `{result.Result.ErrorReason}`\r\n```{line}```");
                errorEmbed.AddInlineField("Line", LineInScript);
                errorEmbed.AddInlineField("Command", cmd);
                return;
            }
        }

        private void LogToConsole(LogMessage msg)
        {
            if (!LOG_ONLY_MODE)
            {
                _writer.WriteEntry(msg);
            }
            else
            {
                Console.WriteLine(JsonConvert.SerializeObject(msg));
            }
            if (msg.Exception != null)
            {
                using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + msg.ToString());
                        sw.Flush();
                    }
                }
            }
        }

        public void LogToConsole(LogMessage msg, ConsoleColor entryColor)
        {
            if (!LOG_ONLY_MODE)
            {
                _writer.WriteEntry(msg, entryColor);
            }
            else
            {
                Console.WriteLine(JsonConvert.SerializeObject(msg));
            }
            if (msg.Exception != null)
            {
                using (FileStream fs = new FileStream("ERRORS.LOG", FileMode.Append))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(DateTime.Today.ToString("MM/dd/yyyy") + "   " + msg.ToString());
                        sw.Flush();
                    }
                }
            }
        }
    }
}
