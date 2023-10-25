using System.Text.Json;
using System.Text.Json.Serialization;
using Administrator.Bot;
using Administrator.Core;
using Administrator.Database;
using Disqord.Bot.Hosting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using Serilog.Events;

var host = new HostBuilder()
    .UseSerilog((context, logger) =>
    {
        logger.MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
            .MinimumLevel.Override("Disqord", LogEventLevel.Information)
            .MinimumLevel.Override("FusionCache", LogEventLevel.Information)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("Logs/log_.txt",
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day);
    })
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("config.json");
        config.AddEnvironmentVariables("ADMIN_BETA_");
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
            app.UseEndpoints(x => x.MapControllers());
            app.UseStaticFiles();
        });
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<HttpClient>();
        services.AddScopedServices();
        
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(context.Configuration["DB_CONNECTION_STRING"]);
        dataSourceBuilder.AddTypeResolverFactory(new CustomJsonSerializerTypeHandlerResolverFactory(
            new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
        
        services.AddMemoryCache();
        //services.AddDbContext<adminContext>();
        services.AddSingleton(dataSourceBuilder.Build());
        services.AddDbContext<AdminDbContext>(builder =>
        {
            builder.UseNpgsql(context.Configuration["DB_CONNECTION_STRING"]);
        });
        
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        //services.AddSwaggerGen();
    })
    .ConfigureDiscordBot<AdministratorBot>((context, bot) =>
    {
        bot.Token = context.Configuration["TOKEN"];
    })
    .Build();
    
try
{
    host.Run();
}
catch (Exception ex)
{
    Log.Logger.ForContext<IHost>().Fatal(ex, "Unhandled top-level exception thrown. Hosting has stopped.");
    //Console.ReadLine();
    Environment.Exit(-1);
}