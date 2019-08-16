using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ModularBOT.Component;
using Discord.Commands;
using System.IO;
using Newtonsoft.Json;
using ModularBOT;
using Discord.Rest;

namespace TestModule
{
    [Summary("A Basic moderation toolkit for ModularBOT")]
    public class TestModule : ModuleBase
    {
        public DiscordShardedClient _client { get; set; }
        public ConsoleIO _writer { get; set; }
        public TestModuleService _jservice { get; set; }

        public PermissionManager _permissions { get; set; }

        public ConfigurationManager _configmgr { get; set; }
        public TestModule(DiscordShardedClient discord, TestModuleService joinservice, ConsoleIO writer, PermissionManager manager, ConfigurationManager cnfgmgr)
        {
            _client = discord;
            _jservice = joinservice;
            _writer = writer;
            _permissions = manager;
            _configmgr = cnfgmgr;
            _writer.WriteEntry(new LogMessage(LogSeverity.Critical, "TestMOD", "Constructor called!!!!!!!!!"));
        }
        [Command("tpmgr"),Remarks("AccessLevels.Administrator")]
        public async Task Showtest()
        {
            if(_permissions.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await ReplyAsync("", false, _permissions.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            await ReplyAsync("You have the correct access level!");
        }

        [Command("Kick", RunMode = RunMode.Async)]
        public async Task Kick(IGuildUser user, [Remainder]string reason = "being an ass")
        {
            #region ERRORS
            if(Context.Guild == null)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Not Supported", 
                    "You cannot use this command here! Please make sure you're calling from a guild.", Color.Red));
                return;
            }
            SocketGuildUser SGUuser = Context.User as SocketGuildUser;
            if(!SGUuser.GuildPermissions.Has(GuildPermission.KickMembers))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Access Denied!",
                    "You must have permission to kick members.", Color.Red));
                return;
            }
            if (!(await Context.Guild.GetCurrentUserAsync(CacheMode.AllowDownload))
                .GuildPermissions.Has(GuildPermission.KickMembers))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("I'm Sorry, but I can't!",
                    "I must have permission to kick members.", Color.Red));
                return;
            }

            if (user.Id == _client.CurrentUser.Id)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't force me to kick myself...", Color.Red));
                return;
            }
            if (user == SGUuser)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't force me to kick you...", Color.Red));
                return;
            }
            if (user == await Context.Guild.GetOwnerAsync())
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't kick the server owner...", Color.Red));
                return;
            }
            #endregion

            try
            {
                await user.KickAsync(reason);
                EmbedBuilder b = new EmbedBuilder
                {
                    Title = $"Kick | Case #{_jservice.GetCaseCount(Context.Guild.Id)}",
                    Timestamp = DateTimeOffset.Now
                };
                b.WithColor(new Color(225, 192, 12));
                string ut = user.IsBot ? "Bot" : "User";
                b.AddField(ut, $"{user.Username}#{user.Discriminator} ({user.Mention})", true);
                b.AddField("Staff Responsible", $"{Context.User.Username}#{Context.User.Discriminator}", true);
                b.AddField("Reason", reason);
                await _jservice.SendModLog(Context.Guild.Id, b.Build());
                await ReplyAsync("", false, GetEmbeddedMessage($"Kicked {user.Username}#{user.Discriminator}", $"**Reason**: {reason}", new Color(225, 192, 12)));
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Critical Failure", "Server responded with a 403.", Color.DarkRed, ex));
                }
                else
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Critical Failure", $"{ex.Message}", Color.DarkRed, ex));

                }
            }
        }

        [Command("mute", RunMode = RunMode.Async)]
        public async Task mute(IGuildUser user, [Remainder]string reason = "being obnoxious")
        {
            #region ERRORS
            if (Context.Guild == null)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Not Supported",
                    "You cannot use this command here! Please make sure you're calling from a guild.", Color.Red));
                return;
            }
            SocketGuildUser SGUuser = Context.User as SocketGuildUser;
            if (!SGUuser.GuildPermissions.Has(GuildPermission.ManageRoles))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Access Denied!",
                    "You must have permission to manage roles.", Color.Red));
                return;
            }
            if (!(await Context.Guild.GetCurrentUserAsync(CacheMode.AllowDownload))
                .GuildPermissions.Has(GuildPermission.ManageRoles))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("I'm Sorry, but I can't!",
                    "I must have permission to manage roles.", Color.Red));
                return;
            }

            if (user.Id == _client.CurrentUser.Id)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't force me to mute myself...", Color.Red));
                return;
            }
            if (user == SGUuser)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't force me to mute you...", Color.Red));
                return;
            }
            if (user == await Context.Guild.GetOwnerAsync())
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't mute the server owner...", Color.Red));
                return;
            }
            #endregion

            try
            {
                ModLogBinding ml = TestModuleService.MLbindings.FirstOrDefault(x => x.GuildID == Context.Guild.Id);
                if (ml != null)
                {
                    await user.AddRoleAsync(Context.Guild.GetRole(ml.MuteRoleID));
                }
                EmbedBuilder b = new EmbedBuilder
                {
                    Title = $"Mute | Case #{_jservice.GetCaseCount(Context.Guild.Id)}",
                    Timestamp = DateTimeOffset.Now
                };
                b.WithColor(new Color(225, 192, 12));
                string ut = user.IsBot ? "Bot" : "User";
                b.AddField(ut, $"{user.Username}#{user.Discriminator} ({user.Mention})", true);
                b.AddField("Staff Responsible", $"{Context.User.Username}#{Context.User.Discriminator}", true);
                b.AddField("Reason", reason);
                await _jservice.SendModLog(Context.Guild.Id, b.Build());
                await ReplyAsync("", false, GetEmbeddedMessage($"Mute {user.Username}#{user.Discriminator}", $"**Reason**: {reason}", new Color(225, 192, 12)));
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Critical Failure", "Server responded with a 403.", Color.DarkRed, ex));
                }
                else
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Critical Failure", $"{ex.Message}", Color.DarkRed, ex));

                }
            }
        }

        [Command("unmute", RunMode = RunMode.Async)]
        public async Task unmute(IGuildUser user, [Remainder]string reason = "Time's up!")
        {
            #region ERRORS
            if (Context.Guild == null)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Not Supported",
                    "You cannot use this command here! Please make sure you're calling from a guild.", Color.Red));
                return;
            }
            SocketGuildUser SGUuser = Context.User as SocketGuildUser;
            if (!SGUuser.GuildPermissions.Has(GuildPermission.ManageRoles))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Access Denied!",
                    "You must have permission to manage roles.", Color.Red));
                return;
            }
            if (!(await Context.Guild.GetCurrentUserAsync(CacheMode.AllowDownload))
                .GuildPermissions.Has(GuildPermission.ManageRoles))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("I'm Sorry, but I can't!",
                    "I must have permission to manage roles.", Color.Red));
                return;
            }

            if (user.Id == _client.CurrentUser.Id)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't force me to unmute myself...", Color.Red));
                return;
            }
            if (user == SGUuser)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't force me to unmute you...", Color.Red));
                return;
            }
            if (user == await Context.Guild.GetOwnerAsync())
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't unmute the server owner...", Color.Red));
                return;
            }
            #endregion

            try
            {
                ModLogBinding ml = TestModuleService.MLbindings.FirstOrDefault(x => x.GuildID == Context.Guild.Id);
                if(ml!= null)
                {
                    await user.RemoveRoleAsync(Context.Guild.GetRole(ml.MuteRoleID));
                }
                EmbedBuilder b = new EmbedBuilder
                {
                    Title = $"Unmute",
                    Timestamp = DateTimeOffset.Now
                };
                b.WithColor(new Color(0, 192, 12));
                string ut = user.IsBot ? "Bot" : "User";
                b.AddField(ut, $"{user.Username}#{user.Discriminator} ({user.Mention})", true);
                b.AddField("Staff Responsible", $"{Context.User.Username}#{Context.User.Discriminator}", true);
                b.AddField("Reason", reason);
                await _jservice.SendModLog(Context.Guild.Id, b.Build());
                await ReplyAsync("", false, GetEmbeddedMessage($"Unmute {user.Username}#{user.Discriminator}", $"**Reason**: {reason}", new Color(0, 192, 12)));
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Critical Failure", "Server responded with a 403.", Color.DarkRed, ex));
                }
                else
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Critical Failure", $"{ex.Message}", Color.DarkRed, ex));

                }
            }
        }

        [Command("ban", RunMode = RunMode.Async)]
        public async Task Ban(IGuildUser user, [Remainder]string reason = "being a supreme ass")
        {

            #region ERRORS
            if (Context.Guild == null)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Not Supported",
                    "You cannot use this command here! Please make sure you're calling from a guild.", Color.Red));
                return;
            }
            SocketGuildUser SGUuser = Context.User as SocketGuildUser;
            if (!SGUuser.GuildPermissions.Has(GuildPermission.BanMembers))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Access Denied!",
                    "You must have permission to ban members.", Color.Red));
                return;
            }
            if (!(await Context.Guild.GetCurrentUserAsync(CacheMode.AllowDownload))
                .GuildPermissions.Has(GuildPermission.BanMembers))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("I'm Sorry, but I can't!",
                    "I must have permission to ban members.", Color.Red));
                return;
            }

            if (user.Id == _client.CurrentUser.Id)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't force me to ban myself...", Color.Red));
                return;
            }
            if (user == SGUuser)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't force me to ban you...", Color.Red));
                return;
            }
            if (user == await Context.Guild.GetOwnerAsync())
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't ban the server owner...", Color.Red));
                return;
            }
            #endregion

            try
            {
                await user.BanAsync(7,reason);
                EmbedBuilder b = new EmbedBuilder
                {
                    Title = $"Ban | Case #{_jservice.GetCaseCount(Context.Guild.Id)}",
                    Timestamp = DateTimeOffset.Now
                };
                b.WithColor(new Color(225, 18, 12));
                string ut = user.IsBot ? "Bot" : "User";
                b.AddField(ut, $"{user.Username}#{user.Discriminator} ({user.Mention})", true);
                b.AddField("Staff Responsible", $"{Context.User.Username}#{Context.User.Discriminator}", true);
                b.AddField("Reason", reason);
                await _jservice.SendModLog(Context.Guild.Id, b.Build());
                await ReplyAsync("", false, GetEmbeddedMessage($"Banned {user.Username}#{user.Discriminator}", $"**Reason**: {reason}", new Color(225, 18, 12)));
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Critical Failure", "Server responded with a 403.", Color.DarkRed, ex));
                }
                else
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Critical Failure", $"{ex.Message}", Color.DarkRed, ex));

                }
            }
        }

        [Command("unban", RunMode = RunMode.Async)]
        public async Task unBan(IGuildUser user, [Remainder]string reason = "Ban Successfully appealed")
        {

            #region ERRORS
            if (Context.Guild == null)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Not Supported",
                    "You cannot use this command here! Please make sure you're calling from a guild.", Color.Red));
                return;
            }
            SocketGuildUser SGUuser = Context.User as SocketGuildUser;
            if (!SGUuser.GuildPermissions.Has(GuildPermission.BanMembers))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Access Denied!",
                    "You must have permission to ban members.", Color.Red));
                return;
            }
            if (!(await Context.Guild.GetCurrentUserAsync(CacheMode.AllowDownload))
                .GuildPermissions.Has(GuildPermission.BanMembers))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("I'm Sorry, but I can't!",
                    "I must have permission to ban members.", Color.Red));
                return;
            }

            if (user.Id == _client.CurrentUser.Id)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't force me to ban myself...", Color.Red));
                return;
            }
            if (user == SGUuser)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't force me to ban you...", Color.Red));
                return;
            }
            if (user == await Context.Guild.GetOwnerAsync())
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't ban the server owner...", Color.Red));
                return;
            }
            #endregion

            try
            {
                await Context.Guild.RemoveBanAsync(user);
                EmbedBuilder b = new EmbedBuilder
                {
                    Title = $"Unban | Case #{_jservice.GetCaseCount(Context.Guild.Id)}",
                    Timestamp = DateTimeOffset.Now
                };
                b.WithColor(new Color(18, 225, 12));
                string ut = user.IsBot ? "Bot" : "User";
                b.AddField(ut, $"{user.Username}#{user.Discriminator} ({user.Mention})", true);
                b.AddField("Staff Responsible", $"{Context.User.Username}#{Context.User.Discriminator}", true);
                b.AddField("Reason", reason);
                await _jservice.SendModLog(Context.Guild.Id, b.Build());
                await ReplyAsync("", false, GetEmbeddedMessage($"unbanned {user.Username}#{user.Discriminator}", $"**Reason**: {reason}", new Color(225, 18, 12)));
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Critical Failure", "Server responded with a 403.", Color.DarkRed, ex));
                }
                else
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Critical Failure", $"{ex.Message}", Color.DarkRed, ex));

                }
            }
        }

        [Command("cpurge", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task Clear(int amount = 90)
        {
            var msgs = await Context.Channel.GetMessagesAsync(amount + 1, CacheMode.AllowDownload).FlattenAsync();
            var f = Context.Channel as ITextChannel;
            await f?.DeleteMessagesAsync(msgs);
            await Context.Channel.SendMessageAsync($"Purged.");
        }
        
        [Command("pollJoin", RunMode = RunMode.Async)]
        public async Task DoJoinCheck(ulong GuildID, ulong ChannelID, ulong RoleID = 0,[Remainder]string WelcomeMessage=null)
        {
            
            LogMessage ER3R = new LogMessage(LogSeverity.Info, "Greetings", "COMMAND CALLED");
            _writer.WriteEntry(ER3R);
            if (Context.User != Context.Client.CurrentUser)
            {
                await ReplyAsync(Context.User.Mention+": `This command is not intended to be run by users, only bot's Startup.CORE script.`");
                return;
            }
            //_jservice.WelcomeMessage = WelcomeMessage;
            GuildQueryItem item = new GuildQueryItem
            {
                DefaultChannel = (ITextChannel)_client.GetGuild(GuildID).GetChannel(ChannelID),
                RoleToAssign = _client.GetGuild(GuildID).GetRole(RoleID)
            };
            if (!string.IsNullOrWhiteSpace(WelcomeMessage))
            {
                LogMessage ERR = new LogMessage(LogSeverity.Info, "Greetings", "Initial Welcome Message: " + WelcomeMessage);
                _writer.WriteEntry(ERR);
                item.WelcomeMessage = WelcomeMessage;
            }
            else
            {
                LogMessage ERR = new LogMessage(LogSeverity.Info, "Greetings", "Using Default Welcome Message.");
                _writer.WriteEntry(ERR);
                item.WelcomeMessage = "Please Enjoy Your Stay.";
            }
            await _jservice.StartListening(Context, item);
            
        }

        [Command("bindDelete", RunMode = RunMode.Async), Summary("Creates a Reaction event for specified server & emote to act as a self-delete button")]
        public async Task StartDelReact(string emote, ulong guildID = 0)
        {
            
            LogMessage ER3R = new LogMessage(LogSeverity.Info, "Binding", "Reaction event binder called");
            _writer.WriteEntry(ER3R);
            //if (Context.User != Context.Client.CurrentUser)
            //{
            //    await ReplyAsync(Context.User.Mention + ": `This command is not intended to be run by users, only bot's Startup.CORE script.`");
            //    return;
            //}
            if (_permissions.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Restricted",
                    "This command is undergoing testing. Only administrators can use it. After testing, it will be restricted to `STARTUP.CORE`", Color.Red));
                return;
            }
            if (!Emote.TryParse(emote, out Emote ZS))
            {

                await ReplyAsync("", false, GetEmbeddedMessage("Invalid Emote",
                    "You must have a valid emote specified. Try using one of your server emotes.", Color.Red));
                return;
            }
            //_jservice.WelcomeMessage = WelcomeMessage;
            if(guildID == 0)
            {
                if(Context.Guild == null)
                {
                    await ReplyAsync("",false,GetEmbeddedMessage("Wait... That's Illegal.", "You gotta be in a guild for this", Color.Red));return;
                }
                else
                {
                    guildID = Context.Guild.Id;
                }
            }
            await _jservice.BindReaction(Context, ZS,guildID);

        }

        [Command("unbindDelete", RunMode = RunMode.Async), Summary("Deletes a Reaction event for specified server & emote to act as a self-delete button")]
        public async Task DelDelReact(ulong guildID = 0)
        {

            LogMessage ER3R = new LogMessage(LogSeverity.Info, "Binding", "Reaction event binder called");
            _writer.WriteEntry(ER3R);
            //if (Context.User != Context.Client.CurrentUser)
            //{
            //    await ReplyAsync(Context.User.Mention + ": `This command is not intended to be run by users, only bot's Startup.CORE script.`");
            //    return;
            //}
            if (_permissions.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Restricted",
                    "This command is undergoing testing. Only administrators can use it. After testing, it will be restricted to `STARTUP.CORE`", Color.Red));
                return;
            }
            //_jservice.WelcomeMessage = WelcomeMessage;
            if (guildID == 0)
            {
                if (Context.Guild == null)
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You gotta be in a guild for this", Color.Red)); return;
                }
                else
                {
                    guildID = Context.Guild.Id;
                }
            }
            await _jservice.UnBindReaction(Context, guildID);

        }

        [Command("ml-bind"), Remarks("AccessLevels.Normal"), 
            Summary("Creates a log channel binding. Requires user permission to 'Manage Channels'. "+
            "Call the command in the channel you want to use as moderator log.")]
        public async Task StartML(SocketRole MuteRole)
        {
            if (Context.Guild == null)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wrong Context!", "You can only use this command in a guild.", Color.Red));
                return;
            }
            if (Context.User is SocketGuildUser SGU)
            {
                if (!SGU.GuildPermissions.Has(GuildPermission.ManageChannels))
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("ACCESS DENIED!", "You lack permission. You must have the ability to manage channels.", Color.Red));
                    return;
                }
                if((await Context.Guild.GetCurrentUserAsync()) is SocketGuildUser bgu)
                {
                    int p = bgu.Roles.Max(x => x.Position);
                    if (p < MuteRole.Position)
                    {
                        await ReplyAsync("", false, GetEmbeddedMessage("ACCESS DENIED!",
                        $"You're trying to use a mute role that I don't have access to! Try moving the mute role below {bgu.Roles.FirstOrDefault(x=>x.Position == p).Mention}", Color.Red));

                        return;
                    }
                    
                    
                }
            }
            await _jservice.BindModLog(Context,MuteRole);
            
        }
        [Command("ml-unbind"),Remarks("AccessLevels.Normal"),
            Summary("Removes a log channel binding. Requires user permission to 'Manage Channels'. " +
            "Call the command in the current moderator log channel.")]
        public async Task stopML()
        {
            if(Context.Guild == null)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wrong Context!", "You can only use this command in a guild.", Color.Red));
                return;
            }
            if(Context.User is SocketGuildUser SGU)
            {
                if(!SGU.GuildPermissions.Has(GuildPermission.ManageChannels))
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("ACCESS DENIED!", "You lack permission. You must have the ability to manage channels.", Color.Red));
                    return;
                }
            }
            await _jservice.UnBindModLog(Context);
            
        }

        #region Messages
        public Embed GetEmbeddedMessage(string title, string message, Color color, Exception e = null)
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(color);
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithTitle(title);
            b.WithDescription(message);
            b.WithFooter("ModularBOT • TestModule");
            if (e != null)
            {
                b.AddField("Extended Details", e.Message);
                b.AddField("For developer", "See the Errors.LOG for more info!!!");
                _writer.WriteErrorsLog(e);
            }
            return b.Build();
        }
        #endregion
    }

    public class TestModuleService
    {
        DiscordShardedClient ShardedClient { get; set; }
        ConsoleIO Writer { get; set; }

        PermissionManager PermissionsManager { get; set; }

        ConfigurationManager CfgMgr { get; set; }

        #region Configuration Bindings

        string ModLogBindingsConfig = "Modules/TestModule/mod-log.json";
        string TrashcanBindingsConfig = "Modules/TestModule/trash-can.json";

        #endregion
        
        static bool doonce = false;

        [DontInject]
        public static Dictionary<ulong, GuildQueryItem> BoundItems { get; set; }//This contains <guildID,role> value pairs to check user's join event.
        
        [DontInject]
        public static Dictionary<ulong,string> Trashcans { get; set; }
        
        public static List<ModLogBinding> MLbindings = new List<ModLogBinding>();
        

        public TestModuleService(DiscordShardedClient _client, ConsoleIO _consoleIO, PermissionManager _permissions, ConfigurationManager _cfgMgr)
        {
            PermissionsManager = _permissions;
            CfgMgr = _cfgMgr;
            ShardedClient = _client;
            Writer = _consoleIO;
            LogMessage constructorLOG = new LogMessage(LogSeverity.Critical, "TMS_Main", "TestModuleService constructor called.");
            Writer.WriteEntry(constructorLOG);
            if (doonce)
            {
                _consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "TMS_Main", "TestModuleService Constructor called again!!!! Why Tho???"));

            }
            if (!doonce)
            {
                
                if (ShardedClient == null)
                {
                    LogMessage ERR = new LogMessage(LogSeverity.Critical, "TMS_Main", "Client is null! You should be ashamed.");
                    _consoleIO.WriteEntry(ERR);
                }
                if (!Directory.Exists(Path.GetDirectoryName(ModLogBindingsConfig)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ModLogBindingsConfig));
                    MLbindings = new List<ModLogBinding>();
                    using (StreamWriter sw = new StreamWriter(ModLogBindingsConfig))
                    {
                        sw.WriteLine(JsonConvert.SerializeObject(MLbindings, Formatting.Indented));
                    }
                }
                else
                {
                    if (!File.Exists(ModLogBindingsConfig))
                    {
                        var fs = File.Create(ModLogBindingsConfig);
                        fs.WriteByte(0);
                        fs.Close();
                    }
                }

                if (!Directory.Exists(Path.GetDirectoryName(TrashcanBindingsConfig)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(TrashcanBindingsConfig));
                    Trashcans = new Dictionary<ulong, string>();
                    using (StreamWriter sw = new StreamWriter(TrashcanBindingsConfig))
                    {
                        sw.WriteLine(JsonConvert.SerializeObject(Trashcans, Formatting.Indented));
                    }
                }
                else
                {
                    if (!File.Exists(TrashcanBindingsConfig))
                    {
                        var fs = File.Create(TrashcanBindingsConfig);
                        fs.WriteByte(0);
                        fs.Close();
                    }
                }

                using (StreamReader sr = new StreamReader(ModLogBindingsConfig))
                {
                    MLbindings = JsonConvert.DeserializeObject<List<ModLogBinding>>(sr.ReadToEnd());
                }

                using (StreamReader sr = new StreamReader(TrashcanBindingsConfig))
                {
                    Trashcans = JsonConvert.DeserializeObject<Dictionary<ulong, string>>(sr.ReadToEnd());
                }

                if (MLbindings == null)
                {
                    MLbindings = new List<ModLogBinding>();
                }

                if (Trashcans == null)
                {
                    Trashcans = new Dictionary<ulong, string>();
                }

                BoundItems = new Dictionary<ulong, GuildQueryItem>();
                ShardedClient.UserJoined += ShardedClient_UserJoined;
                ShardedClient.ReactionAdded += Dsc_ReactionAdded;
                ShardedClient.UserBanned += ShardedClient_UserBanned;
                ShardedClient.UserUnbanned += ShardedClient_UserUnbanned;
                LogMessage Log1 = new LogMessage(LogSeverity.Info, "Reactions", "Added ReactionAdded event handler to client.");
                LogMessage Log2 = new LogMessage(LogSeverity.Info, "Greetings", "Added UserJoin event handler to client.");
                LogMessage Log4 = new LogMessage(LogSeverity.Info, "ModLogs", "Added UserBanned event handler to client.");
                LogMessage Log5 = new LogMessage(LogSeverity.Info, "ModLogs", "Added UserUnbanned event handler to client.");
                _consoleIO.WriteEntry(Log1);
                _consoleIO.WriteEntry(Log2);
                _consoleIO.WriteEntry(Log4);
                _consoleIO.WriteEntry(Log5);



                _consoleIO.WriteEntry(new LogMessage(LogSeverity.Info, "TMS_Config", "Adding entities to the configuration manager"));
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.TrashCanEmote());
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.ModLogChannel());
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.WelcomeChannel());
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.WelcomeMessage());
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.WelcomeRole());
                _consoleIO.WriteEntry(new LogMessage(LogSeverity.Info, "TMS_Config", "Success!! Config entities registered."));
                doonce = true;
            }


        }

        private async Task ShardedClient_UserUnbanned(SocketUser arg1, SocketGuild arg2)
        {

            if (!arg2.CurrentUser.GuildPermissions.Has(GuildPermission.BanMembers | GuildPermission.ViewAuditLog))
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "TMS_Events", $"User: {arg1.Username}#{arg1.Discriminator} was unbanned from guild: {arg2.Name}."));
                Writer.WriteEntry(new LogMessage(LogSeverity.Warning, "TMS_Events", $"unban event detected, but I don't have permission to get the details."));
                return;
            }
            Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "TMS_Events", $"User: {arg1.Username}#{arg1.Discriminator} was unbanned from: {arg2.Name}."));
            EmbedBuilder b = new EmbedBuilder
            {
                Title = $"Unban",
                Timestamp = DateTimeOffset.Now
            };
            b.WithColor(new Color(18, 225, 12));
            string ut = arg1.IsBot ? "Bot" : "User";
            b.AddField(ut, $"{arg1.Username}#{arg1.Discriminator} ({arg1.Mention})", true);
            //b.AddField("Staff Responsible", $"{rb.}", true);
            var audits = await arg2.GetAuditLogsAsync(1).Flatten().ToList();
            var u = audits.FirstOrDefault(x => x.Action == ActionType.Unban)?.User;
            var r = audits.FirstOrDefault(x => x.Action == ActionType.Unban)?.Reason;
            if (u != null)
            {
                b.AddField("Staff Responsible", $"{u.Username}#{u.Discriminator}",true);
            }
            b.AddField("Reason", r != null ? r : "Ban successfully appealed");
            if (u.Id != ShardedClient.CurrentUser.Id)
            {
                await SendModLog(arg2.Id, b.Build());
            }
        }


        private async Task ShardedClient_UserBanned(SocketUser arg1, SocketGuild arg2)
        {
            if(!ModLogBound(arg2.Id))
            {
                return;
            }
            if(!arg2.CurrentUser.GuildPermissions.Has(GuildPermission.BanMembers | GuildPermission.ViewAuditLog))
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "TMS_Events", $"User: {arg1.Username}#{arg1.Discriminator} was banned from guild: {arg2.Name}."));
                Writer.WriteEntry(new LogMessage(LogSeverity.Warning, "TMS_Events", $"Ban event detected, but I don't have permission to get the details."));
                return;
            }
            Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "TMS_Events", $"User: {arg1.Username}#{arg1.Discriminator} was banned from guild: {arg2.Name}."));
            Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "TMS_Events", $"BAN Reason: {arg2.GetBanAsync(arg1).GetAwaiter().GetResult().Reason}"));
            var rb = await arg2.GetBanAsync(arg1);

            EmbedBuilder b = new EmbedBuilder
            {
                Title = $"Ban | Case #{GetCaseCount(arg2.Id)}",
                Timestamp = DateTimeOffset.Now
            };
            b.WithColor(new Color(225, 18, 12));
            string ut = arg1.IsBot ? "Bot" : "User";
            b.AddField(ut, $"{arg1.Username}#{arg1.Discriminator} ({arg1.Mention})", true);
            //b.AddField("Staff Responsible", $"{rb.}", true);
            var audits =  await arg2.GetAuditLogsAsync(1).Flatten().ToList();
            var u = audits.FirstOrDefault(x => x.Action == ActionType.Ban)?.User;
            if(u != null)
            {
                b.AddField("Staff Responsible", $"{u.Username}#{u.Discriminator}",true);
            }
            b.AddField("Reason", rb.Reason);
            if(u.Id != ShardedClient.CurrentUser.Id)
            {
                await SendModLog(arg2.Id, b.Build());
            }


        }

        private async Task ShardedClient_UserJoined(SocketGuildUser arg)
        {
            LogMessage Log = new LogMessage(LogSeverity.Verbose, "Greetings", "A wild user appears!");
            Writer.WriteEntry(Log);
            bool result = BoundItems.TryGetValue(arg.Guild.Id, out GuildQueryItem item);
            if (result)
            {
                Log = new LogMessage(LogSeverity.Verbose, "Greetings", $"What is the default channel? {item.DefaultChannel.Name}");//debuglul
                Writer.WriteEntry(Log);
                await item.DefaultChannel.SendMessageAsync($"Hello `{arg.Username}#{arg.Discriminator}`, Welcome to `{arg.Guild.Name}`! {item.WelcomeMessage}");
                if (item.RoleToAssign == null)
                {
                    Log = new LogMessage(LogSeverity.Warning, "Greetings", $"A role was not specified. Playing welcome message only.");//debuglul
                }
                if (item.RoleToAssign != null)
                {
                    Log = new LogMessage(LogSeverity.Info, "Greetings", $"A role was specified, let's assign to user. ROLE: {item.RoleToAssign.Name}<{item.RoleToAssign.Id}>");//debuglul
                    await arg.AddRoleAsync(item.RoleToAssign); //assign le role
                }
                Log = new LogMessage(LogSeverity.Info, "Greetings", "The GuildUser uses JoinEvent... It's SUPER EFFECTIVE!");//debuglul
                Writer.WriteEntry(Log);
                Log = new LogMessage(LogSeverity.Info, "Greetings", "The GuildUser: " + arg.Username + "\r\n" + "The Guild: " + arg.Guild.Name);//debuglul
                Writer.WriteEntry(Log);
            }
            else
            {
                Log = new LogMessage(LogSeverity.Verbose, "Greetings", "The GuildUser uses JoinEvent... It's Not very effective...");//debuglul
                Writer.WriteEntry(Log);
                Log = new LogMessage(LogSeverity.Verbose, "Greetings", "The GuildUser: " + arg.Username + "\r\n" + "The Guild: " + arg.Guild.Name);//debuglul
                Writer.WriteEntry(Log);
            }
        }

        public Task StartListening(ICommandContext Context, GuildQueryItem roletoadd)
        {
            
            BoundItems.Add(roletoadd.DefaultChannel.GuildId, roletoadd);
            var Log = new LogMessage(LogSeverity.Info, "Greetings", $"Created a Join Event listener using the channel `{roletoadd.DefaultChannel.Name}` located in guild `{roletoadd.DefaultChannel.Guild.Name}`. Using Welcome message: {roletoadd.WelcomeMessage}");//debuglul
            Writer.WriteEntry(Log);
            return Task.Delay(0);
        }

        public async Task BindReaction(ICommandContext Context, Emote emote, ulong GuildID)
        {
            if (Trashcans == null)
            {
                await Context.Channel.SendMessageAsync("DEB: New trashcan config generated");
                Trashcans = new Dictionary<ulong, string>();
            }
            if (Trashcans.ContainsKey(GuildID))
            {
                Trashcans[GuildID] = emote.ToString();
                using (StreamWriter SW = new StreamWriter(TrashcanBindingsConfig))
                {
                    Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction", "Saving Config..."));
                    await SW.WriteLineAsync(JsonConvert.SerializeObject(Trashcans));
                }
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(Context, $"UPDATED: Now Listening for {emote.ToString()} Reactions",
                $"Any of **MY** messages in `{ShardedClient.GetGuild(GuildID).Name}` that receive the reaction {emote.ToString()} will be deleted.", Color.Green));
                return;
            }
            else
            {
               
                Trashcans.Add(GuildID, emote.ToString());
                using (StreamWriter SW = new StreamWriter(TrashcanBindingsConfig))
                {
                    Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction", "Saving Config..."));
                    await SW.WriteLineAsync(JsonConvert.SerializeObject(Trashcans));
                }
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(Context, $"ADDED: Now Listening for {emote.ToString()} Reactions",
                $"Any of **my** messages in `{ShardedClient.GetGuild(GuildID).Name}` that receive the reaction {emote.ToString()} will be deleted.", Color.Green));
                return;
            }
            
            
        }
        public async Task UnBindReaction(ICommandContext Context, ulong GuildID)
        {
            if (Trashcans.ContainsKey(GuildID))
            {
                Emote S = Emote.Parse(Trashcans[GuildID]);
                Trashcans.Remove(GuildID);
                using (StreamWriter SW = new StreamWriter(TrashcanBindingsConfig))
                {
                    Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction", "Saving Config..."));
                    await SW.WriteLineAsync(JsonConvert.SerializeObject(Trashcans));
                }
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(Context, $"Removed: {S.ToString()} Reaction Event",
                $"Messages will no longer be deleted with {S.ToString()}", Color.LightOrange));
                
                return;
            }
            else
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(Context, $"Hmmm... ???",
                $"There was no binding found here. Are you in the right place?", Color.Red));
                return;
            }


        }

        private async Task Dsc_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction", "REACTION FOUND!!!!!!!!!!"));
            SocketTextChannel STC = null;
            if ((arg2 is SocketTextChannel))
            {
                STC = arg2 as SocketTextChannel;
            }
            else
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction", "Channel wasn't a guild channel... RIP"));
                return;
            }

            if(!Trashcans.TryGetValue(STC.Guild.Id,out string Reac))
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction", "Guild not bound... RIP"));
                return;
            }

            if ((await arg1.GetOrDownloadAsync()).Author.Id != ShardedClient.CurrentUser.Id && arg3.Emote.ToString() == Reac)
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction", "Someone reacted to a message with the trigger, but it wasn't mine."));
                return;
            }
            else
            {
                
                if(arg3.Emote.ToString() == Reac.ToString())
                {
                    if (arg3.User.Value is SocketGuildUser SGU)
                    {
                        AccessLevels L = PermissionsManager.GetAccessLevel(SGU);
                        if (L < AccessLevels.Administrator)
                        {
                            if (!SGU.GuildPermissions.Has(GuildPermission.ManageMessages))
                            {
                                Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction",
                                    "Someone reacted to a message with the trigger, but they didn't have the proper permissions..."));
                                return;
                            }
                        }
                    }
                    Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction", "Someone reacted to my message with the trigger. let's delete it"));

                    await (await arg1.GetOrDownloadAsync()).DeleteAsync();
                }
            }
        }

        public async Task SendModLog(ulong GuildID, Embed embed)
        {
            ModLogBinding mb = MLbindings.FirstOrDefault(x => x.GuildID == GuildID);
            if(mb != null)
            {
                if (ShardedClient.GetGuild(GuildID).GetChannel(mb.ChannelID) is SocketTextChannel sfl)
                {
                    await sfl.SendMessageAsync("", false, embed);
                }
            }
        }

        public bool ModLogBound(ulong guildID)
        {
            ModLogBinding mb = MLbindings.FirstOrDefault(x => x.GuildID == guildID);
            return mb != null;
        }

        public ulong GetCaseCount(ulong guildID)
        {
            ModLogBinding mb = MLbindings.FirstOrDefault(x => x.GuildID == guildID);
            
            if(mb!= null)
            {
                mb.CaseCount++;
                using (StreamWriter sw = new StreamWriter(ModLogBindingsConfig))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(MLbindings, Formatting.Indented));
                }
                return mb.CaseCount;
            }
            else
            {
                return 0;
            }
        }

        public async Task BindModLog(ICommandContext context, IRole role)
        {
            var z = await context.Guild.GetCurrentUserAsync(CacheMode.AllowDownload);
            if (!z.GuildPermissions.Has(GuildPermission.KickMembers | GuildPermission.BanMembers | GuildPermission.ViewAuditLog))
            {
                string hasKick = !z.GuildPermissions.Has(GuildPermission.KickMembers) ? "• `KICK MEMBERS`\r\n" : "";
                string hasBan = !z.GuildPermissions.Has(GuildPermission.BanMembers) ? "• `BAN MEMBERS`\r\n" : "";
                string hasAudit = !z.GuildPermissions.Has(GuildPermission.ViewAuditLog) ? "• `VIEW AUDIT LOG`" : "";
                await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Nope!",
               $"In order to set this channel up for ModLog, you must give me the following permissions:\r\n\r\n" +
               $"{hasKick}{hasBan}{hasAudit}", Color.Red));
                return;
            }
            if(MLbindings.FirstOrDefault(x=>x.GuildID == context.Guild.Id) != null)
            {
                await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Nope!",
                $"You can only have one moderation log per guild.", Color.Red));
                return;
            }

            ModLogBinding ml = new ModLogBinding();
            ml.GuildID = context.Guild.Id;
            ml.ChannelID = context.Channel.Id;
            ml.MuteRoleID = role.Id;
            MLbindings.Add(ml);
            using (StreamWriter sw = new StreamWriter(ModLogBindingsConfig))
            {
                sw.WriteLine(JsonConvert.SerializeObject(MLbindings, Formatting.Indented));
            }
            await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Success!",
                $"You've established <#{context.Channel.Id}> as a Moderation Log.\r\n\r\nServer Mute role is currently {role.Mention}.", Color.DarkGreen));
        }

        public async Task UnBindModLog(ICommandContext context)
        {
            
            ModLogBinding mlr = MLbindings.FirstOrDefault(x => x.ChannelID == context.Channel.Id);
            if (mlr == null)
            {
                await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Nope!",
                $"This channel isn't a moderation log. Please make sure you're in the right channel!", Color.Red));
                return;
            }
            if (mlr != null)
            {
                MLbindings.Remove(mlr);
            }
            using (StreamWriter sw = new StreamWriter(ModLogBindingsConfig))
            {
                sw.WriteLine(JsonConvert.SerializeObject(MLbindings, Formatting.Indented));
            }

            await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Moderation Log Removed",
            $"This channel will not receive moderation logs anymore.", new Color(244, 168, 0)));
        }
        
        #region Messages
        public Embed GetEmbeddedMessage(ICommandContext Context, string title, string message, Color color, Exception e = null)
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(color);
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithTitle(title);
            b.WithDescription(message);
            b.WithFooter("ModularBOT • TestModuleService");
            if (e != null)
            {
                b.AddField("Extended Details", e.Message);
                b.AddField("For developer", "See the Errors.LOG for more info!!!");
                Writer.WriteErrorsLog(e);
            }
            return b.Build();
        }
        #endregion
    }

    public class GuildQueryItem
    {
        public ITextChannel DefaultChannel { get; set; }
        public IRole RoleToAssign { get; set; }
        public string WelcomeMessage { get; set; }
    }

    public class ModLogBinding
    {
        public ulong GuildID { get; set; }
        public ulong ChannelID { get; set; }
        public ulong CaseCount { get; set; }
        public ulong MuteRoleID { get; set; }
    }
}
