using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Interfaces.Services;

public interface IProductService
{
    Task<PagedResult<ProductSummaryResponse>> GetPagedAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductResponse> GetByIdAsync(int id, bool includeInactive, CancellationToken ct = default);
    Task<IReadOnlyList<ProductResponse>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<IReadOnlyList<ProductResponse>> GetFeaturedAsync(int take, CancellationToken ct = default);
    Task<SemanticSearchResponse> SearchAsync(SemanticSearchRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ProductSuggestionResponse>> GetSuggestionsAsync(string query, int take, CancellationToken ct = default);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
