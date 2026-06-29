using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Constants;

namespace RehabTracking.Web.Features.Healthcare.AdminDashboard;

public class ProcessOrderCommand : IRequest<bool>
{
    public int OrderId { get; set; }
    public string DeviceSerialNumber { get; set; } = string.Empty;
}

public class ProcessOrderCommandHandler : IRequestHandler<ProcessOrderCommand, bool>
{
    private readonly RehabTrackingContext _context;

    public ProcessOrderCommandHandler(RehabTrackingContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ProcessOrderCommand request, CancellationToken cancellationToken)
    {
        // Sử dụng Transaction vì cần cập nhật 2 bảng (Orders và Devices)
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);
                
            if (order == null)
                throw new Exception(string.Format(AppMessages.OrderNotFound, request.OrderId));

            // BUG FIX: Prevent processing an already-shipped order
            if (order.Status == "Shipped" || order.Status == "Completed")
                throw new Exception(string.Format(AppMessages.OrderAlreadyProcessed, request.OrderId, order.Status));

            // 1. Sinh vòng tay mới (Gán mã MAC)
            // Kiểm tra xem MAC này đã tồn tại chưa để tránh trùng lặp
            var existingDevice = await _context.Devices
                .AnyAsync(d => d.DeviceSerialNumber == request.DeviceSerialNumber, cancellationToken);
                
            if (existingDevice)
                throw new Exception(AppMessages.SerialExists);

            var newDevice = new Device
            {
                DeviceSerialNumber = request.DeviceSerialNumber,
                Status = "Inactive"
                // Tạm thời chưa có PatientId. Sẽ được cập nhật ở luồng ActivateDevice
            };
            
            _context.Devices.Add(newDevice);

            // 2. Chuyển trạng thái Order
            order.Status = "Shipped"; // Pending -> Shipped (hoặc Processing tùy quy trình)

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
