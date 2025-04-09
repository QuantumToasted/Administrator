using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Bot.Jobs;

public class BackpackUpdateJob(ILogger<BackpackUpdateJob> logger, BackpackService backpack) : IAdminJob<BackpackUpdateJob>
{
    public ILogger Logger { get; } = logger;
    
    public async ValueTask Execute(IJobExecutionContext context)
    {
        try
        {
            var currencies = await backpack.GetCurrenciesAsync();
            if (!currencies.IsSuccess)
                throw new Exception(currencies.ErrorMessage);
            
            backpack.UpdateCurrencies(currencies);
            Logger.LogDebug("Currency information updated!");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update backpack.tf currencies.");
        }
        
        try
        {
            var itemPricesResponse = await backpack.GetItemPricesAsync();
            if (!itemPricesResponse.IsSuccess)
                throw new Exception(itemPricesResponse.ErrorMessage);
            
            backpack.UpdateItemPrices(itemPricesResponse);
            Logger.LogDebug("Item price information for {Count} items updated!", itemPricesResponse.Items.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update backpack.tf item prices.");
        }
    }
}