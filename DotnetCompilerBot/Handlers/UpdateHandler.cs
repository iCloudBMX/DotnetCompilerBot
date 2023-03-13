using DotnetCompilerBot.Exceptions;
using DotnetCompilerBot.Extensions;
using DotnetCompilerBot.Models;
using DotnetCompilerBot.Services;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DotnetCompilerBot.Handlers;

public class UpdateHandler
{
    private readonly ITelegramBotClient telegramBotClient;
    private readonly ILogger<UpdateHandler> logger;
    private readonly ICompilerService compilerService;
    private readonly long groupId;

    public UpdateHandler(
        ITelegramBotClient telegramBotClient,
        ILogger<UpdateHandler> logger,
        ICompilerService compilerService)
    {
        this.telegramBotClient = telegramBotClient;
        this.logger = logger;
        this.compilerService = compilerService;
        this.groupId = long.Parse(Environment.GetEnvironmentVariable("GROUP_ID"));
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
        if (message is null ||
            message.Type is not MessageType.Text ||
            message.Text is null)
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
        long chatId = message.From.Id;

        if(message.Chat.Type is ChatType.Group or ChatType.Supergroup)
        {
            chatId = this.groupId;
        }

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
            string messageText = FormatResultMessage(result);

            await SendMessageAsync(chatId, messageText);
        }
        catch (CompileFailedException compileFailedException)
        {
            string errorMessage = compileFailedException.Message;
            await SendMessageAsync(chatId, errorMessage);
        }
        catch (TargetInvocationException targetInvocationException)
        {
            if (targetInvocationException.InnerException is Exception innerException)
            {
                string errorMessage = FormatExceptionMessage(innerException);
                await SendMessageAsync(chatId, errorMessage);
            }
        }
    }

    private async Task HandleNotAvailableCommandAsync(Message message)
    {
        long chatId = message.From.Id;

        if (message.Chat.Type is ChatType.Group or ChatType.Supergroup)
        {
            chatId = this.groupId;
        }

        string notAvailableCommandMessage = FormatNotAvailableCommanMessage();

        await SendMessageAsync(chatId, notAvailableCommandMessage);
    }

    private async Task SendMessageAsync(
        ChatId chatId,
        string message)
    {
        await telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Html);
    }

    private static string FormatNotAvailableCommanMessage()
    {
        return MessageTemplate.GetDecoratedMessage(
            message: "Not available command provided.",
            decoraterType: Models.DecoraterType.Bold);
    }

    private string FormatResultMessage(string result)
    {
        string formatMessage = MessageTemplate.GetDecoratedMessage(
            message: "Result:\n",
            decoraterType: DecoraterType.Bold);

        formatMessage += MessageTemplate.GetDecoratedMessage(
            message: result,
            decoraterType: DecoraterType.Monospace);

        return formatMessage;
    }

    private string FormatExceptionMessage(Exception exception)
    {
        string formatMessage = MessageTemplate.GetDecoratedMessage(
            message: "Unhandled runtime exception:\n",
            decoraterType: DecoraterType.Bold);

        formatMessage += MessageTemplate.GetDecoratedMessage(
            message: exception.Message,
            decoraterType: DecoraterType.Monospace);

        return formatMessage;
    }
}