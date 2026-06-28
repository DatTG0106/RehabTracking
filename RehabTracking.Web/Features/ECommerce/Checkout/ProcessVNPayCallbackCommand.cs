using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.ECommerce.Checkout;

public class ProcessVNPayCallbackCommand : IRequest<bool>
{
    public int OrderId { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
}

public class ProcessVNPayCallbackCommandHandler : IRequestHandler<ProcessVNPayCallbackCommand, bool>
{
    private readonly RehabTrackingContext _context;

    public ProcessVNPayCallbackCommandHandler(RehabTrackingContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ProcessVNPayCallbackCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

        if (order == null || order.Status != "Awaiting Payment")
        {
            return false;
        }

        if (request.ResponseCode == "00") // VNPay success code
        {
            order.Status = "Pending"; // Đã thanh toán, chuyển sang Pending chờ giao hàng
            
            var payment = new Payment
            {
                OrderId = order.OrderId,
                Provider = "VNPay",
                TransactionNo = request.TransactionNo,
                Status = "Success",
                CreatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);
            
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        else
        {
            order.Status = "Payment Failed";
            
            var payment = new Payment
            {
                OrderId = order.OrderId,
                Provider = "VNPay",
                TransactionNo = request.TransactionNo,
                Status = "Failed",
                CreatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);
            
            await _context.SaveChangesAsync(cancellationToken);
            return false;
        }
    }
}
