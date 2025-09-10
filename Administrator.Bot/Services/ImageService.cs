using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using ImageMagick;
using ImageMagick.Drawing;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

[ScopedService]
public sealed class ImageService(DiscordBotBase bot, AttachmentService attachments, AdminDbContext db, EmojiService emojis)
{
    public const int SCALE = 2;
    private const int XP_IMAGE_WIDTH = 450 * SCALE;
    private const int XP_IMAGE_HEIGHT = 300 * SCALE;
    private const uint XP_IMAGE_GUILD_OFFSET = 45 * SCALE;
    private const int XP_IMAGE_AVATAR_SIZE = 50 * SCALE;
    private const int XP_IMAGE_LEVEL_ICON_SIZE = 45 * SCALE;
    private const int XP_IMAGE_GUILD_ICON_SIZE = 18 * SCALE;
   
    private static readonly byte[] DefaultBackgroundBytes;
    
    public async Task<Result<LocalAttachment>> GenerateXpImageAsync(Snowflake? guildId, Snowflake userId)
    {
        var output = new MemoryStream();

        try
        {
            var user = await db.Users.GetOrCreateAsync(userId);
            var longId = (long)userId.RawValue;
            var globalPosition = await db.Users.ToLinqToDBTable()
                .Select(x => new
                {
                    UserId = (long) (ulong) x.UserId,
                    Rank = Sql.Ext.Rank().Over().OrderByDesc(x.TotalXp).ToValue()
                })
                .AsCte()
                .Where(x => x.UserId == longId)
                .Select(x => x.Rank)
                .FirstOrDefaultAsync();

            Member? member = null;
            GuildSettings guildSettings = default;
            var guildPosition = 0L;
            if (guildId.HasValue)
            {
                member = await db.Members.GetOrCreateAsync(guildId.Value, userId);

                var longGuildId = (long)guildId.Value.RawValue;
                guildPosition = await db.Members.ToLinqToDBTable()
                    .Where(x => (long) (ulong) x.GuildId == longGuildId)
                    .Select(x => new
                    {
                        UserId = (long) (ulong) x.UserId,
                        Rank = Sql.Ext.Rank().Over().OrderByDesc(x.TotalXp).ToValue()
                    })
                    .AsCte()
                    .Where(x => x.UserId == longId)
                    .Select(x => x.Rank)
                    .FirstOrDefaultAsync();

                guildSettings = await db.Guilds.GetValueOrDefault(guildId.Value, g => g.Settings);
            }
            
            var guildOffset = guildSettings.HasFlag(GuildSettings.TrackServerXp)
                ? XP_IMAGE_GUILD_OFFSET
                : 0;
            
            using var background = LoadBackgroundImage();
            using var avatar = await LoadAvatarImageAsync(guildId, userId);
            using var currentLevel = await LoadLevelImageAsync(user.GetTier(), user.GetLevel());

            background.DrawOuterBoundingBox(guildOffset)
                .DrawAvatarBoundingBox(guildOffset)
                .DrawAvatar(avatar, guildOffset)
                .DrawAvatarBoundingBoxOutline(guildOffset)
                .DrawUsername(bot.GetUser(userId)?.Name ?? "noname", guildOffset)
                .DrawInnerBox(guildOffset)
                .DrawCurrentXpBar(user.GetCurrentLevelXp(), user.GetNextLevelXp(), guildOffset)
                .DrawCurrentLevelText(user.GetTier(), user.GetLevel(), user.GetGrade(), guildOffset)
                .DrawCurrentXpText(user.TotalXp, user.GetNextLevelTotalXp(), guildOffset)
                .DrawCurrentLevel(currentLevel, guildOffset)
                .DrawCurrentGlobalPosition((int) globalPosition, guildOffset);
            
            if (member is not null && guildOffset > 0)
            {
                using var currentGuildLevel = await LoadLevelImageAsync(member.GetTier(), member.GetLevel());
                using var currentGuildIcon = await LoadGuildIconImageAsync(member.GuildId);
                
                background.DrawGuildBoundingBox()
                    .DrawGuildXpBarOutline()
                    .DrawGuildXpBar(member.GetCurrentLevelXp(), member.GetNextLevelXp())
                    .DrawCurrentGuildLevelText(member.GetTier(), member.GetLevel(), member.GetGrade())
                    .DrawCurrentGuildXpText(member.TotalXp, member.GetNextLevelTotalXp())
                    .DrawCurrentGuildLevel(currentGuildLevel)
                    .DrawCurrentGuildIcon(currentGuildIcon)
                    .DrawCurrentGuildPosition((int) guildPosition)
                    .DrawBlurb(member.Blurb, guildOffset);
            }
            
            await background.WriteAsync(output, MagickFormat.Png);
            output.Seek(0, SeekOrigin.Begin);
            return new LocalAttachment(output, "xp.png");
        }
        catch (Exception ex)
        {
            bot.Logger.LogWarning(ex, "Failed to generate XP image for user {UserId} in guild {GuildId}.", userId.RawValue, guildId?.RawValue);
            return $"Failed to generate XP image. Please report the below text to a developer:\n{ex.Message}";
        }
    }

