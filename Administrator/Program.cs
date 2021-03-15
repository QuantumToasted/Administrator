// oh yeah we on that GOOD shit

using System;
using System.Linq;
using Administrator;
using Administrator.Commands;
using Administrator.Common;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Extensions.Interactivity;
using Disqord.Gateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

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
            .Enrich.With<LogEventEnricher>()
            .Filter.With<LogEventFilter>()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("Logs/log_.txt",
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        x.AddSerilog(logger, true);

        x.Services.Remove(x.Services.First(y => y.ServiceType == typeof(ILogger<>)));
        x.Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
    })
    .ConfigureServices((context, services) =>
    {
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
        bot.Intents += GatewayIntent.DirectMessages;
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