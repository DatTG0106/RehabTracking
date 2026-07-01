using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace RehabTracking.Web.Features.Admin.AccountManagement;

public static class ToggleAccountStatus
{
    public record Command(int TargetUserId) : IRequest<Result>;

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
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == request.TargetUserId, cancellationToken);

            if (user == null)
            {
                return new Result(false, "Không tìm thấy tài khoản này.");
            }

            if (user.Role != null && user.Role.RoleName == "Admin")
            {
                return new Result(false, "Không thể thao tác khóa/mở khóa trên tài khoản Quản trị viên.");
            }

            user.IsActive = !user.IsActive;

            await _db.SaveChangesAsync(cancellationToken);

            string statusText = user.IsActive ? "được mở khóa" : "bị khóa";
            return new Result(true, $"Tài khoản {user.FullName} đã {statusText} thành công.");
        }
    }
}
