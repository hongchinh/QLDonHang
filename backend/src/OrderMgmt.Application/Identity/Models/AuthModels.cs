namespace OrderMgmt.Application.Identity.Models;

public class LoginRequest
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = default!;
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    public CurrentUserDto User { get; set; } = default!;
}

public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
