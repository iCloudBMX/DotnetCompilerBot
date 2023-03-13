using DotnetCompilerBot.Exceptions;
using DotnetCompilerBot.Extensions;
using DotnetCompilerBot.Services;
using System.Text;
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

        char[] seperators = new char[] { ' ', '\n' };

        var command = message.Text
            .Split(seperators)
            .FirstOrDefault()
            ?.Substring(1);

        var task = command switch
        {
            "run" => HandleRunCommandAsync(message),
            _ => HandleNotAvailableCommandAsync(message)
        };

        await task;
    }

    private async Task HandleRunCommandAsync(Message message)
    {
        string command = "/run";

        string sourceCode = message.Text
            .Substring(message.Text.IndexOf(command) + command.Length);

        if (string.IsNullOrEmpty(sourceCode))
        {
            return;
        }

        try
        {
            byte[] compiledCode = this.compilerService.Compile(sourceCode);
            string result = this.compilerService.Execute(compiledCode);
            StringBuilder messageTemplate = new StringBuilder();

            messageTemplate.AppendLine(MessageTemplate.GetDecoratedMessage(
                message: "Result:",
                decoraterType: Models.DecoraterType.Bold));

            messageTemplate.AppendLine(MessageTemplate.GetDecoratedMessage(
                message: result,
                decoraterType: Models.DecoraterType.Monospace));

            await this.telegramBotClient.SendTextMessageAsync(
                chatId: message.From.Id,
                text: messageTemplate.ToString(),
                parseMode: ParseMode.Html);
        }
        catch (CompileFailedException compileFailedException)
        {
            await this.telegramBotClient.SendTextMessageAsync(
                chatId: message.From.Id,
                text: compileFailedException.Message,
                parseMode: ParseMode.Html);
        }
    }

    private async Task HandleNotAvailableCommandAsync(Message message)
    {
        string messageTemplate = MessageTemplate.GetDecoratedMessage(
            message: @"Not available command provided.",
            decoraterType: Models.DecoraterType.Bold);

        await this.telegramBotClient.SendTextMessageAsync(
            chatId: message.From.Id,
            text: messageTemplate,
            parseMode: ParseMode.Html);
    }
}