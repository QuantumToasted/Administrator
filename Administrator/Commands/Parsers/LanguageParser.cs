using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Services;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class LanguageParser : TypeParser<LocalizedLanguage>
    {
        public override ValueTask<TypeParserResult<LocalizedLanguage>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            var localization = context.ServiceProvider.GetRequiredService<LocalizationService>();

            return localization.Languages.FirstOrDefault(x =>
                x.CultureCode.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                x.EnglishName.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                x.NativeName.Equals(value, StringComparison.OrdinalIgnoreCase)) is { } language
                ? TypeParserResult<LocalizedLanguage>.Successful(language)
                : TypeParserResult<LocalizedLanguage>.Unsuccessful(context.Localize("languageparser_notfound"));
        }
    }
}