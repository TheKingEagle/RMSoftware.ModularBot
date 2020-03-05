using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ModularBOT;
using ModularBOT.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuotesDB
{
    //[Group("NewModule")]
    [Summary("Create and view memorable quotes from your guild members.")]
    public class QuotesDB : ModuleBase
    {
        #region PUBLIC INJECTED COMPONENTS

        public DiscordShardedClient _client { get; set; }
        public ConsoleIO _writer { get; set; }
        public QuoteDBService _service { get; set; }

        public PermissionManager _permissions { get; set; }

        public ConfigurationManager _configmgr { get; set; }

        #endregion PUBLIC INJECTED COMPONENTS

        public QuotesDB(DiscordShardedClient discord, QuoteDBService service,
            ConsoleIO writer, PermissionManager manager, ConfigurationManager cnfgmgr)
        {
            _client = discord;
            _service = service;
            _writer = writer;
            _permissions = manager;
            _configmgr = cnfgmgr;
            //Ensure module was injected properly.
            _writer.WriteEntry(new LogMessage(LogSeverity.Critical, "QuotesDB", "Constructor called!"));
        }

        #region PUBLIC COMMANDS

        //TODO: Implement Custom commands with [Command("CommandName")...]

        [Command("addquote")]
        public async Task QDBAddQuote(string Text, string Author, string DateTime)
        {
            await _service.AddQuote(Context, Text, Author, DateTime);
        }

        [Command("addquote")]
        public async Task QDBAddQuote(ulong MessageID)
        {
            IMessage g = await Context.Channel.GetMessageAsync(MessageID);
            if(g == null)
            {
                await ReplyAsync("", false, QuoteDBService.GetEmbeddedMessage(Context, "Message Not Found", "The message with the specified ID could not be found in this channel.", Color.DarkRed));
                return;
            }
            DateTimeOffset dg = g.Timestamp;
            await _service.AddQuote(Context, g.Content, g.Author.Username, $"{dg.ToLocalTime().ToString("D")}");
        }

        [Command("quote")]
        public async Task QDBShowQuote(int index=-1)
        {
            if(index == -1)
            {
                await _service.ShowQuote(Context);
                return;
            }

            await _service.ShowQuote(Context, index);
        }

        #region ABOUT
        [Group("QuotesDB")]
        public class _QuotesDB : ModuleBase
        {
            [Command("about")]
            public async Task DisplayAbout()
            {
                EmbedBuilder eb = new EmbedBuilder()
                {
                    Title = "About QuotesDB",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl(ImageFormat.Auto),
                        Text = $"Requested By: {Context.User.Username} • QuotesDB"
                    },
                    Author = new EmbedAuthorBuilder()
                    {
                        IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto),
                        Name = $"{Context.Client.CurrentUser.Username}#{Context.Client.CurrentUser.Discriminator}"
                    },
                    //TODO: Edit your module's description in the dedicated About command
                    Description = "Make what your guild members say live forever. turn funny and memorable chat moments into 'inspirational' quotes.",
                    Color = Color.DarkBlue
                };
                await ReplyAsync("", false, eb.Build());
            }
        }
        #endregion

        #endregion
    }

    public class QuoteDBService
    {
        #region PRIVATE COMPONENTS [Required by the Module]

        private DiscordShardedClient ShardedClient { get; set; }
        private static ConsoleIO Writer { get; set; }

        private PermissionManager PermissionsManager { get; set; }
        private ConfigurationManager CfgMgr { get; set; }

        #endregion

        #region PRIVATE FIELDS

        private bool doonce = false; //Required check due to ModularBOT bug calling constructors more than once.

        //TODO: Add custom private fields here.
        private readonly string dbdir = "modules/quotedb";

        private List<QuoteContainer> GuildQuoteContainers = new List<QuoteContainer>();

        #endregion

        #region PUBLIC PROPERTIES
        //TODO: Add custom public properties here.
        
        #endregion

        public QuoteDBService(DiscordShardedClient _client, ConsoleIO _consoleIO,
            PermissionManager _permissions, ConfigurationManager _cfgMgr)
        {
            PermissionsManager = _permissions;
            CfgMgr = _cfgMgr;
            ShardedClient = _client;
            Writer = _consoleIO;

            LogMessage constructorLOG = new LogMessage(LogSeverity.Critical, "QDBService", "QuoteDBService constructor called.");
            Writer.WriteEntry(constructorLOG);
            if (doonce)
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Critical, "QDBService", "QuoteDBService Called again after DoOnce!"));
                //log the constructor duplication, to ensure Dev's pain and suffering while trying to figure out why this happens.
            }
            if (!doonce)
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Critical, "QDBService", "QuoteDBService Initializing..."));

                //TODO: Add any One-time initialization here.
                if(!Directory.Exists(dbdir))
                {
                    Directory.CreateDirectory(dbdir);
                }

                foreach (var item in Directory.GetFiles(dbdir,"*.qdb"))
                {
                    using (StreamReader sr = new StreamReader(item))
                    {
                        string json = sr.ReadToEnd();
                        GuildQuoteContainers.Add(JsonConvert.DeserializeObject<QuoteContainer>(json));
                    }
                }
                doonce = true;
            }
        }

        public async Task AddQuote(ICommandContext context, string quotetext, string quoteauthor,string quotedate)
        {
            if(context.Guild == null)
            {
                await context.Channel.SendMessageAsync("", false, 
                    GetEmbeddedMessage(context, "Invalid Context", "You can only do this from a guild/server.", Color.DarkRed));
                return;
            }
            var qdb = GuildQuoteContainers.FirstOrDefault(x => x.GuildID == context.Guild.Id);
            
            if ( qdb != null)
            {
                int qdbindex = GuildQuoteContainers.IndexOf(qdb);
                Writer.WriteEntry(new LogMessage(LogSeverity.Info, "QDB Add", $"Quote Container for {context.Guild.Name} was found"));
                qdb.Quotes.Add(new Quote() { Author = quoteauthor, Text = quotetext, DateTime = quotedate, Index = qdb.Latest + 1 });
                qdb.Latest++;
                //write new json file
                string json = JsonConvert.SerializeObject(qdb, Formatting.Indented);
                using (StreamWriter sw = new StreamWriter($"{dbdir}/{context.Guild.Id}.qdb"))
                {
                    sw.Write(json);
                    sw.Flush();
                }
                //update list item
                GuildQuoteContainers[qdbindex] = qdb;
                await context.Channel.SendMessageAsync("", false,
                    GetEmbeddedMessage(context, "Quote Added", $"Successfully added Quote #{qdb.Latest}.", Color.Green));
                return;
            }
            else
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Info, "QDB Add", $"Quote Container for {context.Guild.Name} was NOT found"));

                qdb = new QuoteContainer()
                {
                    GuildID = context.Guild.Id,
                    Latest = 0,
                    Quotes = new List<Quote>()
                };

                qdb.Quotes.Add(new Quote() { Author = quoteauthor, Text = quotetext, DateTime = quotedate, Index = qdb.Latest + 1 });
                qdb.Latest++;
                //write new json file
                string json = JsonConvert.SerializeObject(qdb, Formatting.Indented);
                using (StreamWriter sw = new StreamWriter($"{dbdir}/{context.Guild.Id}.qdb"))
                {
                    sw.Write(json);
                    sw.Flush();
                }
                //add to list.
                GuildQuoteContainers.Add(qdb);
                await context.Channel.SendMessageAsync("", false,
                    GetEmbeddedMessage(context, "Quote Added", $"Successfully added Quote #{qdb.Latest}.", Color.Green));
                return;

            }
        }

        public async Task ShowQuote(ICommandContext context,int index)
        {
            if (context.Guild == null)
            {
                await context.Channel.SendMessageAsync("", false,
                    GetEmbeddedMessage(context, "Invalid Context", "You can only do this from a guild/server.", Color.DarkRed));
                return;
            }

            var qdb = GuildQuoteContainers.FirstOrDefault(x => x.GuildID == context.Guild.Id);

            if (qdb != null)
            {
                
                Writer.WriteEntry(new LogMessage(LogSeverity.Info, "QDB Show", $"Quote Container for {context.Guild.Name} was found"));
                Quote Q = qdb.Quotes.FirstOrDefault(x => x.Index == index);
                if(Q != null)
                {
                    EmbedBuilder quotebuilder = new EmbedBuilder()
                    {
                        Title = $"Quote #{Q.Index}",
                        Description = $"{Q.Text}",
                        Footer = new EmbedFooterBuilder()
                        {
                            Text = $"{Q.Author} - {Q.DateTime}"
                        },
                        Color = new Color(18, 164, 238)
                    };
                    await context.Channel.SendMessageAsync("", false, quotebuilder.Build());
                }
                else
                {
                    await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Not Found", "This quote doesn't exist!", Color.DarkRed));
                    return;
                }
            }
            else
            {
                await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "No Quote DB", "This guild doesn't have a quoteDB.", Color.DarkRed));
                return;
            }
        }

        public async Task ShowQuote(ICommandContext context)
        {
            if (context.Guild == null)
            {
                await context.Channel.SendMessageAsync("", false,
                    GetEmbeddedMessage(context, "Invalid Context", "You can only do this from a guild/server.", Color.DarkRed));
                return;
            }

            var qdb = GuildQuoteContainers.FirstOrDefault(x => x.GuildID == context.Guild.Id);

            if (qdb != null)
            {

                Writer.WriteEntry(new LogMessage(LogSeverity.Info, "QDB Show", $"Quote Container for {context.Guild.Name} was found"));
                Random r = new Random();
                Quote Q = qdb.Quotes[r.Next(0, qdb.Quotes.Count)];
                if (Q != null)
                {
                    EmbedBuilder quotebuilder = new EmbedBuilder()
                    {
                        Title = $"Quote #{Q.Index}",
                        Description = $"{Q.Text}",
                        Footer = new EmbedFooterBuilder()
                        {
                            Text = $"{Q.Author} - {Q.DateTime}"
                        },
                        Color = new Color(18, 164, 238)
                    };
                    await context.Channel.SendMessageAsync("", false, quotebuilder.Build());
                }
                else
                {
                    await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Not Found", "This quote doesn't exist!", Color.DarkRed));
                    return;
                }
            }
            else
            {
                await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "No Quote DB", "This guild doesn't have a quoteDB.", Color.DarkRed));
                return;
            }
        }


        #region EMBED MESSAGES
        public static Embed GetEmbeddedMessage(ICommandContext Context, string title, string message, Color color, Exception e = null)
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(color);
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithTitle(title);
            b.WithDescription(message);
            b.WithFooter($"{Context.Client.CurrentUser.Username} • QuoteDBService");
            if (e != null)
            {
                b.AddField("Extended Details", e.Message);
                b.AddField("For developer", "See the Errors.LOG for more info!!!");
                Writer.WriteErrorsLog(e);
            }
            return b.Build();
        }

        #endregion EMBED MESSAGES
    }

    public class QuoteContainer
    {
        public ulong GuildID { get; set; }
        public int Latest { get; set; } = 0;

        public List<Quote> Quotes { get; set; } = new List<Quote>();
    }

    public class Quote
    {
        public string Text { get; set; }
        public string Author { get; set; }
        public string DateTime { get; set; }
        public int Index { get; set; } = 1;
    }
}
