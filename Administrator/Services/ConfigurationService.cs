using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Administrator.Services
{
    public sealed class ConfigurationService : IService
    {
        private readonly DiscordClient _restClient;
        private readonly LoggingService _logging;
        
        public ConfigurationService(DiscordClient restClient, LoggingService logging)
        {
            _restClient = restClient;
            _logging = logging;
        }

        public const string SUPPORT_GUILD = @"https://discord.gg/rTvGube";

        [JsonProperty("token")]
        public string DiscordToken { get; private set; }
        
        [JsonProperty("prefix")]
        public string DefaultPrefix { get; private set; }

        [JsonProperty("connectionString")]
        public string PostgresConnectionString { get; private set; }
        
        [JsonProperty("owners")]
        public ICollection<ulong> OwnerIds { get; private set; }

        [JsonProperty("emojiServers")]
        public ICollection<ulong> EmojiServerIds { get; private set; }

        [JsonIgnore]
        public Color SuccessColor => new Color(int.Parse(_successColor, NumberStyles.HexNumber));
        
        [JsonIgnore]
        public Color WarnColor => new Color(int.Parse(_warnColor, NumberStyles.HexNumber));
        
        [JsonIgnore]
        public Color ErrorColor => new Color(int.Parse(_errorColor, NumberStyles.HexNumber));

        [JsonProperty("successColor")]
        private string _successColor;

        [JsonProperty("warningColor")]
        private string _warnColor;

        [JsonProperty("errorColor")]
        private string _errorColor;

        public static ConfigurationService Basic
        {
            get
            {
                var config = new ConfigurationService(null, null);
                try
                {
                    JsonConvert.PopulateObject(File.ReadAllText("./Data/Config.json"), config);

                    if (string.IsNullOrWhiteSpace(config.DiscordToken))
                        throw new ArgumentException("You have not supplied a token for the bot.");

                    if (string.IsNullOrWhiteSpace(config.PostgresConnectionString))
                        throw new ArgumentException(
                            "You have not supplied a connection string for the PostgreSQL database.");
                }
                catch (Exception ex)
                {
                    new LoggingService().LogCriticalAsync(ex, "Configuration");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }

                return config;
            }
        }
        
        async Task IService.InitializeAsync()
        {
            var config = Basic;
            DiscordToken = config.DiscordToken;
            DefaultPrefix = config.DefaultPrefix;
            PostgresConnectionString = config.PostgresConnectionString;
            OwnerIds = config.OwnerIds;
            EmojiServerIds = config.EmojiServerIds;
            _successColor = config._successColor;
            _warnColor = config._warnColor;
            _errorColor = config._errorColor;
            
            if (OwnerIds.Count == 0)
            {
                await _logging.LogDebugAsync("No owner IDs found. Fetching the bot owner's ID.", 
                    "Configuration");

                var app = await _restClient.GetCurrentApplicationAsync();
                await _logging.LogDebugAsync($"Got owner {app.Owner}.", "Configuration");
                
                OwnerIds = new List<ulong>
                {
                    app.Owner.Id
                };
            }

            if (EmojiServerIds.Count == 0)
            {
                await _logging.LogWarningAsync("No emoji servers were defined. Some services (profiles) will break!",
                    "Configuration");
            }

            foreach (var id in EmojiServerIds)
            {
                var guild = await _restClient.GetGuildAsync(id);
                if (guild is null)
                {
                    await _logging.LogCriticalAsync($"Emoji server with ID {id} could not be found or I'm not a member!", "Configuration");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }

                await _logging.LogDebugAsync($"Emoji server with ID {id} found: {guild.Name}", "Configuration");
            }

            using (var ctx = new AdminDatabaseContext(null))
            {
                await _logging.LogDebugAsync("Attempting database connection.", "Configuration");
                if (!await ctx.Database.CanConnectAsync())
                {
                    await _logging.LogCriticalAsync(
                        "Could not connect to the PostgreSQL database using the following connection string:\n" +
                        PostgresConnectionString, "Configuration");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
                else await _logging.LogDebugAsync("Connection successful.", "Configuration");

                await _logging.LogDebugAsync("Checking for pending database migrations.", "Configuration");
                var migrations = (await ctx.Database.GetPendingMigrationsAsync()).ToList();
                if (migrations.Count > 0)
                {
                    await _logging.LogDebugAsync($"{migrations.Count} new migration(s) found.\n" +
                                                 "Attempting to apply migrations.", "Configuration");
                    try
                    {
                        await ctx.Database.MigrateAsync();
                    }
                    catch (Exception ex)
                    {
                        await _logging.LogCriticalAsync($"An error occurred migrating the PostgreSQL database:\n{ex}",
                            "Configuration");
                        Console.ReadKey();
                        Environment.Exit(-1);
                    }
                }
                else await _logging.LogDebugAsync("No migrations found.", "Configuration");
            }

            await _logging.LogInfoAsync("Initialized.", "Configuration");
        }
    }
}
