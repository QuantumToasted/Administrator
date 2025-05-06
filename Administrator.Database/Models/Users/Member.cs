using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Member : IUserXp, IMemberConfiguration, IEntityTypeConfiguration<Member>
{
    private static readonly string[] InitialBlurbChoices =
    {
        "Let's go whack some chuckleheads.",
        "Ready for active duty!",
        "Mmph mphna mprh.",
        "Stand back and watch how it's done, lads!",
        "Run and hide, babies!",
        "This wrench ain't gonna swing itself!",
        "I feel like a million Deutschmarks!",
        "Let's get to work.",
        "They'll never see us coming!"
    };
    
    public Snowflake UserId { get; init; }
    
    public int TotalXp { get; set; }
    
    public DateTimeOffset LastXpGain { get; set; }
    
    public DateTimeOffset LastLevelUp { get; set; }
    
    public Snowflake GuildId { get; init; }

    public string Blurb { get; set; } = null!;
    
    public DateTimeOffset? NextDemeritPointDecay { get; set; }
    
#pragma warning disable CS8618
    public List<Tag> Tags { get; init; }
#pragma warning restore CS8618

    public static Member Create(IMember member) => Create(member.GuildId, member.Id);
    public static Member Create(Snowflake guildId, Snowflake userId)
    {
        return new Member
        {
            UserId = userId,
            GuildId = guildId,
            Blurb = new Random(userId.GetHashCode()).GetItems(InitialBlurbChoices, 1)[0]
        };
    }

    void IEntityTypeConfiguration<Member>.Configure(EntityTypeBuilder<Member> member)
    {
        member.HasKey(x => new { x.GuildId, x.UserId });
        member.Property(x => x.Blurb).HasMaxLength(50);
        member.HasMany(x => x.Tags).WithOne(x => x.Owner).HasForeignKey(x => new { x.GuildId, x.OwnerId }).OnDelete(DeleteBehavior.NoAction);
    }
}