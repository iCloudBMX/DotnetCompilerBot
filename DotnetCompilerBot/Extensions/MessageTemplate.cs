using DotnetCompilerBot.Models;
using System.Text.RegularExpressions;

namespace DotnetCompilerBot.Extensions
{
    public static class MessageTemplate
    {
        public static string GetDecoratedMessage(
            string message,
            DecoraterType decoraterType)
        {
            string decoratedMessage = decoraterType switch
            {
                DecoraterType.Bold => $"<b>{message}</b>",
                DecoraterType.Monospace => $"<pre>{message}</pre>"
            };

            return decoratedMessage;
        }
    }
}
