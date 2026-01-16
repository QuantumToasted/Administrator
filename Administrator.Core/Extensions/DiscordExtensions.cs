using System.Text;
using Disqord;
using NodaTime;
using Qommon;

namespace Administrator.Core;

public static class DiscordExtensions
{
    extension<TEntity>(TEntity entity) where TEntity : INamableEntity, ISnowflakeEntity
    {
        public string Format(bool bold = true, bool code = true)
        {
            // **name** (`id`)
            return new StringBuilder()
                .Append(bold ? Markdown.Bold(Markdown.Escape(entity.Name)) : entity.Name)
                .Append(code ? Markdown.Code(entity.Id) : entity.Id)
                .ToString();
        }
    }
    
    extension<TUser>(TUser user) where TUser : IUser
    {
        public string DisplayName => (user as IMember)?.Nick ?? user.GlobalName ?? user.Name;
    }

    extension<TRole>(TRole role) where TRole : IRole
    {
        public bool CanBeGrantedOrRevoked
        {
            get
            {
                var isSubscriptionRole = (role.Tags as TransientRoleTags)?.Model.PremiumSubscriber.GetValueOrDefault() ?? false;

                return !isSubscriptionRole &&
                       !role.IsManaged &&
                       !role.Tags.BotId.HasValue &&
                       !role.Tags.IntegrationId.HasValue &&
                       !role.Tags.IsNitroBooster;
            }
        }
    }

    extension<TEntity>(TEntity entity) where TEntity : IIdentifiableEntity
    {
        public Instant CreatedAt => Instant.FromDateTimeOffset(entity.CreatedAt());
    }

    extension<TEmbed>(TEmbed embed) where TEmbed : LocalEmbed
    {
        public TEmbed WithTimestamp(Instant instant)
            => embed.WithTimestamp(instant.ToDateTimeOffset());
    }
}