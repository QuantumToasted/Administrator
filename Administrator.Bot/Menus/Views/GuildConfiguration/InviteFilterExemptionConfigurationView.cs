using System.Text;
using System.Text.RegularExpressions;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Bot;

public sealed class InviteFilterExemptionConfigurationView : GuildConfigurationViewBase
{
    private static readonly Regex InviteCodeRegex = new(@"^[a-zA-Z0-9-_]+$", RegexOptions.Compiled);

    private const int MAX_EXEMPTIONS = 20;
    public const string SELECTION_TEXT = "Invite Filter Exemptions";

    private string? _exemptionText;

    public InviteFilterExemptionConfigurationView(IDiscordApplicationGuildCommandContext context, ICollection<InviteFilterExemption> existingExemptions)
        : base(context)
    {
        UpdateContentText(existingExemptions);
    }

    protected override string FormatContent() => new StringBuilder(SELECTION_TEXT).AppendNewline().Append(_exemptionText).ToString();

    [Selection(Placeholder = "Add user or role", Type = SelectionComponentType.Mentionable)]
    public async ValueTask AddUserOrRoleAsync(SelectionEventArgs e)
    {
        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var exemptions = await db.InviteFilterExemptions
            .Where(x => x.GuildId == _context.GuildId)
            .ToListAsync();
        
        var value = e.SelectedEntities[0];
        if (value is IUser user)
        {
            if (exemptions.Any(x => x.TargetId == user.Id))
            {
                await e.Interaction.Response()
                    .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You have already exempted this user!.").WithIsEphemeral());
                return;
            }
            
            if (exemptions.Count(x => x.ExemptionType == InviteFilterExemptionType.User) >= MAX_EXEMPTIONS)
            {
                await e.Interaction.Response()
                    .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You cannot add more than {Markdown.Bold(MAX_EXEMPTIONS)} user exemptions.").WithIsEphemeral());
                return;
            }

            var newUserExemption = new InviteFilterExemption(_context.GuildId, InviteFilterExemptionType.User, user.Id, null);
            db.InviteFilterExemptions.Add(newUserExemption);
            await db.SaveChangesAsync();
            
            await UpdateExemptionsAsync();
            await e.Interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You have exempted any invites posted by the user {user.Mention} from being filtered."));

            return;
        }
        
        var role = (IRole) value;
        if (exemptions.Any(x => x.TargetId == role.Id))
        {
            await e.Interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You have already exempted this role!.").WithIsEphemeral());
            return;
        }
        
        if (exemptions.Count(x => x.ExemptionType == InviteFilterExemptionType.Role) >= MAX_EXEMPTIONS)
        {
            await e.Interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You cannot add more than {Markdown.Bold(MAX_EXEMPTIONS)} role exemptions!").WithIsEphemeral());
            return;
        }
        
        var newRoleExemption = new InviteFilterExemption(_context.GuildId, InviteFilterExemptionType.Role, role.Id, null);
        db.InviteFilterExemptions.Add(newRoleExemption);
        await db.SaveChangesAsync();
            
        await UpdateExemptionsAsync();
        await e.Interaction.Response()
            .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You have exempted any invites posted by users with the role {role.Mention} from being filtered."));
    }

    [Selection(Placeholder = "Add channel", Type = SelectionComponentType.Channel, ChannelTypes = new[] { ChannelType.Text, ChannelType.PublicThread })]
    public async ValueTask AddChannelAsync(SelectionEventArgs e)
    {
        var channel = (IChannel) e.SelectedEntities[0];
        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var exemptions = await db.InviteFilterExemptions
            .Where(x => x.GuildId == _context.GuildId && x.ExemptionType == InviteFilterExemptionType.Channel)
            .ToListAsync();

        if (exemptions.Any(x => x.TargetId == channel.Id))
        {
            await e.Interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You have already exempted this channel!.").WithIsEphemeral());
            return;
        }
        
        if (exemptions.Count >= MAX_EXEMPTIONS)
        {
            await e.Interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You cannot add more than {Markdown.Bold(MAX_EXEMPTIONS)} channel exemptions!").WithIsEphemeral());
            return;
        }

        var newExemption = new InviteFilterExemption(_context.GuildId, InviteFilterExemptionType.Channel, channel.Id, null);
        db.InviteFilterExemptions.Add(newExemption);
        await db.SaveChangesAsync();
        
        await UpdateExemptionsAsync();
        if (channel.Type == ChannelType.Text)
        {
            await e.Interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You have exempted any invites posted in the channel {Mention.Channel(channel.Id)} (or any of its threads) from being filtered."));
        }
        else
        {
            await e.Interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You have exempted any invites posted in the channel {Mention.Channel(channel.Id)} from being filtered."));
        }
    }

