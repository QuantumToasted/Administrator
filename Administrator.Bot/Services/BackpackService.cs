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

public sealed class BackpackService(BackpackClient backpack, ISteamWebInterfaceFactory factory, HttpClient http, AttachmentServiceNew attachments) : DiscordBotService
{
    private readonly EconItems _econItems = factory.CreateSteamWebInterface<EconItems>(AppId.TeamFortress2, http);
    private readonly ConcurrentDictionary<ParticleEffect, FileInfo> _particleEffectImages = new();
    
    public Currency? CraftHatCurrency { get; private set; }

    public Currency? EarbudsCurrency { get; private set; }

    public Currency? CrateKeyCurrency { get; private set; }

    public Currency? RefinedMetalCurrency { get; private set; }

    public IReadOnlyDictionary<int, string> SchemaImages { get; private set; } = null!;
    
    public IReadOnlyDictionary<string, Item> ItemPrices { get; private set; } = new Dictionary<string, Item>();

    public async Task<LocalAttachment> GetItemImageAsync(int defIndex, ParticleEffect? effect)
    {
        await using var scope = Bot.Services.CreateAsyncScope();

        var item = await attachments.GetAttachment(SchemaImages[defIndex]);
        using var itemImage = new MagickImage(item.Data);
        itemImage.Resize(380, 380);

        if (effect.HasValue && GetParticleEffectImage(effect.Value) is { } particleEffectImage)
        {
            using (particleEffectImage)
            {
                itemImage.Composite(particleEffectImage, CompositeOperator.DstOver);
            }
        }

        var output = new MemoryStream();
        await itemImage.WriteAsync(output, MagickFormat.Png);
        output.Seek(0, SeekOrigin.Begin);

        return new LocalAttachment(output, $"{defIndex}.png");
    }

    private MagickImage? GetParticleEffectImage(ParticleEffect effect)
    {
        var found = _particleEffectImages.TryGetValue(effect, out var file);
        if (!found)
        {
            return effect != 0
                ? GetParticleEffectImage(0)
                : null;
        }

        return new MagickImage(file!);
    }

    public Task<ItemPrices> GetItemPricesAsync() => backpack.GetItemPricesAsync(CurrencyValue.Raw);
    
    public Task<Currencies> GetCurrenciesAsync() => backpack.GetCurrenciesAsync(CurrencyValue.Raw);
        
    public void UpdateCurrencies(Currencies currencies)
    {
        CraftHatCurrency = currencies.CraftHat;
        EarbudsCurrency = currencies.Earbuds;
        CrateKeyCurrency = currencies.CrateKey;
        RefinedMetalCurrency = currencies.RefinedMetal;
    }

    public void UpdateItemPrices(ItemPrices itemPricesResponse)
    {
        var itemPrices = ItemPrices.ToDictionary(x => x.Key, x => x.Value);

        foreach (var (name, item) in itemPricesResponse.Items)
        {
            itemPrices[name] = item;
        }
        
        ItemPrices = itemPrices;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);
        await InitializeSchemaImagesAsync();
        InitializeParticleEffectImages();
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

    private void InitializeParticleEffectImages()
    {
        var count = 0;
        foreach (var path in Directory.EnumerateFiles("Data/particles", "*.png"))
        {
            var file = new FileInfo(path);
            var effectId = int.Parse(file.Name.Split('_')[0]);
            var effect = (ParticleEffect)effectId;
            _particleEffectImages[effect] = file;
            count++;
        }
        
        Logger.LogInformation("Loaded {Count} Unusual particle effect images.", count);
    }
}