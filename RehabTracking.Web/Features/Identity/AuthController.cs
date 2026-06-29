using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Features.Identity.Login;
using RehabTracking.Web.Features.Identity.Register;

namespace RehabTracking.Web.Features.Identity;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly RehabTrackingContext _context;

    public AuthController(RehabTrackingContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] LoginCommand command)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == command.Email.Trim().ToLowerInvariant());

        // Verify password: support both hashed (new users) and plain-text (legacy seeded demo accounts)
        if (user != null)
        {
            bool passwordValid = RegisterCommandHandler.VerifyPassword(command.Password, user.PasswordHash ?? "");
            if (!passwordValid)
            {
                return Redirect("/login?error=InvalidCredentials");
            }
        }
        else
        {
            // Demo fallback: if user not found in DB, try demo accounts by email pattern
            if (!command.Email.Contains("@test.com") && !command.Email.Contains("@demo."))
            {
                return Redirect("/login?error=InvalidCredentials");
            }

            var roleName = command.Email.Contains("doctor") ? "Doctor"
                         : command.Email.Contains("admin") ? "Admin"
                         : "Patient";

            // Check if seeded demo user exists
            var demoUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.Contains(roleName.ToLower()));

            if (demoUser != null)
            {
                user = demoUser;
                bool passwordValid = RegisterCommandHandler.VerifyPassword(command.Password, user.PasswordHash ?? "");
                if (!passwordValid)
                    return Redirect("/login?error=InvalidCredentials");
            }
            else
            {
                // True fallback for completely empty DB (first run only)
                user = new User
                {
                    UserId = roleName == "Doctor" ? 1 : (roleName == "Admin" ? 99 : 2),
                    Email = command.Email,
                    FullName = roleName == "Doctor" ? "BS. Nguyễn Văn Bác Sĩ"
                             : roleName == "Admin" ? "Quản Trị Viên"
                             : "Bệnh Nhân Demo",
                    Role = new Role { RoleName = roleName }
                };
            }
        }

        if (user != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Patient")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            string redirectUrl = "/";
            var finalRole = user.Role?.RoleName ?? "Patient";
            if (!string.IsNullOrEmpty(command.ReturnUrl) && command.ReturnUrl.StartsWith("/"))
            {
                redirectUrl = command.ReturnUrl;
            }
            else
            {
                if (finalRole == "Doctor") redirectUrl = "/doctor-dashboard";
                else if (finalRole == "Admin") redirectUrl = "/admin-dashboard";
                else if (finalRole == "Patient") redirectUrl = "/patient-dashboard";
            }

            return Redirect(redirectUrl);
        }

        return Redirect("/login?error=InvalidCredentials");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }
}
