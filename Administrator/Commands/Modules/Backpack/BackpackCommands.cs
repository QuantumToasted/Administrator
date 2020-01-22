using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administrator.Extensions;
using Backpack.Net;
using Disqord;
using Humanizer;
using Humanizer.Localisation;
using Qmmands;

namespace Administrator.Commands.Backpack
{
    [Name("Backpack")]
    [Group("backpack", "bp")]
    public sealed class BackpackCommands : AdminModuleBase
    {
        public BackpackClient BackpackClient { get; set; }

        [Command("", "profile")]
        public async ValueTask<AdminCommandResult> GetBackpackProfileAsync([Remainder] BackpackUser user)
        {
            var now = DateTimeOffset.UtcNow;
            var builder = new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithTitle(Localize("backpack_info_title", user.Name))
                .WithThumbnailUrl(user.AvatarUrl.ToString())
                .AddField(Localize("info_id"), user.Id)
                .AddField(Localize("backpack_info_lastonline"),
                    string.Join('\n', user.LastOnline.ToString("g", Context.Language.Culture),
                        (now - user.LastOnline).HumanizeFormatted(Localization, Context.Language, TimeUnit.Second,
                            true)))
                .AddField(Localize("backpack_info_flags"),
                    user.Flags == BackpackUserFlags.None
                        ? Localize("info_none")
                        : user.Flags.Humanize(LetterCasing.Title));

            if (user.AmountDonated != default)
            {
                builder.AddField(Localize("backpack_info_donated"), $"${user.AmountDonated:F} USD");
            }

            if (user.PremiumMonthsGifted != default)
            {
                builder.AddField(Localize("backpack_info_premiumgifts"), user.PremiumMonthsGifted);
            }

            if (user.Trust.Negative != default || user.Trust.Positive != default)
            {
                var netTrust = user.Trust.Positive - user.Trust.Negative;

                builder.AddField(Localize("backpack_info_trust"),
                    $"{(netTrust > 0 ? "+" : string.Empty)}{netTrust} (+{user.Trust.Positive}/-{user.Trust.Negative})");
            }

            var currencies = await BackpackClient.GetCurrenciesAsync();
            var inventoryValue = user.Inventory.Keys * currencies.CrateKey.Price.Value + user.Inventory.Metal +
                                 user.Inventory.Value;

            builder.AddField(Localize("backpack_info_worth"),
                Localize("backpack_info_worth_text", (inventoryValue / currencies.CrateKey.Price.Value).ToString("F"), 
                    inventoryValue.ToString("F"),
                    (inventoryValue * currencies.RefinedMetal.Price.Value).ToString("F")));


            if (!user.SiteBans.IsDefaultOrEmpty ||
                user.Flags.HasFlag(BackpackUserFlags.SteamCommunityBanned) ||
                user.Flags.HasFlag(BackpackUserFlags.SteamRepCaution) ||
                user.Flags.HasFlag(BackpackUserFlags.SteamRepScammer) ||
                user.Flags.HasFlag(BackpackUserFlags.SteamVACBanned) ||
                user.Flags.HasFlag(BackpackUserFlags.SteamEconomyBanned) ||
                user.Flags.HasFlag(BackpackUserFlags.ValveGameBanned))
            {
                builder.WithErrorColor()
                    .WithFooter(Localize("backpack_info_caution",
                        Markdown.Code($"{Context.Prefix}backpack bans <user>")));
            }
                

            return CommandSuccess(embed: builder.Build());
        }

        /*
        [Command("bans")]
        public async ValueTask<AdminCommandResult> GetBackpackBansAsync([Remainder] BackpackUser user)
        {

        }

        [Command("convert")]
        public async ValueTask<AdminCommandResult> ConvertCurrencyAsync(decimal value)
        { }
        */
    }
}