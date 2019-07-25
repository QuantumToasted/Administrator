using System;
using System.Linq.Expressions;
using Administrator.Common;
using Discord;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Administrator.Database
{
    public sealed class EmoteConverter : ValueConverter<IEmote, string>
    {
        private static readonly Expression<Func<IEmote, string>> InExpression = emote =>
            emote.ToString();

        private static readonly Expression<Func<string, IEmote>> OutExpression = str =>
            EmoteTools.Parse(str);

        public EmoteConverter() 
            : base(InExpression, OutExpression)
        { }
    }
}