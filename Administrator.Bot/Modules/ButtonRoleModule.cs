using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Disqord.Http;
using Disqord.Rest;
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

[SlashGroup("button-role")]
[RequireInitialAuthorPermissions(Permissions.ManageRoles)]
public sealed class ButtonRoleModule(AdminDbContext db, ButtonRoleService buttonRoles, AutoCompleteService autoComplete) : DiscordApplicationGuildModuleBase
{
    [SlashCommand("list")]
    [Description("Lists all button roles for this server.")]
    public async Task<IResult> ListAsync()
    {
        var guildButtonRoles = await EntityFrameworkQueryableExtensions.ToListAsync(db.ButtonRoles.Where(x => x.GuildId == Context.GuildId));

        var pages = guildButtonRoles.Chunk(10)
            .Select(chunk =>
            {
                var embed = new LocalEmbed()
                    .WithUnusualColor();

                foreach (var buttonRole in chunk)
                {
                    var nameBuilder = new StringBuilder($"#{buttonRole.Id} - ");
                    if (buttonRole.Emoji is not null)
                        nameBuilder.Append(buttonRole.Emoji).Append(' ');

                    if (!string.IsNullOrWhiteSpace(buttonRole.Text))
                        nameBuilder.Append(buttonRole.Text);

                    var valueBuilder = new StringBuilder()
                        .AppendNewline($"Row {buttonRole.Row}, Position {buttonRole.Position}");

                    if (buttonRole.ExclusiveGroupId.HasValue)
                        valueBuilder.AppendNewline($"Group #{buttonRole.ExclusiveGroupId.Value}");

                    var role = Bot.GetRole(Context.GuildId, buttonRole.RoleId);
                    valueBuilder.AppendNewline($"Grants role {role?.Mention ?? Markdown.Code(buttonRole.RoleId)}")
                        .AppendNewline(Markdown.Link("View message",
                            Discord.MessageJumpLink(Context.GuildId, buttonRole.ChannelId, buttonRole.MessageId)));

                    embed.AddField(nameBuilder.ToString(), valueBuilder.ToString());
                }

                return new Page().AddEmbed(embed);
            }).ToList();
        
        return pages.Count switch
        {
            0 => Response("No button roles exist on this server!").AsEphemeral(),
            1 => Response(pages[0].Embeds.Value[0]),
            _ => Menu(new AdminInteractionMenu(new AdminPagedView(pages), Context.Interaction))
        };
    }
    
