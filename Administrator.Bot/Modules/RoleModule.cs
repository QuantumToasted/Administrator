﻿using System.Text;
using Administrator.Core;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Prompt;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Qmmands;

namespace Administrator.Bot;

public enum MoveDirection
{
    [ChoiceName("above")]
    Above,
    [ChoiceName("below")]
    Below
}

[SlashGroup("role")]
[RequireInitialAuthorPermissions(Permissions.ManageRoles)]
[RequireBotPermissions(Permissions.ManageRoles)]
public sealed class RoleModule(AttachmentService attachmentService) : DiscordApplicationGuildModuleBase
{
    [SlashCommand("info")]
    [Description("Displays information about a role.")]
    public IResult DisplayInfo(
        [Description("The role to display information for.")]
            IRole role)
    {
        return Response(FormatRoleInfo(role));
    }

    [SlashCommand("grant")]
    [Description("Grants (gives) a role to a member.")]
    public async Task<IResult> GrantAsync(
        [Description("The member to give the role to.")]
            IMember member,
        [Description("The role to give to the member.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole role)
    {
        if (member.RoleIds.Contains(role.Id))
            return Response($"{member.Mention} already has the role {role.Mention}!").AsEphemeral();

        await member.GrantRoleAsync(role.Id);

        return Response($"{member.Mention} has been given the role {role.Mention}.");
    }

    [SlashCommand("grant-all")]
    [Description("Grants (gives) a role to all members.")]
    public async Task<IResult> GrantAllAsync(
        [Description("The role to give to the members.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole roleToGive,
        [Description("Only give role-to-give to members with this role. Defaults to no role (everyone).")]
            IRole? membersWithRole = null)
    {
        var membersToProcess = Bot.GetMembers(Context.GuildId).Values
            .Where(x => !x.RoleIds.Contains(roleToGive.Id))
            .ToList();

        if (membersWithRole is not null)
            membersToProcess = membersToProcess.Where(x => x.RoleIds.Contains(membersWithRole.Id)).ToList();

        if (membersToProcess.Count == 0)
            return Response($"No members need to have the role {roleToGive.Mention} given.").AsEphemeral();

        var promptContentBuilder = new StringBuilder()
            .Append($"{"member".ToQuantity(membersToProcess.Count)} ")
            .Append(membersWithRole is not null ? $"with the role {membersWithRole.Mention} " : string.Empty)
            .AppendNewline($"will be given the role {roleToGive.Mention}.")
            .AppendNewline("This operation may take a long time for large numbers of members.");

        var view = new PromptView(x => x.WithContent(promptContentBuilder.ToString()));
        await View(view);

        if (view.Result)
        {
            _ = Task.Run(async () =>
            {
                var appliedCount = 0;
                var failedCount = 0;

                foreach (var member in membersToProcess)
                {
                    try
                    {
                        await member.GrantRoleAsync(roleToGive.Id);
                        appliedCount++;
                    }
                    catch
                    {
                        failedCount++;
                    }
                }

                await Response($"Done! Applied: {appliedCount}/{membersToProcess.Count}, Failed: {failedCount}/{membersToProcess.Count}.");
            });
        }

        return default!;
    }

    [SlashCommand("create")]
    [Description("Creates a new role.")]
    public async Task<IResult> CreateAsync(
        [Description("The name of the new role.")]
            string name,
        [Description("The color of the new role.")]
            Color? color = null,
        [Description("The role to move the new role above after creation.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole? aboveRole = null,
        [Description("Whether the new role should be hoisted (visible in the member list). Default: False")]
            bool hoisted = false,
        [Description("Whether the new role should be able to be mentioned by other members. Default: False")]
            bool mentionable = false,
        [Description("The icon for the new role. (Requires the server to have access to this feature.)")]
        [Image]
        [NonNitroAttachment]
            IAttachment? icon = null)
    {
        await Deferral();

        var attachment = icon is not null
            ? await attachmentService.GetAttachmentAsync(icon)
            : null;

        var newRole = await Bot.CreateRoleAsync(Context.GuildId, x =>
        {
            x.Name = name;
            x.Color = color;
            x.IsHoisted = hoisted;
            x.IsMentionable = mentionable;

            if (attachment is not null)
                x.Icon = attachment.Stream;
        });

        if (aboveRole is not null)
            await newRole.ModifyAsync(x => x.Position = aboveRole.Position + 1);

        return Response($"New role {newRole.Mention} created.");
    }

    [SlashCommand("clone")]
    [Description("Clones an existing role, including all of its information and permissions.")]
    public async Task<IResult> CloneAsync(
        [Description("The role to clone.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole role,
        [Description("The name of the cloned role. Defaults to the name of the original role.")]
            string? name = null)
    {
        await Deferral();

        var attachment = !string.IsNullOrWhiteSpace(role.IconHash)
            ? await attachmentService.GetAttachmentAsync(role.GetIconUrl()!)
            : null;

        var newRole = await Bot.CreateRoleAsync(Context.GuildId, x =>
        {
            x.Name = name ?? role.Name;
            x.Color = role.Color;
            x.IsHoisted = role.IsHoisted;
            x.IsMentionable = role.IsMentionable;
            x.Permissions = role.Permissions;

            if (attachment is not null)
                x.Icon = attachment.Stream;

            if (role.UnicodeEmoji is not null)
                x.UnicodeEmoji = LocalEmoji.FromEmoji(role.UnicodeEmoji)!;
        });

        await newRole.ModifyAsync(x => x.Position = Math.Max(role.Position - 1, 1));

        return Response($"{role.Mention} was successfully cloned into the new role {newRole.Mention}.");
    }

    [SlashCommand("modify")]
    [Description("Modifies an existing role.")]
    public Task ModifyAsync(
        [Description("The role to modify.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole role)
    {
        var nameInput = LocalComponent.TextInput("name", "Name", TextInputComponentStyle.Short)
            .WithIsRequired()
            .WithPrefilledValue(role.Name);

        var colorInput = LocalComponent.TextInput("color", "Color", TextInputComponentStyle.Short)
            .WithIsRequired(false)
            .WithPlaceholder("Leave blank for no color");

        if (role.Color.HasValue)
            colorInput.WithPrefilledValue(role.Color.Value.ToString());

        var hoistedInput = LocalComponent.TextInput("hoisted", "Hoisted?", TextInputComponentStyle.Short)
            .WithIsRequired()
            .WithMaximumInputLength(5)
            .WithPlaceholder("True/False")
            .WithPrefilledValue(role.IsHoisted.ToString());

        var mentionableInput = LocalComponent.TextInput("mentionable", "Mentionable?", TextInputComponentStyle.Short)
            .WithIsRequired()
            .WithMaximumInputLength(5)
            .WithPlaceholder("True/False")
            .WithPrefilledValue(role.IsMentionable.ToString());

        return Context.Interaction.Response().SendModalAsync(new LocalInteractionModalResponse()
            .WithTitle("Modify Role")
            .WithCustomId($"Role:Modify:{role.Id}")
            .WithComponents(LocalComponent.Row(nameInput), LocalComponent.Row(colorInput),
                LocalComponent.Row(hoistedInput), LocalComponent.Row(mentionableInput)));
    }

    [SlashCommand("move")]
    [Description("Moves an existing role above or below another role.")]
    public async Task<IResult> MoveAsync(
        [Description("The role to move above/below target-role.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole roleToMove,
        [Description("Whether to move role-to-move above or below target-role.")]
            MoveDirection direction,
        [Description("The role to move role-to-move above/below.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole targetRole)
    {
        await Deferral();

        // direction is defined as Below = -1, Above = 1
        await roleToMove.ModifyAsync(x => x.Position = targetRole.Position + (int) direction);
        return Response($"{roleToMove.Mention} has been moved {direction.Humanize(LetterCasing.LowerCase)} {targetRole.Mention}.");
    }

    /*
    [SlashCommand("color")]
    [Description("Changes (or removes) an existing role's color.")]
    public async Task<IResult> ColorAsync(
        [Description("The role whose color you are changing.")]
        [RequireAuthorRoleHierarchy]
        [RequireBotRoleHierarchy]
            IRole role,
        [Description("The new color for the role. Supplying nothing removes the role's color.")]
            Color? color = null)
    {
        var oldColorStr = role.Color?.ToString() ?? "(no color)";
        var newColorStr = color?.ToString() ?? "(no color)";
        await role.ModifyAsync(x => x.Color = color);
        return Response($"{role.Mention} has had its color changed from {oldColorStr} to {newColorStr}.");
    }
    */

    [SlashCommand("delete")]
    [Description("Deletes an existing role permanently.")]
    public async Task<IResult> DeleteAsync(
        [Description("The role to delete.")] 
        [RequireAuthorRoleHierarchy] 
        [RequireBotRoleHierarchy]
            IRole role)
    {
        var view = new PromptView(x =>
            x.WithContent($"Are you sure you want to delete the role {role.Mention}?\n" +
                          $"This action is {Markdown.Bold("IRREVERSIBLE")}.").AddEmbed(FormatRoleInfo(role)));

        await View(view);
        if (!view.Result)
            return default!;

        await role.DeleteAsync();
        return Response($"Role {role.Name} ({role.Id}) deleted.");
    }

    private LocalEmbed FormatRoleInfo(IRole role)
    {
        var position = role.GetOrderedPosition(out var roleAbove, out var roleBelow);
        var embed = new LocalEmbed()
            .WithColor(role.Color ?? Colors.Unusual)
            .WithTitle($"Information for role {role.Name}")
            .AddField("ID", role.Id, true)
            .AddField("Created", Markdown.Timestamp(role.CreatedAt(), Markdown.TimestampFormat.RelativeTime), true)
            .AddField("Mention", role.Mention, true)
            .AddField("Color", role.Color?.ToString() ?? "(no color)")
            .AddField("Permissions", role.Permissions.Humanize(LetterCasing.Title));

        var positionField = new LocalEmbedField()
            .WithName($"Position ({position}/{Bot.GetRoles(Context.GuildId).Count})");

        if (roleAbove is null && roleBelow is not null)
        {
            positionField.WithValue("Above all other roles");
        }
        else if (roleBelow is null && roleAbove is not null)
        {
            positionField.WithValue("Below all other roles");
        }
        else
        {
            positionField.WithValue($"Above {roleBelow!.Mention}\n" +
                                    $"Below {roleAbove!.Mention}");
        }

        embed.AddField(positionField);

        if (!string.IsNullOrWhiteSpace(role.IconHash))
            embed.WithThumbnailUrl(role.GetIconUrl()!);

        var membersWithRole = Bot.GetMembers(Context.GuildId).Values
            .Where(x => x.RoleIds.Contains(role.Id))
            .OrderByDescending(x => x.GetHighestRole()?.Position ?? 0)
            .ToList();

        var memberListField = new LocalEmbedField()
            .WithName($"Members ({membersWithRole.Count})");

        var memberListBuilder = new StringBuilder()
            .AppendJoinTruncated(", ", membersWithRole.Select(x => x.Tag),
                Discord.Limits.Message.Embed.Field.MaxValueLength);
        /*
        foreach (var member in membersWithRole)
        {
            var memberStr = member.Tag;
            if (memberListBuilder.Length + memberStr.Length >= Discord.Limits.Message.Embed.Field.MaxValueLength - 3)
            {
                memberListBuilder.Append('…');
                break;
            }

            memberListBuilder.Append($"{memberStr}, ");
        }
        */

        memberListField.WithValue(memberListBuilder.ToString().TrimEnd(' ', ','));
        embed.AddField(memberListField);

        return embed;
    }
}