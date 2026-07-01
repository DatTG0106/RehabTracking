using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace RehabTracking.Web.Features.Admin.AccountManagement;

public class GetAccountStatsQuery : IRequest<AccountStatsDto>
{
}

public class AccountStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalLocked { get; set; }
}

public class GetAccountStatsQueryHandler : IRequestHandler<GetAccountStatsQuery, AccountStatsDto>
{
    private readonly RehabTrackingContext _context;

    public GetAccountStatsQueryHandler(RehabTrackingContext context)
    {
        _context = context;
    }

    public async Task<AccountStatsDto> Handle(GetAccountStatsQuery request, CancellationToken cancellationToken)
    {
        var users = await _context.Users.Include(u => u.Role).ToListAsync(cancellationToken);

        return new AccountStatsDto
        {
            TotalUsers = users.Count,
            TotalDoctors = users.Count(u => u.Role?.RoleName == "Doctor"),
            TotalPatients = users.Count(u => u.Role?.RoleName == "Patient"),
            TotalLocked = users.Count(u => !u.IsActive)
        };
    }
}
