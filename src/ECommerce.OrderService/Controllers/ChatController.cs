using ECommerce.Application.DTOs.Chat;
using ECommerce.Application.Interfaces.Services;
using ECommerce.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.OrderService.Controllers;

/// <summary>
/// Store support assistant. Available to anonymous visitors for product and FAQ
/// questions; order-specific details are only read for the signed-in customer.
/// </summary>
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatbotService _chatbot;

    public ChatController(IChatbotService chatbot)
    {
        _chatbot = chatbot;
    }

    /// <summary>Answers a customer support question.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> Ask(ChatRequest request, CancellationToken ct)
    {
        var result = await _chatbot.AskAsync(CurrentUserId, request, ct);
        return Ok(result);
    }

    private int? CurrentUserId =>
        User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
}
