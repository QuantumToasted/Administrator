using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using ImageMagick;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

[ScopedService]
public sealed class ImageService(DiscordBotBase bot, AttachmentService attachments, AdminDbContext db, EmojiService emojis)
{
    private const int SCALE = 2;
    private const int XP_IMAGE_WIDTH = 450 * SCALE;
    private const int XP_IMAGE_HEIGHT = 300 * SCALE;
    private const int XP_IMAGE_GUILD_OFFSET = 45 * SCALE;
    private const int XP_IMAGE_AVATAR_SIZE = 50 * SCALE;
    private const int XP_IMAGE_LEVEL_ICON_SIZE = 45 * SCALE;
    private const int XP_IMAGE_GUILD_ICON_SIZE = 18 * SCALE;
    
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
            Guild? guild = null;
            var guildPosition = 0L;
            if (guildId.HasValue)
            {
                member = await db.Members.GetOrCreateAsync(guildId.Value, userId);
                guild = await db.Guilds.GetOrCreateAsync(guildId.Value);

                guildPosition = await db.Members.ToLinqToDBTable()
                    .Where(x => x.GuildId == guildId.Value)
                    .Select(x => new
                    {
                        UserId = (long) (ulong) x.UserId,
                        Rank = Sql.Ext.Rank().Over().OrderByDesc(x.TotalXp).ToValue()
                    })
                    .AsCte()
                    .Where(x => x.UserId == longId)
                    .Select(x => x.Rank)
                    .FirstOrDefaultAsync();
            }

            var guildOffset = guild?.HasSetting(GuildSettings.TrackServerXp) == true
                ? XP_IMAGE_GUILD_OFFSET
                : 0;
            
            using var background = LoadBackgroundImage();
            using var avatar = await LoadAvatarImageAsync(guildId, userId);
            using var currentLevel = await LoadLevelImageAsync(user.Tier, user.Level);

            // Draw outer bounding box
            {
                const int leftX = 10 * SCALE;
                var topY = 190 * SCALE - guildOffset;
                const int rightX = 440 * SCALE;
                var bottomY = 290 * SCALE - guildOffset;
                
                background.Draw(new Drawables()
                    .FillColor(Colors.DarkButTransparent)
                    .Polygon(new PointD(leftX, topY),
                        new PointD(rightX, topY),
                        new PointD(rightX, bottomY),
                        new PointD(leftX, bottomY)));
            }
            
            // Draw avatar bounding box
            {
                const int leftX = 385 * SCALE;
                var topY = 215 * SCALE - guildOffset;
                const int rightX = 435 * SCALE;
                var bottomY = 265 * SCALE - guildOffset;
                
                background.Draw(new Drawables()
                    .FillColor(Colors.Blurple)
                    .Polygon(new PointD(leftX, topY),
                        new PointD(rightX, topY),
                        new PointD(rightX, bottomY),
                        new PointD(leftX, bottomY)));
            }
            
            
            // Draw avatar
            {
                const int originX = 385 * SCALE;
                var originY = 215 * SCALE - guildOffset;
                background.Composite(avatar, originX, originY, CompositeOperator.Atop);
            }
            
            // Draw avatar bounding box outline
            {
                const int leftX = 385 * SCALE;
                var topY = 215 * SCALE - guildOffset;
                const int rightX = 435 * SCALE;
                var bottomY = 265 * SCALE - guildOffset;
                
                background.Draw(new Drawables()
                    .FillColor(MagickColors.Transparent)
                    .StrokeColor(MagickColors.WhiteSmoke)
                    .StrokeWidth(2d)
                    .Path(new PathLineToAbs(new PointD(leftX, topY),
                        new PointD(rightX, topY),
                        new PointD(rightX, bottomY),
                        new PointD(leftX, bottomY),
                        new PointD(leftX, topY)))); // end where we start
            }
            
            // Write username
            {
                const int fontSize = 20 * SCALE;
                const int originX = 15 * SCALE;
                var originY = 195 * SCALE - guildOffset;
                
                background.Draw(new Drawables()
                    .Font("Data/tf2build.ttf")
                    .FontPointSize(fontSize)
                    .FillColor(MagickColors.WhiteSmoke)
                    .Gravity(Gravity.Northwest)
                    .Text(originX, originY, bot.GetUser(userId)!.Name));
            }
            
            // Draw inner box (XP bar outline)
            {
                const int leftX = 75 * SCALE;
                var topY = 272 * SCALE - guildOffset;
                const int rightX = 435 * SCALE;
                var bottomY = 285 * SCALE - guildOffset;
                
                background.Draw(new Drawables()
                    .FillColor(Colors.LessDark)
                    .Polygon(new PointD(leftX, topY),
                        new PointD(rightX, topY),
                        new PointD(rightX, bottomY),
                        new PointD(leftX, bottomY)));
            }
            
