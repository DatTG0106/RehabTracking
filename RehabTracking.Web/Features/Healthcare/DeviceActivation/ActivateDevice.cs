using System.ComponentModel.DataAnnotations;
using System.Linq;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.Healthcare.DeviceActivation;

public static class ActivateDevice
{
    public class Command : IRequest<Result>
    {
        public int CurrentUserId { get; set; }
        
        [Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(RehabTracking.Web.Resources.ErrorMessages))]
        public string DeviceSerialNumber { get; set; } = string.Empty;
    }

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
            // BƯỚC 1: Kiểm tra tính hợp lệ của thiết bị
            var device = await _db.Devices
                .FirstOrDefaultAsync(d => d.DeviceSerialNumber == request.DeviceSerialNumber, cancellationToken);

            if (device == null)
                return new Result(false, RehabTracking.Web.Resources.ErrorMessages.DeviceNotFound);
                
            if (device.Status != "Inactive")
                return new Result(false, RehabTracking.Web.Resources.ErrorMessages.DeviceAlreadyActivated);

            // Kểm tra xem user đã có PatientProfile chưa, nếu có rồi thì không cần tạo mới
            var existingProfile = await _db.PatientProfiles
                .FirstOrDefaultAsync(p => p.UserId == request.CurrentUserId, cancellationToken);

            int patientId;

            if (existingProfile == null)
            {
                // BƯỚC 2: Thuật toán Round-Robin - Tìm Bác sĩ ít bệnh nhân nhất
                // Lấy RoleId của Doctor (giả định role name là "Doctor")
                var doctorRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Doctor", cancellationToken);
                if (doctorRole == null)
                    return new Result(false, RehabTracking.Web.Resources.ErrorMessages.DoctorRoleNotFound);

                var query = _db.Users
                    .Where(u => u.RoleId == doctorRole.RoleId)
                    .Select(doctor => new 
                    {
                        DoctorId = doctor.UserId,
                        PatientCount = _db.PatientProfiles.Count(p => p.DoctorId == doctor.UserId)
                    });

                var optimalDoctorId = await Queryable.OrderBy(query, x => x.PatientCount)
                    .Select(x => x.DoctorId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (optimalDoctorId == 0)
                    return new Result(false, RehabTracking.Web.Resources.ErrorMessages.NoDoctorAvailable);

                // BƯỚC 3: Tạo Hồ sơ bệnh nhân mới và gán Bác sĩ vừa tìm được
                var newPatientProfile = new PatientProfile
                {
                    UserId = request.CurrentUserId,
                    DoctorId = optimalDoctorId,
                    Gender = "Unknown", // Placeholder, can be updated later
                    MedicalHistory = "Hồ sơ mới khởi tạo từ việc kích hoạt thiết bị."
                };

                _db.PatientProfiles.Add(newPatientProfile);
                await _db.SaveChangesAsync(cancellationToken); 
                
                patientId = newPatientProfile.PatientId;
            }
            else
            {
                patientId = existingProfile.PatientId;
            }

            // BƯỚC 4: Cập nhật trạng thái vòng tay
            device.Status = "Active";
            device.PatientId = patientId;
            device.ActivatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return new Result(true, RehabTracking.Web.Resources.Messages.DeviceActivationSuccess);
        }
    }
}
