#undef MIGRATION_MODE

using System;
using System.Linq;
using System.Net.Http;
using Administrator;
using Administrator.Commands;
using Administrator.Common;
using Administrator.Database;
using Administrator.Services;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Extensions.Interactivity;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

#if MIGRATION_MODE
    throw new InvalidOperationException("Undefine MIGRATION_MODE to run this.");
#endif

var host = new HostBuilder()
    .ConfigureHostConfiguration(x =>
    {
        x.AddCommandLine(args);
    })
    .ConfigureAppConfiguration(x =>
    {
        x.AddCommandLine(args);
        x.AddEnvironmentVariables("ADMIN_");
    })
    .ConfigureLogging(x =>
    {
        var logger = new LoggerConfiguration()
            .Destructure.With<DestructuringPolicy>()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .MinimumLevel.Override("Disqord", LogEventLevel.Information)
            .Filter.With<LogEventFilter>()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("Logs/log_.txt",
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        x.AddSerilog(logger, true);
        
        x.Services.Remove(x.Services.First(y => y.ServiceType == typeof(ILogger<>)));
        x.Services.AddSingleton(typeof(ILogger<>), typeof(Administrator.Common.Logger<>));
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<HttpClient>();
        services.AddSingleton<Random>();
        
        services.AddEntityFrameworkNpgsql();
        services.AddMemoryCache();
        services.AddDbContext<AdminDbContext>((provider, builder) =>
        {
            builder.UseNpgsql(context.Configuration["DB_CONNECTION_STRING"]);
            builder.UseInternalServiceProvider(provider);
        });
        
        services.AddInteractivity();
        services.AddPrefixProvider<AdminPrefixProvider>();
        services.AddCommands(x =>
        {
            x.DefaultArgumentParser = new AdminArgumentParser();
        });
    })
    .ConfigureDiscordBot<AdministratorBot>((context, bot) =>
    {
        bot.Token = context.Configuration["TOKEN"];
        bot.UseMentionPrefix = true;
        bot.Intents = GatewayIntents.All;
    })
    .Build();

try
{
    using (host)
    {
        host.Run();
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    Console.ReadLine();
}