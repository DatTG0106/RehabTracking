using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Features.Identity.Register;
using System.Threading;
using System.Threading.Tasks;

namespace RehabTracking.Web.Features.Admin.AccountManagement;

public static class ResetPassword
{
    public record Command(int TargetUserId, string NewPassword) : IRequest<Result>;

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
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return new Result(false, "Mật khẩu mới phải có ít nhất 6 ký tự.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == request.TargetUserId, cancellationToken);
            if (user == null)
            {
                return new Result(false, "Không tìm thấy tài khoản.");
            }

            user.PasswordHash = RegisterCommandHandler.HashPassword(request.NewPassword);
            await _db.SaveChangesAsync(cancellationToken);

            return new Result(true, $"Đã đặt lại mật khẩu thành công cho {user.Email}.");
        }
    }
}
