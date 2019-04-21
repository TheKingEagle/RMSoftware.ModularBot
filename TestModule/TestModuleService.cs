using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ModularBOT.Component;
using Discord.Commands;
namespace TestModule
{
    [Summary("A Basic moderation toolkit for ModularBOT")]
    public class TestModule : ModuleBase
    {
        public DiscordShardedClient _client { get; set; }
        public ConsoleIO _writer { get; set; }
        public TestModuleService _jservice { get; set; }
        public TestModule(DiscordShardedClient discord, TestModuleService joinservice, ConsoleIO writer)
        {
            _client = discord;
            _jservice = joinservice;
            _writer = writer;
            _writer.WriteEntry(new LogMessage(LogSeverity.Critical, "TestMOD", "Constructor called!!!!!!!!!"));
        }

        [Command("Kick", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.KickMembers), RequireBotPermission(GuildPermission.KickMembers), RequireContext(ContextType.Guild)]
        public async Task Kick(IGuildUser user, [Remainder]string reason = "being an ass")
        {

            try
            {
                EmbedBuilder b = new EmbedBuilder
                {
                    Title = $"Kicked {user.Username} from guild.",
                    Description = $"Kick command was issued by {Context.User.Username} with attached reason.",
                    Timestamp = DateTimeOffset.Now
                };
                b.WithColor(new Color(128, 12, 12));
                b.AddField("Reason", reason);
                await user.KickAsync(reason);
                await Context.Channel.SendMessageAsync("", false, b.Build());
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    await Context.Channel.SendMessageAsync($"\u26A0 `You really thought you could do that? Very funny.`");
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"\u26A0 `{ex.Message}`");
                }
            }
        }

        [Command("ban", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers), RequireContext(ContextType.Guild)]
        public async Task Ban(IGuildUser user, [Remainder]string reason = "being a supreme ass")
        {

            try
            {
                EmbedBuilder b = new EmbedBuilder
                {
                    Title = $"Banned {user.Username} from guild.",
                    Description = $"ban command was issued by {Context.User.Username} with attached reason.",
                    Timestamp = DateTimeOffset.Now
                };
                b.WithColor(new Color(255, 0, 0));
                b.AddField("Reason", reason);
                await Context.Guild.AddBanAsync(user, 0, reason);
                await Context.Channel.SendMessageAsync("", false, b.Build());
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    await Context.Channel.SendMessageAsync($"\u26A0 `You really thought you could do that? Very funny.`");
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"\u26A0 `{ex.Message}`");
                }
            }
        }

        [Command("cpurge", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task Clear(int amount = 90)
        {
            await Context.Channel.SendMessageAsync($"Clearing messages... this might take some time.");
            var msgs = await Context.Channel.GetMessagesAsync(amount + 1, CacheMode.AllowDownload).FlattenAsync();
            foreach (var item in msgs)
            {
                await Context.Channel.DeleteMessageAsync(item, new RequestOptions { RetryMode = RetryMode.AlwaysRetry });
            }
            await Context.Channel.SendMessageAsync($"done.");
        }
        
        [Command("pollJoin", RunMode = RunMode.Async)]
        public async Task DoJoinCheck(ulong GuildID, ulong ChannelID, ulong RoleID = 0,[Remainder]string WelcomeMessage=null)
        {
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
        
      
    }

    public class TestModuleService
    {
        DiscordShardedClient ShardedClient { get; set; }
        ConsoleIO Writer { get; set; }
        
        [DontInject]
        public Dictionary<ulong, GuildQueryItem> BoundItems { get; set; }//This contains <guildID,role> value pairs to check user's join event.


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


            BoundItems = new Dictionary<ulong, GuildQueryItem>();
            ShardedClient.UserJoined += ShardedClient_UserJoined;
            LogMessage Log = new LogMessage(LogSeverity.Info, "Greetings", "Added UserJoin event handler to client.");
            _consoleIO.WriteEntry(Log);


        }

        private async Task ShardedClient_UserJoined(SocketGuildUser arg)
        {
            LogMessage Log = new LogMessage(LogSeverity.Info, "Greetings", "A wild user appears!");
            Writer.WriteEntry(Log);
            bool result = BoundItems.TryGetValue(arg.Guild.Id, out GuildQueryItem item);
            if (result)
            {
                Log = new LogMessage(LogSeverity.Info, "Greetings", $"What is the default channel? {item.DefaultChannel.Name}");//debuglul
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
                Log = new LogMessage(LogSeverity.Info, "Greetings", "The GuildUser uses JoinEvent... It's Not very effective...");//debuglul
                Writer.WriteEntry(Log);
                Log = new LogMessage(LogSeverity.Info, "Greetings", "The GuildUser: " + arg.Username + "\r\n" + "The Guild: " + arg.Guild.Name);//debuglul
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
    }
    public class GuildQueryItem
    {
        public ITextChannel DefaultChannel { get; set; }
        public IRole RoleToAssign { get; set; }
        public string WelcomeMessage { get; set; }
    }
}
