using DotnetCompilerBot.Handlers;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace DotnetCompilerBot.Controllers;

[Route("bot")]
[ApiController]
public class BotController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] Update update,
        [FromServices] UpdateHandler updateHandler)
    {
        await updateHandler
            .UpdateHandlerAsync(update);

        return Ok();
    }
}