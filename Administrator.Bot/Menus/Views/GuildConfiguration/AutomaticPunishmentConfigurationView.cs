using System.Text;
using Administrator.Bot.AutoComplete;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Bot;

public sealed class AutomaticPunishmentConfigurationView : GuildConfigurationViewBase
{
    public const string SELECTION_TEXT = "Automatic Punishments";
    private readonly TimeSpanTypeParser _parser = new();
    private readonly SelectionViewComponent _countSelection;
    private readonly SelectionViewComponent _punishmentTypeSelection;
    private readonly SelectionViewComponent _durationSelection;
    
    private int? _count;
    private PunishmentType? _type;
    private ICollection<AutomaticPunishment> _automaticPunishments;

    public AutomaticPunishmentConfigurationView(IDiscordApplicationGuildCommandContext context, ICollection<AutomaticPunishment> existingAutomaticPunishments)
        : base(context)
    {
        _countSelection = new SelectionViewComponent(SelectCountAsync)
        {
            Placeholder = "Number of demerit points..."
        };

        _punishmentTypeSelection = new SelectionViewComponent(SelectPunishmentTypeAsync)
        {
            Placeholder = "Punishment type...",
            IsDisabled = true
        };

        _durationSelection = new SelectionViewComponent(SelectDurationAsync)
        {
            Placeholder = "Duration...",
            IsDisabled = true
        };
        
        ResetSelections();
        
        AddComponent(_countSelection);
        AddComponent(_punishmentTypeSelection);
        AddComponent(_durationSelection);
        
        //UpdateContextText(existingWarningPunishments);
        _automaticPunishments = existingAutomaticPunishments;
    }
    
