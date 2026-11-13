using ECommerce.Application.DTOs.Recommendations;

namespace ECommerce.Application.Interfaces.Services;

public interface IRecommendationService
{
    Task<RecommendationResponse> GetHomeAsync(int? userId, CancellationToken ct = default);

    Task<RecommendationResponse> GetRelatedAsync(int productId, int? userId, CancellationToken ct = default);
}
