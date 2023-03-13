using DotnetCompilerBot.Handlers;
using DotnetCompilerBot.Services;
using Telegram.Bot;

namespace DotnetCompilerBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBotClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        string botApiKey = configuration.GetSection("BotConfig:Token").Value;
       
        services.AddSingleton<ITelegramBotClient, TelegramBotClient>(
            x => new TelegramBotClient(botApiKey));
        
        return services;
    }

    public static IServiceCollection AddControllerMappers(
        this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddNewtonsoftJson();

        services.AddEndpointsApiExplorer();

        return services;
    }

    public static IServiceCollection AddUpdateHandler(
        this IServiceCollection services)
    {
        services.AddTransient<UpdateHandler>();

        return services;
    }

    public static IServiceCollection AddCompilerService(
        this IServiceCollection services)
    {
        services.AddTransient<ICompilerService, CompilerService>();

        return services;
    }
}