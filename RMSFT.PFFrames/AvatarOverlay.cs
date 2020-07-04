using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ModularBOT;
using ModularBOT.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMSFT.PFFrames
{
    //[Group("frame")]
    [Summary("TODO: Describe your module.")]
    public class AvatarOverlay : ModuleBase
    {
        #region PUBLIC INJECTED COMPONENTS

        public DiscordShardedClient _client { get; set; }
        public ConsoleIO _writer { get; set; }
        public AvatarService _service { get; set; }

        public PermissionManager _permissions { get; set; }

        public ConfigurationManager _configmgr { get; set; }

        #endregion PUBLIC INJECTED COMPONENTS

        public AvatarOverlay(DiscordShardedClient discord, AvatarService service,
            ConsoleIO writer, PermissionManager manager, ConfigurationManager cnfgmgr)
        {
            _client = discord;
            _service = service;
            _writer = writer;
            _permissions = manager;
            _configmgr = cnfgmgr;
            //Ensure module was injected properly.
            _writer.WriteEntry(new LogMessage(LogSeverity.Critical, "PFFrames", "AvatarOverlayFrame Constructor called!"));
        }

        #region PUBLIC COMMANDS

        //TODO: Implement Custom commands with [Command("CommandName")...]

        #region ABOUT
        [Group("PFFrames")]
        public class _AvatarOverlay : ModuleBase
        {
            [Command("about")]
            public async Task DisplayAbout()
            {
                EmbedBuilder eb = new EmbedBuilder()
                {
                    Title = "About PFFrames",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl(ImageFormat.Auto),
                        Text = $"Requested By: {Context.User.Username} • PFFrames"
                    },
                    Author = new EmbedAuthorBuilder()
                    {
                        IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto),
                        Name = $"{Context.Client.CurrentUser.Username}#{Context.Client.CurrentUser.Discriminator}"
                    },
                    //TODO: Edit your module's description in the dedicated About command
                    Description = "This module allows you to create a customized version of current avatar with a frame, or overlay." +
                    "Choose one of many designs, or bring your own. No fancy editing skills needed!\r\n\r\n" +
                    "TODO: Literally EVERYTHING.", 
                    
                    Color = Color.Purple
                };
                await ReplyAsync("", false, eb.Build());
            }
        }
        #endregion

        #endregion
    }

    public class AvatarService
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

        #endregion

        #region PUBLIC PROPERTIES
        //TODO: Add custom public properties here.

        #endregion

        public AvatarService(DiscordShardedClient _client, ConsoleIO _consoleIO,
            PermissionManager _permissions, ConfigurationManager _cfgMgr)
        {
            PermissionsManager = _permissions;
            CfgMgr = _cfgMgr;
            ShardedClient = _client;
            Writer = _consoleIO;

            LogMessage constructorLOG = new LogMessage(LogSeverity.Critical, "Avatar", "AvatarService constructor called.");
            Writer.WriteEntry(constructorLOG);

            if (doonce)
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Critical, "Avatar", "AvatarService Called again after DoOnce!"));
                //log the constructor duplication, to ensure Dev's pain and suffering while trying to figure out why this happens.
            }
            if (!doonce)
            {
                Writer.WriteEntry(new LogMessage(LogSeverity.Critical, "Avatar", "AvatarService Called again after DoOnce!"));

                //TODO: Add any One-time initialization here.

                doonce = true;
            }
        }
    }
}