    private static MagickImage LoadBackgroundImage()
    {
        if (DefaultBackgroundBytes.Length == 0) // image not loaded or empty(?)
            return new MagickImage(MagickColors.DarkGray,  (XP_IMAGE_WIDTH * SCALE),  (XP_IMAGE_HEIGHT * SCALE));

        var image = new MagickImage(DefaultBackgroundBytes);
        if (image.Width != XP_IMAGE_WIDTH || image.Height != XP_IMAGE_HEIGHT)
            image.Resize(XP_IMAGE_WIDTH, XP_IMAGE_HEIGHT);

        return image;
    }

    private async Task<MagickImage> LoadAvatarImageAsync(Snowflake? guildId, Snowflake userId)
    {
        var avatarUrl = guildId.HasValue && bot.GetMember(guildId.Value, userId) is { } member
            ? member.GetGuildAvatarUrl(CdnAssetFormat.Png)
            : bot.GetUser(userId)?.GetAvatarUrl() ?? Discord.Cdn.GetDefaultAvatarUrl(userId);

        try
        {
            var (bytes, _) = await attachments.GetAttachment(avatarUrl);
            var image = new MagickImage(bytes);
            image.Resize(XP_IMAGE_AVATAR_SIZE, XP_IMAGE_AVATAR_SIZE);
            return image;
        }
        catch
        {
            return LoadEmptyImage(XP_IMAGE_AVATAR_SIZE, XP_IMAGE_AVATAR_SIZE);
        }
    }

    private async Task<MagickImage> LoadLevelImageAsync(int tier, int level)
    {
        var levelEmoji = emojis.GetLevelEmoji(tier, level);

        try
        {
            var (bytes, _) = await attachments.GetAttachment(levelEmoji.GetUrl(CdnAssetFormat.Png));
            var image = new MagickImage(bytes);
            image.Resize(XP_IMAGE_LEVEL_ICON_SIZE / image.Height * image.Width, XP_IMAGE_LEVEL_ICON_SIZE);
            return image;
        }
        catch (Exception ex)
        {
            bot.Logger.LogWarning(ex, "Failed to load level image for tier {Tier}, level {Level}. Falling back to empty image.", tier, level);
            return LoadEmptyImage(XP_IMAGE_LEVEL_ICON_SIZE, XP_IMAGE_LEVEL_ICON_SIZE);
        }
    }

    private async Task<MagickImage> LoadGuildIconImageAsync(Snowflake guildId)
    {
        var guild = bot.GetGuild(guildId);

        try
        {
            var (bytes, _) = await attachments.GetAttachment(guild!.GetIconUrl(CdnAssetFormat.Png)!);
            var image = new MagickImage(bytes);
            image.Resize(XP_IMAGE_GUILD_ICON_SIZE, XP_IMAGE_GUILD_ICON_SIZE);
            return image;
        }
        catch
        {
            return LoadEmptyImage(XP_IMAGE_GUILD_ICON_SIZE, XP_IMAGE_GUILD_ICON_SIZE);
        }
    }

