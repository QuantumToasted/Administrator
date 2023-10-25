using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;
using Qommon;

namespace Administrator.Bot;

public sealed class TagLimitConfigurationView : GuildConfigurationViewBase
{
    public const string SELECTION_TEXT = "Per-User Tag Limits";

    private readonly SelectionViewComponent _limitComponent;
    private int? _limit;

    public TagLimitConfigurationView(IDiscordApplicationGuildCommandContext context, int? limit)
        : base(context)
    {
        _limit = limit;
        AddComponent(_limitComponent = new SelectionViewComponent(UpdateLimitAsync));
        RegenerateSelectionOptions();
    }

    public async ValueTask UpdateLimitAsync(SelectionEventArgs e)
    {
        IInteraction interaction = e.Interaction;

        int? value = int.Parse(e.SelectedOptions[0].Value.Value);
        if (value == 0) // "Infinite" (null)
        {
            value = null;
        }
        else if (value < 0) // "Custom"
        {
            const string customId = "Config:TagLimits:SetLimit";
            var modal = new LocalInteractionModalResponse()
                .WithCustomId(customId)
                .WithTitle("Set custom tag limit")
                .AddComponent(LocalComponent.Row(LocalComponent
                    .TextInput("limit", "Tag limit", TextInputComponentStyle.Short)
                    .WithIsRequired()
                    .WithMaximumInputLength(3)
                    .WithPlaceholder("The maximum tag count...")));
            await interaction.Response().SendModalAsync(modal);
            var modalInteraction = await _context.Bot.WaitForInteractionAsync<IModalSubmitInteraction>(e.ChannelId, customId, x => x.AuthorId == _context.AuthorId);
            if (modalInteraction is null)
                return;

            interaction = modalInteraction;
            if (!int.TryParse(((ITextInputComponent)((IRowComponent) modalInteraction.Components[0]).Components[0]).Value, out var newValue) ||
                newValue <= 0)
            {
                await interaction.Response()
                    .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You must supply a valid custom tag limit!").WithIsEphemeral());
                return;
            }
            
            value = newValue;
        }
            
        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var guildConfig = await db.GetOrCreateGuildConfigAsync(_context.GuildId);

        guildConfig.MaximumTagsPerUser = value;
        await db.SaveChangesAsync();

        await interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
            .WithContent("Tag limit updated."));
        
        _limit = value;
        RegenerateSelectionOptions();
    }

    protected override string FormatContent()
        => SELECTION_TEXT;

    private void RegenerateSelectionOptions()
    {
        var options = new List<LocalSelectionComponentOption>
            { new LocalSelectionComponentOption("Infinite", "0").WithIsDefault(!_limit.HasValue) };

        foreach (var num in Enumerable.Range(1, 5).Select(x => x * 5))
        {
            var labelBuilder = new StringBuilder(num.ToString());
            var option = new LocalSelectionComponentOption()
                .WithValue(num.ToString());

            if (num == _limit)
            {
                labelBuilder.Append(" (current)");
                option.WithIsDefault();
            }
            
            options.Add(option.WithLabel(labelBuilder.ToString()));
        }

        if (options.All(x => !x.IsDefault.GetValueOrDefault()) && _limit.HasValue)
        {
            options.Add(new LocalSelectionComponentOption($"{_limit} (current)", _limit.Value.ToString())
                .WithIsDefault());
        }
        
        options.Add(new LocalSelectionComponentOption("Custom", "-1"));

        _limitComponent.Options = options;
        
        ReportChanges();
    }
}