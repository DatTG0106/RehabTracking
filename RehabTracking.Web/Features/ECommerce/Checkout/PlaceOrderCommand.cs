using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Features.ECommerce.Cart;

namespace RehabTracking.Web.Features.ECommerce.Checkout;

public class PlaceOrderCommand : IRequest<int>
{
    public int UserId { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public string ShippingAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD"; // COD or VNPay
}

public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, int>
{
    private readonly RehabTrackingContext _context;

    public PlaceOrderCommandHandler(RehabTrackingContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        // Sử dụng một Transaction để đảm bảo tính toàn vẹn dữ liệu
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var order = new Order
            {
                CustomerId = request.UserId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = request.Items.Sum(i => i.Product.Price * i.Quantity),
                Status = request.PaymentMethod == "VNPay" ? "Awaiting Payment" : "Pending",
                OrderDetails = new List<OrderDetail>()
            };

            foreach (var item in request.Items)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == item.Product.ProductId, cancellationToken);
                
                if (product == null)
                    throw new Exception($"Sản phẩm không tồn tại (ID: {item.Product.ProductId})");
                    
                if (product.StockQuantity < item.Quantity)
                    throw new Exception($"Sản phẩm '{product.ProductName}' không đủ số lượng trong kho (Còn lại: {product.StockQuantity}).");
                
                // Trừ tồn kho
                product.StockQuantity -= item.Quantity;
                
                // Thêm chi tiết đơn hàng
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = product.ProductId,
                    Quantity = item.Quantity,
                    Price = product.Price
                });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);
            
            await transaction.CommitAsync(cancellationToken);
            return order.OrderId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