            // Draw current XP bar
            {
                const int leftX = 77 * SCALE;
                var topY = 274 * SCALE - guildOffset;
                var rightX = (356d * ((double) user.CurrentLevelXp / user.NextLevelXp) + 77) * SCALE;
                var bottomY = 283 * SCALE - guildOffset;
                
                background.Draw(new Drawables()
                    .FillColor(Colors.XpBar)
                    .Polygon(new PointD(leftX, topY),
                        new PointD(rightX, topY),
                        new PointD(rightX, bottomY),
                        new PointD(leftX, bottomY)));
            }
            
            // Write current level text
            {
                const int fontSize = 13 * SCALE;
                const int originX = 255 * SCALE;
                var originY = 269 * SCALE - guildOffset;
                
                background.Draw(Fonts.TF2()
                    .FontPointSize(fontSize)
                    .FillColor(Colors.GetGradeColor(user.Grade))
                    //.Gravity(Gravity.North)
                    .TextAlignment(TextAlignment.Center)
                    .Text(originX, originY, $"Tier {user.Tier}, Level {user.Level} ({user.Grade} Grade)")); // 259
            }
            
            
            // Write current XP text
            {
                const int fontSize = 13 * SCALE;
                const int originX = 255 * SCALE;
                var originY = 284 * SCALE - guildOffset;
                
                background.Draw(Fonts.TF2()
                    .FontPointSize(fontSize)
                    .FillColor(MagickColors.WhiteSmoke)
                    //.Gravity(Gravity.North)
                    .TextAlignment(TextAlignment.Center)
                    .Text(originX, originY, $"{user.TotalXp} / {user.NextLevelTotalXp} XP")); // 273
            }
            
            // Draw current level
            {
                const int originX = 45 * SCALE;
                var originY = 285 * SCALE - guildOffset;
                var justifiedOrigin = Justify(originX, originY, currentLevel, Gravity.South);
                
                background.Composite(currentLevel, justifiedOrigin.X,  justifiedOrigin.Y, CompositeOperator.Atop);
            }
            
            // Write current global position
            {
                const int fontSize = 11 * SCALE;
                const int originX = 255 * SCALE;
                var originY = 258 * SCALE - guildOffset;
                
                background.Draw(Fonts.TF2()
                    .FontPointSize(fontSize)
                    .FillColor(MagickColors.WhiteSmoke)
                    //.Gravity(Gravity.North)
                    .TextAlignment(TextAlignment.Center)
                    .Text(originX, originY, $"Global rank #{globalPosition}")); // 248
            }
            

