using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

[Table("guild_users")]
[PrimaryKey(nameof(GuildId), nameof(UserId))]
public sealed record GuildUser(
    [property: Column("guild")] ulong GuildId,
    ulong UserId) : User(UserId)
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
    
    public List<Tag> Tags { get; init; }

    private static string CreateInitialBlurb()
        => Random.Shared.GetItems(InitialBlurbChoices, 1)[0];
}