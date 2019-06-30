﻿using System;
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
namespace TestModule
{
    [Summary("A Basic moderation toolkit for ModularBOT")]
    public class TestModule : ModuleBase
    {
        public DiscordShardedClient _client { get; set; }
        public ConsoleIO _writer { get; set; }
        public TestModuleService _jservice { get; set; }

        public PermissionManager PermissionsManager { get; set; }
        public TestModule(DiscordShardedClient discord, TestModuleService joinservice, ConsoleIO writer, PermissionManager manager)
        {
            _client = discord;
            _jservice = joinservice;
            _writer = writer;
            PermissionsManager = manager;
            _writer.WriteEntry(new LogMessage(LogSeverity.Critical, "TestMOD", "Constructor called!!!!!!!!!"));
        }
        [Command("tpmgr"),Remarks("AccessLevels.Administrator")]
        public async Task Showtest()
        {
            if(PermissionsManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await ReplyAsync("", false, PermissionsManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
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
                ModLogBinding ml = _jservice.MLbindings.FirstOrDefault(x => x.GuildID == Context.Guild.Id);
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
                ModLogBinding ml = _jservice.MLbindings.FirstOrDefault(x => x.GuildID == Context.Guild.Id);
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

        [Command("ml-bind")]
        public async Task StartML(SocketRole MuteRole)
        {
            await _jservice.BindModLog(Context,MuteRole);
            await ReplyAsync("", false, GetEmbeddedMessage("Guild's Mod Log - Bound", 
                $"This channel will be used for all future mod log entries. This guild's Muted role is `{MuteRole.Name}`", Color.Green));
        }
        [Command("ml-unbind")]
        public async Task stopML()
        {
            await _jservice.UnBindModLog(Context);
            await ReplyAsync("", false, GetEmbeddedMessage("Guild's Mod Log - Unbound", "This channel will no longer be used for mod log entries.", Color.Green));
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

        string ModLogBindingsConfig = "Modules/TestModule/mod-log.json";

        [DontInject]
        public Dictionary<ulong, GuildQueryItem> BoundItems { get; set; }//This contains <guildID,role> value pairs to check user's join event.

        
        public List<ModLogBinding> MLbindings = new List<ModLogBinding>();

        public TestModuleService(DiscordShardedClient _client, ConsoleIO _consoleIO)
        {
            ShardedClient = _client;
            Writer = _consoleIO;
            LogMessage constructorLOG = new LogMessage(LogSeverity.Critical, "Greetings", "TestModuleService constructor called.");
            Writer.WriteEntry(constructorLOG);
            if (ShardedClient == null)
            {
                LogMessage ERR = new LogMessage(LogSeverity.Critical, "Greetings", "Client is null! You should be ashamed.");
                _consoleIO.WriteEntry(ERR);
            }
            if(!Directory.Exists(Path.GetDirectoryName(ModLogBindingsConfig)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ModLogBindingsConfig));
                MLbindings = new List<ModLogBinding>();
                using (StreamWriter sw = new StreamWriter(ModLogBindingsConfig))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(MLbindings,Formatting.Indented));
                }
            }
            using (StreamReader sr = new StreamReader(ModLogBindingsConfig))
            {
                MLbindings = JsonConvert.DeserializeObject<List<ModLogBinding>>(sr.ReadToEnd());
            }
            if(MLbindings == null)
            {
                MLbindings = new List<ModLogBinding>();
            }
                BoundItems = new Dictionary<ulong, GuildQueryItem>();
            ShardedClient.UserJoined += ShardedClient_UserJoined;
            LogMessage Log = new LogMessage(LogSeverity.Info, "Greetings", "Added UserJoin event handler to client.");
            _consoleIO.WriteEntry(Log);


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
                await item.DefaultChannel.SendMessageAsync($"Hello {arg.Mention}, Welcome to `{arg.Guild.Name}`! {item.WelcomeMessage}");
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

        public Task BindModLog(ICommandContext context, IRole role)
        {
            ModLogBinding ml = new ModLogBinding();
            ml.GuildID = context.Guild.Id;
            ml.ChannelID = context.Channel.Id;
            ml.MuteRoleID = role.Id;
            MLbindings.Add(ml);
            using (StreamWriter sw = new StreamWriter(ModLogBindingsConfig))
            {
                sw.WriteLine(JsonConvert.SerializeObject(MLbindings, Formatting.Indented));
            }
            return Task.Delay(1);
        }

        public Task UnBindModLog(ICommandContext context)
        {

            ModLogBinding mlr = MLbindings.FirstOrDefault(x => x.ChannelID == context.Channel.Id);
            if (mlr != null)
            {
                MLbindings.Remove(mlr);
            }
            using (StreamWriter sw = new StreamWriter(ModLogBindingsConfig))
            {
                sw.WriteLine(JsonConvert.SerializeObject(MLbindings, Formatting.Indented));
            }
            return Task.Delay(1);
        }

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
