using MediatR;
using RehabTracking.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace RehabTracking.Web.Features.Identity.Login;

public class LoginCommand : IRequest<bool>
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(RehabTracking.Web.Resources.ErrorMessages))]
    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessageResourceName = "InvalidFormat", ErrorMessageResourceType = typeof(RehabTracking.Web.Resources.ErrorMessages))]
    public string Email { get; set; } = string.Empty;
    
    [System.ComponentModel.DataAnnotations.Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(RehabTracking.Web.Resources.ErrorMessages))]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, bool>
{
    private readonly RehabTrackingContext _context;

    public LoginCommandHandler(RehabTrackingContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Mock authentication check
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.PasswordHash == request.Password, cancellationToken);
            
        return user != null;
    }
}