    private static MagickImage LoadEmptyImage(uint width, uint height)
        => new(MagickColors.Transparent, width, height);
    
    static ImageService()
    {
        const string xpImagePath = "Data/defaultXp.png";
        try
        {
            DefaultBackgroundBytes = File.ReadAllBytes(xpImagePath);
        }
        catch
        {
            DefaultBackgroundBytes = [];
        }
    }
}

public static class MagickImageExtensions
{
    private static readonly char[] AllowedSpecialCharacters =
    [
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
        'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', ' ', '#',
        '$', '€', '£', '+', '-', '*', '/', '÷', '=', '%', '"', '\'', 
        '@', '&', '_', '(', ')', ',', '.', ';', ':', '¿', '?', '¡', '!', 
        '\\', '{', '}', '<', '>', '[', ']', '`', '^', '~', '©', '®', '™',
        'À', 'Á', 'Â', 'Ã', 'Ä', 'Å', 'Æ', 'Ç', 'È', 'É', 'Ê', 'Ë', 'Ì', 'Í', 
        'Î', 'Ï', 'Ñ', 'Ò', 'Ó', 'Ô', 'Õ', 'Ö', 'Ø', 'Ù', 'Ú', 'Û', 'Ü', 'ß',
        'à', 'á', 'â', 'ã', 'ä', 'å', 'æ', 'ç', 'è', 'é', 'ê', 'ë', 'ì', 'í', 
        'î', 'ï', 'ñ', 'ò', 'ó', 'ô', 'õ', 'ö', 'ø', 'œ', 'ù', 'ú', 'û', 'ü', 'ß'
    ];
    
    public static MagickImage DrawOuterBoundingBox(this MagickImage image, uint offset)
    {
        const int leftX = 10 * ImageService.SCALE;
        var topY = 190 * ImageService.SCALE - offset;
        const int rightX = 440 * ImageService.SCALE;
        var bottomY = 290 * ImageService.SCALE - offset;
                
        image.Draw(new Drawables()
            .FillColor(Colors.DarkButTransparent)
            .Polygon(new PointD(leftX, topY),
                new PointD(rightX, topY),
                new PointD(rightX, bottomY),
                new PointD(leftX, bottomY)));
    
        return image;
    }

    public static MagickImage DrawAvatarBoundingBox(this MagickImage image, uint offset)
    {
        const int leftX = 385 * ImageService.SCALE;
        var topY = 215 * ImageService.SCALE - offset;
        const int rightX = 435 * ImageService.SCALE;
        var bottomY = 265 * ImageService.SCALE - offset;
                
        image.Draw(new Drawables()
            .FillColor(Colors.Blurple)
            .Polygon(new PointD(leftX, topY),
                new PointD(rightX, topY),
                new PointD(rightX, bottomY),
                new PointD(leftX, bottomY)));
    
        return image;
    }

    public static MagickImage DrawAvatar(this MagickImage image, MagickImage avatar, uint offset)
    {
        const int originX = 385 * ImageService.SCALE;
        var originY = 215 * ImageService.SCALE - offset;
        image.Composite(avatar, originX, (int) originY, CompositeOperator.Atop);
    
        return image;
    }

    public static MagickImage DrawAvatarBoundingBoxOutline(this MagickImage image, uint offset)
    {
        const int leftX = 385 * ImageService.SCALE;
        var topY = 215 * ImageService.SCALE - offset;
        const int rightX = 435 * ImageService.SCALE;
        var bottomY = 265 * ImageService.SCALE - offset;
                
        image.Draw(new Drawables()
            .FillColor(MagickColors.Transparent)
            .StrokeColor(MagickColors.WhiteSmoke)
            .StrokeWidth(2d)
            .Path(new PathLineToAbs(new PointD(leftX, topY),
                new PointD(rightX, topY),
                new PointD(rightX, bottomY),
                new PointD(leftX, bottomY),
                new PointD(leftX, topY)))); // end where we start
    
        return image;
    }

