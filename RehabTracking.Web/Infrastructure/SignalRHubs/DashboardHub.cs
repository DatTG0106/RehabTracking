using Microsoft.AspNetCore.SignalR;

namespace RehabTracking.Web.Infrastructure.SignalRHubs;

public class DashboardHub : Hub
{
    // Khi một màn hình Dashboard mở lên, nó sẽ gọi hàm này để xin gia nhập vào phòng của Bệnh nhân đó
    public async Task JoinPatientGroup(int patientId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Patient_{patientId}");
    }

    public async Task SimulateData(int patientId)
    {
        var rnd = new Random();
        double avgEmg = 40 + (rnd.NextDouble() * 80); // 40-120
        double maxRom = 10 + (rnd.NextDouble() * 30);
        
        await Clients.Group($"Patient_{patientId}")
            .SendCoreAsync("ReceiveNewMetrics", new object[] { avgEmg, maxRom });
    }
}
