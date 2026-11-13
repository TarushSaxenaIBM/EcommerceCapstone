using ECommerce.Application.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

public interface IJwtTokenService
{
    JwtToken CreateToken(User user);
}