    public static MagickImage DrawUsername(this MagickImage image, string username, uint offset)
    {
        const int fontSize = 20 * ImageService.SCALE;
        const int originX = 15 * ImageService.SCALE;
        var originY = 195 * ImageService.SCALE - offset;
                
        image.Draw(Fonts.TF2()
            .FontPointSize(fontSize)
            .FillColor(MagickColors.WhiteSmoke)
            .Gravity(Gravity.Northwest)
            .Text(originX, originY, username));
    
        return image;
    }

    public static MagickImage DrawInnerBox(this MagickImage image, uint offset)
    {
        const int leftX = 75 * ImageService.SCALE;
        var topY = 272 * ImageService.SCALE - offset;
        const int rightX = 435 * ImageService.SCALE;
        var bottomY = 285 * ImageService.SCALE - offset;
                
        image.Draw(new Drawables()
            .FillColor(Colors.LessDark)
            .Polygon(new PointD(leftX, topY),
                new PointD(rightX, topY),
                new PointD(rightX, bottomY),
                new PointD(leftX, bottomY)));
    
        return image;
    }

    public static MagickImage DrawCurrentXpBar(this MagickImage image, int currentXp, int nextLevelXp, uint offset)
    {
        const int leftX = 77 * ImageService.SCALE;
        var topY = 274 * ImageService.SCALE - offset;
        var rightX = (356d * ((double) currentXp / nextLevelXp) + 77) * ImageService.SCALE;
        var bottomY = 283 * ImageService.SCALE - offset;
                
        image.Draw(new Drawables()
            .FillColor(Colors.XpBar)
            .Polygon(new PointD(leftX, topY),
                new PointD(rightX, topY),
                new PointD(rightX, bottomY),
                new PointD(leftX, bottomY)));
    
        return image;
    }

    public static MagickImage DrawCurrentLevelText(this MagickImage image, int tier, int level, Grade grade, uint offset)
    {
        const int fontSize = 13 * ImageService.SCALE;
        const int originX = 255 * ImageService.SCALE;
        var originY = 269 * ImageService.SCALE - offset;
                
        image.Draw(Fonts.TF2()
            .FontPointSize(fontSize)
            .FillColor(Colors.GetGradeColor(grade))
            //.Gravity(Gravity.North)
            .TextAlignment(TextAlignment.Center)
            .Text(originX, originY, $"Tier {tier}, Level {level} ({grade} Grade)")); // 259
    
        return image;
    }

    public static MagickImage DrawCurrentXpText(this MagickImage image, int totalXp, int nextLevelTotalXp, uint offset)
    {
        const int fontSize = 13 * ImageService.SCALE;
        const int originX = 255 * ImageService.SCALE;
        var originY = 284 * ImageService.SCALE - offset;
                
        image.Draw(Fonts.TF2()
            .FontPointSize(fontSize)
            .FillColor(MagickColors.WhiteSmoke)
            //.Gravity(Gravity.North)
            .TextAlignment(TextAlignment.Center)
            .Text(originX, originY, $"{totalXp} / {nextLevelTotalXp} XP")); // 273
    
        return image;
    }

    public static MagickImage DrawCurrentLevel(this MagickImage image, MagickImage currentLevel, uint offset)
    {
        const int originX = 45 * ImageService.SCALE;
        var originY = 285 * ImageService.SCALE - offset;
        var justifiedOrigin = Justify(originX, originY, currentLevel, Gravity.South);
                
        image.Composite(currentLevel, (int) justifiedOrigin.X, (int) justifiedOrigin.Y, CompositeOperator.Atop);
        
        return image;
    }

    public static MagickImage DrawCurrentGlobalPosition(this MagickImage image, int globalPosition, uint offset)
    {
        const int fontSize = 11 * ImageService.SCALE;
        const int originX = 255 * ImageService.SCALE;
        var originY = 258 * ImageService.SCALE - offset;
                
        image.Draw(Fonts.TF2()
            .FontPointSize(fontSize)
            .FillColor(MagickColors.WhiteSmoke)
            //.Gravity(Gravity.North)
            .TextAlignment(TextAlignment.Center)
            .Text(originX, originY, $"Global rank #{globalPosition}")); // 248
    
        return image;}

