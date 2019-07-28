using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ModularBOT.Component;

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
        /// Override in derived class.
        /// </summary>
        /// <param name="NewValue">the new parameter for the setting.</param>
        public virtual async Task ExecuteSet(DiscordShardedClient Client, DiscordNET _discordNET, ICommandContext Context, string value)
        {
            await Task.Delay(1);
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
