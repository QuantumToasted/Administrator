using System.Globalization;
using Administrator.Api;
using Administrator.Bot;
using Administrator.Core;
using Administrator.Database;
using Amazon.Runtime;
using Amazon.S3;
using Backpack.Net;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Gateway.Default;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quartz;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SteamWebAPI2.Utilities;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LinqToDBForEFTools = LinqToDB.EntityFrameworkCore.LinqToDBForEFTools;

CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

var host = new HostBuilder()
    .UseSerilog((context, logger) =>
    {
        var config = new AdministratorLoggingConfiguration();
        context.Configuration.GetSection(IAdministratorConfiguration<AdministratorLoggingConfiguration>.SectionName)
            .Bind(config);
        
        logger
            .MinimumLevel.Is(
#if DEBUG
                LogEventLevel.Debug)
#else
                config.DefaultLevel)
#endif
            .Filter.ByExcluding(x => x.MessageTemplate.Text.StartsWith("Pending guild"))
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("Logs/log_.txt",
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day);

        foreach (var (name, level) in config.Overrides)
        {
            logger.MinimumLevel.Override(name, level);
        }
    })
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("config.json");
        //config.AddEnvironmentVariables("ADMIN_");
    })
    .ConfigureWebHost(webHost =>
    {
        webHost.UseKestrel();
        webHost.Configure(app =>
        {
            /*
            app.UseSwagger();
            app.UseSwaggerUI(x =>
            {
                x.SwaggerEndpoint("/swagger/v1/swagger.json", "Administrator API v1");
                x.RoutePrefix = string.Empty;
            });
            */
            
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseStaticFiles();

            app.UseEndpoints(x => x.MapPunishments());
        });
    })
    .ConfigureServices((context, services) =>
    {
        services.AddConfiguration<AdministratorAppealConfiguration>()
            .AddConfiguration<AdministratorBackblazeConfiguration>(context.Configuration, out var b2Configuration)
            .AddConfiguration<AdministratorBackpackConfiguration>(context.Configuration, out var backpackConfiguration)
            .AddConfiguration<AdministratorBotConfiguration>()
            .AddConfiguration<AdministratorDatabaseConfiguration>(context.Configuration, out var dbConfiguration)
            .AddConfiguration<AdministratorHelpConfiguration>()
            .AddConfiguration<AdministratorSteamConfiguration>(context.Configuration, out var steamConfiguration)
            .AddConfiguration<AdministratorLoggingConfiguration>()
            .AddConfiguration<AdministratorCacheConfiguration>();

        services.AddQuartz(x =>
        {
            x.InterruptJobsOnShutdown = true;
            x.AddJobListener<AdminJobListener>();
            x.AddSchedulerListener<AdminSchedulerListener>();
        });
        
        /* TODO: start scheduler manually - this may not be necessary
        services.AddQuartzHostedService(x =>
        {
            x.WaitForJobsToComplete = false;
        });
        */
        
        services.AddSingleton<HttpClient>();
        services.AddScopedServices(typeof(AdministratorBot).Assembly);
        services.AddScoped<IPlaceholderFormatter>(x => x.GetRequiredService<DiscordPlaceholderFormatter>());
        services.AddSingleton<IClient>(x => x.GetRequiredService<DiscordBotBase>());
        
        // while the space saved by not serializing "null" values is neat, it's not worth the trouble.
        //dataSourceBuilder.AddTypeResolverFactory(new CustomJsonSerializerTypeHandlerResolverFactory(
        //    new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
        
        services.AddMemoryCache();

        LinqToDBForEFTools.Implementation = new Administrator.Database.LinqToDBForEFTools(dbConfiguration.ConnectionString);
        var dataSource = new NpgsqlDataSourceBuilder(dbConfiguration.ConnectionString).EnableDynamicJson().Build();
        services.AddDbContext<AdminDbContext>(builder =>
        {
            builder.UseNpgsql(dataSource, x => x.CommandTimeout(240)).UseSnakeCaseNamingConvention().UseLinqToDB();
        });
        
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        
        services.Configure<DefaultGatewayCacheProviderConfiguration>(x => x.MessagesPerChannel = 500);
        services.AddSingleton(new AmazonS3Client(new BasicAWSCredentials(b2Configuration.KeyId, b2Configuration.Key),
            new AmazonS3Config { ServiceURL = b2Configuration.BaseUrl }));
        services.AddSingleton(new BackpackClient(backpackConfiguration.ApiKey));
        services.AddSteamWebInterfaceFactory(x => x.SteamWebApiKey = steamConfiguration.ApiKey);

        services.AddSingleton<IDiscordEntityRequester>(x => x.GetRequiredService<AdministratorBot>());
        services.AddSingleton<IPunishmentService>(x => x.GetRequiredService<PunishmentService>());
        //services.AddSwaggerGen();
    })
    .ConfigureDiscordBot<AdministratorBot>((context, bot) =>
    {
        var config = new AdministratorBotConfiguration();
        context.Configuration.GetSection(IAdministratorConfiguration<AdministratorBotConfiguration>.SectionName)
            .Bind(config);
        
        bot.Token = config.Token;
        bot.ServiceAssemblies = [typeof(AdministratorBot).Assembly];
        bot.Activities = [new LocalActivity("/help for help", ActivityType.Watching)];
        bot.Intents = GatewayIntents.LibraryRecommended & ~GatewayIntents.VoiceStates;
    })
    .Build();

ILogger? logger = null;

try
{
    LinqToDBForEFTools.Initialize();
    logger = host.Services.GetRequiredService<ILogger<IHost>>();
    host.Run();
}
catch (Exception ex)
{
    logger?.LogCritical(ex, "Unhandled top-level exception thrown. Hosting has stopped.");
    //Console.ReadLine();
    Environment.Exit(-1);
}