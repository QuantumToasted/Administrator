using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Disqord;
using Newtonsoft.Json;
using Qommon.Collections;

namespace Administrator.Common
{
    public sealed class JsonEmbed
    {
        private JsonEmbed()
        { }

        public JsonEmbed(LocalEmbed embed)
            : this(null, embed) 
        { }

        public JsonEmbed(string text, LocalEmbed embed)
        {
            Text = text;
            if (embed is null) return;
            Title = embed.Title;
            Description = embed.Description;
            ImageUrl = embed.ImageUrl;
            ThumbnailUrl = embed.ThumbnailUrl;
            Color = embed.Color;
            Timestamp = embed.Timestamp;
            Footer = embed.Footer is { } ? new JsonEmbedFooter(embed.Footer) : null;
            Author = embed.Author is { } ? new JsonEmbedAuthor(embed.Author) : null; 
            Fields = embed.Fields.Count > 0 ? embed.Fields.Select(x => new JsonEmbedField(x)).ToList() : null;
        }

        [JsonProperty("text")]
        public string Text { get; private set; }

        [JsonProperty("title")]
        public string Title { get; private set; }

        [JsonProperty("description")]
        public string Description { get; private set; }

        [JsonProperty("image")]
        public string ImageUrl { get; private set; }

        [JsonProperty("thumbnail")]
        public string ThumbnailUrl { get; private set; }

        [JsonProperty("color")]
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color? Color { get; private set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset? Timestamp { get; private set; }

        [JsonProperty("footer")]
        public JsonEmbedFooter Footer { get; private set; }

        [JsonProperty("author")]
        public JsonEmbedAuthor Author { get; private set; }

        [JsonProperty("fields")]
        public IReadOnlyList<JsonEmbedField> Fields { get; private set; }

        public LocalEmbed ToLocalEmbed()
        {
            if (Title is null &&
                Description is null &&
                ImageUrl is null &&
                ThumbnailUrl is null &&
                Color is null &&
                Timestamp is null &&
                Footer is null &&
                Author is null &&
                Fields is null)
                return null;

            var builder = new LocalEmbedBuilder()
                .WithTitle(Title)
                .WithDescription(Description)
                .WithImageUrl(ImageUrl)
                .WithThumbnailUrl(ThumbnailUrl)
                .WithColor(Color)
                .WithTimestamp(Timestamp);

            if (Footer is { })
                builder.WithFooter(Footer.Text, Footer.IconUrl);

            if (Author is { })
                builder.WithAuthor(Author.Name, Author.IconUrl, Author.Url);

            if (Fields is { })
            {
                foreach (var field in Fields)
                {
                    builder.AddField(field.Name, field.Value, field.IsInline);
                }
            }

            return builder.Build();
        }

        public static bool TryParse(string text, out JsonEmbed embed)
        {
            embed = null;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            try
            {
                embed = JsonConvert.DeserializeObject<JsonEmbed>(text);
            }
            catch { return false; }

            return true;
        }

        public static JsonEmbed Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text must not be whitespace.", nameof(text));

            return JsonConvert.DeserializeObject<JsonEmbed>(text);
        }
    }
}