    [Selection(Placeholder = "Remove user or role", Type = SelectionComponentType.Mentionable)]
    public async ValueTask RemoveUserOrRoleAsync(SelectionEventArgs e)
    {
        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var exemptions = await db.InviteFilterExemptions
            .Where(x => x.GuildId == _context.GuildId)
            .ToListAsync();
        
        var value = e.SelectedEntities[0];
        if (value is IUser user)
        {
            if (exemptions.FirstOrDefault(x => x.TargetId == user.Id) is not { } userExemption)
            {
                await e.Interaction.Response()
                    .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You have not exempted this user!.").WithIsEphemeral());
                return;
            }

            db.InviteFilterExemptions.Remove(userExemption);
            await db.SaveChangesAsync();
            
            await UpdateExemptionsAsync();
            await e.Interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You have removed the user {user.Mention} from the invite filter exemption list."));

            return;
        }
        
        var role = (IRole) value;
        if (exemptions.FirstOrDefault(x => x.TargetId == role.Id) is not { } roleExemption)
        {
            await e.Interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You have not exempted this role!.").WithIsEphemeral());
            return;
        }

        db.InviteFilterExemptions.Remove(roleExemption);
        await db.SaveChangesAsync();
            
        await UpdateExemptionsAsync();
        await e.Interaction.Response()
            .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You have removed the role {role.Mention} from the invite filter exemption list."));
    }

    [Selection(Placeholder = "Remove channel", Type = SelectionComponentType.Channel, ChannelTypes = new[] { ChannelType.Text, ChannelType.PublicThread })]
    public async ValueTask RemoveChannelAsync(SelectionEventArgs e)
    {
        var channel = (IChannel) e.SelectedEntities[0];
        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var exemptions = await db.InviteFilterExemptions
            .Where(x => x.GuildId == _context.GuildId && x.ExemptionType == InviteFilterExemptionType.Channel)
            .ToListAsync();

        if (exemptions.FirstOrDefault(x => x.TargetId == channel.Id) is not { } exemption)
        {
            await e.Interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You have not exempted this channel!.").WithIsEphemeral());
            return;
        }

        db.InviteFilterExemptions.Remove(exemption);
        await db.SaveChangesAsync();
        
        await UpdateExemptionsAsync();
        await e.Interaction.Response()
            .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You have removed the channel {Mention.Channel(channel.Id)} from the invite filter exemption list."));
    }

    [Button(Label = "Add server ID", Style = LocalButtonComponentStyle.Success)]
    public async ValueTask AddServerIdAsync(ButtonEventArgs e)
    {
        const string customId = "InviteFilterExemption:Add:GuildId";
        var modal = new LocalInteractionModalResponse()
            .WithTitle("Add server ID")
            .WithCustomId(customId)
            .WithComponents(LocalComponent.Row(LocalComponent
                .TextInput("guildId", "Server ID", TextInputComponentStyle.Short)
                .WithIsRequired()
                .WithMaximumInputLength(22)
                .WithPlaceholder("The ID of the server...")));

        await e.Interaction.Response().SendModalAsync(modal);
        var interaction = await _context.Bot.WaitForInteractionAsync<IModalSubmitInteraction>(e.ChannelId, customId, x => x.AuthorId == _context.AuthorId);
        if (interaction is null)
            return;

        var value = ((ITextInputComponent)((IRowComponent)interaction.Components[0]).Components[0]).Value;
        if (string.IsNullOrWhiteSpace(value) || !Snowflake.TryParse(value, out var guildId))
        {
            await interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You must supply a valid server ID!").WithIsEphemeral());
            return;
        }

        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var exemptions = await db.InviteFilterExemptions
            .Where(x => x.GuildId == _context.GuildId && x.ExemptionType == InviteFilterExemptionType.Guild)
            .ToListAsync();
        
        if (exemptions.Any(x => x.TargetId == guildId))
        {
            await interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You have already exempted this server ID!.").WithIsEphemeral());
            return;
        }
        
        if (exemptions.Count >= MAX_EXEMPTIONS)
        {
            await interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You cannot add more than {Markdown.Bold(MAX_EXEMPTIONS)} server ID exemptions!").WithIsEphemeral());
            return;
        }

        var newExemption = new InviteFilterExemption(_context.GuildId, InviteFilterExemptionType.Guild, guildId, null);
        db.InviteFilterExemptions.Add(newExemption);
        await db.SaveChangesAsync();

        await UpdateExemptionsAsync();
        await interaction.Response()
            .SendMessageAsync(new LocalInteractionMessageResponse().WithContent(
                $"You have exempted any invites from the server with the ID {Markdown.Code(value)} from being filtered."));
    }

