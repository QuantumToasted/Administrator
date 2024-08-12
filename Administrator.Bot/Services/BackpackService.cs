using System.Collections.Concurrent;
using Backpack.Net;
using Disqord;
using Disqord.Bot.Hosting;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

namespace Administrator.Bot;

public sealed class BackpackService(BackpackClient backpack, ISteamWebInterfaceFactory factory, HttpClient http) : DiscordBotService
{
    private readonly EconItems _econItems = factory.CreateSteamWebInterface<EconItems>(AppId.TeamFortress2, http);
    private readonly ConcurrentDictionary<ParticleEffect, byte[]> _particleEffectImages = new();
    private DateTimeOffset? _lastCheck;
    
    public Currency? CraftHatCurrency { get; private set; }

    public Currency? EarbudsCurrency { get; private set; }

    public Currency? CrateKeyCurrency { get; private set; }

    public Currency? RefinedMetalCurrency { get; private set; }

    public IReadOnlyDictionary<int, string> SchemaImages { get; private set; } = null!;
    
    public IReadOnlyDictionary<string, Item> ItemPrices { get; private set; } = new Dictionary<string, Item>();

    public async Task<LocalAttachment> GetItemImageAsync(int defIndex, ParticleEffect? effect)
    {
        await using var scope = Bot.Services.CreateAsyncScope();
        var attachments = scope.ServiceProvider.GetRequiredService<AttachmentService>();

        using MemoryStream itemStream = await attachments.GetAttachmentAsync(SchemaImages[defIndex]);
        using var item = new MagickImage(itemStream);
        item.Resize(380, 380);

        if (effect.HasValue)
        {
            var particleEffectImage = await GetParticleEffectImageAsync(effect.Value);
            using var particleEffect = new MagickImage(particleEffectImage);
            
            item.Composite(particleEffect, CompositeOperator.DstOver);
        }

        var output = new MemoryStream();
        await item.WriteAsync(output, MagickFormat.Png);
        output.Seek(0, SeekOrigin.Begin);

        return new LocalAttachment(output, $"{defIndex}.png");
    }

    private async ValueTask<byte[]> GetParticleEffectImageAsync(ParticleEffect effect)
    {
        const string particleEffectSource = "https://backpack.tf/images/440/particles/{0}_380x380.png";
        
        if (_particleEffectImages.TryGetValue(effect, out var bytes))
            return bytes;

        await using var scope = Bot.Services.CreateAsyncScope();
        var attachments = scope.ServiceProvider.GetRequiredService<AttachmentService>();

        using MemoryStream effectStream = await attachments.GetAttachmentAsync(string.Format(particleEffectSource, (int)effect));
        return _particleEffectImages[effect] = effectStream.ToArray();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);

        await InitializeSchemaImagesAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdateCurrenciesAsync();
            await UpdateItemPricesAsync();
            
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task UpdateCurrenciesAsync()
    {
        try
        {
            var currencies = await backpack.GetCurrenciesAsync(CurrencyValue.Raw);
            if (!currencies.IsSuccess)
                throw new Exception(currencies.ErrorMessage);
            
            CraftHatCurrency = currencies.CraftHat;
            EarbudsCurrency = currencies.Earbuds;
            CrateKeyCurrency = currencies.CrateKey;
            RefinedMetalCurrency = currencies.RefinedMetal;
            
            Logger.LogDebug("Currency information updated!");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update backpack.tf currency information.");
        }
    }

    private async Task UpdateItemPricesAsync()
    {
        var itemPrices = ItemPrices.ToDictionary(x => x.Key, x => x.Value);
        
        try
        {
            var itemPriceResponse = await backpack.GetItemPricesAsync(CurrencyValue.Raw, _lastCheck);
            if (!itemPriceResponse.IsSuccess)
                throw new Exception(itemPriceResponse.ErrorMessage);

            var count = 0;
            foreach (var (name, item) in itemPriceResponse.Items)
            {
                itemPrices[name] = item;
                count++;
            }
            
            if (count > 0)
                Logger.LogDebug("Item price information for {Count} items updated!", count);
            
            _lastCheck = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update backpack.tf item price information.");
        }

        ItemPrices = itemPrices;
    }

    private async Task InitializeSchemaImagesAsync()
    {
        var schemaImages = new Dictionary<int, string>();

        try
        {
            uint? next = null;
            do
            {
                var response = await _econItems.GetSchemaItemsForTF2Async(start: next);
                foreach (var item in response.Data.Result.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.ImageUrlLarge))
                        continue;

                    schemaImages[(int)item.DefIndex] = item.ImageUrlLarge;
                }

                next = response.Data.Result.Next;
            } while (next.HasValue);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to initialize schema image dictionary.");
        }
        
        SchemaImages = schemaImages;
    }
}