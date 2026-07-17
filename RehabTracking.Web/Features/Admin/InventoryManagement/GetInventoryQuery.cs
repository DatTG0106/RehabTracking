using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.Admin.InventoryManagement;

public class GetInventoryQuery : IRequest<List<Product>>
{
}

public class GetInventoryQueryHandler : IRequestHandler<GetInventoryQuery, List<Product>>
{
    private readonly RehabTrackingContext _context;

    public GetInventoryQueryHandler(RehabTrackingContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
    {
        return await _context.Products
            .OrderBy(p => p.ProductName)
            .ToListAsync(cancellationToken);
    }
}