    [SlashCommand("create")]
    [Description("Creates a new button role, granting a role when the button is pressed on a message.")]
    public async Task<IResult> CreateAsync(
        [Description("The role to grant.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole role,
        [Description("The channel the message is in.")]
        [ChannelTypes(ChannelType.Text)]
        [RequireBotChannelPermissions(Permissions.ViewChannels)]
            IChannel channel,
        [Description("The ID of the message to add the button to.")]
            Snowflake messageId,
        [Description("The text to display on the button. Optional if an emoji is set.")]
        [Maximum(Discord.Limits.Component.Button.MaxLabelLength)]
            string? text = null,
        [Description("The emoji to display on the button. Optional if text is set.")]
            IEmoji? emoji = null,
        [Description("The style of the button.")]
            LocalButtonComponentStyle style = LocalButtonComponentStyle.Primary,
        [Description("The row (1-5) the button will be on. Default: Automatic")]
        [Range(1, 5)]
            int? row = null,
        [Description("The position on the row the button will be on. Default: Automatic")]
        [Range(1, 5)]
            int? position = null,
        [Description("Only one role from this group ID can be selected at a time.")]
        [Minimum(1)]
            int? exclusiveGroup = null)
    {
        const int maxComponentsPerRow = 5;
        const int maxRowsPerMessage = 5;

        if (string.IsNullOrWhiteSpace(text) && emoji is null)
            return Response("A button can have text and/or an emoji, but not neither.").AsEphemeral();

        await Deferral();
        
        IUserMessage message;

        try
        {
            var m = await Bot.FetchMessageAsync(channel.Id, messageId);
            Guard.IsNotNull(m);
            message = Guard.IsAssignableToType<IUserMessage>(m);
        }
        catch (Exception ex) when (ex is ArgumentException or RestApiException { StatusCode: HttpResponseStatusCode.NotFound })
        {
            return Response($"The message with ID {Markdown.Code(messageId)} in {Mention.Channel(channel.Id)} does not exist or could not be found!")
                .AsEphemeral();
        }

        if (message.Author.Id != Bot.CurrentUser.Id)
            return Response("Button roles cannot be added to messages that aren't from me!");
        
        var existingButtonRoles = await EntityFrameworkQueryableExtensions.ToListAsync(db.ButtonRoles.Where(x => x.MessageId == messageId));
        if (exclusiveGroup is { } group && existingButtonRoles.Any(x => x.ExclusiveGroupId == group && x.RoleId == role.Id))
            return Response("You cannot have multiple buttons with the same role AND exclusive group ID!");
        
        if (existingButtonRoles.Count >= maxComponentsPerRow * maxRowsPerMessage)
            return Response("You have already reached the maximum button roles for this message!");

        if (row.HasValue && existingButtonRoles.Count(x => x.Row == row.Value) >= maxComponentsPerRow)
            return Response($"There are already {maxComponentsPerRow} buttons on row {row.Value}!");

        if (row.HasValue && position.HasValue && existingButtonRoles
                .FirstOrDefault(x => x.Row == row.Value && x.Position == position.Value) is { } button)
        {
            var roleName = Bot.GetRole(button.GuildId, button.RoleId) is { } r
                ? Markdown.Bold(r.Name)
                : $"with ID {Markdown.Code(button.RoleId)}";

            return Response($"There is already a button for the role {roleName} at row {row.Value}, position {position.Value}!");
        }

        row ??= 1;
        for (var r = 1; existingButtonRoles.Count > 0 && r <= maxRowsPerMessage; r++)
        {
            if (existingButtonRoles.Count(x => x.Row == r) < maxComponentsPerRow)
            {
                row = r;
                break;
            }
        }

        position ??= 1;
        for (var p = 1; existingButtonRoles.Count > 0 && p <= maxComponentsPerRow; p++)
        {
            if (!existingButtonRoles.Any(x => x.Row == row.Value && x.Position == p))
                continue;

            position = p;
        }

        var buttonRole = new ButtonRole(Context.GuildId, channel.Id, messageId, row.Value, position.Value, emoji?.ToString(), text, style, role.Id)
        {
            ExclusiveGroupId = exclusiveGroup
        };
        
        db.ButtonRoles.Add(buttonRole);
        await db.SaveChangesAsync();

        await buttonRoles.ReloadButtonCommandsAsync(Context.GuildId, channel.Id, messageId);
        return Response(new LocalInteractionMessageResponse()
            .WithContent($"New button role {buttonRole} created! " +
                         $"Users will receive the role {role.Mention} for pressing this button in {Mention.Channel(channel.Id)}:")
            .AddComponent(LocalComponent.Row(buttonRole.ToButton(Bot).WithCustomId(Guid.NewGuid().ToString()))));
    }

    [SlashCommand("modify")]
    [Description("Modifies an existing button role.")]
    public async Task<IResult> ModifyAsync(
        [Name("button-role")] 
        [Description("The ID of the button to remove.")]
            int buttonRoleId,
        [Description("The new role this button will assign.")]
            IRole? role = null,
        [Description("The new text for this button role.")]
            string? text = null,
        [Description("The ID of the button to remove.")]
            IEmoji? emoji = null,
        [Description("The ID of the button to remove.")]
            LocalButtonComponentStyle? style = null)
    {
        await Deferral();
        if (await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.ButtonRoles, x => x.GuildId == Context.GuildId && x.Id == buttonRoleId) is not { } buttonRole)
            return Response($"No button role could be found with the ID {Markdown.Bold(buttonRoleId)}!");

        buttonRole = buttonRole with
        {
            RoleId = role?.Id ?? buttonRole.RoleId,
            Text = text ?? buttonRole.Text,
            Emoji = emoji?.ToString() ?? buttonRole.Emoji,
            Style = style ?? buttonRole.Style
        };

        db.ButtonRoles.Update(buttonRole);
        await db.SaveChangesAsync();
        
        await buttonRoles.ReloadButtonCommandsAsync(Context.GuildId, buttonRole.ChannelId, buttonRole.MessageId);
        return Response($"Button role {buttonRole} updated.");
    }

    [SlashCommand("remove")]
    [Description("Removes an existing button role.")]
    public async Task<IResult> RemoveAsync(
        [Name("button-role")]
        [Description("The ID of the button to remove.")]
            int buttonRoleId)
    {
        await Deferral();
        if (await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.ButtonRoles, x => x.GuildId == Context.GuildId && x.Id == buttonRoleId) is not { } buttonRole)
            return Response($"No button role could be found with the ID {Markdown.Bold(buttonRoleId)}!");

        db.ButtonRoles.Remove(buttonRole);
        await db.SaveChangesAsync();

        if (await EntityFrameworkQueryableExtensions.CountAsync(db.ButtonRoles, x => x.MessageId == buttonRole.MessageId) == 0)
        {
            _ = Bot.ModifyMessageAsync(buttonRole.ChannelId, buttonRole.MessageId, x => x.Components = new List<LocalRowComponent>());
        }

        await buttonRoles.ReloadButtonCommandsAsync(Context.GuildId, buttonRole.ChannelId, buttonRole.MessageId);
        return Response($"Button role {buttonRole} successfully removed.");
    }

    [AutoComplete("modify")]
    [AutoComplete("remove")]
    public async Task AutoCompleteButtonRolesAsync(AutoComplete<int> buttonRole)
    {
        var guildButtonRoles = await EntityFrameworkQueryableExtensions.ToListAsync(db.ButtonRoles.Where(x => x.GuildId == Context.GuildId));
        autoComplete.AutoComplete(buttonRole, guildButtonRoles);
    }
}