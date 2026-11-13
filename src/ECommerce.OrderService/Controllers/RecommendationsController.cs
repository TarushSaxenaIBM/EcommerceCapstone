using ECommerce.Application.DTOs.Recommendations;
using ECommerce.Application.Interfaces.Services;
using ECommerce.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.OrderService.Controllers;

/// <summary>
/// Personalized recommendations. Anonymous callers receive popular products;
/// signed-in callers get suggestions based on their own order history.
/// </summary>
[ApiController]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendations;

    public RecommendationsController(IRecommendationService recommendations)
    {
        _recommendations = recommendations;
    }

    /// <summary>Returns homepage recommendations, personalized when signed in.</summary>
    [HttpGet("home")]
    [ProducesResponseType(typeof(RecommendationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RecommendationResponse>> GetHome(CancellationToken ct)
    {
        var result = await _recommendations.GetHomeAsync(CurrentUserId, ct);
        return Ok(result);
    }

    /// <summary>Returns products related to the one being viewed.</summary>
    [HttpGet("related/{productId:int}")]
    [ProducesResponseType(typeof(RecommendationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecommendationResponse>> GetRelated(int productId, CancellationToken ct)
    {
        var result = await _recommendations.GetRelatedAsync(productId, CurrentUserId, ct);
        return Ok(result);
    }

    private int? CurrentUserId =>
        User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
}
