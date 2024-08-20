using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record Member(Snowflake GuildId, Snowflake UserId) : UserBase(UserId)
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
    
    public string Blurb { get; set; } = CreateInitialBlurb();
    
    public DateTimeOffset? NextDemeritPointDecay { get; set; }
    
    //public int DemeritPoints { get; set; }
    
#pragma warning disable CS8618
    public List<Tag> Tags { get; init; }
#pragma warning restore CS8618

    private static string CreateInitialBlurb()
        => Random.Shared.GetItems(InitialBlurbChoices, 1)[0];

    private sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
    {
        public void Configure(EntityTypeBuilder<Member> user)
        {
            user.HasKey(x => new { x.GuildId, x.UserId });
            user.HasMany(x => x.Tags).WithOne(x => x.Owner).HasForeignKey(x => new { x.GuildId, x.OwnerId }).OnDelete(DeleteBehavior.NoAction);
        }
    }
}