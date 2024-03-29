﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ModularBOT.Component;
using Microsoft.Extensions.DependencyInjection;

namespace ModularBOT.Entity
{
    /// <summary>
    /// A configuration object that is added to another object. Must inherit base
    /// </summary>
    public class ConfigEntity
    {
        public string ConfigIdentifier { get; protected set; }

        public bool ReadOnly { get; protected set; }

        /// <summary>
        /// Override in derived class.
        /// </summary>
        /// <param name="inline"></param>
        /// <returns></returns>
        public virtual EmbedFieldBuilder ExecuteView(DiscordNET _DiscordNet, ICommandContext Context, bool inline)
        {
            EmbedFieldBuilder ef = new EmbedFieldBuilder()
            {
                IsInline = inline,
                Name = ConfigIdentifier,
                Value = $"`NOT SPECIFIED`"
            };
            return ef;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_DiscordNet"></param>
        /// <param name="Context"></param>
        /// <param name="inline"></param>
        /// <returns></returns>
        public virtual string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context,string value)
        {
            string np = "• "+ ConfigIdentifier.PadRight(28, ' ') + " :: "+value;

            return np;
        }

        public virtual string ExecuteView(DiscordNET _DiscordNet, ICommandContext Context)
        {
            string np = "• " + ConfigIdentifier.PadRight(28, ' ') + " :: NOT SPECIFIED";

            return np;
        }


        /// <summary>
        /// Override in derived class. call this base method to send confirmation.
        /// </summary>
        /// <param name="NewValue">the new parameter for the setting.</param>
        public virtual async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _discordNET, ICommandContext Context, string value)
        {
            await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(
                _discordNET.serviceProvider.GetRequiredService<ConsoleIO>(),
                Context,
                "Configuration Updated", $"Successfully Updated value of `{ConfigIdentifier}` to `{value}`", Color.Green));
            return;
        }

        #region Messages
        public Embed GetEmbeddedMessage(ConsoleIO ConsoleIO, ICommandContext Context, string title, string message, Color color, Exception e = null)
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(color);
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithTitle(title);
            b.WithDescription(message);
            b.WithFooter("ModularBOT • Core");
            if (e != null)
            {
                b.AddField("Extended Details", e.Message);
                b.AddField("For developer", "See the Errors.LOG for more info!!!");
                ConsoleIO.WriteErrorsLog(e);
            }
            return b.Build();
        }
        #endregion
    }
}
