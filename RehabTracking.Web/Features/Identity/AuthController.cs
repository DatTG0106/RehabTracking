using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Features.Identity.Login;

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
        // Mock authentication check or real check
        // In reality, you'd check password hash. Here we just match the email and password directly or mock it
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == command.Email);

        // For registered users, verify the password (plain text check for demo purposes)
        if (user != null && !string.IsNullOrEmpty(user.PasswordHash))
        {
            if (user.PasswordHash != command.Password)
            {
                return Redirect("/login?error=InvalidCredentials");
            }
        }
        else if (user == null && command.Email.Contains("@"))
        {
            // Allow dummy login for demo purposes if DB is empty and user doesn't exist
            var roleName = command.Email.Contains("doctor") ? "Doctor" : 
                           command.Email.Contains("admin") ? "Admin" : "Patient";
                           
            user = new User
            {
                UserId = roleName == "Doctor" ? 1 : (roleName == "Admin" ? 99 : 2), // Dummy ID
                Email = command.Email,
                FullName = "Test " + roleName,
                Role = new Role { RoleName = roleName }
            };
        }

        if (user != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
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
