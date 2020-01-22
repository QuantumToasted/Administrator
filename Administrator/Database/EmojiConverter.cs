using System;
using System.Linq.Expressions;
using Administrator.Common;
using Disqord;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Administrator.Database
{
    public sealed class EmojiConverter : ValueConverter<IEmoji, string>
    {
        private static readonly Expression<Func<IEmoji, string>> InExpression = emoji =>
            emoji.ToString();

        private static readonly Expression<Func<string, IEmoji>> OutExpression = str =>
            EmojiTools.Parse(str);

        public EmojiConverter() 
            : base(InExpression, OutExpression)
        { }
    }
}