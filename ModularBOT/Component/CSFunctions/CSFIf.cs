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
    public class CSFIf : CSFunction
    {
        public CSFIf()
        {
            Name = "IF";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CoreScript",
                                        "IF Statement hit."), ConsoleColor.DarkYellow);
            string rs = line.Remove(0, Name.Length).Trim();
            string[] Component = rs.Split(' ');

            #region '==' Compare
            if (Component[0].Contains("=="))
            {
                string[] ConditionalCompare = { "==" };
                engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "Conditional =="), ConsoleColor.DarkYellow);
                string[] parsedCondition = Component[0].Split(ConditionalCompare, 2, StringSplitOptions.None);
                if (parsedCondition.Length < 2)
                {
                    return ScriptError("Syntax is not correct.",
                    "%variable1%==%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                if (engine.ProcessVariableString(gobj, parsedCondition[0], cmd, client, message) == engine.ProcessVariableString(gobj, parsedCondition[1], cmd, client, message))
                {
                    if (rs.Remove(0, Component[0].Length + 1).ToUpper() == "EXIT")
                    {
                        engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "IF STATEMENT had EXIT. Terminating"));
                        engine.EXIT = true;
                        return true;
                    }
                    string SubScript = "```DOS\r\n" + rs.Remove(0, Component[0].Length + 1) + "\r\n```";
                    await engine.EvaluateScript(gobj, SubScript, cmd, client, message, CSEmbed);
                    return true;
                }
            }
            #endregion

            #region '!=' Compare
            if (Component[0].Contains("!="))
            {
                string[] ConditionalCompare = { "!=" };
                engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "Conditional !="), ConsoleColor.DarkYellow);
                string[] parsedCondition = Component[0].Split(ConditionalCompare, 2, StringSplitOptions.None);
                if (parsedCondition.Length < 2)
                {
                    return ScriptError("Syntax is not correct.",
                    "%variable1%!=%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                if (engine.ProcessVariableString(gobj, parsedCondition[0], cmd, client, message) != engine.ProcessVariableString(gobj, parsedCondition[1], cmd, client, message))
                {
                    if (rs.Remove(0, Component[0].Length + 1).ToUpper() == "EXIT")
                    {
                        engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "IF STATEMENT had EXIT. Terminating"));
                        engine.EXIT = true;
                        return true;
                    }
                    string SubScript = "```DOS\r\n" + rs.Remove(0, Component[0].Length + 1) + "\r\n```";
                    await engine.EvaluateScript(gobj, SubScript, cmd, client, message, CSEmbed);
                    return true;
                }
            }
            #endregion

            #region '>=' Compare
            if (Component[0].Contains(">="))
            {
                string[] ConditionalCompare = { ">=" };
                engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "Conditional >="), ConsoleColor.DarkYellow);
                string[] parsedCondition = Component[0].Split(ConditionalCompare, 2, StringSplitOptions.None);
                if (parsedCondition.Length < 2)
                {
                    return ScriptError("Syntax is not correct.",
                    "%variable1%>=%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                string sleft = engine.ProcessVariableString(gobj, parsedCondition[0], cmd, client, message);
                string sright = engine.ProcessVariableString(gobj, parsedCondition[1], cmd, client, message);
                if (!long.TryParse(sleft, out long left))
                {
                    return ScriptError("Type Mismatch. Expected numeric left-side value",
                    "%variable1%>=%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                if (!long.TryParse(sright, out long right))
                {
                    return ScriptError("Type Mismatch. Expected numeric right-side value",
                    "%variable1%>=%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                if (left >= right)
                {
                    if (rs.Remove(0, Component[0].Length + 1).ToUpper() == "EXIT")
                    {
                        engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "IF STATEMENT had EXIT. Terminating"));
                        engine.EXIT = true;
                        return true;
                    }
                    string SubScript = "```DOS\r\n" + rs.Remove(0, Component[0].Length + 1) + "\r\n```";
                    await engine.EvaluateScript(gobj, SubScript, cmd, client, message, CSEmbed);
                    return true;
                }
            }
            #endregion

            #region '>' Compare
            if (Component[0].Contains(">") && !Component[0].Contains(">="))
            {
                string[] ConditionalCompare = { ">" };
                engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "Conditional >"), ConsoleColor.DarkYellow);
                string[] parsedCondition = Component[0].Split(ConditionalCompare, 2, StringSplitOptions.None);
                if (parsedCondition.Length < 2)
                {
                    return ScriptError("Syntax is not correct.",
                    "%variable1%>%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                string sleft = engine.ProcessVariableString(gobj, parsedCondition[0], cmd, client, message);
                string sright = engine.ProcessVariableString(gobj, parsedCondition[1], cmd, client, message);
                if (!long.TryParse(sleft, out long left))
                {
                    return ScriptError("Type Mismatch. Expected numeric left-side value",
                    "%variable1%>%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                if (!long.TryParse(sright, out long right))
                {
                    return ScriptError("Type Mismatch. Expected numeric right-side value",
                    "%variable1%>%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                if (left > right)
                {
                    if (rs.Remove(0, Component[0].Length + 1).ToUpper() == "EXIT")
                    {
                        engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "IF STATEMENT had EXIT. Terminating"));
                        engine.EXIT = true;
                        return true;
                    }
                    string SubScript = "```DOS\r\n" + rs.Remove(0, Component[0].Length + 1) + "\r\n```";
                    await engine.EvaluateScript(gobj, SubScript, cmd, client, message, CSEmbed);
                    return true;
                }
            }
            #endregion

            #region '<=' Compare
            if (Component[0].Contains("<="))
            {
                string[] ConditionalCompare = { "<=" };
                engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "Conditional <="), ConsoleColor.DarkYellow);
                string[] parsedCondition = Component[0].Split(ConditionalCompare, 2, StringSplitOptions.None);
                if (parsedCondition.Length < 2)
                {
                    return ScriptError("Syntax is not correct.",
                    "%variable1%<=%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                string sleft = engine.ProcessVariableString(gobj, parsedCondition[0], cmd, client, message);
                string sright = engine.ProcessVariableString(gobj, parsedCondition[1], cmd, client, message);
                if (!long.TryParse(sleft, out long left))
                {
                    return ScriptError("Type Mismatch. Expected numeric left-side value",
                    "%variable1%<=%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                if (!long.TryParse(sright, out long right))
                {
                    return ScriptError("Type Mismatch. Expected numeric right-side value",
                    "%variable1%<=%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                if (left <= right)
                {
                    if (rs.Remove(0, Component[0].Length + 1).ToUpper() == "EXIT")
                    {
                        engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "IF STATEMENT had EXIT. Terminating"));
                        engine.EXIT = true;
                        return true;
                    }
                    string SubScript = "```DOS\r\n" + rs.Remove(0, Component[0].Length + 1) + "\r\n```";
                    await engine.EvaluateScript(gobj, SubScript, cmd, client, message, CSEmbed);
                    return true;
                }
            }
            #endregion

            #region '<' Compare
            if (Component[0].Contains("<") && !Component[0].Contains("<="))
            {
                string[] ConditionalCompare = { "<" };
                engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "Conditional <"), ConsoleColor.DarkYellow);
                string[] parsedCondition = Component[0].Split(ConditionalCompare, 2, StringSplitOptions.None);
                if (parsedCondition.Length < 2)
                {
                    return ScriptError("Syntax is not correct.",
                     "%variable1%<%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                string sleft = engine.ProcessVariableString(gobj, parsedCondition[0], cmd, client, message);
                string sright = engine.ProcessVariableString(gobj, parsedCondition[1], cmd, client, message);
                if (!long.TryParse(sleft, out long left))
                {
                    return ScriptError("Type Mismatch. Expected numeric left-side value",
                    "%variable1%<%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                if (!long.TryParse(sright, out long right))
                {
                    return ScriptError("Type Mismatch. Expected numeric right-side value",
                    "%variable1%<%variable2% <Function>", cmd, errorEmbed, LineInScript, line);
                }
                if (left < right)
                {
                    if (rs.Remove(0, Component[0].Length + 1).ToUpper() == "EXIT")
                    {
                        engine.LogToConsole(new LogMessage(LogSeverity.Critical, "CSCond", "IF STATEMENT had EXIT. Terminating"));
                        engine.EXIT = true;
                        return true;
                    }
                    string SubScript = "```DOS\r\n" + rs.Remove(0, Component[0].Length + 1) + "\r\n```";
                    await engine.EvaluateScript(gobj, SubScript, cmd, client, message, CSEmbed);
                    return true;
                }
            }
            #endregion

            return true;
        }
    }
}