    [Button(Label = "Add invite code", Style = LocalButtonComponentStyle.Success)]
    public async ValueTask AddInviteCodeAsync(ButtonEventArgs e)
    {
        const string customId = "InviteFilterExemption:Add:InviteCode";
        var modal = new LocalInteractionModalResponse()
            .WithTitle("Add invite code")
            .WithCustomId(customId)
            .WithComponents(LocalComponent.Row(LocalComponent
                .TextInput("inviteCode", "Invite Code", TextInputComponentStyle.Short)
                .WithIsRequired()
                .WithMaximumInputLength(20)
                .WithPlaceholder("The invite code to the server...")));

        await e.Interaction.Response().SendModalAsync(modal);
        var interaction = await _context.Bot.WaitForInteractionAsync<IModalSubmitInteraction>(e.ChannelId, customId, x => x.AuthorId == _context.AuthorId);
        if (interaction is null)
            return;

        var value = ((ITextInputComponent)((IRowComponent)interaction.Components[0]).Components[0]).Value;
        if (string.IsNullOrWhiteSpace(value) || !InviteCodeRegex.IsMatch(value))
        {
            await interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You must supply a valid invite code!").WithIsEphemeral());
            return;
        }

        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var exemptions = await db.InviteFilterExemptions
            .Where(x => x.GuildId == _context.GuildId && x.ExemptionType == InviteFilterExemptionType.InviteCode)
            .ToListAsync();
        
        if (exemptions.Any(x => x.ExemptionType == InviteFilterExemptionType.InviteCode && x.InviteCode == value))
        {
            await interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You have already exempted this invite code!.").WithIsEphemeral());
            return;
        }

        if (exemptions.Count >= MAX_EXEMPTIONS)
        {
            await interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent($"You cannot add more than {Markdown.Bold(MAX_EXEMPTIONS)} invite code exemptions!").WithIsEphemeral());
            return;
        }

        var newExemption = new InviteFilterExemption(_context.GuildId, InviteFilterExemptionType.InviteCode, null, value);
        db.InviteFilterExemptions.Add(newExemption);
        await db.SaveChangesAsync();

