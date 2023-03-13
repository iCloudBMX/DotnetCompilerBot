using DotnetCompilerBot.Exceptions;
using DotnetCompilerBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DotnetCompilerBot.Handlers;

public class UpdateHandler
{
    private readonly ITelegramBotClient telegramBotClient;
    private readonly ILogger<UpdateHandler> logger;
    private readonly ICompilerService compilerService;

    public UpdateHandler(
        ITelegramBotClient telegramBotClient,
        ILogger<UpdateHandler> logger,
        ICompilerService compilerService)
    {
        this.telegramBotClient = telegramBotClient;
        this.logger = logger;
        this.compilerService = compilerService;
    }

    public async Task UpdateHandlerAsync(Update update)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => HandleCommandAsync(update.Message),
            _ => HandleNotAvailableCommandAsync(update.Message)
        };

        await handler;
    }

    public async Task HandleCommandAsync(Message message)
    {
        if (message is null || message.Text is null)
        {
            return;
        }

        if (message.Text.StartsWith("/") is false)
        {
            return;
        }

        var command = message.Text.Split(' ').First().Substring(1);

        var task = command switch
        {
            "run" => HandleRunCommandAsync(message),
            _ => HandleNotAvailableCommandAsync(message)
        };

        await task;
    }

    private async Task HandleRunCommandAsync(Message message)
    {
        string sourceCode = message.Text
            .Substring(message.Text.IndexOf("/run" + 4));

        try
        {
            byte[] compiledCode = this.compilerService.Compile(sourceCode);
            string result = this.compilerService.Execute(compiledCode);

            await this.telegramBotClient.SendTextMessageAsync(
                chatId: message.From.Id,
                text: $"<b>Result:\n{result}</b>",
                parseMode: ParseMode.Html);
        }
        catch(CompileFailedException compileFailedException)
        {
            await this.telegramBotClient.SendTextMessageAsync(
                chatId: message.From.Id,
                text: compileFailedException.Message);
        }
    }

    private async Task HandleNotAvailableCommandAsync(Message message)
    {
        await this.telegramBotClient.SendTextMessageAsync(
            chatId: message.From.Id,
            text: "Not available command provided.",
            parseMode: ParseMode.Html);
    }
}