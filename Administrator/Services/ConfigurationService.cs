using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Administrator.Services
{
    public sealed class ConfigurationService : IService
    {
        private readonly DiscordShardedClient _client;
        
        public ConfigurationService(DiscordShardedClient client)
        {
            try
            {
                JsonConvert.PopulateObject(File.ReadAllText("./Data/Config.json"), this);

                if (string.IsNullOrWhiteSpace(DiscordToken))
                    throw new ArgumentException("You have not supplied a token for the bot.");

                if (string.IsNullOrWhiteSpace(PostgresConnectionString))
                    throw new ArgumentException(
                        "You have not supplied a connection string for the PostgreSQL database.");
            }
            catch (Exception ex)
            {
                Log.Critical(ex);
                Console.ReadKey();
                Environment.Exit(-1);
            }

            _client = client;
            Log.VerboseEnabled = Verbose;
        }

        public async Task InitializeAsync()
        {
            if (OwnerIds.Count == 0)
            {
                Log.Warning("No owner IDs found. Fetching the bot owner's ID.");

                var app = await _client.GetApplicationInfoAsync();
                Log.Verbose($"Got owner {app.Owner}.");
                OwnerIds = new List<ulong>
                {
                    app.Owner.Id
                };
            }

            using (var ctx = new AdminDatabaseContext())
            {
                if (!await ctx.Database.CanConnectAsync())
                {
                    Log.Critical("Could not connect to the PostgreSQL database using the following connection string:\n" +
                                 PostgresConnectionString);
                    Console.ReadKey();
                    Environment.Exit(-1);
                }

                var migrations = await ctx.Database.GetPendingMigrationsAsync();
                if (migrations.Any())
                {
                    try
                    {
                        await ctx.Database.MigrateAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Critical($"An error occurred migrating the PostgreSQL database:\n{ex}");
                        Console.ReadKey();
                        Environment.Exit(-1);
                    }
                }
            }
            
            Log.Verbose("Initialized.");
        }
        
        [JsonProperty("token")]
        public string DiscordToken { get; private set; }
        
        [JsonProperty("prefix")]
        public string DefaultPrefix { get; private set; }

        [JsonProperty("connectionString")]
        public string PostgresConnectionString { get; private set; }
        
        [JsonProperty("owners")]
        public ICollection<ulong> OwnerIds { get; private set; }

        [JsonIgnore]
        public Color SuccessColor => new Color(uint.Parse(_successColor, NumberStyles.HexNumber));
        
        [JsonIgnore]
        public Color WarnColor => new Color(uint.Parse(_warnColor, NumberStyles.HexNumber));
        
        [JsonIgnore]
        public Color ErrorColor => new Color(uint.Parse(_errorColor, NumberStyles.HexNumber));
        
        [JsonProperty("verbose")]
        public bool Verbose { get; private set; }

        [JsonProperty("successColor")]
        private readonly string _successColor;

        [JsonProperty("warnColor")]
        private readonly string _warnColor;

        [JsonProperty("errorColor")]
        private readonly string _errorColor;
    }
}
