using Disqord;
using Disqord.Bot.Commands.Interaction;

namespace Administrator.Bot;

public static partial class DiscordExtensions
{
    public static DiscordInteractionResponseCommandResult AsEphemeral(this DiscordInteractionResponseCommandResult result, bool isEphemeral = true)
    {
        var message = (LocalInteractionMessageResponse) result.Message;
        message.IsEphemeral = isEphemeral;
        return result;
    }

    /*
    public static void AddFormatted<TOptionType, TFormatter>(this AutoComplete<TOptionType>.ChoiceCollection collection, DiscordBotBase bot, TFormatter formatter)
        where TOptionType : notnull
        where TFormatter : IAutoCompleteFormatter<TOptionType>
    {
        collection.Add(formatter.FormatAutoCompleteName(bot).Truncate(Discord.Limits.ApplicationCommand.Option.Choice.MaxNameLength), 
            formatter.FormatAutoCompleteValue(bot));
    }

    public static void AddFormattedRange<TOptionType, TFormatter>(this AutoComplete<TOptionType>.ChoiceCollection collection, DiscordBotBase bot, IEnumerable<TFormatter> formatters)
        where TOptionType : notnull
        where TFormatter : IAutoCompleteFormatter<TOptionType>
    {
        collection.AddRange(formatters.Take(Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount)
            .ToDictionary(
                x => x.FormatAutoCompleteName(bot)
                    .Truncate(Discord.Limits.ApplicationCommand.Option.Choice.MaxNameLength),
                x => x.FormatAutoCompleteValue(bot)));
    }

    public static void GenerateChoices<TValue, TFormatter>(this AutoComplete<TValue> autoComplete, DiscordBotBase bot, ICollection<TFormatter> collection,
        Func<TFormatter, string> comparisonSelector)
        where TValue : notnull
        where TFormatter : IAutoCompleteFormatter<TValue>
    {
        autoComplete.GenerateChoices(bot, collection, comparisonSelector,
            static (x, b) => x.FormatAutoCompleteName(b),
            static (x, b) => x.FormatAutoCompleteValue(b));
    }
    
    public static void GenerateChoices<TModel, TValue>(this AutoComplete<TValue> autoComplete, ICollection<TModel> collection, 
        Func<TModel, string> comparisonSelector, Func<TModel, string> nameFactory, Func<TModel, TValue> valueFactory)
        where TModel : notnull
        where TValue : notnull
    {
        if (!autoComplete.IsFocused || collection.Count == 0)
            return;
        
        if (string.IsNullOrWhiteSpace(autoComplete.RawArgument))
        {
            autoComplete.Choices.AddRange(collection.Take(Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount)
                .ToDictionary(nameFactory, valueFactory));
            return;
        }

        var comparisonDict = collection.ToDictionary(comparisonSelector);
        if (comparisonDict.FirstOrDefault(x => Equals(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase)) is TModel exactMatch)
        {
            autoComplete.Choices.Add(nameFactory(exactMatch), valueFactory(exactMatch));
            return;
        }

        var closeMatches = comparisonDict.Where(x => x.Key.Contains(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value).ToList();
        if (closeMatches.Count > 0)
        {
            autoComplete.Choices.AddRange(closeMatches.Take(Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount)
                .ToDictionary(nameFactory, valueFactory));
        }
    }
    
    public static void GenerateChoices<TModel, TValue>(this AutoComplete<TValue> autoComplete, DiscordBotBase bot,
        ICollection<TModel> collection, Func<TModel, string> comparisonSelector, 
        Func<TModel, DiscordBotBase, string> nameFactory, Func<TModel, DiscordBotBase, TValue> valueFactory)
        where TModel : notnull
        where TValue : notnull
    {
        if (!autoComplete.IsFocused || collection.Count == 0)
            return;
        
        if (string.IsNullOrWhiteSpace(autoComplete.RawArgument))
        {
            autoComplete.Choices.AddRange(collection.Take(Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount)
                .ToDictionary(x => nameFactory(x, bot), x => valueFactory(x, bot)));
            return;
        }

        var comparisonDict = collection.ToDictionary(comparisonSelector);
        if (comparisonDict.FirstOrDefault(x => Equals(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase)) is TModel exactMatch)
        {
            autoComplete.Choices.Add(nameFactory(exactMatch, bot), valueFactory(exactMatch, bot));
            return;
        }

        var closeMatches = comparisonDict.Where(x => x.Key.Contains(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase)).ToList();
        if (closeMatches.Count > 0)
        {
            autoComplete.Choices.AddRange(closeMatches.Take(Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount)
                .ToDictionary(x => nameFactory(x.Value, bot), x => valueFactory(x.Value, bot)));
        }
    }
    */
}