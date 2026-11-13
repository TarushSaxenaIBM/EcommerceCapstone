using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces.Repositories;

public interface IAddressRepository
{
    Task AddAsync(Address address, CancellationToken ct = default);
}
