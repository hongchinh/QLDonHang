using OrderMgmt.Domain.Entities.Identity;

namespace OrderMgmt.Application.Identity.Interfaces;

public interface IJwtTokenGenerator
{
    JwtTokenResult Generate(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
}

public record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAt);
