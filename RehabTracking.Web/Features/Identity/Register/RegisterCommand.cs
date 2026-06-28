using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

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

    public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (existingUser != null)
        {
            return new Result(false, "Email is already registered.");
        }

        // Check if Patient role exists, if not use default or create it (Ideally we'd seed it)
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Patient", cancellationToken);
        if (defaultRole == null)
        {
            defaultRole = new Role { RoleName = "Patient" };
            _context.Roles.Add(defaultRole);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var newUser = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            // In a real system, you would hash the password here (e.g., using BCrypt or ASP.NET Core Identity PasswordHasher)
            // For demo purposes, we will store it plain, or you can implement a simple hash.
            PasswordHash = request.Password, 
            RoleId = defaultRole.RoleId
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(cancellationToken);

        return new Result(true, "Registration successful!");
    }
}
