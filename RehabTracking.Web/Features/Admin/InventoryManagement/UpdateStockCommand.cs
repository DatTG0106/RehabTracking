using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.Admin.InventoryManagement;

public class UpdateStockCommand : IRequest<bool>
{
    public int ProductId { get; set; }
    public int QuantityToAdd { get; set; }
}

public class UpdateStockCommandHandler : IRequestHandler<UpdateStockCommand, bool>
{
    private readonly RehabTrackingContext _context;

    public UpdateStockCommandHandler(RehabTrackingContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == request.ProductId, cancellationToken);

        if (product == null)
            return false;

        product.StockQuantity += request.QuantityToAdd;

        // Ensure stock doesn't go below 0 if somehow negative quantity is passed
        if (product.StockQuantity < 0)
            product.StockQuantity = 0;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