    public static MagickImage DrawGuildBoundingBox(this MagickImage image)
    {
        const int leftX = 10 * ImageService.SCALE;
        const int topY = 250 * ImageService.SCALE;
        const int rightX = 440 * ImageService.SCALE;
        const int bottomY = 295 * ImageService.SCALE;
                    
        image.Draw(new Drawables()
            .FillColor(Colors.DarkButTransparent)
            .Polygon(new PointD(leftX, topY),
                new PointD(rightX, topY),
                new PointD(rightX, bottomY),
                new PointD(leftX, bottomY)));
    
        return image;}

    public static MagickImage DrawGuildXpBarOutline(this MagickImage image)
    {
        const int leftX = 75 * ImageService.SCALE;
        const int topY = 277 * ImageService.SCALE;
        const int rightX = 435 * ImageService.SCALE;
        const int bottomY = 290 * ImageService.SCALE;
                    
        image.Draw(new Drawables()
            .FillColor(Colors.LessDark)
            .Polygon(new PointD(leftX, topY),
                new PointD(rightX, topY),
                new PointD(rightX, bottomY),
                new PointD(leftX, bottomY)));
    
        return image;}

    public static MagickImage DrawGuildXpBar(this MagickImage image, int currentXp, int nextLevelXp)
    {
        const int leftX = 77 * ImageService.SCALE;
        const int topY = 279 * ImageService.SCALE;
        var rightX = (356d * ((double) currentXp / nextLevelXp) + 77) * ImageService.SCALE;
        const int bottomY = 288 * ImageService.SCALE;
                    
        image.Draw(new Drawables()
            .FillColor(Colors.XpBar)
            .Polygon(new PointD(leftX, topY),
                new PointD(rightX, topY),
                new PointD(rightX, bottomY),
                new PointD(leftX, bottomY)));
    
        return image;}

    public static MagickImage DrawCurrentGuildLevelText(this MagickImage image, int tier, int level, Grade grade)
    {
        const int fontSize = 13 * ImageService.SCALE;
        const int originX = 255 * ImageService.SCALE;
        const int originY = 274 * ImageService.SCALE;
                    
        image.Draw(Fonts.TF2()
            .FontPointSize(fontSize)
            .FillColor(Colors.GetGradeColor(grade))
            //.Gravity(Gravity.North)
            .TextAlignment(TextAlignment.Center)
            .Text(originX, originY, $"Tier {tier}, Level {level} ({grade} Grade)")); // 264
        
        return image;
    }

    public static MagickImage DrawCurrentGuildXpText(this MagickImage image, int totalXp, int nextLevelTotalXp)
    {
        const int fontSize = 13 * ImageService.SCALE;
        const int originX = 255 * ImageService.SCALE;
        const int originY = 289 * ImageService.SCALE;
                    
        image.Draw(Fonts.TF2()
            .FontPointSize(fontSize)
            .FillColor(MagickColors.WhiteSmoke)
            //.Gravity(Gravity.North)
            .TextAlignment(TextAlignment.Center)
            .Text(originX, originY, $"{totalXp} / {nextLevelTotalXp} XP")); // 278
        
        return image;
    }

    public static MagickImage DrawCurrentGuildLevel(this MagickImage image, MagickImage currentGuildLevel)
    {
        const int originX = 45 * ImageService.SCALE;
        const int originY = 292 * ImageService.SCALE;
                    
        var justifiedOrigin = Justify(originX, originY, currentGuildLevel, Gravity.South);
        image.Composite(currentGuildLevel, (int) justifiedOrigin.X,  (int) justifiedOrigin.Y, CompositeOperator.Atop);
        
        return image;
    }

    public static MagickImage DrawCurrentGuildIcon(this MagickImage image, MagickImage currentGuildIcon)
    {
        const int originX = 435 * ImageService.SCALE;
        const int originY = 255 * ImageService.SCALE;
                    
        var justifiedOrigin = Justify(originX, originY, currentGuildIcon, Gravity.Northeast);
        image.Composite(currentGuildIcon,  (int) justifiedOrigin.X, (int) justifiedOrigin.Y, CompositeOperator.Atop);
        return image;
    }

