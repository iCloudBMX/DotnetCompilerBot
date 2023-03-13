using DotnetCompilerBot.Extensions;
using DotnetCompilerBot.Middlewares;
using Telegram.Bot;

namespace DotnetCompilerBot;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddTelegramBotClient(builder.Configuration)
            .AddUpdateHandler()
            .AddCompilerService()
            .AddControllerMappers();

        var app = builder.Build();

        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.MapControllers();
        
        SetWebHook(app, builder.Configuration);
        
        app.Run();
    }

    private static void SetWebHook(
            IApplicationBuilder app,
            IConfiguration configuration)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
            var baseUrl = configuration.GetSection("BotConfig:Host").Value;
            var webhookUrl = $"{baseUrl}/bot";
            var webhookInfo = botClient.GetWebhookInfoAsync().Result;

            if (webhookInfo is null || webhookInfo.Url != webhookUrl)
            {
                botClient.SetWebhookAsync(webhookUrl).Wait();
            }
        }
    }
}