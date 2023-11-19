using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
namespace ModularBOT.Entity
{
    internal class PseudoMessage : IMessage, IUserMessage
    {
        string _content = "";
        SocketUser _author;
        IGuildChannel _c;
        MessageSource _source;
        MessageType _type;
        public PseudoMessage(string content, SocketUser author, IGuildChannel ch, MessageSource source)
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
            get;
        }

        public MessageActivity Activity;

        public MessageApplication Application;

        public bool IsSuppressed = false;

        public MessageReference Reference;

        public bool MentionedEveryone = false;

        public IReadOnlyCollection<ISticker> Stickers;

        public MessageFlags? Flags => MessageFlags.None;

        public IUserMessage ReferencedMessage => null;

        bool IMessage.IsSuppressed => false;

        bool IMessage.MentionedEveryone => false;

        MessageActivity IMessage.Activity => new MessageActivity();

        MessageApplication IMessage.Application => new MessageApplication();

        MessageReference IMessage.Reference => null;


        public string CleanContent => throw new NotImplementedException();

        public IThreadChannel Thread => throw new NotImplementedException();

        public IReadOnlyCollection<IMessageComponent> Components => throw new NotImplementedException();

        IReadOnlyCollection<IStickerItem> IMessage.Stickers => throw new NotImplementedException();

        public IMessageInteraction Interaction => throw new NotImplementedException();

        public MessageRoleSubscriptionData RoleSubscriptionData => throw new NotImplementedException();

        public MessageResolvedData ResolvedData => throw new NotImplementedException();

        Task IDeletable.DeleteAsync(RequestOptions options)
        {
            return Task.Delay(0);
        }

        public Task ModifyAsync(Action<MessageProperties> func, RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public Task PinAsync(RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public Task UnpinAsync(RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public Task AddReactionAsync(IEmote emote, RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public Task RemoveReactionAsync(IEmote emote, IUser user, RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public Task RemoveAllReactionsAsync(RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<IUser>> GetReactionUsersAsync(string emoji, int limit = 100, ulong? afterUserId = default(ulong?), RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public string Resolve(TagHandling userHandling = TagHandling.Name, TagHandling channelHandling = TagHandling.Name, TagHandling roleHandling = TagHandling.Name, TagHandling everyoneHandling = TagHandling.Ignore, TagHandling emojiHandling = TagHandling.Name)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetReactionUsersAsync(IEmote emoji, int limit, RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public Task RemoveReactionAsync(IEmote emote, ulong userId, RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public Task ModifySuppressionAsync(bool suppressEmbeds, RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public Task RemoveAllReactionsForEmoteAsync(IEmote emote, RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public Task CrosspostAsync(RequestOptions options = null)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetReactionUsersAsync(IEmote emoji, int limit, RequestOptions options = null, ReactionType type = ReactionType.Normal)
        {
            throw new NotImplementedException();
        }
    }
}
