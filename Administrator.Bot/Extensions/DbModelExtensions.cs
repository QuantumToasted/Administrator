using Administrator.Core;
using Administrator.Database;
using Amazon.S3;
using Amazon.S3.Model;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    private static readonly string[] ImageExtensions = ["png", "jpeg", "jpg", "webp"];
    
    public static bool IsImageAttachment(this RemoteAttachment attachment)
        => Path.GetExtension(attachment.FileName) is { } extension && ImageExtensions.Contains(extension[1..], StringComparer.InvariantCultureIgnoreCase); 
    
    public static void IncrementXp<TUser>(this TUser user, int xp, TimeSpan xpGainInterval, out bool leveledUp)
        where TUser : IUserXp
    {
        leveledUp = false;
        var now = DateTimeOffset.UtcNow;
        
        if (now < user.LastXpGain + xpGainInterval)
            return;

        var currentLevel = user.GetLevel();
        user.TotalXp += xp;
        user.LastXpGain = now;

        if (currentLevel != user.GetLevel())
        {
            leveledUp = true;
            user.LastLevelUp = DateTimeOffset.UtcNow;
        }
    }

    public static Task ApplyAsync(this RoleLevelReward reward, IMember member)
    {
        var roleIds = member.RoleIds.ToHashSet();
        foreach (var roleId in reward.GrantedRoleIds)
        {
            roleIds.Add(roleId);
        }

        foreach (var roleId in reward.RevokedRoleIds)
        {
            roleIds.Remove(roleId);
        }

        return member.ModifyAsync(x => x.RoleIds = roleIds);
    }

    public static Task RevokeAsync(this RoleLevelReward reward, IMember member)
    {
        var roleIds = member.RoleIds.ToHashSet();
        foreach (var roleId in reward.GrantedRoleIds)
        {
            roleIds.Remove(roleId);
        }

        foreach (var roleId in reward.RevokedRoleIds)
        {
            roleIds.Add(roleId);
        }

        return member.ModifyAsync(x => x.RoleIds = roleIds);
    }

    public static async Task<bool> UploadAsync(this RemoteAttachment attachment, DiscordBotBase bot, byte[] data)
    {
        var s3 = bot.Services.GetRequiredService<AmazonS3Client>();
        var bucket = bot.CurrentUser.Id.ToString();
        var key = attachment.Key.ToString();

        try
        {
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = new MemoryStream(data),
                ContentType = MimeTypes.GetMimeType(attachment.FileName)
            }, bot.StoppingToken);
            return true;
        }
        catch (Exception ex)
        {
            bot.Logger.LogError(ex, "Failed to PUT object to B2.");
            return false;
        }
    }

    public static async Task<LocalAttachment?> DownloadAsync(this RemoteAttachment attachment, DiscordBotBase bot)
    {
        var s3 = bot.Services.GetRequiredService<AmazonS3Client>();
        var bucket = bot.CurrentUser.Id.ToString();
        var key = attachment.Key.ToString();
        var output = new MemoryStream();

        try
        {
            using var obj = await s3.GetObjectAsync(bucket, key, bot.StoppingToken);
            await obj.ResponseStream.CopyToAsync(output);
            output.Seek(0, SeekOrigin.Begin);
            return new LocalAttachment(output, attachment.FileName);
        }
        catch (Exception ex)
        {
            bot.Logger.LogError(ex, "Failed to GET object from B2.");
            return null;
        }
    }

    /*
    public static LocalAttachment ToLocalAttachment(this Attachment attachment)
    {
        var output = new MemoryStream(attachment.Data);
        output.Seek(0, SeekOrigin.Begin);
        return new LocalAttachment(output, attachment.FileName);
    }
    */
    
    public static TimeZoneInfo GetTimeZone(this User user)
        => user.TimeZone ?? TimeZoneInfo.Utc;
}