            if (member is not null && guildOffset > 0)
            {
                // Draw guild bounding box
                {
                    const int leftX = 10 * SCALE;
                    const int topY = 250 * SCALE;
                    const int rightX = 440 * SCALE;
                    const int bottomY = 295 * SCALE;
                    
                    background.Draw(new Drawables()
                        .FillColor(Colors.DarkButTransparent)
                        .Polygon(new PointD(leftX, topY),
                            new PointD(rightX, topY),
                            new PointD(rightX, bottomY),
                            new PointD(leftX, bottomY)));
                }
                
                
                // Draw guild XP bar outline
                {
                    const int leftX = 75 * SCALE;
                    const int topY = 277 * SCALE;
                    const int rightX = 435 * SCALE;
                    const int bottomY = 290 * SCALE;
                    
                    background.Draw(new Drawables()
                        .FillColor(Colors.LessDark)
                        .Polygon(new PointD(leftX, topY),
                            new PointD(rightX, topY),
                            new PointD(rightX, bottomY),
                            new PointD(leftX, bottomY)));
                }
                
                // Draw guild XP bar
                {
                    const int leftX = 77 * SCALE;
                    const int topY = 279 * SCALE;
                    var rightX = (356d * ((double) user.CurrentLevelXp / user.NextLevelXp) + 77) * SCALE;
                    const int bottomY = 288 * SCALE;
                    
                    background.Draw(new Drawables()
                        .FillColor(Colors.XpBar)
                        .Polygon(new PointD(leftX, topY),
                            new PointD(rightX, topY),
                            new PointD(rightX, bottomY),
                            new PointD(leftX, bottomY)));
                }
                
                // Write current guild level text
                {
                    const int fontSize = 13 * SCALE;
                    const int originX = 255 * SCALE;
                    const int originY = 274 * SCALE;
                    
                    background.Draw(Fonts.TF2()
                        .FontPointSize(fontSize)
                        .FillColor(Colors.GetGradeColor(member.Grade))
                        //.Gravity(Gravity.North)
                        .TextAlignment(TextAlignment.Center)
                        .Text(originX, originY, $"Tier {member.Tier}, Level {member.Level} ({member.Grade} Grade)")); // 264
                }
                
                // Write current guild XP text
                {
                    const int fontSize = 13 * SCALE;
                    const int originX = 255 * SCALE;
                    const int originY = 289 * SCALE;
                    
                    background.Draw(Fonts.TF2()
                        .FontPointSize(fontSize)
                        .FillColor(MagickColors.WhiteSmoke)
                        //.Gravity(Gravity.North)
                        .TextAlignment(TextAlignment.Center)
                        .Text(originX, originY, $"{member.TotalXp} / {member.NextLevelTotalXp} XP")); // 278
                }
                
                // Draw current guild level
                {
                    const int originX = 45 * SCALE;
                    const int originY = 292 * SCALE;
                    
                    using var currentGuildLevel = await LoadLevelImageAsync(member.Tier, member.Level);
                    var justifiedOrigin = Justify(originX, originY, currentGuildLevel, Gravity.South);
                    background.Composite(currentGuildLevel,  justifiedOrigin.X,  justifiedOrigin.Y, CompositeOperator.Atop);
                }
                
                // Draw current guild icon
                {
                    const int originX = 435 * SCALE;
                    const int originY = 255 * SCALE;
                    
                    using var currentGuildIcon = await LoadGuildIconImageAsync(member.GuildId);
                    var justifiedOrigin = Justify(originX, originY, currentGuildIcon, Gravity.Northeast);
                    background.Composite(currentGuildIcon,  justifiedOrigin.X,  justifiedOrigin.Y, CompositeOperator.Atop);
                }
                
                // Write current guild position
                {
                    const int fontSize = 11 * SCALE;
                    const int originX = 255 * SCALE;
                    const int originY = 263 * SCALE;
                    
                    background.Draw(Fonts.TF2()
                        .FontPointSize(fontSize)
                        .FillColor(MagickColors.WhiteSmoke)
                        //.Gravity(Gravity.North)
                        .TextAlignment(TextAlignment.Center)
                        .Text(originX, originY, $"Guild rank #{guildPosition}")); // 253
                }
                
                // Write blurb
                {
                    const int fontSize = 11 * SCALE;
                    const int originX = 15 * SCALE;
                    var originY = 215 * SCALE - guildOffset;
                    const int textBoxWith = 360 * SCALE;
                    var sanitizedBlurb = new string(member.Blurb.Where(AllowedSpecialCharacters.Contains).ToArray());

                    var settings = new MagickReadSettings
                    {
                        Font = "Data/TF2secondary.ttf",
                        BackgroundColor = MagickColors.Transparent,
                        FillColor = MagickColors.WhiteSmoke,
                        StrokeColor = MagickColors.Transparent,
                        FontPointsize = fontSize,
                        Width =  textBoxWith
                    };

                    using var image = new MagickImage($"caption:\"{sanitizedBlurb}\"", settings);
                    background.Composite(image,  originX,  originY, CompositeOperator.Atop);
                }
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
            using var attachment = await attachments.GetAttachmentAsync(avatarUrl);
            var image = new MagickImage(attachment.Stream.ToArray());
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
            using var attachment = await attachments.GetAttachmentAsync(levelEmoji.GetUrl(CdnAssetFormat.Png));
            var image = new MagickImage(attachment.Stream.ToArray());
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
            using var attachment = await attachments.GetAttachmentAsync(guild!.GetIconUrl(CdnAssetFormat.Png)!);
            var image = new MagickImage(attachment.Stream.ToArray());
            image.Resize(XP_IMAGE_GUILD_ICON_SIZE, XP_IMAGE_GUILD_ICON_SIZE);
            return image;
        }
        catch
        {
            return LoadEmptyImage(XP_IMAGE_GUILD_ICON_SIZE, XP_IMAGE_GUILD_ICON_SIZE);
        }
    }

    private static MagickImage LoadEmptyImage(int width, int height)
        => new(MagickColors.Transparent, width, height);
    
    public static (int X, int Y) Justify(int x, int y, MagickImage image, Gravity gravity)
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

    private static class Fonts
    {
        public static IDrawables<ushort> TF2(FontStyleType type = FontStyleType.Normal) => new Drawables().Font("Data/tf2build.ttf", type, FontWeight.Normal, FontStretch.Normal)
            .StrokeColor(MagickColors.Transparent);
    }

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