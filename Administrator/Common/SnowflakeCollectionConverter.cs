using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Administrator.Common
{
    public sealed class SnowflakeCollectionConverter : ValueConverter<List<ulong>, string>
    {
        public SnowflakeCollectionConverter(ConverterMappingHints mappingHints = null)
            : base(InExpression, OutExpression, mappingHints)
        { }

        private static readonly Expression<Func<List<ulong>, string>> InExpression = collection
            => collection != null && collection.Any()
                ? string.Join(',', collection)
                : string.Empty;

        private static readonly Expression<Func<string, List<ulong>>> OutExpression = str
            => !string.IsNullOrWhiteSpace(str)
                ? str.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ulong.Parse).ToList()
                : new List<ulong>();
    }

}