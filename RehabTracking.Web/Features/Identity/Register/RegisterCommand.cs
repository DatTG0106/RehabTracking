using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using System.Security.Cryptography;
using System.Text;

namespace RehabTracking.Web.Features.Identity.Register;

public class RegisterCommand : IRequest<Result>
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public record Result(bool IsSuccess, string Message);

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
{
    private readonly RehabTrackingContext _context;

    public RegisterCommandHandler(RehabTrackingContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Hash password dùng SHA256 kết hợp salt cố định cho demo.
    /// Trong production nên dùng BCrypt hoặc ASP.NET Core Identity PasswordHasher.
    /// </summary>
    public static string HashPassword(string password)
    {
        // Simple salted hash for demo - in production use BCrypt
        var saltedPassword = $"RehabTracking_Salt_{password}_2026";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool VerifyPassword(string password, string hash)
    {
        // Allow plain-text demo passwords (legacy / seeded accounts)
        if (hash == password) return true;
        return HashPassword(password) == hash;
    }

    public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (existingUser != null)
        {
            return new Result(false, "Email này đã được đăng ký. Vui lòng dùng email khác hoặc đăng nhập.");
        }

        // Check if Patient role exists, if not create it
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Patient", cancellationToken);
        if (defaultRole == null)
        {
            defaultRole = new Role { RoleName = "Patient" };
            _context.Roles.Add(defaultRole);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var newUser = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = HashPassword(request.Password),
            RoleId = defaultRole.RoleId
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(cancellationToken);

        return new Result(true, "Đăng ký thành công! Vui lòng đăng nhập.");
    }
}
