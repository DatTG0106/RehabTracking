using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Infrastructure;

/// <summary>
/// Khởi tạo dữ liệu mẫu cho database khi chạy lần đầu (Development).
/// Chỉ seed nếu bảng chưa có dữ liệu — an toàn để chạy nhiều lần.
/// </summary>
public class DbInitializer
{
    // ----------------------------------------------------------------
    // Entry point: gọi từ Program.cs sau khi app.Build()
    // ----------------------------------------------------------------
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RehabTrackingContext>();

        // Đảm bảo DB đã tồn tại (không migrate tự động, chỉ seed)
        await SeedRolesAsync(db);
        await SeedUsersAsync(db);
        await SeedProductsAsync(db);
        await SeedPatientProfilesAsync(db);
        await SeedTreatmentPlansAsync(db);
        await SeedExerciseSessionsAsync(db);
        await SeedOrdersAsync(db);
    }

    // ----------------------------------------------------------------
    // 1. Roles
    // ----------------------------------------------------------------
    private static async Task SeedRolesAsync(RehabTrackingContext db)
    {
        if (await db.Roles.AnyAsync()) return;

        db.Roles.AddRange(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "Doctor" },
            new Role { RoleId = 3, RoleName = "Patient" }
        );
        await SaveChangesWithIdentityInsertAsync(db, "[Roles]");
    }

    // ----------------------------------------------------------------
    // 2. Users (Admin / Doctor / Patient)
    //    Mật khẩu demo hash bằng BCrypt: "123456"
    //    Nếu dự án dùng SHA256 thay vì BCrypt, đổi hàm HashPassword()
    // ----------------------------------------------------------------
    private static async Task SeedUsersAsync(RehabTrackingContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var passwordHash = HashPassword("123456");

        db.Users.AddRange(

            // Admin
            new User
            {
                UserId    = 1,
                FullName  = "Quản trị viên STEP",
                Email     = "admin@test.com",
                PasswordHash = passwordHash,
                RoleId    = 1,
                PhoneNumber = "0900000001",
                Address   = "123 Nguyễn Huệ, Quận 1, TP.HCM",
                IsActive  = true,
                CreatedAt = DateTime.UtcNow
            },

            // Doctor 1
            new User
            {
                UserId    = 2,
                FullName  = "BS. Trần Minh Khoa",
                Email     = "doctor@test.com",
                PasswordHash = passwordHash,
                RoleId    = 2,
                PhoneNumber = "0900000002",
                Address   = "456 Lê Văn Lương, Quận 7, TP.HCM",
                IsActive  = true,
                CreatedAt = DateTime.UtcNow
            },

            // Doctor 2
            new User
            {
                UserId    = 3,
                FullName  = "BS. Lê Thị Hồng Nhung",
                Email     = "doctor2@test.com",
                PasswordHash = passwordHash,
                RoleId    = 2,
                PhoneNumber = "0900000003",
                Address   = "789 Đinh Tiên Hoàng, Quận Bình Thạnh, TP.HCM",
                IsActive  = true,
                CreatedAt = DateTime.UtcNow
            },

            // Patient 1
            new User
            {
                UserId    = 4,
                FullName  = "Nguyễn Văn An",
                Email     = "patient@test.com",
                PasswordHash = passwordHash,
                RoleId    = 3,
                PhoneNumber = "0901234567",
                Address   = "12 Hoàng Diệu, Quận 4, TP.HCM",
                IsActive  = true,
                CreatedAt = DateTime.UtcNow
            },

            // Patient 2
            new User
            {
                UserId    = 5,
                FullName  = "Trần Thị Bình",
                Email     = "patient2@test.com",
                PasswordHash = passwordHash,
                RoleId    = 3,
                PhoneNumber = "0912345678",
                Address   = "34 Nguyễn Thị Minh Khai, Quận 3, TP.HCM",
                IsActive  = true,
                CreatedAt = DateTime.UtcNow
            },

            // Patient 3
            new User
            {
                UserId    = 6,
                FullName  = "Lê Quang Vinh",
                Email     = "patient3@test.com",
                PasswordHash = passwordHash,
                RoleId    = 3,
                PhoneNumber = "0923456789",
                Address   = "56 Phan Xích Long, Quận Phú Nhuận, TP.HCM",
                IsActive  = true,
                CreatedAt = DateTime.UtcNow
            }
        );
        await SaveChangesWithIdentityInsertAsync(db, "[Users]");
    }

    // ----------------------------------------------------------------
    // 3. Products
    // ----------------------------------------------------------------
    private static async Task SeedProductsAsync(RehabTrackingContext db)
    {
        if (await db.Products.AnyAsync()) return;

        db.Products.AddRange(
            new Product
            {
                ProductId    = 1,
                ProductName  = "STEP Smart Glove Pro",
                Description  = "Găng tay phục hồi chức năng thông minh thế hệ mới. Theo dõi EMG, ROM và số lần lặp theo thời gian thực. Kết nối Bluetooth 5.0, pin 8 giờ liên tục, chống nước IPX4.",
                Price        = 4_500_000,
                StockQuantity = 50,
                ProductType  = "Thiết bị y tế",
                ImageUrl     = "/images/glove-pro.jpg"
            },
            new Product
            {
                ProductId    = 2,
                ProductName  = "STEP Smart Glove Lite",
                Description  = "Phiên bản nhỏ gọn cho người mới bắt đầu. Đầy đủ tính năng cơ bản: đo ROM, đếm reps, kết nối app STEP. Pin 6 giờ, thiết kế thoáng khí.",
                Price        = 2_900_000,
                StockQuantity = 80,
                ProductType  = "Thiết bị y tế",
                ImageUrl     = "/images/glove-lite.jpg"
            },
            new Product
            {
                ProductId    = 3,
                ProductName  = "Bộ điện cực EMG thay thế (10 miếng)",
                Description  = "Điện cực cảm biến EMG dùng với thiết bị STEP. Vật liệu y tế an toàn, bám dính tốt, dùng được 15-20 lần mỗi miếng. Phù hợp cả hai dòng Pro và Lite.",
                Price        = 350_000,
                StockQuantity = 200,
                ProductType  = "Phụ kiện",
                ImageUrl     = "/images/electrodes.jpg"
            },
            new Product
            {
                ProductId    = 4,
                ProductName  = "Dây sạc USB-C STEP (1.5m)",
                Description  = "Cáp sạc chính hãng STEP, đầu nối từ tính, chống đứt gãy. Tương thích tất cả thiết bị STEP 2024 trở lên.",
                Price        = 150_000,
                StockQuantity = 300,
                ProductType  = "Phụ kiện",
                ImageUrl     = "/images/cable.jpg"
            },
            new Product
            {
                ProductId    = 5,
                ProductName  = "Túi đựng thiết bị STEP",
                Description  = "Túi vải cao cấp chống va đập, đựng vừa thiết bị STEP + phụ kiện. Có ngăn riêng cho cáp sạc và điện cực.",
                Price        = 200_000,
                StockQuantity = 150,
                ProductType  = "Phụ kiện",
                ImageUrl     = "/images/bag.jpg"
            }
        );
        await SaveChangesWithIdentityInsertAsync(db, "[Products]");
    }

    // ----------------------------------------------------------------
    // 4. PatientProfiles (gán bác sĩ cho từng bệnh nhân)
    // ----------------------------------------------------------------
    private static async Task SeedPatientProfilesAsync(RehabTrackingContext db)
    {
        if (await db.PatientProfiles.AnyAsync()) return;

        db.PatientProfiles.AddRange(
            new PatientProfile
            {
                PatientId      = 1,
                UserId         = 4,          // Nguyễn Văn An
                DoctorId       = 2,          // BS. Trần Minh Khoa
                DateOfBirth    = new DateOnly(1979, 3, 15),
                Gender         = "Nam",
                MedicalHistory = "Yếu cơ tay phải sau đột quỵ nhẹ. Cần phục hồi sức mạnh cầm nắm và biên độ khớp cổ tay."
            },
            new PatientProfile
            {
                PatientId      = 2,
                UserId         = 5,          // Trần Thị Bình
                DoctorId       = 2,          // BS. Trần Minh Khoa
                DateOfBirth    = new DateOnly(1985, 7, 22),
                Gender         = "Nữ",
                MedicalHistory = "Phục hồi sau chấn thương dây chằng cổ tay trái. Cần tăng cường ROM và phối hợp cơ."
            },
            new PatientProfile
            {
                PatientId      = 3,
                UserId         = 6,          // Lê Quang Vinh
                DoctorId       = 3,          // BS. Lê Thị Hồng Nhung
                DateOfBirth    = new DateOnly(1990, 11, 5),
                Gender         = "Nam",
                MedicalHistory = "Hội chứng ống cổ tay mãn tính. Đang điều trị bảo tồn, theo dõi phản ứng điện cơ."
            }
        );
        await SaveChangesWithIdentityInsertAsync(db, "[PatientProfiles]");
    }

    // ----------------------------------------------------------------
    // 5. TreatmentPlans
    // ----------------------------------------------------------------
    private static async Task SeedTreatmentPlansAsync(RehabTrackingContext db)
    {
        if (await db.TreatmentPlans.AnyAsync()) return;

        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };

        var plan1Json = System.Text.Json.JsonSerializer.Serialize(new
        {
            ExerciseRoutine = new List<ExerciseRoutineItem>
            {
                new ExerciseRoutineItem { Name = "Gập duỗi gối", Sets = 3, Reps = 10, DurationSeconds = 5, RestSeconds = 30, VideoUrl = "https://www.youtube.com/watch?v=YyvSfVjQeL0", Notes = "Giữ thẳng lưng" },
                new ExerciseRoutineItem { Name = "Nâng chân thẳng", Sets = 3, Reps = 12, DurationSeconds = 3, RestSeconds = 20, VideoUrl = "https://www.youtube.com/watch?v=l4kQd9eWclE" }
            },
            Schedule = new WorkoutSchedule { DaysOfWeek = new List<int> { 1, 3, 5 }, ReminderTime = "08:00" },
            DoctorNotes = "Tập trung vào cầm nắm và mở bàn tay. Bắt đầu nhẹ, tăng dần cường độ mỗi tuần. Nghỉ ít nhất 1 ngày sau mỗi 3 ngày tập.",
            LastUpdated = DateTime.UtcNow
        }, jsonOptions);

        var plan2Json = System.Text.Json.JsonSerializer.Serialize(new
        {
            ExerciseRoutine = new List<ExerciseRoutineItem>
            {
                new ExerciseRoutineItem { Name = "Xoay cổ tay", Sets = 3, Reps = 15, DurationSeconds = 3, RestSeconds = 20 },
                new ExerciseRoutineItem { Name = "Căng cơ cẳng tay", Sets = 2, Reps = 1, DurationSeconds = 30, RestSeconds = 15 }
            },
            Schedule = new WorkoutSchedule { DaysOfWeek = new List<int> { 2, 4, 6 }, ReminderTime = "17:00" },
            DoctorNotes = "Tăng cường ROM toàn phần. Kết hợp bài tập kháng lực nhẹ. Theo dõi tín hiệu EMG để phát hiện co cứng bất thường.",
            LastUpdated = DateTime.UtcNow
        }, jsonOptions);

        var plan3Json = System.Text.Json.JsonSerializer.Serialize(new
        {
            ExerciseRoutine = new List<ExerciseRoutineItem>
            {
                new ExerciseRoutineItem { Name = "Trượt gân", Sets = 3, Reps = 10, DurationSeconds = 5, RestSeconds = 30 }
            },
            Schedule = new WorkoutSchedule { DaysOfWeek = new List<int> { 0, 1, 2, 3, 4, 5, 6 }, ReminderTime = "20:00" },
            DoctorNotes = "Bài tập giải phóng dây thần kinh giữa. Tránh vận động mạnh. Ghi nhận các đỉnh EMG bất thường để điều chỉnh phác đồ.",
            LastUpdated = DateTime.UtcNow
        }, jsonOptions);

        db.TreatmentPlans.AddRange(
            new TreatmentPlan
            {
                PlanId             = 1,
                PatientId          = 1,
                DoctorId           = 2,
                Title              = "Phục hồi sau đột quỵ — Giai đoạn 1",
                Description        = plan1Json,
                TargetRepetitions  = 20,
                TargetDuration     = 30,
                IsActive           = true,
                CreatedAt          = DateTime.UtcNow
            },
            new TreatmentPlan
            {
                PlanId             = 2,
                PatientId          = 2,
                DoctorId           = 2,
                Title              = "Phục hồi dây chằng cổ tay — Giai đoạn 2",
                Description        = plan2Json,
                TargetRepetitions  = 15,
                TargetDuration     = 25,
                IsActive           = true,
                CreatedAt          = DateTime.UtcNow
            },
            new TreatmentPlan
            {
                PlanId             = 3,
                PatientId          = 3,
                DoctorId           = 3,
                Title              = "Điều trị hội chứng ống cổ tay",
                Description        = plan3Json,
                TargetRepetitions  = 12,
                TargetDuration     = 20,
                IsActive           = true,
                CreatedAt          = DateTime.UtcNow
            }
        );
        await SaveChangesWithIdentityInsertAsync(db, "[TreatmentPlans]");
    }

    // ----------------------------------------------------------------
    // 6. ExerciseSessions — lịch sử tập luyện 14 ngày gần nhất
    // ----------------------------------------------------------------
    private static async Task SeedExerciseSessionsAsync(RehabTrackingContext db)
    {
        if (await db.ExerciseSessions.AnyAsync()) return;

        var sessions = new List<ExerciseSession>();
        var rng = new Random(42); // seed cố định để dữ liệu lặp lại

        // Helper tạo session
        ExerciseSession MakeSession(int patientId, int daysAgo, int reps, int durationMin, double avgEmg, double maxRom) =>
            new ExerciseSession
            {
                PatientId        = patientId,
                StartTime        = DateTime.UtcNow.AddDays(-daysAgo).AddHours(rng.Next(7, 10)),
                EndTime          = DateTime.UtcNow.AddDays(-daysAgo).AddHours(rng.Next(7, 10)).AddMinutes(durationMin),
                RepetitionsCount = reps,
                DurationMinutes  = durationMin,
                AvgEmg           = avgEmg,
                MaxRom           = maxRom,
                DeviceType       = "STEP Smart Glove Pro"
            };

        // Patient 1 — Nguyễn Văn An (14 ngày, tiến bộ dần)
        sessions.AddRange(new[]
        {
            MakeSession(1, 13, 8,  18, 45.2, 28),
            MakeSession(1, 11, 10, 20, 48.7, 32),
            MakeSession(1, 9,  12, 22, 52.1, 36),
            MakeSession(1, 7,  14, 24, 55.3, 40),
            MakeSession(1, 5,  16, 25, 58.9, 43),
            MakeSession(1, 3,  18, 28, 61.4, 46),
            MakeSession(1, 1,  20, 30, 63.8, 49),
        });

        // Patient 2 — Trần Thị Bình (xen kẽ, ROM cải thiện rõ)
        sessions.AddRange(new[]
        {
            MakeSession(2, 14, 10, 22, 38.5, 40),
            MakeSession(2, 12, 12, 25, 42.0, 45),
            MakeSession(2, 10, 13, 25, 44.5, 50),
            MakeSession(2, 7,  15, 25, 47.2, 55),
            MakeSession(2, 4,  15, 25, 49.8, 58),
            MakeSession(2, 2,  15, 25, 51.3, 60),
        });

        // Patient 3 — Lê Quang Vinh (mới bắt đầu, ít buổi)
        sessions.AddRange(new[]
        {
            MakeSession(3, 10, 8, 15, 30.1, 25),
            MakeSession(3, 7,  9, 17, 32.4, 28),
            MakeSession(3, 4, 10, 18, 34.7, 30),
            MakeSession(3, 1, 11, 20, 36.0, 33),
        });

        db.ExerciseSessions.AddRange(sessions);
        await db.SaveChangesAsync();
    }

    // ----------------------------------------------------------------
    // 7. Orders — đơn hàng mẫu
    // ----------------------------------------------------------------
    private static async Task SeedOrdersAsync(RehabTrackingContext db)
    {
        if (await db.Orders.AnyAsync()) return;

        db.Orders.AddRange(
            new Order
            {
                OrderId         = 1,
                CustomerId      = 4,   // Nguyễn Văn An
                OrderDate       = DateTime.UtcNow.AddDays(-10),
                TotalAmount     = 4_500_000,
                ShippingAddress = "Nguyễn Văn An, 12 Hoàng Diệu, Quận 4, TP.HCM",
                Status          = "Shipped"
            },
            new Order
            {
                OrderId         = 2,
                CustomerId      = 5,   // Trần Thị Bình
                OrderDate       = DateTime.UtcNow.AddDays(-5),
                TotalAmount     = 3_200_000,
                ShippingAddress = "Trần Thị Bình, 34 Nguyễn Thị Minh Khai, Quận 3, TP.HCM",
                Status          = "Processing"
            },
            new Order
            {
                OrderId         = 3,
                CustomerId      = 6,   // Lê Quang Vinh
                OrderDate       = DateTime.UtcNow.AddDays(-2),
                TotalAmount     = 2_900_000,
                ShippingAddress = "Lê Quang Vinh, 56 Phan Xích Long, Quận Phú Nhuận, TP.HCM",
                Status          = "Pending"
            }
        );
        await SaveChangesWithIdentityInsertAsync(db, "[Orders]");
    }

    // ----------------------------------------------------------------
    // Helper: Hash password — phải khớp với RegisterCommandHandler.HashPassword()
    // Công thức: SHA256( "RehabTracking_Salt_{password}_2026" )
    // ----------------------------------------------------------------
    private static string HashPassword(string password)
    {
        var saltedPassword = $"RehabTracking_Salt_{password}_2026";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    // ----------------------------------------------------------------
    // Helper: Cho phép insert ID cố định vào cột IDENTITY
    // ----------------------------------------------------------------
    private static async Task SaveChangesWithIdentityInsertAsync(RehabTrackingContext db, string tableName)
    {
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} ON");
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} OFF");
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}