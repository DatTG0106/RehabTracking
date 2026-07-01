using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Features.Identity.Register;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace RehabTracking.Web.Features.Admin.AccountManagement;

public static class CreateDoctorAccount
{
    public record Command(string FullName, string Email, string Password) : IRequest<Result>;

    public record Result(bool IsSuccess, string Message);

    public class Handler : IRequestHandler<Command, Result>
    {
        private readonly RehabTrackingContext _db;

        public Handler(RehabTrackingContext db)
        {
            _db = db;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email.Trim().ToLower(), cancellationToken);
            if (emailExists)
            {
                return new Result(false, "Email này đã được sử dụng trong hệ thống.");
            }

            var doctorRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Doctor", cancellationToken);
            if (doctorRole == null)
            {
                return new Result(false, "Không tìm thấy Role 'Doctor' trong hệ thống.");
            }

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email.Trim().ToLower(),
                PasswordHash = RegisterCommandHandler.HashPassword(request.Password),
                RoleId = doctorRole.RoleId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            return new Result(true, $"Tạo tài khoản Bác sĩ cho {user.FullName} thành công.");
        }
    }
}
