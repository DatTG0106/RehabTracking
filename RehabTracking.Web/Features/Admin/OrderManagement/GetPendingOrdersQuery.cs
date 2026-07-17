using System.Linq;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.Admin.OrderManagement;

public class GetPendingOrdersQuery : IRequest<List<Order>>
{
}

public class GetPendingOrdersQueryHandler : IRequestHandler<GetPendingOrdersQuery, List<Order>>
{
    private readonly RehabTrackingContext _context;

    public GetPendingOrdersQueryHandler(RehabTrackingContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> Handle(GetPendingOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Where(o => o.Status == "Pending");

        return await Queryable.OrderBy(query, o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }
}
