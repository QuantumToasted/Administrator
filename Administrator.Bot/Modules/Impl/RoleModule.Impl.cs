using System.Text;
using Administrator.Core;
using Disqord;
using Disqord.Bot.Commands.Application;
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

public sealed partial class RoleModule(AttachmentService attachmentService) : DiscordApplicationGuildModuleBase
{
    public partial IResult Info(IRole role)
        => Response(FormatRoleInfo(role));

    public partial async Task<IResult> Mention(IRole role)
    {
        await Deferral();
        var needsModification = !role.IsMentionable &&
                                Bot.GetCurrentMember(Context.GuildId)?.CalculateGuildPermissions().HasFlag(Permissions.MentionEveryone) != true;

        try
        {
            if (needsModification)
            {
                await role.ModifyAsync(x => x.IsMentionable = true);
                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }

            return Response(new LocalInteractionMessageResponse()
                .WithContent(role.Mention)
                .WithAllowedMentions(new LocalAllowedMentions().WithRoleIds(role.Id)));

        }
        catch (Exception ex)
        {
            return Response($"Failed to make the role {role.Mention} mentionable. The below error may help?\n{Markdown.CodeBlock(ex.Message)}");
        }
        finally
        {
            if (needsModification)
                _ = role.ModifyAsync(x => x.IsMentionable = false);
        }
    }
    
    public partial async Task<IResult> Grant(IMember member, IRole role)
    {
        if (!role.CanBeGrantedOrRevoked())
            return Response($"The role {role.Mention} is managed by the server or an integration/bot and cannot be granted or revoked.").AsEphemeral();

        await member.GrantRoleAsync(role.Id);

        return Response($"{member.Mention} has been given the role {role.Mention}.");
    }

    public partial async Task<IResult> GrantAll(IRole roleToGive, IRole? membersWithRole)
    {
        if (!roleToGive.CanBeGrantedOrRevoked())
            return Response($"The role {roleToGive.Mention} is managed by the server or an integration/bot and cannot be granted or revoked.").AsEphemeral();
        
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

        var view = new AdminPromptView(promptContentBuilder.ToString()).OnConfirm("Modifying member roles now...");
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

                    await Task.Delay(TimeSpan.FromSeconds(1), Bot.StoppingToken);
                }

                await Response($"Done! Applied: {appliedCount}/{membersToProcess.Count}, Failed: {failedCount}/{membersToProcess.Count}.");
            });
        }

        return default!;
    }
    
    public partial async Task<IResult> Revoke(IMember member, IRole role)
    {
        if (!role.CanBeGrantedOrRevoked())
            return Response($"The role {role.Mention} is managed by the server or an integration/bot and cannot be granted or revoked.").AsEphemeral();

        await member.RevokeRoleAsync(role.Id);

        return Response($"{member.Mention} has had the role {role.Mention} removed.");
    }

    public partial async Task<IResult> RevokeAll(IRole roleToRevoke, IRole? membersWithRole)
    {
        if (!roleToRevoke.CanBeGrantedOrRevoked())
            return Response($"The role {roleToRevoke.Mention} is managed by the server or an integration/bot and cannot be granted or revoked.").AsEphemeral();
        
        var membersToProcess = Bot.GetMembers(Context.GuildId).Values
            .Where(x => x.RoleIds.Contains(roleToRevoke.Id))
            .ToList();

        if (membersWithRole is not null)
            membersToProcess = membersToProcess.Where(x => x.RoleIds.Contains(membersWithRole.Id)).ToList();

        if (membersToProcess.Count == 0)
            return Response($"No members need to have the role {roleToRevoke.Mention} revoked.").AsEphemeral();

        var promptContentBuilder = new StringBuilder()
            .Append($"{"member".ToQuantity(membersToProcess.Count)} ")
            .Append(membersWithRole is not null ? $"with the role {membersWithRole.Mention} " : string.Empty)
            .AppendNewline($"will have the role {roleToRevoke.Mention} revoked.")
            .AppendNewline("This operation may take a long time for large numbers of members.");

        var view = new AdminPromptView(promptContentBuilder.ToString()).OnConfirm("Modifying member roles now...");
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
                        await member.RevokeRoleAsync(roleToRevoke.Id);
                        appliedCount++;
                    }
                    catch
                    {
                        failedCount++;
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(1), Bot.StoppingToken);
                }

                await Response($"Done! Applied: {appliedCount}/{membersToProcess.Count}, Failed: {failedCount}/{membersToProcess.Count}.");
            });
        }

        return default!;
    }

    public partial async Task<IResult> Create(string name, Color? color, IRole? aboveRole, bool hoisted, bool mentionable, IAttachment? icon)
    {
        await Deferral();

        var attachment = icon is not null
            ? await attachmentService.GetAttachmentAsync(icon)
            : null;

        IRole newRole;
        try
        {
            newRole = await Bot.CreateRoleAsync(Context.GuildId, x =>
            {
                x.Name = name;
                x.Color = color;
                x.IsHoisted = hoisted;
                x.IsMentionable = mentionable;

                if (attachment is not null)
                    x.Icon = attachment.Stream;
            });
        }
        catch (RestApiException ex) when (ex.Message.Contains("boosts")) // This server needs more boosts to perform this action
        {
            return Response("This server requires a higher boost level to be able to create roles with icons.");
        }
        
        if (aboveRole is not null)
            await newRole.ModifyAsync(x => x.Position = aboveRole.Position + 1);

        return Response($"New role {newRole.Mention} created.");
    }

    public partial async Task<IResult> Clone(IRole role, string? name)
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
        
        await Bot.ReorderRolesAsync(Context.GuildId, new Dictionary<Snowflake, int>
        {
            [role.Id] = role.Position,
            [newRole.Id] = role.Position
        });

        //await newRole.ModifyAsync(x => x.Position = Math.Max(role.Position - 1, 1));

        return Response($"{role.Mention} was successfully cloned into the new role {newRole.Mention}.");
    }

    public partial Task Modify(IRole role)
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

    public partial async Task<IResult> Move(IRole roleToMove, MoveDirection direction, IRole targetRole)
    {
        if (roleToMove.Id == Context.GuildId)
            return Response("The @\u200beveryone role cannot be moved above or below another role.").AsEphemeral();
        
        if (targetRole.Id == Context.GuildId && direction == MoveDirection.Below)
            return Response("Roles cannot be moved below the @\u200beveryone role.").AsEphemeral();
        
        await Deferral();
        await roleToMove.ModifyAsync(x => x.Position = targetRole.Position + (direction == MoveDirection.Above ? 1 : -1));
        return Response($"{roleToMove.Mention} has been moved {direction.Humanize(LetterCasing.LowerCase)} {targetRole.Mention}.");
    }

    public partial async Task Delete(IRole role)
    {
        if (!role.CanBeGrantedOrRevoked())
        {
            await Response("This role is managed by the server or a bot/integration, and cannot be deleted.").AsEphemeral();
            return;
        }
            
        var view = new AdminPromptView($"Are you sure you want to delete the role {role.Mention}?\n" +
                                        $"This action is {Markdown.Bold("IRREVERSIBLE")}.", FormatRoleInfo(role))
            .OnConfirm($"Role {role.Name} ({Markdown.Code(role.Id)}) deleted.");

        await View(view);
        if (view.Result)
            await role.DeleteAsync();
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
            .AppendJoinTruncated(", ", membersWithRole.Select(x => Markdown.Escape(x.Tag)),
                Discord.Limits.Message.Embed.Field.MaxValueLength);

        memberListField.WithValue(memberListBuilder.ToString());
        embed.AddField(memberListField);

        return embed;
    }
}