        await UpdateExemptionsAsync();
        await interaction.Response()
            .SendMessageAsync(new LocalInteractionMessageResponse().WithContent(
                $"You have exempted the invite code {Markdown.Code(value)} from being filtered."));
    }

    [Button(Label = "Remove server ID", Style = LocalButtonComponentStyle.Danger)]
    public async ValueTask RemoveServerIdAsync(ButtonEventArgs e)
    {
        const string customId = "InviteFilterExemption:Remove:GuildId";
        var modal = new LocalInteractionModalResponse()
            .WithTitle("Remove server ID")
            .WithCustomId(customId)
            .AddComponent(LocalComponent.Row(LocalComponent
                .TextInput("guildId", "Server ID", TextInputComponentStyle.Short)
                .WithIsRequired()
                .WithMaximumInputLength(22)
                .WithPlaceholder("The ID of the server...")));

        await e.Interaction.Response().SendModalAsync(modal);
        var interaction = await _context.Bot.WaitForInteractionAsync<IModalSubmitInteraction>(e.ChannelId, customId, x => x.AuthorId == _context.AuthorId);
        if (interaction is null)
            return;

        var value = ((ITextInputComponent)((IRowComponent)interaction.Components[0]).Components[0]).Value;
        if (string.IsNullOrWhiteSpace(value) || !Snowflake.TryParse(value, out var guildId))
        {
            await interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You must supply a valid server ID!").WithIsEphemeral());
            return;
        }

        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var exemptions = await db.InviteFilterExemptions
            .Where(x => x.GuildId == _context.GuildId && x.ExemptionType == InviteFilterExemptionType.Guild)
            .ToListAsync();
        
        if (exemptions.FirstOrDefault(x => x.TargetId == guildId) is not { } exemption)
        {
            await interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You haven't exempted this server ID!.").WithIsEphemeral());
            return;
        }

        db.InviteFilterExemptions.Remove(exemption);
        await db.SaveChangesAsync();

        await UpdateExemptionsAsync();
        await interaction.Response()
            .SendMessageAsync(new LocalInteractionMessageResponse().WithContent(
                $"You have removed the server with the ID {Markdown.Code(value)} from the invite filter exemption list."));
    }

    [Button(Label = "Remove invite code", Style = LocalButtonComponentStyle.Danger)]
    public async ValueTask RemoveInviteCodeAsync(ButtonEventArgs e)
    {
        const string customId = "InviteFilterExemption:Remote:InviteCode";
        var modal = new LocalInteractionModalResponse()
            .WithTitle("Remove invite code")
            .WithCustomId(customId)
            .WithComponents(LocalComponent.Row(LocalComponent
                .TextInput("inviteCode", "Invite Code", TextInputComponentStyle.Short)
                .WithIsRequired()
                .WithMaximumInputLength(20)
                .WithPlaceholder("The invite code to the server...")));

        await e.Interaction.Response().SendModalAsync(modal);
        var interaction = await _context.Bot.WaitForInteractionAsync<IModalSubmitInteraction>(e.ChannelId, customId, x => x.AuthorId == _context.AuthorId);
        if (interaction is null)
            return;

        var value = ((ITextInputComponent)((IRowComponent)interaction.Components[0]).Components[0]).Value;
        if (string.IsNullOrWhiteSpace(value) || !InviteCodeRegex.IsMatch(value))
        {
            await interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You must supply a valid invite code!").WithIsEphemeral());
            return;
        }

        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var exemptions = await db.InviteFilterExemptions
            .Where(x => x.GuildId == _context.GuildId && x.ExemptionType == InviteFilterExemptionType.InviteCode)
            .ToListAsync();
        
        if (exemptions.FirstOrDefault(x => x.ExemptionType == InviteFilterExemptionType.InviteCode && x.InviteCode == value) is not { } exemption)
        {
            await interaction.Response()
                .SendMessageAsync(new LocalInteractionMessageResponse().WithContent("You have not exempted this invite code!.").WithIsEphemeral());
            return;
        }

        db.InviteFilterExemptions.Remove(exemption);
        await db.SaveChangesAsync();

        await UpdateExemptionsAsync();
        await interaction.Response()
            .SendMessageAsync(new LocalInteractionMessageResponse().WithContent(
                $"You have removed the invite code {Markdown.Code(value)} from the invite filter exemption list."));
    }

    private async Task UpdateExemptionsAsync()
    {
        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var exemptions = await db.InviteFilterExemptions.Where(x => x.GuildId == _context.GuildId).ToListAsync();
        UpdateContentText(exemptions);
        ReportChanges();
    }

    private void UpdateContentText(ICollection<InviteFilterExemption> exemptions)
    {
        var builder = new StringBuilder(SELECTION_TEXT)
            .AppendNewline();
        
        var userOrRoleExemptions = exemptions.Where(x => x.ExemptionType is InviteFilterExemptionType.User or InviteFilterExemptionType.Role).ToList();
        if (userOrRoleExemptions.Count > 0)
        {
            builder.AppendNewline(Markdown.Bold("User/role exemptions:"))
                .AppendJoin("\n", userOrRoleExemptions.Select(x => x.ExemptionType == InviteFilterExemptionType.User
                    ? Mention.User(x.TargetId!.Value)
                    : Mention.Role(x.TargetId!.Value)))
                .AppendNewline();
        }

        var channelExemptions = exemptions.Where(x => x.ExemptionType == InviteFilterExemptionType.Channel).ToList();
        if (channelExemptions.Count > 0)
        {
            builder.AppendNewline(Markdown.Bold("Channel exemptions:"))
                .AppendJoin("\n", channelExemptions.Select(x => Mention.Channel(x.TargetId!.Value)))
                .AppendNewline();
        }

        var guildExemptions = exemptions.Where(x => x.ExemptionType == InviteFilterExemptionType.Guild).ToList();
        if (guildExemptions.Count > 0)
        {
            builder.AppendNewline(Markdown.Bold("Server ID exemptions:"))
                .AppendJoin("\n", guildExemptions.Select(x => Markdown.Code(x.TargetId!.Value)))
                .AppendNewline();
        }

        var inviteCodeExemptions = exemptions.Where(x => x.ExemptionType == InviteFilterExemptionType.InviteCode).ToList();
        if (inviteCodeExemptions.Count > 0)
        {
            builder.AppendNewline(Markdown.Bold("Invite code exemptions:"))
                .AppendJoin("\n", inviteCodeExemptions.Select(x => Markdown.Code(x.InviteCode!)))
                .AppendNewline();
        }

        _exemptionText = builder.ToString();
    }
}