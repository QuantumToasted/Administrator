using System.Text;
using Administrator.Bot.AutoComplete;
using Backpack.Net;
using Disqord;
using Disqord.Bot.Commands.Application;
using Humanizer;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

public enum ItemType
{
    [System.ComponentModel.Description("Craftable")]
    Craftable,
    [System.ComponentModel.Description("Non-Craftable")]
    NonCraftable
}

public enum CurrencyType
{
    RefinedMetal,
    CrateKeys,
    Earbuds,
    CraftHats
}

public sealed partial class BackpackModule(BackpackService backpack, AutoCompleteService autoComplete, SlashCommandMentionService mentions)
    : DiscordApplicationModuleBase
{
    private static readonly ParticleEffect[] ParticleEffects = Enum.GetValues<ParticleEffect>();
    private readonly List<ItemAutoCompleteFormatter.TF2Item> _items = backpack.ItemPrices.Select(x => new ItemAutoCompleteFormatter.TF2Item(x.Key, x.Value)).ToList();

    public partial IResult Profile(BackpackUser user)
    {
        var embed = new LocalEmbed()
            .WithUnusualColor()
            .WithTitle($"{user.Name}'s backpack.tf profile")
            .WithThumbnailUrl(user.AvatarUrl.ToString())
            .AddField("Steam ID", user.Id)
            .AddField("Last logged in", user.LastOnline.HasValue
                ? Markdown.Timestamp(user.LastOnline.Value, Markdown.TimestampFormat.RelativeTime)
                : "This user has never logged in to backpack.tf.")
            .AddField("User attributes", user.Flags == BackpackUserFlags.None
                ? "None"
                : string.Join(", ", user.Flags.GetFlagValues().Select(x => x.Humanize(LetterCasing.Title))));

        if (user.AmountDonated != default)
        {
            embed.AddField("Amount donated", $"{user.AmountDonated:C} USD");
        }

        if (user.PremiumMonthsGifted != default)
        {
            embed.AddField("Months of Premium gifted", user.PremiumMonthsGifted);
        }

        if (user.Trust?.Negative > 0 || user.Trust?.Positive > 0)
        {
            var netTrust = user.Trust.Positive - user.Trust.Negative;

            embed.AddField("Trust score",
                $"{(netTrust > 0 ? "+" : string.Empty)}{netTrust} (+{user.Trust.Positive}/-{user.Trust.Negative})");
        }


        if (backpack.CrateKeyCurrency is null)
        {
            embed.AddField("Inventory value", "backpack.tf currency data has not yet been loaded. Please be patient.");
        }
        else
        {
            var inventoryValue = user.Inventory.Keys * backpack.CrateKeyCurrency.Price.Value +
                                 user.Inventory.Metal +
                                 user.Inventory.Value;

            embed.AddField("Inventory value", $"{inventoryValue / backpack.CrateKeyCurrency.Price.Value:N2} keys " +
                                              $"({inventoryValue:N2} refined metal, {inventoryValue * backpack.RefinedMetalCurrency!.Price.Value:C} USD)");
        }


        if (user.SiteBans.Count > 0 ||
            user.Flags.HasFlag(BackpackUserFlags.SteamCommunityBanned) ||
            user.Flags.HasFlag(BackpackUserFlags.SteamRepCaution) ||
            user.Flags.HasFlag(BackpackUserFlags.SteamRepScammer) ||
            user.Flags.HasFlag(BackpackUserFlags.SteamVACBanned) ||
            user.Flags.HasFlag(BackpackUserFlags.SteamEconomyBanned) ||
            user.Flags.HasFlag(BackpackUserFlags.ValveGameBanned))
        {
            embed.WithCollectorsColor()
                .WithFooter($"Take caution! This user may have bans or be a scammer.\n" +
                            $"Use {mentions.GetMention("backpack bans")} to view more info.");
        }

        return Response(embed);
    }

    public partial IResult Bans(BackpackUser user)
    {
        var embed = new LocalEmbed()
            .WithTitle($"backpack.tf ban information for {user.Name}")
            .WithThumbnailUrl(user.AvatarUrl.ToString());

        var descriptionBuilder = new StringBuilder();

        if (user.Flags.HasFlag(BackpackUserFlags.SteamCommunityBanned))
            descriptionBuilder.AppendLine($"This user has a {Markdown.Bold("Steam Community ban.")}");
        if (user.Flags.HasFlag(BackpackUserFlags.SteamEconomyBanned))
            descriptionBuilder.AppendLine($"This user has a {Markdown.Bold("Steam Economy ban.")}");
        if (user.Flags.HasFlag(BackpackUserFlags.SteamVACBanned))
            descriptionBuilder.AppendLine($"This user has a {Markdown.Bold("Steam VAC ban.")}");
        if (user.Flags.HasFlag(BackpackUserFlags.ValveGameBanned))
            descriptionBuilder.AppendLine($"This user has a {Markdown.Bold("Steam Game ban")} from one or more games.");
        if (user.Flags.HasFlag(BackpackUserFlags.SteamRepCaution))
            descriptionBuilder.AppendLine($"This user has has a {Markdown.Bold("Caution")} flag on their SteamRep profile.");
        if (user.Flags.HasFlag(BackpackUserFlags.SteamRepScammer))
            descriptionBuilder.AppendLine($"This user has has been marked as a {Markdown.Bold("Scammer")} on their SteamRep.");

        foreach (var ban in user.SiteBans)
        {
            embed.AddField($"Site ban from " +
                $"{(ban.Type == SiteBanType.All ? "all bp.tf features" : ban.Type.Humanize(LetterCasing.LowerCase))}",
                $"{ban.Reason?.Truncate(250) ?? "No reason provided."}\n" +
                (ban.Expires.HasValue ? $"Expires {Markdown.Timestamp(ban.Expires.Value, Markdown.TimestampFormat.RelativeTime)}" : "(permanent ban)"));              
        }

        if (descriptionBuilder.Length > 0 || embed.Fields.GetValueOrDefault()?.Count > 0)
        {
            embed.WithCollectorsColor();
            
            if (descriptionBuilder.Length > 0)
                embed.WithDescription(descriptionBuilder.ToString());
        }
        else
        {
            embed.WithUnusualColor()
                .WithDescription("No Steam or site bans could be found for this user.");
        }

        return Response(embed);
    }

    public partial IResult Convert(double amount, CurrencyType type)
    {
        if (backpack.CraftHatCurrency is null)
            return Response("Please wait for backpack.tf currency data to update.").AsEphemeral();
        
        switch (type)
        {
            case CurrencyType.Earbuds:
                amount = Math.Floor(amount);
                var rawValue = backpack.EarbudsCurrency!.Price.RawValue!.Value * (decimal) amount;
                var keyValue = rawValue / backpack.CrateKeyCurrency!.Price.Value;
                var usdValue = rawValue * backpack.RefinedMetalCurrency!.Price.Value;

                return Response(
                    $"{amount:N2} earbud(s) are currently worth:\n" +
                    $"{rawValue:N2} refined metal.\n" +
                    $"{keyValue:N2} keys/pure.\n" +
                    $"{usdValue:C} USD.");
            case CurrencyType.CraftHats:
                amount = Math.Floor(amount);

                rawValue = backpack.CraftHatCurrency.Price.RawValue!.Value * (decimal) amount;
                keyValue = rawValue / backpack.CrateKeyCurrency!.Price.Value;
                usdValue = rawValue * backpack.RefinedMetalCurrency!.Price.Value;

                return Response(
                    $"{amount:N2} craft hats are currently worth:\n" +
                    $"{rawValue:N2} refined metal.\n" +
                    $"{keyValue:N2} keys/pure.\n" +
                    $"{usdValue:C} USD.");
            case CurrencyType.CrateKeys:
                rawValue = backpack.CrateKeyCurrency!.Price.RawValue!.Value * (decimal) amount;
                usdValue = rawValue * backpack.RefinedMetalCurrency!.Price.Value;

                return Response(
                    $"{amount:N2} keys/pure are worth:\n" +
                    $"{rawValue:N2} refined metal.\n" +
                    $"{usdValue:C} USD.");
            case CurrencyType.RefinedMetal:
                var wholeAmount = Math.Truncate(amount);
                var decimals = amount - wholeAmount;

                amount = decimals switch
                {
                    < 0.66d and >= 0.33d => wholeAmount + 0.33d,
                    <= 0.99d and > 0.33d => wholeAmount + 0.66d,
                    _ => wholeAmount
                };

                //usdValue = backpack.RefinedMetalCurrency!.Price.Value * (decimal)amount;
                //rawValue = backpack.CrateKeyCurrency.Price.Value
                keyValue = (decimal) amount / backpack.CrateKeyCurrency!.Price.Value;
                usdValue = (decimal) amount * backpack.RefinedMetalCurrency!.Price.Value;

                return Response(
                    $"{amount:N2} refined metal are worth:\n" +
                    $"{keyValue:N2} keys/pure.\n" +
                    $"{usdValue:C} USD.");
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public partial async Task<IResult> PriceCheck(string itemName, Quality quality, ParticleEffect? particleEffect, ItemType type)
    {
        if (backpack.ItemPrices.Count == 0)
            return Response("Please wait for the backpack.tf item price data to update.").AsEphemeral();
        
        if (!backpack.ItemPrices.TryGetValue(itemName, out var item))
            return Response($"No item could be found with the name \"{itemName}\"!").AsEphemeral();

        if (particleEffect.HasValue)
            quality = Quality.Unusual;

        if (!item.Qualities.TryGetValue(quality, out var itemPrice))
        {
            return Response($"There is no pricing information for a {Markdown.Bold(quality)} quality \"{itemName}\".\n" +
                            $"Available options are {string.Join(", ", item.Qualities.Keys.Select(x => Markdown.Bold(x)))}.");
        }

        var dict = type == ItemType.Craftable
            ? itemPrice.Craftable
            : itemPrice.NonCraftable;

        string remoteUrl;
        string title;
        Price price;
        if (particleEffect.HasValue)
        {
            if (!dict.TryGetValue(PriceIndex.Unusual(particleEffect.Value), out var unusualPrice))
            {
                if (type == ItemType.Craftable)
                {
                    return await PriceCheck(itemName, quality, particleEffect, ItemType.NonCraftable);
                }

                return Response(
                    $"There is no pricing information for a {Markdown.Bold(particleEffect.Value.ToString("G").TrimStart('_'))} unusual effect.\n" +
                    $"Available options are {string.Join(", ", dict.Keys.OfType<UnusualPriceIndex>().Select(x => Markdown.Bold(x.ParticleEffect.ToString("G"))))}.");
            }

            price = unusualPrice;
            remoteUrl = $"https://backpack.tf/stats/{quality}/{Uri.EscapeDataString(itemName)}/Tradable/{type.Humanize()}/{(int) particleEffect.Value}";
            title = $"Price information for {particleEffect.Value.Humanize(LetterCasing.Title).TrimStart('_')} {itemName}";
        }
        else
        {
            if (!dict.TryGetValue(PriceIndex.Default, out var normalPrice))
            {
                if (type == ItemType.Craftable)
                {
                    return await PriceCheck(itemName, quality, particleEffect, ItemType.NonCraftable);
                }

                return Response("Price not found. Prince index debug: \n" +
                                string.Join(", ", dict.Keys));
            }

            price = normalPrice;
            remoteUrl = $"https://backpack.tf/stats/{quality}/{Uri.EscapeDataString(itemName)}/Tradable/{type.Humanize()}";
            title = $"Price information for {quality} quality {itemName}";
        }

        await Deferral();
        var attachment = await backpack.GetItemImageAsync(item.DefinitionIndexes[0], particleEffect);
        
        var response = new LocalInteractionMessageResponse()
            .AddComponent(new LocalRowComponent()
                .AddComponent(new LocalLinkButtonComponent()
                    .WithLabel("View on backpack.tf")
                    .WithUrl(remoteUrl)));

        var embed = new LocalEmbed()
            .WithQualityColor(quality)
            .WithTitle(title)
            .AddField("Value",
                $"{price.RawValue:N2} refined\n" +
                $"{price.RawValue / backpack.CrateKeyCurrency.Price.RawValue:N2} keys\n" +
                $"{price.RawValue * backpack.RefinedMetalCurrency.Price.Value:C} USD")
            .WithFooter("This price data is from")
            .WithTimestamp(price.LastUpdate);
        
        response.AddAttachment(attachment);
        embed.WithThumbnailUrl($"attachment://{attachment.FileName.Value}");
        return Response(response.AddEmbed(embed));
    }

    public partial void AutoCompleteItems(AutoComplete<string> itemName, AutoComplete<string> particleEffect)
    {
        if (itemName.IsFocused)
            autoComplete.AutoComplete(itemName, _items);

        if (particleEffect.IsFocused)
            autoComplete.AutoComplete(particleEffect, ParticleEffects);
    }
}