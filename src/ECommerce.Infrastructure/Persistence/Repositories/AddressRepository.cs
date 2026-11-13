using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Orders;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class AddressRepository : IAddressRepository
{
    private readonly OrderDbContext _db;

    public AddressRepository(OrderDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Address address, CancellationToken ct = default) =>
        await _db.Addresses.AddAsync(address, ct);
}