    public static MagickImage DrawCurrentGuildPosition(this MagickImage image, int guildPosition)
    {
        const int fontSize = 11 * ImageService.SCALE;
        const int originX = 255 * ImageService.SCALE;
        const int originY = 263 * ImageService.SCALE;
                    
        image.Draw(Fonts.TF2()
            .FontPointSize(fontSize)
            .FillColor(MagickColors.WhiteSmoke)
            //.Gravity(Gravity.North)
            .TextAlignment(TextAlignment.Center)
            .Text(originX, originY, $"Server rank #{guildPosition}")); // 253

        return image;
    }

    public static MagickImage DrawBlurb(this MagickImage image, string blurb, uint offset)
    {
        const int fontSize = 11 * ImageService.SCALE;
        const int originX = 15 * ImageService.SCALE;
        var originY = 215 * ImageService.SCALE - offset;
        const int textBoxWith = 360 * ImageService.SCALE;
        var sanitizedBlurb = new string(blurb.Where(AllowedSpecialCharacters.Contains).ToArray());

        var settings = new MagickReadSettings
        {
            Font = "Data/TF2secondary.ttf",
            BackgroundColor = MagickColors.Transparent,
            FillColor = MagickColors.WhiteSmoke,
            StrokeColor = MagickColors.Transparent,
            FontPointsize = fontSize,
            Width =  textBoxWith
        };

        using var blurbImage = new MagickImage($"caption:\"{sanitizedBlurb}\"", settings);
        image.Composite(blurbImage, originX, (int) originY, CompositeOperator.Atop);

        return image;
    }
    
    private static (uint X, uint Y) Justify(uint x, uint y, MagickImage image, Gravity gravity)
    {
        return gravity switch
        {
            Gravity.Northwest => (x, y),
            Gravity.North => (x - image.Width / 2, y),
            Gravity.Northeast => (x - image.Width, y),
            Gravity.West => (x, y - image.Height / 2),
            Gravity.Center => (x - image.Width / 2, y - image.Height / 2),
            Gravity.East => (x - image.Width, y - image.Height / 2),
            Gravity.Southwest => (x, y - image.Height),
            Gravity.South => (x - image.Width / 2, y - image.Height),
            Gravity.Southeast => (x - image.Width, y - image.Height),
            _ => (x, y)
        };
    }
    
    private static class Fonts
    {
        public static IDrawables<ushort> TF2(FontStyleType type = FontStyleType.Normal) => new Drawables().Font("Data/tf2build.ttf", type, FontWeight.Normal, FontStretch.Normal)
            .StrokeColor(MagickColors.Transparent);
    }
        
    private static class Colors
    {
        public static MagickColor GetGradeColor(Grade grade)
        {
            return grade switch
            {
                Grade.Civilian => MagickColor.FromRgb(176, 195, 217),
                Grade.Freelance => MagickColor.FromRgb(94, 152, 217),
                Grade.Mercenary => MagickColor.FromRgb(75, 105, 255),
                Grade.Commando => MagickColor.FromRgb(136, 71, 255),
                Grade.Assassin => MagickColor.FromRgb(211, 44, 230),
                Grade.Elite => MagickColor.FromRgb(235, 75, 75),
                _ => MagickColors.White
            };
        }
        
        internal static MagickColor XpBar
            => MagickColor.FromRgb(94, 151, 45);

        internal static MagickColor Background
            => MagickColor.FromRgb(44, 47, 51);

        internal static MagickColor DarkButTransparent
            => MagickColor.FromRgba(35, 39, 42, 225);

        internal static MagickColor Blurple
            => MagickColor.FromRgb(88, 101, 242);

        internal static MagickColor LessDark
            => MagickColor.FromRgb(67, 74, 79);

        internal static MagickColor WayLessDark
            => MagickColor.FromRgb(104, 110, 117);
    }
}