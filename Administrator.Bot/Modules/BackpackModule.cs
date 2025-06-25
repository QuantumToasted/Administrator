using Backpack.Net;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("backpack")]
public sealed partial class BackpackModule
{
    [SlashCommand("profile")]
    [Description("Gets a user's basic backpack.tf profile information, if they have any.")]
    public partial IResult Profile(
        [Description("The SteamID64 or profile link of the user you wish to view information for.")]
            BackpackUser user);

    [SlashCommand("bans")]
    [Description("Get detailed Steam and backpack.tf site ban information about a user.")]
    public partial IResult Bans(
        [Description("The SteamID64 or profile link of the user you wish to view information for.")]
            BackpackUser user);

    [SlashCommand("convert")]
    [Description("Converts a certain amount of one currency to another.")]
    public partial IResult Convert(
        [Description("The amount of the target currency.")] 
        [Range(0.01d, 1_000_000d)]
            double amount,
        [Description("The type of currency.")] 
            CurrencyType type);

    [SlashCommand("price-check")]
    [Description("Checks the price of an item.")]
    public partial Task<IResult> PriceCheck(
        [Description("The full name of the item.")]
            string itemName,
        [Description("The quality of the item. Default: Unique")]
            Quality quality = Quality.Unique,
        [Description("The particle effect of a particular Unusual item. Forces quality to be Unusual.")]
            ParticleEffect? particleEffect = null,
        [Description("Whether to search for Craftable or Non-Craftable prices. Default: Craftable")]
            ItemType type = ItemType.Craftable);

    [AutoComplete("price-check")]
    public partial void AutoCompleteItems(AutoComplete<string> itemName, AutoComplete<string> particleEffect);
}