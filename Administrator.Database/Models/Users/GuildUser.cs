using System.ComponentModel.DataAnnotations.Schema;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Administrator.Database;

public sealed record GuildUser(Snowflake GuildId, Snowflake UserId) : User(UserId), IStaticEntityTypeConfiguration<GuildUser>
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
    
    [Column("blurb")] 
    public string Blurb { get; set; } = CreateInitialBlurb();
    
    public Guild? Guild { get; init; }
    
#pragma warning disable CS8618
    public List<Tag> Tags { get; init; }
#pragma warning restore CS8618

    private static string CreateInitialBlurb()
        => Random.Shared.GetItems(InitialBlurbChoices, 1)[0];

    static void IStaticEntityTypeConfiguration<GuildUser>.ConfigureBuilder(EntityTypeBuilder<GuildUser> user)
    {
        user.ToTable("guild_users");
        user.HasKey(x => new { x.GuildId, x.UserId });

        user.HasPropertyWithColumnName(x => x.GuildId, "guild");
        user.HasPropertyWithColumnName(x => x.UserId, "user");
        user.HasPropertyWithColumnName(x => x.Blurb, "blurb");

        user.HasMany(x => x.Tags).WithOne(x => x.Owner).HasForeignKey(x => new { x.GuildId, x.OwnerId });
    }
}