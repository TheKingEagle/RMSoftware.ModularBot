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
        #region PUBLIC INJECTED COMPONENTS

        public DiscordShardedClient _client { get; set; }
        public ConsoleIO _writer { get; set; }
        public TestModuleService _jservice { get; set; }

        public PermissionManager _permissions { get; set; }

        public ConfigurationManager _configmgr { get; set; }

        #endregion PUBLIC INJECTED COMPONENTS

        public TestModule(DiscordShardedClient discord, TestModuleService joinservice, 
            ConsoleIO writer, PermissionManager manager, ConfigurationManager cnfgmgr)
        {
            _client = discord;
            _jservice = joinservice;
            _writer = writer;
            _permissions = manager;
            _configmgr = cnfgmgr;
            _writer.WriteEntry(new LogMessage(LogSeverity.Critical, "TestMOD", "Constructor called!!!!!!!!!"));
        }

        #region PUBLIC COMMANDS

        #region MODERATION COMMANDS

        [Command("Kick", RunMode = RunMode.Async)]
        public async Task Kick(IGuildUser user, [Remainder]string reason = "being an ass")
        {
            #region ERRORS
            if (Context.Guild == null)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Not Supported",
                    "You cannot use this command here! Please make sure you're calling from a guild.", Color.Red));
                return;
            }
            SocketGuildUser SGUuser = user as SocketGuildUser;
            SocketGuildUser SGUInvoker = Context.User as SocketGuildUser;
            if (!SGUInvoker.GuildPermissions.Has(GuildPermission.KickMembers))
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
            if (user == SGUInvoker)
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

            if (SGUuser.Hierarchy >= SGUInvoker.Hierarchy)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Access Denied!",
                    "You can't kick this user.", Color.Red));
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
                if (ml != null)
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
            SocketGuildUser SGUuser = user as SocketGuildUser;
            SocketGuildUser SGUInvoker = Context.User as SocketGuildUser;

            if (!SGUInvoker.GuildPermissions.Has(GuildPermission.BanMembers))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Access Denied!",
                    "You must have permission to ban members.", Color.Red));
                return;
            }
            if (!(await Context.Guild.GetCurrentUserAsync(CacheMode.AllowDownload))
                .GuildPermissions.Has(GuildPermission.BanMembers))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Access Denied!",
                    "I must have permission to ban members.", Color.Red));
                return;
            }

            if (user.Id == _client.CurrentUser.Id)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Wait... That's illegal...",
                    "You can't force me to ban myself...", Color.Red));
                return;
            }
            if (SGUuser == SGUInvoker)
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

            if (SGUuser.Hierarchy >= SGUInvoker.Hierarchy)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Access Denied!",
                    "You can't ban this user.", Color.Red));
                return;
            }
            #endregion

            try
            {
                await user.BanAsync(7, reason);
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

        [Command("cpurge", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task PurgeMessages(int amount = 90)
        {
            try
            {
                var msgs = await Context.Channel.GetMessagesAsync(amount + 1, CacheMode.AllowDownload).FlattenAsync();
                var f = Context.Channel as ITextChannel;
                await f?.DeleteMessagesAsync(msgs);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("",false,GetEmbeddedMessage("Purge Error",ex.Message,Color.DarkRed,ex));
            }
        }

        #endregion MODERATION COMMANDS

        #region BINDING COMMANDS

        [Command("pollJoin", RunMode = RunMode.Async), Alias("pollJoin","bindWelcomeMessage","bw"),Remarks("AccessLevels.BotOnly")]
        public async Task BindWelcomeMessage(ulong GuildID, ulong ChannelID, ulong RoleID = 0, [Remainder]string WelcomeMessage = null)
        {

            LogMessage ER3R = new LogMessage(LogSeverity.Info, "Greetings", "COMMAND CALLED");
            _writer.WriteEntry(ER3R);
            if (Context.User != Context.Client.CurrentUser)
            {
                await ReplyAsync("",false,GetEmbeddedMessage("Invalid Action","This command is designed to be called from OnStart.CORE.",Color.DarkRed));
                return;
            }
            
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
            await _jservice.BindWelcomeMessage(Context, item);

        }

        [Command("bindTrashcan", RunMode = RunMode.Async), Summary("Creates a Reaction event for specified " +
            "server & emote to act as a self-delete button"),Alias("bt", "binddelete", "bindtrash","mktrash"), Remarks("AccessLevels.GuildPermission")]
        public async Task BindTrashcan(string emote, ulong guildID = 0)
        {

            LogMessage ER3R = new LogMessage(LogSeverity.Info, "Binding", "Reaction event binder called");
            _writer.WriteEntry(ER3R);
            //if (Context.User != Context.Client.CurrentUser)
            //{
            //    await ReplyAsync(Context.User.Mention + ": `This command is not intended to be run by users, only bot's Startup.CORE script.`");
            //    return;
            //}
            if (_permissions.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Restricted",
                    "Only `CommandManagers` can use this command.", Color.Red));
                return;
            }
            if (!Emote.TryParse(emote, out Emote ZS))
            {

                await ReplyAsync("", false, GetEmbeddedMessage("Invalid Emote",
                    "You must have a valid emote specified. Try using one of your server emotes.", Color.Red));
                return;
            }
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
            await _jservice.BindTrashcanReaction(Context, ZS, guildID);

        }

        [Command("unbindTrashcan", RunMode = RunMode.Async), Summary("Deletes a Reaction event for specified server " +
            "& emote to act as a self-delete button"), Alias("ubt","unbinddelete","deltrash"), Remarks("AccessLevels.GuildPermission")]
        public async Task UnbindTrashcan(ulong guildID = 0)
        {

            LogMessage ER3R = new LogMessage(LogSeverity.Info, "Binding", "Reaction event unbinder called");
            _writer.WriteEntry(ER3R);
            if (_permissions.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Restricted",
                    "Only `CommandManagers` can use this command.", Color.Red));
                return;
            }
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
            await _jservice.UnBindTrashcanReaction(Context, guildID);

        }

        [Command("bindmodlog"),
            Summary("Creates a log channel binding. Requires user permission to 'Manage Channels'. " +
            "Call the command in the channel you want to use as moderator log."), Alias("ml-bind", "mlb", "bml"), Remarks("AccessLevels.GuildPermission")]
        public async Task BindModLog(SocketRole MuteRole)
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
                if ((await Context.Guild.GetCurrentUserAsync()) is SocketGuildUser bgu)
                {
                    int p = bgu.Roles.Max(x => x.Position);
                    if (p < MuteRole.Position)
                    {
                        await ReplyAsync("", false, GetEmbeddedMessage("ACCESS DENIED!",
                        $"You're trying to use a mute role that I don't have access to! Try moving the mute role below {bgu.Roles.FirstOrDefault(x => x.Position == p).Mention}", Color.Red));

                        return;
                    }


                }
            }
            await _jservice.BindModLog(Context, MuteRole);

        }

        [Command("unbindmodlog"),
            Summary("Removes a log channel binding. Requires user permission to 'Manage Channels'. " +
            "Call the command in the current moderator log channel."), Alias("ml-unbind", "mlu", "uml"), Remarks("AccessLevels.GuildPermission")]
        public async Task UnbindModLog()
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
            }
            await _jservice.UnBindModLog(Context);

        }
        #endregion BINDING COMMANDS

        #region MISC COMMANDS

        [Command("tpmgr"), Remarks("AccessLevels.NotSpecified")]
        public async Task Showtest()
        {
            if (_permissions.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await ReplyAsync("", false, _permissions.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            await ReplyAsync("You have the correct access level!");
        }

        [Command("sbstars"), Remarks("AccessLevels.NotSpecified")]
        public async Task StarTest()
        {
            await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Starboard Unicode testing", "`\\u2B50` should be \u2B50 and `\\uD83C\\uDF1F` should be \uD83C\uDF1F.", Color.Purple));
        }

        [Command("reason", RunMode = RunMode.Async), Summary("Modify MODLOG reason for a case id.")]
        public async Task ModifyReason(ulong caseID, [Remainder]string REASON)
        {
            Tuple<bool, string> StatusCode = await _jservice.ModLogEditReason(Context.Guild.Id, caseID, REASON);
            if (!StatusCode.Item1)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Reason Edit Failed", StatusCode.Item2, Color.DarkRed));
            }
            if (StatusCode.Item1)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Reason Edited", StatusCode.Item2, Color.Green));
            }
        }

        [Group("TestModule")]
        public class _TestModule:ModuleBase
        {
            [Command("about")]
            public async Task DisplayAbout()
            {
                EmbedBuilder eb = new EmbedBuilder()
                {
                    Title = "About TestModule",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl(ImageFormat.Auto),
                        Text = $"Requested By: {Context.User.Username} • TestModuleService"
                    },
                    Author = new EmbedAuthorBuilder()
                    {
                        IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto),
                        Name = $"{Context.Client.CurrentUser.Username}#{Context.Client.CurrentUser.Discriminator}"
                    },
                    Description = "This is a module designed for ModularBOT. Implementing Starboard, Welcome Messages, and a moderation suite.",
                    Color = Color.DarkBlue
                };
                await ReplyAsync("", false, eb.Build());
            }
        }
        

        #endregion MISC COMMANDS

        #endregion PUBLIC COMMANDS

        #region EMBED MESSAGES
        public Embed GetEmbeddedMessage(string title, string message, Color color, Exception e = null)
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(color);
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithTitle(title);
            b.WithDescription(message);
            b.WithFooter($"{Context.Client.CurrentUser.Username} • TestModule");
            if (e != null)
            {
                b.AddField("Extended Details", e.Message);
                b.AddField("For developer", "See the Errors.LOG for more info!!!");
                _writer.WriteErrorsLog(e);
            }
            return b.Build();
        }

        #endregion EMBED MESSAGES
    }

    public class TestModuleService
    {
        #region PRIVATE COMPONENTS

        private DiscordShardedClient ShardedClient { get; set; }
        private static ConsoleIO Writer { get; set; }
        
        private PermissionManager PermissionsManager { get; set; }
        private ConfigurationManager CfgMgr { get; set; }

        #endregion PRIVATE COMPONENTS

        #region PRIVATE FIELDS

        private readonly string ModLogBindingsConfig = "Modules/TestModule/mod-log.json";
        private readonly string TrashcanBindingsConfig = "Modules/TestModule/trash-can.json";
        private static readonly string StarboardBindingsConfig = "Modules/TestModule/starboard.json";
        private static bool doonce = false;

        #endregion PRIVATE FIELDS
        
        #region PUBLIC PROPERTIES

        [DontInject]
        public static Dictionary<ulong, GuildQueryItem> BoundItems { get; set; }//This contains <guildID,role> value pairs to check user's join event.

        [DontInject]
        public static Dictionary<ulong, string> Trashcans { get; set; }

        [DontInject]
        public static List<ModLogBinding> MLbindings { get; set;} =  new List<ModLogBinding>();

        [DontInject]
        public static Dictionary<Tuple<ulong, ulong>, ulong> MessageCaseIDs { get; set; } = new Dictionary<Tuple<ulong, ulong>, ulong>();

        [DontInject]
        public static Dictionary<ulong, StarboardBinding> SBBindings { get; set; } = new Dictionary<ulong, StarboardBinding>();

        #endregion PUBLIC PROPERTIES

        public TestModuleService(DiscordShardedClient _client, ConsoleIO _consoleIO, 
            PermissionManager _permissions, ConfigurationManager _cfgMgr)
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
                if (!Directory.Exists(Path.GetDirectoryName(StarboardBindingsConfig)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(StarboardBindingsConfig));
                    SBBindings = new Dictionary<ulong, StarboardBinding>();
                    using (StreamWriter sw = new StreamWriter(StarboardBindingsConfig))
                    {
                        sw.WriteLine(JsonConvert.SerializeObject(SBBindings, Formatting.Indented));
                    }
                }
                else
                {
                    if (!File.Exists(StarboardBindingsConfig))
                    {
                        var fs = File.Create(StarboardBindingsConfig);
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
                using (StreamReader sr = new StreamReader(StarboardBindingsConfig))
                {
                    SBBindings = JsonConvert.DeserializeObject<Dictionary<ulong, StarboardBinding>>(sr.ReadToEnd());
                }

                if (MLbindings == null)
                {
                    MLbindings = new List<ModLogBinding>();
                }

                if (Trashcans == null)
                {
                    Trashcans = new Dictionary<ulong, string>();
                }

                if (SBBindings == null)
                {
                    SBBindings = new Dictionary<ulong, StarboardBinding>();
                }

                BoundItems = new Dictionary<ulong, GuildQueryItem>();
                ShardedClient.UserJoined += ShardedClient_UserJoined;
                ShardedClient.ReactionAdded += ShardedClient_ReactionAdded;
                ShardedClient.ReactionRemoved += ShardedClient_ReactionRemoved;
                ShardedClient.UserBanned += ShardedClient_UserBanned;
                ShardedClient.UserUnbanned += ShardedClient_UserUnbanned;
                LogMessage Log1 = new LogMessage(LogSeverity.Info, "Reactions", "Added ReactionAdded event handler to client.");
                LogMessage Log3 = new LogMessage(LogSeverity.Info, "Reactions", "Added ReactionRemoved event handler to client.");
                LogMessage Log2 = new LogMessage(LogSeverity.Info, "Greetings", "Added UserJoin event handler to client.");
                LogMessage Log4 = new LogMessage(LogSeverity.Info, "ModLogs", "Added UserBanned event handler to client.");
                LogMessage Log5 = new LogMessage(LogSeverity.Info, "ModLogs", "Added UserUnbanned event handler to client.");
                _consoleIO.WriteEntry(Log1);
                _consoleIO.WriteEntry(Log3);
                _consoleIO.WriteEntry(Log2);
                _consoleIO.WriteEntry(Log4);
                _consoleIO.WriteEntry(Log5);



                _consoleIO.WriteEntry(new LogMessage(LogSeverity.Info, "TMS_Config", "Adding entities to the configuration manager"));
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.TrashCanEmote());
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.ModLogChannel());
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.ServerMuteRole());
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.WelcomeChannel());
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.WelcomeMessage());
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.WelcomeRole());
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.StarboardChannel());
                _cfgMgr.RegisterGuildConfigEntity(new ConfigEntities.StarboardAliasMode());
                _consoleIO.WriteEntry(new LogMessage(LogSeverity.Info, "TMS_Config", "Success!! Config entities registered."));
                doonce = true;
            }
        }

        #region PRIVATE EVENTS

        private async Task ShardedClient_UserUnbanned(SocketUser arg1, SocketGuild arg2)
        {
            if (!ModLogBound(arg2.Id))
            {
                return;
            }
            if (!arg2.CurrentUser.GuildPermissions.Has(GuildPermission.BanMembers | GuildPermission.ViewAuditLog))
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "TMS_Events", $"User: {arg1.Username}#{arg1.Discriminator} was unbanned from guild: {arg2.Name}."));
                Writer.WriteEntry(new LogMessage(LogSeverity.Warning, "TMS_Events", $"unban event detected, but I don't have permission to get the details. Guild: {arg2.Name}"));
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

            ulong caseid = GetCaseCount(arg2.Id);
            EmbedBuilder b = new EmbedBuilder
            {
                Title = $"Ban | Case #{caseid}",
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
            string BR = rb.Reason;
            if(string.IsNullOrWhiteSpace(BR))
            {
                BR = $"Ban reason not specified! Please execute the `reason {caseid} <Reason>` command.";
            }
            b.AddField("Reason", BR);
            if(u.Id != ShardedClient.CurrentUser.Id)
            {
                RestUserMessage r = await SendModLog(arg2.Id, b.Build());
                MessageCaseIDs.Add(Tuple.Create(arg2.Id,caseid), r.Id);
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

        private async Task ShardedClient_ReactionAdded(Cacheable<IUserMessage, ulong> arg1,
            ISocketMessageChannel arg2, SocketReaction arg3)
        {
            Writer.WriteEntry(new LogMessage(LogSeverity.Debug, "Reaction", "Reaction Event: Reaction Added!"));
            SocketGuildChannel STC = null;
            if (arg2 is SocketGuildChannel)
            {
                STC = arg2 as SocketGuildChannel;
            }
            else
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Debug, "Reaction", "Channel wasn't a guild channel... RIP"));
                return;
            }

            await Dsc_trashcanCheck(STC, arg1, arg2, arg3);

            #region Starboards

            if (arg3.Emote.ToString() == "\u2B50" || arg3.Emote.ToString() == "\uD83C\uDF1F") //STAR or GLOWING STAR
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Debug, "Starboard", "Reaction was a STAR!"));
                if (SBBindings.TryGetValue(STC.Guild.Id, out StarboardBinding binding))
                {
                    if (arg2.Id != binding.ChannelID) // Starred message is NOT on the starboard channel. 
                    {
                        Writer.WriteEntry(new LogMessage(LogSeverity.Debug, "Starboard", "SBBinding found. NOT Starboard embed."));
                        var sbmessage = binding.StarboardData.FirstOrDefault(x => x.StarredMessageID == arg1.Id);
                        if (sbmessage != null)
                        {
                            //message is already in the starboard channel Modify the starcount.
                            sbmessage.StarCount++;
                            binding.StarboardData[binding.StarboardData.IndexOf(sbmessage)].StarCount = sbmessage.StarCount;
                            SBBindings[STC.Guild.Id] = binding;
                            var StarredMessage = await STC.Guild.GetTextChannel(sbmessage.StarredMsgChannelID)
                                .GetMessageAsync(sbmessage.StarredMessageID);
                            
                            string nick = (StarredMessage.Author as IGuildUser).Nickname;
                            string sname = StarredMessage.Author.Username + "#" + StarredMessage.Author.Discriminator;
                            if (string.IsNullOrWhiteSpace(nick))
                            {
                                nick = sname;
                            }
                            string name = binding.UseAlias ? nick : sname;
                            //modify the starboard message
                            EmbedBuilder SBEntryEmbed = new EmbedBuilder()
                            {
                                Author = new EmbedAuthorBuilder()
                                {
                                    IconUrl = StarredMessage.Author.GetAvatarUrl(ImageFormat.Auto),
                                    Name = name
                                },
                                Description = StarredMessage.Content,
                                Color = new Color(255, 234, 119),
                                Footer = new EmbedFooterBuilder()
                                {
                                    Text = $"MessageID: {sbmessage.StarredMessageID}"
                                }

                            };
                            if (StarredMessage.Embeds.Count > 0)
                            {
                                IEmbed first = StarredMessage.Embeds.First();
                                foreach (var field in first.Fields)
                                {
                                    SBEntryEmbed.AddField($"[{field.Name}]", field.Value, field.Inline);
                                }
                                if (first.Image.HasValue)
                                {
                                    SBEntryEmbed.WithImageUrl(first.ToEmbedBuilder().ImageUrl);
                                }
                                if (first.Title.Length > 0)
                                {
                                    SBEntryEmbed.Description += $"\r\n**[{first.Title}]**\r\n";
                                }
                                if (first.Description.Length > 0)
                                {
                                    SBEntryEmbed.Description += $"\r\n{first.Description}\r\n";
                                }
                            }
                            if (StarredMessage.Attachments.Count > 0)
                            {
                                if (StarredMessage.Attachments.First().Width.HasValue)
                                {
                                    if (!StarredMessage.Attachments.First().Filename.StartsWith("SPOILER_"))
                                    {
                                        SBEntryEmbed.ImageUrl = StarredMessage.Attachments.First().Url;
                                    }
                                    else
                                    {
                                        SBEntryEmbed.AddField("Secret Attachment", $"||{StarredMessage.Attachments.First().Url}||");
                                    }
                                }
                            }
                            if (await STC.Guild.GetTextChannel(binding.ChannelID).GetMessageAsync(sbmessage.SbMessageID) is IUserMessage sum)
                            {
                                await sum.ModifyAsync(
                                    x =>
                                    {
                                        x.Content = $"🌟 **{sbmessage.StarCount}** | <#{sbmessage.StarredMsgChannelID}>";
                                        x.Embed = SBEntryEmbed.Build();
                                    }
                                    );
                            }
                            else //SUM was NULL! Create the new message instead.
                            {
                                SBEntryEmbed = new EmbedBuilder()
                                {
                                    Author = new EmbedAuthorBuilder()
                                    {
                                        IconUrl = StarredMessage.Author.GetAvatarUrl(ImageFormat.Auto),
                                        Name = name
                                    },
                                    Description = StarredMessage.Content,
                                    Color = new Color(255, 234, 119),
                                    Footer = new EmbedFooterBuilder()
                                    {
                                        Text = $"MessageID: {StarredMessage.Id} | {StarredMessage.Timestamp.ToString()}"
                                    }

                                };
                                if(StarredMessage.Embeds.Count > 0)
                                {
                                    IEmbed first = StarredMessage.Embeds.First();
                                    foreach (var field in first.Fields)
                                    {
                                        SBEntryEmbed.AddField($"[{field.Name}]", field.Value, field.Inline);
                                    }
                                    if(first.Image.HasValue)
                                    {
                                        SBEntryEmbed.WithImageUrl(first.ToEmbedBuilder().ImageUrl);
                                    }
                                    if(first.Title.Length > 0)
                                    {
                                        SBEntryEmbed.Description += $"\r\n**[{first.Title}]**\r\n";
                                    }
                                    if(first.Description.Length > 0)
                                    {
                                        SBEntryEmbed.Description += $"\r\n{first.Description}\r\n";
                                    }
                                }
                                if (StarredMessage.Attachments.Count > 0)
                                {
                                    if (StarredMessage.Attachments.First().Width.HasValue)
                                    {
                                        if (!StarredMessage.Attachments.First().Filename.StartsWith("SPOILER_"))
                                        {
                                            SBEntryEmbed.ImageUrl = StarredMessage.Attachments.First().Url;
                                        }
                                        else
                                        {
                                            SBEntryEmbed.AddField("Secret Attachment", $"||{StarredMessage.Attachments.First().Url}||");
                                        }
                                    }
                                }
                                var channel = STC.Guild.GetTextChannel(binding.ChannelID);
                                var newSBMessage = await channel.SendMessageAsync($"🌟 **1** | " +
                                    $"<#{StarredMessage.Channel.Id}>", false, SBEntryEmbed.Build());
                            }
                        }

                        if (sbmessage == null)
                        {
                            //Create a new embed.
                            var StarredMessage = await arg1.GetOrDownloadAsync();
                            string nick = (StarredMessage.Author as IGuildUser).Nickname;
                            string sname = StarredMessage.Author.Username + "#" + StarredMessage.Author.Discriminator;
                            if (string.IsNullOrWhiteSpace(nick))
                            {
                                nick = sname;
                            }
                            string name = binding.UseAlias ? nick : sname;
                            //modify the starboard message
                            EmbedBuilder SBEntryEmbed = new EmbedBuilder()
                            {
                                Author = new EmbedAuthorBuilder()
                                {
                                    IconUrl = StarredMessage.Author.GetAvatarUrl(ImageFormat.Auto),
                                    Name = name
                                },
                                Description = StarredMessage.Content,
                                Color = new Color(255, 234, 119),
                                Footer = new EmbedFooterBuilder()
                                {
                                    Text = $"MessageID: {StarredMessage.Id}"
                                }

                            };
                            if (StarredMessage.Embeds.Count > 0)
                            {
                                IEmbed first = StarredMessage.Embeds.First();
                                foreach (var field in first.Fields)
                                {
                                    SBEntryEmbed.AddField($"[{field.Name}]", field.Value, field.Inline);
                                }
                                if (first.Image.HasValue)
                                {
                                    SBEntryEmbed.WithImageUrl(first.ToEmbedBuilder().ImageUrl);
                                }
                                if (!string.IsNullOrWhiteSpace(first.Title))
                                {
                                    SBEntryEmbed.Description += $"\r\n**[{first.Title}]**\r\n";
                                }
                                if (!string.IsNullOrWhiteSpace(first.Description))
                                {
                                    SBEntryEmbed.Description += $"\r\n{first.Description}\r\n";
                                }
                            }
                            if (StarredMessage.Attachments.Count > 0)
                            {
                                if (StarredMessage.Attachments.First().Width.HasValue)
                                {
                                    if (!StarredMessage.Attachments.First().Filename.StartsWith("SPOILER_"))
                                    {
                                        SBEntryEmbed.ImageUrl = StarredMessage.Attachments.First().Url;
                                    }
                                    else
                                    {
                                        SBEntryEmbed.AddField("Secret Attachment", $"||{StarredMessage.Attachments.First().Url}||");
                                    }
                                }
                            }
                            var channel = STC.Guild.GetTextChannel(binding.ChannelID);
                            var newSBMessage = await channel.SendMessageAsync($"\uD83C\uDF1F **1** | " +
                                $"<#{StarredMessage.Channel.Id}>", false, SBEntryEmbed.Build());


                            await newSBMessage.AddReactionAsync(new Emoji("\u2B50"));
                            //create a new starboard entry.
                            sbmessage = new SBEntry()
                            {
                                SbMessageID = newSBMessage.Id,
                                StarredMessageID = arg1.Id,
                                StarCount = 1,
                                StarredMsgChannelID = (await arg1.GetOrDownloadAsync()).Channel.Id
                            };
                            binding.StarboardData.Add(sbmessage);
                            SBBindings[STC.Guild.Id] = binding;

                        }


                    } //Starred message IS NOT from Starboard channel.

                    if (arg2.Id == binding.ChannelID) // Starred message IS from Starboard channel. 
                    {

                        Writer.WriteEntry(new LogMessage(LogSeverity.Info, "Starboard", "SBBinding found. Starboard embed."));
                        var ebsbmessage = binding.StarboardData.FirstOrDefault(x => x.SbMessageID == (arg1.GetOrDownloadAsync().GetAwaiter().GetResult()).Id);
                        if (ebsbmessage != null)
                        {
                            //message is already in the starboard Modify the starcount.
                            ebsbmessage.StarCount++;
                            binding.StarboardData[binding.StarboardData.IndexOf(ebsbmessage)] = ebsbmessage;
                            SBBindings[STC.Guild.Id] = binding;
                            var StarredMessage = await STC.Guild.GetTextChannel(ebsbmessage.StarredMsgChannelID)
                                .GetMessageAsync(ebsbmessage.StarredMessageID);
                            //modify the starboard message
                            string nick = (StarredMessage.Author as IGuildUser).Nickname;
                            string sname = StarredMessage.Author.Username + "#" + StarredMessage.Author.Discriminator;
                            if (string.IsNullOrWhiteSpace(nick))
                            {
                                nick = sname;
                            }
                            string name = binding.UseAlias ? nick : sname;
                            //modify the starboard message
                            EmbedBuilder SBEntryEmbed = new EmbedBuilder()
                            {
                                Author = new EmbedAuthorBuilder()
                                {
                                    IconUrl = StarredMessage.Author.GetAvatarUrl(ImageFormat.Auto),
                                    Name = name
                                },
                                Description = StarredMessage.Content,
                                Color = new Color(255, 234, 119),
                                Footer = new EmbedFooterBuilder()
                                {
                                    Text = $"MessageID: {ebsbmessage.StarredMessageID}"
                                }

                            };
                            if (StarredMessage.Embeds.Count > 0)
                            {
                                IEmbed first = StarredMessage.Embeds.First();
                                foreach (var field in first.Fields)
                                {
                                    SBEntryEmbed.AddField($"[{field.Name}]", field.Value, field.Inline);
                                }
                                if (first.Image.HasValue)
                                {
                                    SBEntryEmbed.WithImageUrl(first.ToEmbedBuilder().ImageUrl);
                                }
                                if (first.Title.Length > 0)
                                {
                                    SBEntryEmbed.Description += $"\r\n**[{first.Title}]**\r\n";
                                }
                                if (first.Description.Length > 0)
                                {
                                    SBEntryEmbed.Description += $"\r\n{first.Description}\r\n";
                                }
                            }
                            if (StarredMessage.Attachments.Count > 0)
                            {
                                if (StarredMessage.Attachments.First().Width.HasValue)
                                {
                                    if (!StarredMessage.Attachments.First().Filename.StartsWith("SPOILER_"))
                                    {
                                        SBEntryEmbed.ImageUrl = StarredMessage.Attachments.First().Url;
                                    }
                                    else
                                    {
                                        SBEntryEmbed.AddField("Secret Attachment", $"||{StarredMessage.Attachments.First().Url}||");
                                    }
                                }
                            }
                            var sum = await STC.Guild.GetTextChannel(binding.ChannelID).GetMessageAsync(ebsbmessage.SbMessageID) as IUserMessage;
                            if (sum != null)
                            {
                                await sum.ModifyAsync(
                                    x =>
                                    {
                                        x.Content = $"🌟 **{ebsbmessage.StarCount}** | <#{ebsbmessage.StarredMsgChannelID}>";
                                        x.Embed = SBEntryEmbed.Build();
                                    }
                                    );
                            }
                            else //SUM was NULL! Create the new message instead.
                            {
                                SBEntryEmbed = new EmbedBuilder()
                                {
                                    Author = new EmbedAuthorBuilder()
                                    {
                                        IconUrl = StarredMessage.Author.GetAvatarUrl(ImageFormat.Auto),
                                        Name = name
                                    },
                                    Description = StarredMessage.Content,
                                    Color = new Color(255, 234, 119),
                                    Footer = new EmbedFooterBuilder()
                                    {
                                        Text = $"MessageID: {StarredMessage.Id} | {StarredMessage.Timestamp.ToString()}"
                                    }

                                };
                                if (StarredMessage.Embeds.Count > 0)
                                {
                                    IEmbed first = StarredMessage.Embeds.First();
                                    foreach (var field in first.Fields)
                                    {
                                        SBEntryEmbed.AddField($"[{field.Name}]", field.Value, field.Inline);
                                    }
                                    if (first.Image.HasValue)
                                    {
                                        SBEntryEmbed.WithImageUrl(first.ToEmbedBuilder().ImageUrl);
                                    }
                                    if (first.Title.Length > 0)
                                    {
                                        SBEntryEmbed.Description += $"\r\n**[{first.Title}]**\r\n";
                                    }
                                    if (first.Description.Length > 0)
                                    {
                                        SBEntryEmbed.Description += $"\r\n{first.Description}\r\n";
                                    }
                                }
                                if (StarredMessage.Attachments.Count > 0)
                                {
                                    if (StarredMessage.Attachments.First().Width.HasValue)
                                    {
                                        if (!StarredMessage.Attachments.First().Filename.StartsWith("SPOILER_"))
                                        {
                                            SBEntryEmbed.ImageUrl = StarredMessage.Attachments.First().Url;
                                        }
                                        else
                                        {
                                            SBEntryEmbed.AddField("Secret Attachment", $"||{StarredMessage.Attachments.First().Url}||");
                                        }
                                    }
                                }
                                var channel = STC.Guild.GetTextChannel(binding.ChannelID);
                                var newSBMessage = await channel.SendMessageAsync($"🌟 **1** | " +
                                    $"<#{StarredMessage.Channel.Id}>", false, SBEntryEmbed.Build());
                            }
                        }
                    } //Reaction is from starboard channel

                    using (StreamWriter sw = new StreamWriter(StarboardBindingsConfig))
                    {
                        sw.WriteLine(JsonConvert.SerializeObject(SBBindings, Formatting.Indented));
                    }
                }
            }

            #endregion

        }

        private async Task ShardedClient_ReactionRemoved(Cacheable<IUserMessage, ulong> arg1,
            ISocketMessageChannel arg2, SocketReaction arg3)
        {
            Writer.WriteEntry(new LogMessage(LogSeverity.Debug, "Reaction", "Reaction Event: Reaction Removed!"));
            SocketGuildChannel STC = null;
            if (arg2 is SocketGuildChannel)
            {
                STC = arg2 as SocketGuildChannel;
            }
            else
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Debug, "Reaction", "Channel wasn't a guild channel... RIP"));
                return;
            }

            #region Starboards

            if (arg3.Emote.ToString() == "\u2B50" || arg3.Emote.ToString() == "\uD83C\uDF1F")
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Debug, "Starboard", "Reaction was a STAR!"));
                if (SBBindings.TryGetValue(STC.Guild.Id, out StarboardBinding binding))
                {
                    if (arg2.Id != binding.ChannelID) // Starred message is NOT on the starboard. 
                    {

                        Writer.WriteEntry(new LogMessage(LogSeverity.Info, "Starboard", "SBBinding found. NOT Starboard embed."));
                        var sbmessage = binding.StarboardData.FirstOrDefault(x => x.SbMessageID == (arg1.GetOrDownloadAsync().GetAwaiter().GetResult()).Id);
                        if (sbmessage != null)
                        {
                            //message is already in the starboard Modify the starcount.
                            sbmessage.StarCount--;
                            binding.StarboardData[binding.StarboardData.IndexOf(sbmessage)] = sbmessage;
                            SBBindings[STC.Guild.Id] = binding;
                            var StarredMessage = await STC.Guild.GetTextChannel(sbmessage.StarredMsgChannelID)
                                .GetMessageAsync(sbmessage.StarredMessageID);
                            string nick = (StarredMessage.Author as IGuildUser).Nickname;
                            string sname = StarredMessage.Author.Username + "#" + StarredMessage.Author.Discriminator;
                            if (string.IsNullOrWhiteSpace(nick))
                            {
                                nick = sname;
                            }
                            string name = binding.UseAlias ? nick : sname;
                            //modify the starboard message
                            EmbedBuilder SBEntryEmbed = new EmbedBuilder()
                            {
                                Author = new EmbedAuthorBuilder()
                                {
                                    IconUrl = StarredMessage.Author.GetAvatarUrl(ImageFormat.Auto),
                                    Name = name
                                },
                                Description = StarredMessage.Content,
                                Color = new Color(255, 234, 119),
                                Footer = new EmbedFooterBuilder()
                                {
                                    Text = $"MessageID: {sbmessage.StarredMessageID}"
                                }

                            };
                            if (StarredMessage.Embeds.Count > 0)
                            {
                                IEmbed first = StarredMessage.Embeds.First();
                                foreach (var field in first.Fields)
                                {
                                    SBEntryEmbed.AddField($"[{field.Name}]", field.Value, field.Inline);
                                }
                                if (first.Image.HasValue)
                                {
                                    SBEntryEmbed.WithImageUrl(first.ToEmbedBuilder().ImageUrl);
                                }
                                if (first.Title.Length > 0)
                                {
                                    SBEntryEmbed.Description += $"\r\n**[{first.Title}]**\r\n";
                                }
                                if (first.Description.Length > 0)
                                {
                                    SBEntryEmbed.Description += $"\r\n{first.Description}\r\n";
                                }
                            }
                            if (StarredMessage.Attachments.Count > 0)
                            {
                                if (StarredMessage.Attachments.First().Width.HasValue)
                                {
                                    if (!StarredMessage.Attachments.First().Filename.StartsWith("SPOILER_"))
                                    {
                                        SBEntryEmbed.ImageUrl = StarredMessage.Attachments.First().Url;
                                    }
                                    else
                                    {
                                        SBEntryEmbed.AddField("Secret Attachment", $"||{StarredMessage.Attachments.First().Url}||");
                                    }
                                }
                            }
                            if (await STC.Guild.GetTextChannel(binding.ChannelID).GetMessageAsync(sbmessage.SbMessageID) is IUserMessage sum)
                            {
                                await sum.ModifyAsync(
                                    x =>
                                    {
                                        x.Content = $"🌟 **{sbmessage.StarCount}** | <#{sbmessage.StarredMsgChannelID}>";
                                        x.Embed = SBEntryEmbed.Build();
                                    }
                                    );
                            }
                        }

                    } //message not on starboard channel

                    if (arg2.Id == binding.ChannelID) // Starred message IS on the starboard. 
                    {

                        Writer.WriteEntry(new LogMessage(LogSeverity.Info, "Starboard", "SBBinding found. Starboard embed."));
                        var ebsbmessage = binding.StarboardData.FirstOrDefault(x => x.SbMessageID == arg1.Id);
                        if (ebsbmessage != null)
                        {
                            //message is already in the starboard Modify the starcount.
                            ebsbmessage.StarCount--;
                            binding.StarboardData[binding.StarboardData.IndexOf(ebsbmessage)] = ebsbmessage;
                            SBBindings[STC.Guild.Id] = binding;
                            var StarredMessage = await STC.Guild.GetTextChannel(ebsbmessage.StarredMsgChannelID)
                                .GetMessageAsync(ebsbmessage.StarredMessageID);
                            string nick = (StarredMessage.Author as IGuildUser).Nickname;
                            string sname = StarredMessage.Author.Username + "#" + StarredMessage.Author.Discriminator;
                            if (string.IsNullOrWhiteSpace(nick))
                            {
                                nick = sname;
                            }
                            string name = binding.UseAlias ? nick : sname;
                            //modify the starboard message
                            EmbedBuilder SBEntryEmbed = new EmbedBuilder()
                            {
                                Author = new EmbedAuthorBuilder()
                                {
                                    IconUrl = StarredMessage.Author.GetAvatarUrl(ImageFormat.Auto),
                                    Name = name
                                },
                                Description = StarredMessage.Content,
                                Color = new Color(255, 234, 119),
                                Footer = new EmbedFooterBuilder()
                                {
                                    Text = $"MessageID: {ebsbmessage.StarredMessageID}"
                                }

                            };
                            if (StarredMessage.Embeds.Count > 0)
                            {
                                IEmbed first = StarredMessage.Embeds.First();
                                foreach (var field in first.Fields)
                                {
                                    SBEntryEmbed.AddField($"[{field.Name}]", field.Value, field.Inline);
                                }
                                if (first.Image.HasValue)
                                {
                                    SBEntryEmbed.WithImageUrl(first.ToEmbedBuilder().ImageUrl);
                                }
                                if (first.Title.Length > 0)
                                {
                                    SBEntryEmbed.Description += $"\r\n**[{first.Title}]**\r\n";
                                }
                                if (first.Description.Length > 0)
                                {
                                    SBEntryEmbed.Description += $"\r\n{first.Description}\r\n";
                                }
                            }
                            if (StarredMessage.Attachments.Count > 0)
                            {
                                if (StarredMessage.Attachments.First().Width.HasValue)
                                {
                                    if (!StarredMessage.Attachments.First().Filename.StartsWith("SPOILER_"))
                                    {
                                        SBEntryEmbed.ImageUrl = StarredMessage.Attachments.First().Url;
                                    }
                                    else
                                    {
                                        SBEntryEmbed.AddField("Secret Attachment", $"||{StarredMessage.Attachments.First().Url}||");
                                    }
                                }
                            }
                            if (await STC.Guild.GetTextChannel(binding.ChannelID).GetMessageAsync(ebsbmessage.SbMessageID) is IUserMessage sum)
                            {
                                await sum.ModifyAsync(
                                    x =>
                                    {
                                        x.Content = $"🌟 **{ebsbmessage.StarCount}** | <#{ebsbmessage.StarredMsgChannelID}>";
                                        x.Embed = SBEntryEmbed.Build();
                                    }
                                    );
                            }
                        }
                    } //Message is on starboard channel

                    using (StreamWriter sw = new StreamWriter(StarboardBindingsConfig))
                    {
                        sw.WriteLine(JsonConvert.SerializeObject(SBBindings, Formatting.Indented));
                    }
                }
            }

            #endregion
        }

        private async Task Dsc_trashcanCheck(SocketGuildChannel STC, Cacheable<IUserMessage, ulong> arg1,
            ISocketMessageChannel arg2, SocketReaction arg3)
        {
            #region Trashcans

            if (!Trashcans.TryGetValue(STC.Guild.Id, out string Reac))
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction", "Guild not bound... RIP"));
                return;
            }
            if (string.IsNullOrWhiteSpace(Reac))
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction", "Reaction was blank?!"));
                return;
            }
            if ((await arg1.GetOrDownloadAsync()).Author.Id != ShardedClient.CurrentUser.Id && arg3.Emote.ToString() == Reac)
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Verbose, "Reaction", "Someone reacted to a message with the trigger, but it wasn't mine."));
                return;
            }
            else
            {

                if (arg3.Emote.ToString() == Reac.ToString())
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

            #endregion Trashcans
        }


        #endregion PRIVATE EVENTS

        #region PUBLIC METHODS

        #region MODLOG

        public async Task<Tuple<bool,string>> ModLogEditReason(ulong guild, ulong caseID, string REASON)
        {
            if(string.IsNullOrWhiteSpace(REASON))
            {
                return new Tuple<bool, string>(false, "You did not specify a reason!");
            }
            if(MessageCaseIDs.TryGetValue(Tuple.Create(guild, caseID), out ulong MessageID))
            {
                SocketGuild gguild = ShardedClient.GetGuild(guild);
                if(gguild == null)
                {
                    return new Tuple<bool, string>(false, "Specified GuildID is not valid!");
                }
                SocketTextChannel ch = (SocketTextChannel)gguild.GetChannel(MLbindings.FirstOrDefault(x => x.GuildID == guild).ChannelID);
                if (ch == null)
                {
                    return new Tuple<bool, string>(false, "Specified channel is not valid!");
                }
                var message = await ch.GetMessageAsync(MessageID) as IUserMessage;
                if(message == null)
                {
                    return new Tuple<bool, string>(false, "The message could not be found!");
                    
                }
                Embed oldembed = (Embed)message.Embeds.ToList()[0];
                EmbedBuilder b = new EmbedBuilder
                {
                    Title = oldembed.Title,
                    Timestamp = oldembed.Timestamp
                };
                b.WithColor(new Color(225, 18, 12));
                foreach (EmbedField item in oldembed.Fields)
                {
                    if(item.Name !="Reason")
                    {
                        b.AddField(item.Name, item.Value, item.Inline);
                    }
                    else
                    {
                        b.AddField(item.Name, REASON, item.Inline);
                    }
                }
                await message.ModifyAsync(x => x.Embed = b.Build());
                return new Tuple<bool, string>(true,"Successfully edited");
            }
            else
            {
                return new Tuple<bool, string>(false, "Case ID was not found!!");
            }
        }

        public async Task<RestUserMessage> SendModLog(ulong GuildID, Embed embed)
        {
            ModLogBinding mb = MLbindings.FirstOrDefault(x => x.GuildID == GuildID);
            if (mb != null)
            {
                if (ShardedClient.GetGuild(GuildID).GetChannel(mb.ChannelID) is SocketTextChannel sfl)
                {
                    return await sfl.SendMessageAsync("", false, embed);
                }
            }
            return null;
        }

        public bool ModLogBound(ulong guildID)
        {
            ModLogBinding mb = MLbindings.FirstOrDefault(x => x.GuildID == guildID);
            return mb != null;
        }

        public ulong GetCaseCount(ulong guildID)
        {
            ModLogBinding mb = MLbindings.FirstOrDefault(x => x.GuildID == guildID);

            if (mb != null)
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
            if (MLbindings.FirstOrDefault(x => x.GuildID == context.Guild.Id) != null)
            {
                await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Nope!",
                $"You can only have one moderation log per guild.", Color.Red));
                return;
            }

            ModLogBinding ml = new ModLogBinding
            {
                GuildID = context.Guild.Id,
                ChannelID = context.Channel.Id,
                MuteRoleID = role.Id
            };
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

        #endregion MODLOG

        public Task BindWelcomeMessage(ICommandContext Context, GuildQueryItem roletoadd)
        {
            
            BoundItems.Add(roletoadd.DefaultChannel.GuildId, roletoadd);
            var Log = new LogMessage(LogSeverity.Info, "Greetings", $"Created a Join Event listener using the channel `{roletoadd.DefaultChannel.Name}` located in guild `{roletoadd.DefaultChannel.Guild.Name}`. Using Welcome message: {roletoadd.WelcomeMessage}");//debuglul
            Writer.WriteEntry(Log);
            return Task.Delay(0);
        }

        #region TRASHCAN

        public async Task BindTrashcanReaction(ICommandContext Context, Emote emote, ulong GuildID)
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

        public async Task UnBindTrashcanReaction(ICommandContext Context, ulong GuildID)
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

        #endregion TRASHCAN

        #region STARBOARD

        public static async Task BindStarboard(ICommandContext context, ulong ChannelID)
        {
            if (!((await context.Guild.GetChannelAsync(ChannelID)) is SocketTextChannel stc))
            {
                await context.Channel.SendMessageAsync("", false,
                    GetEmbeddedMessage(context, "Invalid Channel", "You must specify an ID that points to a TEXT channel in this guild.", Color.DarkRed));
                return;
            }
            if (SBBindings.TryGetValue(context.Guild.Id, out StarboardBinding sb))
            {
                sb.ChannelID = ChannelID;
                SBBindings[context.Guild.Id] = sb;
                using (StreamWriter sw = new StreamWriter(StarboardBindingsConfig))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(SBBindings, Formatting.Indented));
                }
                await context.Channel.SendMessageAsync("", false,
                    GetEmbeddedMessage(context, "Updated Configuration", $"Starboard has been bound to {stc.Mention}", Color.Green));
                return;
            }
            else
            {
                SBBindings[context.Guild.Id] = new StarboardBinding()
                {
                    ChannelID = ChannelID,
                    StarboardData = new List<SBEntry>()
                };
                using (StreamWriter sw = new StreamWriter(StarboardBindingsConfig))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(SBBindings, Formatting.Indented));
                }
                await context.Channel.SendMessageAsync("", false,
                    GetEmbeddedMessage(context, "Created Configuration", $"Starboard has been bound to {stc.Mention}", Color.Green));
                return;
            }
        }

        public static async Task UnbindStarboard(ICommandContext context)
        {
            if (SBBindings.TryGetValue(context.Guild.Id, out _))
            {
                SBBindings.Remove(context.Guild.Id);
                await context.Channel.SendMessageAsync("", false,
                    GetEmbeddedMessage(context, "Updated Configuration", $"Starboard has been disabled.", Color.Green));
                return;
            }
            else
            {
                await context.Channel.SendMessageAsync("", false,
                    GetEmbeddedMessage(context, "Operation Failed", $"Starboard is not enabled.", Color.Red));
                return;
            }
        }

        public static async Task SBSetAliasMode(ICommandContext context, bool AliasMode)
        {

            if (SBBindings.TryGetValue(context.Guild.Id, out StarboardBinding sb))
            {
                sb.UseAlias = AliasMode;
                SBBindings[context.Guild.Id] = sb;
                using (StreamWriter sw = new StreamWriter(StarboardBindingsConfig))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(SBBindings, Formatting.Indented));
                }
                await context.Channel.SendMessageAsync("", false,
                    GetEmbeddedMessage(context, "Updated Configuration", sb.UseAlias ? $"Starboard will run in `Alias` mode." : $"Starboard will run in `Standard` mode.", Color.Green));
                return;
            }
            else
            {

                await context.Channel.SendMessageAsync("", false,
                    GetEmbeddedMessage(context, "Configuration Error", $"Starboard is not bound to a channel in this guild. Please do this first.", Color.Orange));
                return;
            }
        }

        #endregion STARBOARD

        #region EMBED MESSAGES
        public static Embed GetEmbeddedMessage(ICommandContext Context, string title, string message, Color color, Exception e = null)
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(color);
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithTitle(title);
            b.WithDescription(message);
            b.WithFooter($"{Context.Client.CurrentUser.Username} • TestModuleService");
            if (e != null)
            {
                b.AddField("Extended Details", e.Message);
                b.AddField("For developer", "See the Errors.LOG for more info!!!");
                Writer.WriteErrorsLog(e);
            }
            return b.Build();
        }

        #endregion EMBED MESSAGES

        #endregion PUBLIC METHODS

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

    public class StarboardBinding
    {
        public ulong ChannelID { get; set; }
        public bool UseAlias { get; set; } = false;
        public List<SBEntry> StarboardData { get; set; }
    }

    public class SBEntry
    {
        public ulong StarCount { get; set; }
        public ulong StarredMessageID { get; set; }
        public ulong StarredMsgChannelID { get; set; }
        public ulong SbMessageID { get; set; }
    }

    public class MLentry
    {
        public ulong CaseID { get; set; }
        public ulong GuildID { get; set; }
    }
}
