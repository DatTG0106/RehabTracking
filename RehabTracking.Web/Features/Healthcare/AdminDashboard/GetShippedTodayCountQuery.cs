using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.Healthcare.AdminDashboard;

/// <summary>
/// Query đếm số đơn hàng đã giao (status = "Shipped") trong ngày hôm nay.
/// BUG FIX: Thay thế biến cục bộ shippedTodayCount chỉ tồn tại trong phiên,
/// bằng query trực tiếp từ DB để dữ liệu chính xác khi reload.
/// </summary>
public class GetShippedTodayCountQuery : IRequest<int> { }

public class GetShippedTodayCountQueryHandler : IRequestHandler<GetShippedTodayCountQuery, int>
{
    private readonly RehabTrackingContext _context;

    public GetShippedTodayCountQueryHandler(RehabTrackingContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(GetShippedTodayCountQuery request, CancellationToken cancellationToken)
    {
        var todayUtc = DateTime.UtcNow.Date;
        return await _context.Orders
            .CountAsync(o => o.Status == "Shipped" && o.OrderDate.HasValue && o.OrderDate.Value.Date == todayUtc, cancellationToken);
    }
}
