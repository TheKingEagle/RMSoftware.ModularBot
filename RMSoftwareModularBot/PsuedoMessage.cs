using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMSoftware.ModularBot
{
    public class PsuedoMessage : IMessage, IUserMessage
    {
        string _content = "";
        SocketUser _author;
        IGuildChannel _c;
        MessageSource _source;
        MessageType _type;
        public PsuedoMessage(string content, SocketUser author, IGuildChannel ch, MessageSource source)
        {
            _content = content;
            _author = author;
            _c = ch;
            _source = source;
            _type = MessageType.Default;
        }
 

        IReadOnlyCollection<IAttachment> IMessage.Attachments
        {
            get;
        }

        IUser IMessage.Author
        {
            get { return _author; }
        }

        IMessageChannel IMessage.Channel
        {
            get { return _c as IMessageChannel; }
        }

        string IMessage.Content
        {
            get { return _content; }
        }

        DateTimeOffset ISnowflakeEntity.CreatedAt
        {
            get;
        }

        DateTimeOffset? IMessage.EditedTimestamp
        {
            get;
        }

        IReadOnlyCollection<IEmbed> IMessage.Embeds
        {
            get;
        }

        ulong IEntity<ulong>.Id
        {
            get { return (ulong)new Random().Next(0, int.MaxValue); }
        }

        bool IMessage.IsPinned
        {
            get { return false; }
        }

        bool IMessage.IsTTS
        {
            get { return false; }
        }

        IReadOnlyCollection<ulong> IMessage.MentionedChannelIds
        {
            get;
        }

        IReadOnlyCollection<ulong> IMessage.MentionedRoleIds
        {
            get;
        }

        IReadOnlyCollection<ulong> IMessage.MentionedUserIds
        {
            get;
        }

        MessageSource IMessage.Source
        {
            get { return _source; }
        }

        IReadOnlyCollection<ITag> IMessage.Tags
        {
            get;
        }

        DateTimeOffset IMessage.Timestamp
        {
            get;
        }

        MessageType IMessage.Type
        {
            get { return _type; }
        }

        public IReadOnlyDictionary<IEmote, ReactionMetadata> Reactions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Task IDeletable.DeleteAsync(RequestOptions options)
        {
            return Task.Delay(0);
        }

        public Task ModifyAsync(Action<MessageProperties> func, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task PinAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task UnpinAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task AddReactionAsync(IEmote emote, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task RemoveReactionAsync(IEmote emote, IUser user, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAllReactionsAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IUser>> GetReactionUsersAsync(string emoji, int limit = 100, ulong? afterUserId = default(ulong?), RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public string Resolve(TagHandling userHandling = TagHandling.Name, TagHandling channelHandling = TagHandling.Name, TagHandling roleHandling = TagHandling.Name, TagHandling everyoneHandling = TagHandling.Ignore, TagHandling emojiHandling = TagHandling.Name)
        {
            throw new NotImplementedException();
        }
    }
}
