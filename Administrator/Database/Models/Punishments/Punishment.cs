using System;
using System.Threading.Tasks;
using Administrator.Common;
using Disqord;
using Disqord.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database
{
    public abstract class Punishment : Keyed, 
        IEntityTypeConfiguration<Punishment>,
        IGuildDbEntity,
        IUserDbEntity
    {
        public Snowflake GuildId { get; set; }
        
        public Snowflake TargetId { get; set; }
        
        public string TargetTag { get; set; }
        
        public Snowflake ModeratorId { get; set; }
        
        public string ModeratorTag { get; set; }
        
        public string Reason { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; }
        
        public Snowflake LogMessageId { get; set; }
        
        public Snowflake LogChannelId { get; set; }
        
        public Upload Attachment { get; set; }

        public abstract Task<LocalMessage> FormatLogMessageAsync(DiscordBotBase bot);

        public abstract Task<LocalMessage> FormatDmMessageAsync(DiscordBotBase bot);

        public void SetLogMessage(IUserMessage message)
        {
            LogMessageId = message.Id;
            LogChannelId = message.ChannelId;
        }
        
        void IEntityTypeConfiguration<Punishment>.Configure(EntityTypeBuilder<Punishment> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            
            builder.HasDiscriminator<string>("punishment_type")
                .HasValue<Ban>("ban")
                .HasValue<Kick>("kick")
                .HasValue<Mute>("mute")
                .HasValue<Warning>("warning");
        }

        Snowflake IUserDbEntity.UserId
        {
            get => TargetId;
            set => TargetId = value;
        }
    }
}