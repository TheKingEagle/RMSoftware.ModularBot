using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ModularBOT.Entity;
using System.Data;
using System.Net;
namespace ModularBOT.Component
{
    public class CoreScript
    {
        #region Private Fields & Properties
        internal CustomCommandManager ccmgr;
        internal Dictionary<string,(object value,bool hidden)> Variables { get; set; }

        internal Dictionary<ulong, Dictionary<string, (object value, bool hidden)>> UserVariableDictionaries { get; set; }

        internal Dictionary<ulong, (IMessage InvokerMessage, ulong ChannelID, string PromptReply)> ActivePrompts { get; set; }
        public List<CSFunction> CoreScriptFunctions = new List<CSFunction>();
        public List<SystemVariable> SystemVariables = new List<SystemVariable>();
        internal CommandService cmdsvr;
        public readonly IServiceProvider Services;
        public short OutputCount = 0;

        public bool terminated = false;
        public bool EXIT = false;
        private bool ClMRHandlerBound = false;
        internal static Dictionary<ulong, int> MessageCounter = new Dictionary<ulong, int>(); //MessageCounter<Channel,Count>


        internal bool contextToDM = false;
        internal ulong channelTarget = 0;
        #endregion

        //TODO: Priority=LOW; Save custom global & user variables, and populate them via constructor.
        public CoreScript(CustomCommandManager ccmgr,
            ref IServiceProvider _services, 
            Dictionary<string, (object value, bool hidden)> dict = null, 
            Dictionary<ulong, (IMessage InvokerMessage, ulong ChannelID, string PromptReply)> ap= null,
            Dictionary<ulong, Dictionary<string, (object value, bool hidden)>> uv = null)
        {
            cmdsvr = _services.GetRequiredService<CommandService>();
            Services = _services;
            this.ccmgr = ccmgr;
            Variables = dict ?? new Dictionary<string, (object value, bool hidden)>();
            ActivePrompts = ap ?? new Dictionary<ulong, (IMessage InvokerMessage, ulong ChannelID, string PromptReply)>();
            UserVariableDictionaries = uv ?? new Dictionary<ulong, Dictionary<string, (object value, bool hidden)>>();

            #region =================================== [CoreScript FUNCTIONS] ===================================
            CoreScriptFunctions.Add(new CSFunctions.CSFAttach());
            CoreScriptFunctions.Add(new CSFunctions.CSFEcho());
            CoreScriptFunctions.Add(new CSFunctions.CSFCounterStart());
            CoreScriptFunctions.Add(new CSFunctions.CSFCounterStop());
            CoreScriptFunctions.Add(new CSFunctions.CSFRoleAdd());
            CoreScriptFunctions.Add(new CSFunctions.CSFRoleAssign());
            CoreScriptFunctions.Add(new CSFunctions.CSFRoleDel());
            CoreScriptFunctions.Add(new CSFunctions.CSFDelMsg());
            CoreScriptFunctions.Add(new CSFunctions.CSFIf());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbed());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbedDesc());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbedImage());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbedThImage());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbedFooter());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbedFooterI());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbedAuthor());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbedAuthorI());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbedColor());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbedAddField());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbedAddFieldI());
            CoreScriptFunctions.Add(new CSFunctions.CSFEmbedSend());
            CoreScriptFunctions.Add(new CSFunctions.CSFSetTarget());
            CoreScriptFunctions.Add(new CSFunctions.CSFSet());
            CoreScriptFunctions.Add(new CSFunctions.CSFCmd());
            CoreScriptFunctions.Add(new CSFunctions.CSFTitle());
            CoreScriptFunctions.Add(new CSFunctions.CSFOrb());
            CoreScriptFunctions.Add(new CSFunctions.CSFStrmStatus());
            CoreScriptFunctions.Add(new CSFunctions.CSFWait());
            #endregion

            #region =================================== [CoreScript SYSTEMVAR] ===================================
            SystemVariables.Add(new SystemVariables.Invoker());
            SystemVariables.Add(new SystemVariables.Invoker_Nick());
            SystemVariables.Add(new SystemVariables.Invoker_NoMention());
            SystemVariables.Add(new SystemVariables.Invoker_Avatar());
            SystemVariables.Add(new SystemVariables.Self());
            SystemVariables.Add(new SystemVariables.Self_Nick());
            SystemVariables.Add(new SystemVariables.Self_NoMention());
            SystemVariables.Add(new SystemVariables.Self_Avatar());
            SystemVariables.Add(new SystemVariables.Bot_Owner());
            SystemVariables.Add(new SystemVariables.Bot_Owner_NoMention());
            SystemVariables.Add(new SystemVariables.Bot_Owner_Avatar());
            SystemVariables.Add(new SystemVariables.Guild_Owner());
            SystemVariables.Add(new SystemVariables.Go_Nick());
            SystemVariables.Add(new SystemVariables.Go_Avatar());
            SystemVariables.Add(new SystemVariables.Command());
            SystemVariables.Add(new SystemVariables.Command_Count());
            SystemVariables.Add(new SystemVariables.Latency());
            SystemVariables.Add(new SystemVariables.Prefix());
            SystemVariables.Add(new SystemVariables.PrefixPF());
            SystemVariables.Add(new SystemVariables.Version());
            SystemVariables.Add(new SystemVariables.OS_Name());
            SystemVariables.Add(new SystemVariables.OS_Bit());
            SystemVariables.Add(new SystemVariables.OS_Ver());
            SystemVariables.Add(new SystemVariables.Bot_Mem());
            SystemVariables.Add(new SystemVariables.Guild());
            SystemVariables.Add(new SystemVariables.Guild_ID());
            SystemVariables.Add(new SystemVariables.Guild_Count());
            SystemVariables.Add(new SystemVariables.Guild_UserCount());
            SystemVariables.Add(new SystemVariables.Guild_Icon());
            SystemVariables.Add(new SystemVariables.Channel_NoMention());
            SystemVariables.Add(new SystemVariables.Channel());
            SystemVariables.Add(new SystemVariables.Channel_ID());
            SystemVariables.Add(new SystemVariables.MsgCount());
            SystemVariables.Add(new SystemVariables.Counter());

            #endregion

            Task.Run(() => OutputThrottleRS());//output "throttle" loop. [script rate-limiting]
        }

        #region Public Methods
        public void Set(string var, object value, bool hidden = false)
        {
            if(var.Length > 20)
            {
                throw (new ArgumentException("Variable name limit exceeded! Keep variable names 20 characters or less"));
            }
            if (string.IsNullOrEmpty((string)value))
            {
                throw (new ArgumentException($"You cannot set `{var}` to a value of `null`"));
            }
            if (SystemVariables.Where(x => x.Name == var).Count() >= 1)
            {
                throw (new ArgumentException("This variable cannot be modified."));
            }
            string function = "";
            int findex = ((string)value).IndexOf('(');
            if (findex > 0) function = ((string)value).ToLower().Remove(findex);

            bool result = Variables.TryGetValue(var, out (object value, bool hidden) v);
            if (!result)
            {
                object functionResult = EvaluateVarFunction(value, function);
                //add the new variable.
                object ev = functionResult ?? value;
                Variables.Add(var, (ev, hidden));
                Services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Debug, "Variables", $"Result False. Creating variable. Name:{var}; Value: {value}; Hidden: {hidden}"));
                return;
            }
            else
            {
                object functionResult = EvaluateVarFunction(value, function);
                object ev = functionResult ?? value;

                Variables[var] = (ev, hidden);
                Variables = Variables;
                Services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Debug, "Variables", $"Result true. modifying variable. Name:{var}; Value: {Variables[var].value}; Hidden: {Variables[var].hidden};"));
                return;
            }
        }

        private static object EvaluateVarFunction(object value, string function)
        {
            object functionResult = null;
            string parserdata;
            if (function.ToLower() == "eval")
            {
                string val = ((string)value).ToLower();
                parserdata = val.Replace("eval(", "");
                parserdata = parserdata.Remove(parserdata.LastIndexOf(")"));
                functionResult = new DataTable().Compute(parserdata, null);
            }
            if (function.ToLower() == "urlencode")
            {
                string val = ((string)value).ToLower();
                parserdata = val.Replace("urlencode(", "");
                parserdata = parserdata.Remove(parserdata.LastIndexOf(")"));
                functionResult = WebUtility.UrlEncode(parserdata);
            }
            if (function.ToLower() == "rand")
            {
                string val = ((string)value).ToLower();
                parserdata = val.Replace("rand(", "");
                parserdata = parserdata.Remove(parserdata.LastIndexOf(")"));
                string[] randrange = parserdata.Split(',');
                if (randrange.Length < 2 || randrange.Length > 2)
                {
                    throw new ArgumentException("You must specify a range. Usage: RAND(low number,high number)");
                }
                if (!int.TryParse(randrange[0], out int randmin)) throw new ArgumentException("You must specify valid number for minimum. Usage: RAND(low number,high number)");
                if (!int.TryParse(randrange[1], out int randmax)) throw new ArgumentException("You must specify valid number for maximum. Usage: RAND(low number,high number)");
                if (randmin > randmax) throw new ArgumentException("Your minimum must not be higher than your maximum. Usage: RAND(minimum number,maximum number)");
                functionResult = (new Random()).Next(randmin, randmax + 1);
            }

            return functionResult;
        }

        public void SetUserVar(ulong KEY,string var, object value, bool hidden = false)
        {
            if(KEY == 0)
            {
                throw (new ArgumentException("A User ID is required."));
            }
            if (var.Length > 20)
            {
                throw (new ArgumentException("Variable name limit exceeded! Keep variable names 20 characters or less"));
            }
            if (string.IsNullOrEmpty((string)value))
            {
                throw (new ArgumentException($"You cannot set `{var}` to a value of `null`"));
            }
            if (SystemVariables.Where(x => x.Name == var).Count() >= 1)
            {
                throw (new ArgumentException("This variable cannot be modified."));
            }

            bool HasDictionaryResult = UserVariableDictionaries.TryGetValue(KEY, out Dictionary<string, (object value, bool hidden)> userVarDictionary);

            string function = "";
            int findex = ((string)value).IndexOf('(');
            if (findex > 0) function = ((string)value).ToLower().Remove(findex);
            if (!HasDictionaryResult)                                                       //NO DICRIONARY FOUND!
            {
                userVarDictionary = new Dictionary<string, (object value, bool hidden)>();      //Create new dictionary;
                object functionResult = EvaluateVarFunction(value, function);
                object ev = functionResult ?? value;

                userVarDictionary.Add(var,( ev, hidden));                                    //Add variable to new dictionary;
                UserVariableDictionaries.Add(KEY, userVarDictionary);                           //Add the new dictionary to the master dictionary;
                UserVariableDictionaries = UserVariableDictionaries;                            //Probably overkill?

                Services.GetRequiredService<ConsoleIO>()                                        //Tell Console about it.
                    .WriteEntry(new LogMessage(LogSeverity.Debug, "Variables", 
                    $"User did not have a variable dictionary. " +
                    $"Created new one with new variable. KEY: {KEY} VarName: {var} Value: {value} Hidden: {hidden}"));

                return;
            }
            else                                                                            //DICTIONARY FOUND!
            {
                bool HasVariable = userVarDictionary.TryGetValue(var, out (object value, bool hidden) v);

                if(!HasVariable)                                                                //NO VARIABLE FOUND!
                {
                    object functionResult = EvaluateVarFunction(value, function);
                    object ev = functionResult ?? value;

                    userVarDictionary.Add(var, (ev, hidden));                                    //Add the variable to the dictionary.
                    UserVariableDictionaries[KEY] = userVarDictionary;                              //SET the user dictionary in master dictionary.
                    UserVariableDictionaries = UserVariableDictionaries;                            //Probably overkill?

                    Services.GetRequiredService<ConsoleIO>()                                        //Tell Console about it.
                    .WriteEntry(new LogMessage(LogSeverity.Debug, "Variables",
                    $"User Dictionary did not have the variable. " +
                    $"Created new variable and updated the variable list. KEY: {KEY} VarName: {var} Value: {value} Hidden: {hidden}"));
                    return;
                }
                else                                                                            //VARIABLE FOUND!
                {
                    object functionResult = EvaluateVarFunction(value, function);
                    //add the new variable.
                    object ev = functionResult ?? value;

                    userVarDictionary[var] = (ev, hidden);                                       //SET the variable in the dictionary
                    UserVariableDictionaries[KEY] = userVarDictionary;                              //SET the dictionary in the master dictionary.
                    UserVariableDictionaries = UserVariableDictionaries;                            //Probably overkill?

                    Services.GetRequiredService<ConsoleIO>()                                        //Tell Console about it.
                    .WriteEntry(new LogMessage(LogSeverity.Debug, "Variables",
                    $"User Dictionary HAD the variable dictionary AND variable. " +
                    $"Updating everything. KEY: {KEY} VarName: {var} Value: {value} Hidden: {hidden}"));
                }
                return;                                                                     
            }
        }

        public object Get(string var, ulong userid = 0)
        {
            if(userid > 0)                                                                          //USER ID is set.
            {
                if(UserVariableDictionaries.ContainsKey(userid))
                {
                    bool cuservar = UserVariableDictionaries[userid].TryGetValue(var, out (object value, bool hidden) uservar);
                    if (!cuservar)
                    {
                        bool result = Variables.TryGetValue(var, out (object value, bool hidden) v);

                        if (!result)
                        {
                            return null;
                        }
                        else
                        {
                            return v.value;
                        }
                    }
                    else
                    {
                        return uservar.value;
                    }
                }
                else
                {
                    bool result = Variables.TryGetValue(var, out (object value, bool hidden) v);
                    if (!result)
                    {
                        return null;
                    }
                    else
                    {
                        return v.value;
                    }
                }
            }
            else                                                                                    //USER ID NOT SET.
            {
                bool result = Variables.TryGetValue(var, out (object value, bool hidden) v);
                if (!result)
                {
                    return null;
                }
                else
                {
                    return v.value;
                }
            }

        }

        public string ProcessVariableString(GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message)
        {
            string Processed = response;

            //Check for use of system-defined variables.

            foreach (Match item in Regex.Matches(Processed, @"%[^%]*%"))
            {
                string vname = item.Value.Replace("%", "");
                var sysvar = SystemVariables.FirstOrDefault(x => x.Name == vname);
                if ( sysvar != null)
                {
                    string replacedvar = sysvar.GetReplacedString(gobj, Processed, cmd, client, message,cmdsvr);
                    Processed = replacedvar;
                }
            }

            //Check for use of Custom-defined variables.

            foreach (Match item in Regex.Matches(Processed, @"%[^%]*%"))
            {
                string vname = item.Value.Replace("%", "");
                if (Get(vname, message.Author.Id) != null)
                {
                    string replacedvar = Get(vname, message.Author.Id).ToString();
                    Processed = Processed.Replace(item.Value, replacedvar);
                }
            }
            //Final result.
            return Processed;
        }

        public void SetTarget(bool CtxToDM, ulong target)
        {
            channelTarget = target;
            contextToDM = CtxToDM;
        }

        public async Task EvaluateScript(GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder CSEmbed = null, bool isFile = false)
        {
            int LineInScript = 0;
            bool error = false;
            contextToDM = false;
            channelTarget = 0;
            terminated = false;
            EmbedBuilder errorEmbed = new EmbedBuilder();
            if(CSEmbed == null)
            {

                CSEmbed = new EmbedBuilder();
            }
            //CSEmbed.WithAuthor(client.CurrentUser);
            if (!ClMRHandlerBound)
            {
                ((DiscordShardedClient)client).MessageReceived += CoreScript_MessageReceived;
                ClMRHandlerBound = true;
            }
            
            if (!response.EndsWith("```"))
            {
                error = true;
                errorEmbed.WithDescription("The codeblock was not closed.");
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
            }
            errorEmbed.WithAuthor(client.CurrentUser);
            errorEmbed.WithTitle("CoreScript Error");
            errorEmbed.WithColor(Color.Red);
            errorEmbed.WithFooter("CoreScript Engine • ModularBOT");
            //For the sake of in-chat scripts, they should be smaller.
            using (StringReader sr = new StringReader(response))
            {
                try
                {

                    while ((!error) & (!EXIT))
                    {

                        if (sr.Peek() == -1)
                        {
                            if (!terminated)
                            {
                                error = true;
                                //errorMessage = $"SCRIPT ERROR:```The codeblock was not closed.\r\nCoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                errorEmbed.WithDescription("The codeblock was not closed.");
                                errorEmbed.AddField("Line", LineInScript, true);
                                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);

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
                                errorEmbed.AddField("Line", LineInScript, true);
                                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);

                                break;
                            }
                        }
                        if (LineInScript >= 1)
                        {
                            #region Structure Verification
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
                                Services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "CoreScript", "Comment: " + line));
                                LineInScript++;
                                continue;
                            }

                            if (line == "```DOS")
                            {
                                error = true;
                                //errorMessage = $"SCRIPT ERROR:```\r\nDuplicate header:``` ```{line.Split(' ')[0]}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                errorEmbed.WithDescription($"Duplicate Header: ```{line.Split(' ')[0]}```");
                                errorEmbed.AddField("Line", LineInScript, true);
                                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);

                                break;
                            }
                            #endregion

                            #region Function Execution

                            var function = CoreScriptFunctions.FirstOrDefault(x => x.Name == line.Split(' ')[0].ToUpper());
                            if (function == null)
                            {
                                errorEmbed.WithDescription($"Unexpected function: \r\n```\r\n{line.Split(' ')[0]}\r\n```");
                                errorEmbed.AddField("Line", LineInScript, true);
                                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                error = true;
                                break;
                            }
                            if (function != null)
                            {
                                error = !await function.Evaluate(this, gobj, response, cmd, client, message, errorEmbed, LineInScript, line, contextToDM, channelTarget, CSEmbed,isFile);
                                if (error)
                                {
                                    break;
                                }
                            }

                            #endregion
                        }

                        LineInScript++;
                    }

                    //CLEAR embeds.
                    CSEmbed = null;
                    
                }
                catch (Exception ex)
                {

                    error = true;
                    //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                    errorEmbed.WithDescription($"The script failed due to an exception.");

                    errorEmbed.AddField("details", $"```{ex.Message}```");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                }
                
            }
            if (error)
            {
                await message.Channel.SendMessageAsync("", false, errorEmbed.Build());
                return;
            }
        }

        public async Task EvaluateScriptFile(GuildObject gobj, string filename, IDiscordClient client, IMessage message, GuildCommand cmd = null)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                string response = $"```DOS\r\n{sr.ReadToEnd()}\r\n```";
                await EvaluateScript(gobj, response, cmd, client, message, null,true);
            }
        }
        #endregion
        
        #region CORESCRIPT EVENTS
        private Task CoreScript_MessageReceived(SocketMessage arg)
        {

            if (MessageCounter.ContainsKey(arg.Channel.Id))
            {
                MessageCounter[arg.Channel.Id]++;

            }
            if (ActivePrompts.TryGetValue(arg.Author.Id, out (IMessage InvokerMessage, ulong ChannelID, string PromptReply) result))
            {
                if (arg.Author.Id == result.InvokerMessage.Author.Id && arg.Channel.Id == result.ChannelID)
                {
                    ActivePrompts[arg.Author.Id] = (result.InvokerMessage, result.ChannelID, arg.Content);
                    ActivePrompts = ActivePrompts;//Why was this here?
                }
            }
            return Task.Delay(0);
        }

        #endregion
        
        #region Private methods
        private void CaseSetVar(string line, ref bool error, ref EmbedBuilder errorEmbed, ref int LineInScript, ref GuildCommand cmd, ref GuildObject gobj, ref IDiscordClient client,ref IMessage message, bool hidden=false)
        {
            string output = line;
            if (output.Split(' ').Length < 2)
            {
                error = true;
                //errorMessage = $"SCRIPT ERROR:```\r\nThe syntax of this function is incorrect.```"
                //+ $" ```Function {line.Split(' ')[0]}```" +
                //$"```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                errorEmbed.WithDescription($"The Syntax of this function is incorrect. ```{line}```");
                errorEmbed.AddField("Function", line.Split(' ')[0]);
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return;
            }
            if (hidden)
            {
                output = line.Remove(0, 9).Trim();// SET /H 
            }
            if (!hidden)
            {
                output = line.Remove(0, 6).Trim(); //SET 
            }
            string varname = output.Split('=')[0];
            output = output.Split('=')[1];
            output = output.Trim();
            output = ProcessVariableString(gobj, output, cmd, client, message);
            try
            {
                Set(varname, output,hidden);
            }
            catch (ArgumentException ex)
            {
                error = true;

                errorEmbed.WithDescription($"{ex.Message}\r\n");
                errorEmbed.AddField("Function", "```" + line.Split(' ')[0] + "```", true);
                errorEmbed.AddField("Variable Name", "```" + varname + "```", true);

                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return;
            }

        }

        
        private void CaseExecCmd(string line, CustomCommandManager ccmg, GuildObject guildObject, ref bool error, ref EmbedBuilder errorEmbed, ref int LineInScript,
            ref IDiscordClient client, ref GuildCommand cmd, ref IMessage ArgumentMessage)
        {
            
            ulong gid = 0;
            if (ArgumentMessage.Channel is SocketGuildChannel channel)
            {
                gid = channel.Guild.Id;
            }
            guildObject = ccmg.GuildObjects.FirstOrDefault(x => x.ID == gid) ?? ccmg.GuildObjects.FirstOrDefault(x => x.ID == 0);
            
            string ecmd = line.Remove(0, 4);
            if (cmd != null)
            {

                //if (ecmd == cmd.Name)
                //{
                //    error = true;
                //    errorEmbed.WithDescription($"CMD Function Error: You cannot call this command here, that's API abuse.\r\n```{line}```");
                //    errorEmbed.AddField("Line", LineInScript, true);
                //    errorEmbed.AddField("Execution Context", ecmd ?? "No context", true);
                //    return;
                //}
            }
            string resp = ccmg.ProcessMessage(new PseudoMessage(guildObject.CommandPrefix + ecmd, ArgumentMessage.Author as SocketUser,
                (ArgumentMessage.Channel as IGuildChannel), MessageSource.Bot));
            if (resp != "SCRIPT" && resp != "EXEC" && resp != "" && resp != "CLI_EXEC" && resp != null)
            {

                ArgumentMessage.Channel.SendMessageAsync(resp);
                LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
                LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "CustomCMD Success..."));
                return;
            }
            if ((resp == "SCRIPT" || resp == "EXEC" || resp == "" || resp == "CLI_EXEC") && resp != null)
            {

                LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
                LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "CustomCMD Success..."));
                return;
            }
            //Damn, I can't be sassy here... If it was a command, but not a ccmg command, then try the context for modules. If THAT didn't work
            //Then it will output the result of the context.
            var context = new CommandContext(client, new PseudoMessage(guildObject.CommandPrefix + ecmd, ArgumentMessage.Author as SocketUser, (ArgumentMessage.Channel as IGuildChannel), MessageSource.Bot));
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = cmdsvr.ExecuteAsync(context, guildObject.CommandPrefix.Length, Services);
            LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
            LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", result.Result.ToString()));
            if (!result.Result.IsSuccess)
            {
                error = true;
                errorEmbed.WithDescription($"CMD Function Error: The command context returned the following error:\r\n`{result.Result.ErrorReason}`\r\n```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", ecmd ?? "No context", true);
                return;
            }
        }

        internal void LogToConsole(LogMessage msg)
        {

            Services.GetRequiredService<ConsoleIO>().WriteEntry(msg);

            if (msg.Exception != null)
            {
                Services.GetRequiredService<ConsoleIO>().WriteErrorsLog(msg.Exception);
            }
        }

        public void LogToConsole(LogMessage msg, ConsoleColor entryColor)
        {
            Services.GetRequiredService<ConsoleIO>().WriteEntry(msg, entryColor);

            if (msg.Exception != null)
            {
                Services.GetRequiredService<ConsoleIO>().WriteErrorsLog(msg.Exception);
            }
        }

        private void OutputThrottleRS()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(5000);
                OutputCount = 0;//reset every 5 seconds.
            }
        }

        #endregion

    }
}