    public async ValueTask SelectCountAsync(SelectionEventArgs e)
    {
        var value = int.Parse(e.SelectedOptions[0].Value.Value);

        if (value < 0) // "Custom"
        {
            const string customId = "Config:AutomaticPunishment:SetCount";
            var modal = new LocalInteractionModalResponse()
                .WithCustomId(customId)
                .WithTitle("Set custom demerit point count")
                .AddComponent(LocalComponent.Row(LocalComponent
                    .TextInput("demeritPointCount", "Demerit point count", TextInputComponentStyle.Short)
                    .WithIsRequired()
                    .WithMaximumInputLength(3)
                    .WithPlaceholder("The number of demerit points...")));

            await e.Interaction.Response().SendModalAsync(modal);
            var interaction = await _context.Bot.WaitForInteractionAsync<IModalSubmitInteraction>(e.ChannelId, customId, x => x.AuthorId == _context.AuthorId);
            if (interaction is null)
                return;

            if (!uint.TryParse(((ITextInputComponent)((IRowComponent)interaction.Components[0]).Components[0]).Value, out var newValue))
            {
                await interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse()
                    .WithContent("You must supply a valid demerit point count!").WithIsEphemeral());
                return;
            }

            value = (int) newValue;
            e.SelectedOptions[0].WithLabel($"Custom ({value})");
            await interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse()
                .WithContent("Count set. (Select additional values to complete.)").WithIsEphemeral());
        }

        _count = value;
        e.SelectedOptions[0].IsDefault = true;
        _punishmentTypeSelection.IsDisabled = false;
        ReportChanges();
    }

    public async ValueTask SelectPunishmentTypeAsync(SelectionEventArgs e)
    {
        var value = e.SelectedOptions[0].Value.Value;

        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (!Enum.TryParse(value, out PunishmentType type)) // [Remove]
        {
            if (await db.AutomaticPunishments.FindAsync(_context.GuildId, _count!.Value) is not { } existingWarningPunishment)
            {
                await e.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse()
                    .WithContent($"No automatic punishment exists for {"demerit point".ToQuantity(_count.Value)}!").WithIsEphemeral());
                return;
            }

            db.AutomaticPunishments.Remove(existingWarningPunishment);
            await db.SaveChangesAsync();

            await e.Interaction.RespondOrFollowupAsync(
                new LocalInteractionMessageResponse().WithContent($"Automatic punishments cleared for {"demerit point".ToQuantity(_count.Value)}."));
            await UpdateAutomaticPunishmentsAsync();
            ResetSelections();
            return;
        }

        if (type == PunishmentType.Kick)
        {
            if (await db.AutomaticPunishments.FindAsync(_context.GuildId, _count!.Value) is { } automaticPunishment)
            {
                automaticPunishment.PunishmentDuration = null;
                automaticPunishment.PunishmentType = PunishmentType.Kick;
            }
            else
            {
                automaticPunishment = new AutomaticPunishment(_context.GuildId, _count.Value, type, null);
                db.AutomaticPunishments.Add(automaticPunishment);
            }

            await db.SaveChangesAsync();
            await e.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse().WithContent(
                    $"Automatic punishment created/updated:\n{AutomaticPunishmentAutoCompleteFormatter.FormatWarningPunishment(automaticPunishment)}"));
            
            await UpdateAutomaticPunishmentsAsync();
            ResetSelections();
            return;
        }

        _type = type;
        e.SelectedOptions[0].IsDefault = true;
        _durationSelection.IsDisabled = false;
        ReportChanges();
    }

    public async ValueTask SelectDurationAsync(SelectionEventArgs e)
    {
        var value = e.SelectedOptions[0].Value.Value;

        var interaction = (IInteraction) e.Interaction;

        TimeSpan? duration;
        if (value == "Permanent")
        {
            if (_type == PunishmentType.Timeout)
            {
                await interaction.RespondOrFollowupAsync(
                    new LocalInteractionMessageResponse().WithContent("Timeouts cannot be permanent!").WithIsEphemeral());
                return;
            }
            
            duration = null;
        }
        else
        {
            if (value == "Custom")
            {
                const string customId = "Config:AutomaticPunishment:SetDuration";
                var modal = new LocalInteractionModalResponse()
                    .WithCustomId(customId)
                    .WithTitle("Set custom duration")
                    .AddComponent(LocalComponent.Row(LocalComponent
                        .TextInput("duration", "Duration", TextInputComponentStyle.Short)
                        .WithIsRequired()
                        .WithMaximumInputLength(20)
                        .WithPlaceholder("Duration (i.e. 3d, 1d12h)...")));

                await e.Interaction.Response().SendModalAsync(modal);
                var modalInteraction = await _context.Bot.WaitForInteractionAsync<IModalSubmitInteraction>(e.ChannelId, customId, x => x.AuthorId == _context.AuthorId);
                if (modalInteraction is null)
                    return;

                interaction = modalInteraction;
                value = ((ITextInputComponent)((IRowComponent) modalInteraction.Components[0]).Components[0]).Value;
            }
            
            var result = await _parser.ParseAsync(_context, null!, value.AsMemory());
            if (!result.IsSuccessful)
            {
                await interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse().WithContent(result.FailureReason!).WithIsEphemeral());
                return;
            }
            
            duration = result.ParsedValue.Value;
        }
        
        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (await db.AutomaticPunishments.FindAsync(_context.GuildId, _count!.Value) is { } automaticPunishment)
        {
            automaticPunishment.PunishmentDuration = duration;
            automaticPunishment.PunishmentType = _type!.Value;
        }
        else
        {
            automaticPunishment = new AutomaticPunishment(_context.GuildId, _count.Value, _type!.Value, duration);
            db.AutomaticPunishments.Add(automaticPunishment);
        }

        await db.SaveChangesAsync();
        await e.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse().WithContent(
                $"Automatic punishment created/updated:\n{AutomaticPunishmentAutoCompleteFormatter.FormatWarningPunishment(automaticPunishment)}"));

        await UpdateAutomaticPunishmentsAsync();
        ResetSelections();
    }

    protected override string FormatContent()
    {
        var builder = new StringBuilder(SELECTION_TEXT);

        if (_automaticPunishments.Count > 0)
        {
            builder.AppendNewline()
                .AppendJoin("\n", _automaticPunishments.Select(x => AutomaticPunishmentAutoCompleteFormatter.FormatWarningPunishment(x)));
        }

        return builder.ToString();
    }

    private async Task UpdateAutomaticPunishmentsAsync()
    {
        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        _automaticPunishments = await db.AutomaticPunishments.Where(x => x.GuildId == _context.GuildId)
            .OrderBy(x => x.DemeritPoints)
            .ToListAsync();
    }

    private void ResetSelections()
    {
        _countSelection.Options = Enumerable.Range(1, 10)
            .Select(x => new LocalSelectionComponentOption(x.ToString(), x.ToString()))
            .Append(new LocalSelectionComponentOption("Custom", "-1"))
            .ToList();

        _punishmentTypeSelection.IsDisabled = true;
        _punishmentTypeSelection.Options = Enum.GetValues<PunishmentType>()
            .Except(new[] { PunishmentType.Warning })
            .Select(x => new LocalSelectionComponentOption(x.Humanize(LetterCasing.Sentence), x.ToString()))
            .Append(new LocalSelectionComponentOption("[Remove]", "Remove"))
            .ToList();

        _durationSelection.IsDisabled = true;
        _durationSelection.Options =
            new[] { TimeSpan.FromHours(1), TimeSpan.FromHours(12), TimeSpan.FromDays(1), TimeSpan.FromDays(7) }
                .Select(x =>
                    new LocalSelectionComponentOption(x.Humanize(int.MaxValue, maxUnit: TimeUnit.Week),
                        FormatTimespanAsDuration(x)))
                .Append(new LocalSelectionComponentOption("None (permanent)", "Permanent"))
                .Append(new LocalSelectionComponentOption("Custom", "Custom"))
                .ToList();
        
        ReportChanges();
    }
    
    private static string FormatTimespanAsDuration(TimeSpan ts)
    {
        var builder = new StringBuilder();
        if (ts.Days > 0)
            builder.Append($"{ts.Days}d");
        if (ts.Hours > 0)
            builder.Append($"{ts.Hours}h");
        if (ts.Minutes > 0)
            builder.Append($"{ts.Minutes}m");
        if (ts.Seconds > 0)
            builder.Append($"{ts.Seconds}s");
        return builder.ToString();
    }
}