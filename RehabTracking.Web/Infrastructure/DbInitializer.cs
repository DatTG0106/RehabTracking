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

        // Seed phân hệ Phục hồi chức năng & Chăm sóc sức khỏe STEP
        await SeedBadgesAsync(db);
        await SeedExercisesAsync(db);
        await SeedNutritionArticlesAsync(db);
        await SeedGamificationProfilesAsync(db);
        await SeedAppointmentsAndRemindersAsync(db);
        await SeedDietaryMealPlansAsync(db);
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
        var hasProducts = await db.Products.AnyAsync();
        if (!hasProducts)
        {
            db.Products.AddRange(
                new Product
                {
                    ProductId    = 1,
                    ProductName  = "STEP Smart Band Pro",
                    Description  = "Vòng đeo tay hỗ trợ phục hồi chức năng thông minh thế hệ mới tích hợp cảm biến ESP32S3 và MPU6050. Theo dõi tầm vận động (ROM) và số lần lặp theo thời gian thực. Kết nối Bluetooth 5.0, pin 8 giờ liên tục, chống nước IPX4.",
                    Price        = 4_500_000,
                    StockQuantity = 50,
                    ProductType  = "Thiết bị y tế",
                    ImageUrl     = "/images/glove-pro.jpg"
                },
                new Product
                {
                    ProductId    = 2,
                    ProductName  = "STEP Smart Band Lite",
                    Description  = "Phiên bản vòng đeo nhỏ gọn cho người mới bắt đầu tích hợp cảm biến ESP32S3 và MPU6050. Đầy đủ tính năng cơ bản: đo ROM, đếm reps, kết nối app STEP. Pin 6 giờ, thiết kế thoáng khí.",
                    Price        = 2_900_000,
                    StockQuantity = 80,
                    ProductType  = "Thiết bị y tế",
                    ImageUrl     = "/images/glove-lite.jpg"
                },
                new Product
                {
                    ProductId    = 3,
                    ProductName  = "Đai đeo cổ tay thay thế STEP",
                    Description  = "Đai đeo chất liệu vải co giãn cao cấp dùng cố định cảm biến STEP Smart Band. Thoáng mát, dễ giặt giũ, phù hợp cả hai dòng Pro và Lite.",
                    Price        = 250_000,
                    StockQuantity = 200,
                    ProductType  = "Phụ kiện",
                    ImageUrl     = "/images/wristband.jpg"
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
                    Description  = "Túi vải cao cấp chống va đập, đựng vừa thiết bị STEP + phụ kiện. Có ngăn riêng cho cáp sạc.",
                    Price        = 200_000,
                    StockQuantity = 150,
                    ProductType  = "Phụ kiện",
                    ImageUrl     = "/images/bag.jpg"
                }
            );
            await SaveChangesWithIdentityInsertAsync(db, "[Products]");
        }
        else
        {
            // Tự động đồng bộ hóa DB cũ lên thiết kế Smart Band mới
            var p1 = await db.Products.FindAsync(1);
            if (p1 != null)
            {
                p1.ProductName = "STEP Smart Band Pro";
                p1.Description = "Vòng đeo tay hỗ trợ phục hồi chức năng thông minh thế hệ mới tích hợp cảm biến ESP32S3 và MPU6050. Theo dõi tầm vận động (ROM) và số lần lặp theo thời gian thực. Kết nối Bluetooth 5.0, pin 8 giờ liên tục, chống nước IPX4.";
                p1.Price = 4_500_000;
                p1.ProductType = "Thiết bị y tế";
                p1.ImageUrl = "/images/glove-pro.jpg";
            }
            var p2 = await db.Products.FindAsync(2);
            if (p2 != null)
            {
                p2.ProductName = "STEP Smart Band Lite";
                p2.Description = "Phiên bản vòng đeo nhỏ gọn cho người mới bắt đầu tích hợp cảm biến ESP32S3 và MPU6050. Đầy đủ tính năng cơ bản: đo ROM, đếm reps, kết nối app STEP. Pin 6 giờ, thiết kế thoáng khí.";
                p2.Price = 2_900_000;
                p2.ProductType = "Thiết bị y tế";
                p2.ImageUrl = "/images/glove-lite.jpg";
            }
            var p3 = await db.Products.FindAsync(3);
            if (p3 != null)
            {
                p3.ProductName = "Đai đeo cổ tay thay thế STEP";
                p3.Description = "Đai đeo chất liệu vải co giãn cao cấp dùng cố định cảm biến STEP Smart Band. Thoáng mát, dễ giặt giũ, phù hợp cả hai dòng Pro và Lite.";
                p3.Price = 250_000;
                p3.ProductType = "Phụ kiện";
                p3.ImageUrl = "/images/wristband.jpg";
            }
            var p4 = await db.Products.FindAsync(4);
            if (p4 != null)
            {
                p4.ProductName = "Dây sạc USB-C STEP (1.5m)";
                p4.Description = "Cáp sạc chính hãng STEP, đầu nối từ tính, chống đứt gãy. Tương thích tất cả thiết bị STEP 2024 trở lên.";
                p4.Price = 150_000;
                p4.ProductType = "Phụ kiện";
                p4.ImageUrl = "/images/cable.jpg";
            }
            var p5 = await db.Products.FindAsync(5);
            if (p5 != null)
            {
                p5.ProductName = "Túi đựng thiết bị STEP";
                p5.Description = "Túi vải cao cấp chống va đập, đựng vừa thiết bị STEP + phụ kiện. Có ngăn riêng cho cáp sạc.";
                p5.Price = 200_000;
                p5.ProductType = "Phụ kiện";
                p5.ImageUrl = "/images/bag.jpg";
            }
            await db.SaveChangesAsync();
        }
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
                MedicalHistory = "Hạn chế vận động khớp cổ tay phải sau chấn thương. Cần hỗ trợ phục hồi sức mạnh cầm nắm và biên độ khớp cổ tay."
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
            DoctorNotes = "Tăng cường ROM toàn phần. Kết hợp bài tập kháng lực nhẹ. Theo dõi sát góc gập duỗi (ROM) để phát hiện bất thường.",
            LastUpdated = DateTime.UtcNow
        }, jsonOptions);

        var plan3Json = System.Text.Json.JsonSerializer.Serialize(new
        {
            ExerciseRoutine = new List<ExerciseRoutineItem>
            {
                new ExerciseRoutineItem { Name = "Trượt gân", Sets = 3, Reps = 10, DurationSeconds = 5, RestSeconds = 30 }
            },
            Schedule = new WorkoutSchedule { DaysOfWeek = new List<int> { 0, 1, 2, 3, 4, 5, 6 }, ReminderTime = "20:00" },
            DoctorNotes = "Bài tập giải phóng dây thần kinh giữa. Tránh vận động mạnh. Ghi nhận tiến trình cải thiện góc gập duỗi (ROM) để điều chỉnh phác đồ.",
            LastUpdated = DateTime.UtcNow
        }, jsonOptions);

        db.TreatmentPlans.AddRange(
            new TreatmentPlan
            {
                PlanId             = 1,
                PatientId          = 1,
                DoctorId           = 2,
                Title              = "Hỗ trợ phục hồi vận động cổ tay — Giai đoạn 1",
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
                DeviceType       = "STEP Smart Band Pro"
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
    // 8. Seed Badges
    // ----------------------------------------------------------------
    private static async Task SeedBadgesAsync(RehabTrackingContext db)
    {
        if (await db.Badges.AnyAsync()) return;

        db.Badges.AddRange(
            new Badge { Code = "STREAK_3D", Name = "Khởi đầu Bền bỉ", Description = "Hoàn thành 3 ngày tập liên tiếp không gián đoạn", IconClass = "bi-fire", XPBonus = 50 },
            new Badge { Code = "STREAK_7D", Name = "Chiến binh Tuần", Description = "Duy trì chuỗi 7 ngày tập luyện kiên trì", IconClass = "bi-lightning-charge-fill", XPBonus = 150 },
            new Badge { Code = "PERFECT_SESSION", Name = "Chuẩn xác 100%", Description = "Hoàn thành trọn vẹn 100% mục tiêu của một buổi tập", IconClass = "bi-check2-circle", XPBonus = 50 },
            new Badge { Code = "ROOKIE_GRADUATE", Name = "Vượt qua Tân binh", Description = "Tích lũy 500 XP và thăng hạng lên Cấp Bền bỉ", IconClass = "bi-shield-check", XPBonus = 100 },
            new Badge { Code = "LOW_PAIN_CHAMP", Name = "Kiểm soát Cơn đau", Description = "Thang điểm đau VAS giảm về mức an toàn <= 2 điểm", IconClass = "bi-heart-pulse-fill", XPBonus = 100 }
        );
        await db.SaveChangesAsync();
    }

    // ----------------------------------------------------------------
    // 9. Seed Exercises (Kho bài tập thông minh)
    // ----------------------------------------------------------------
    private static async Task SeedExercisesAsync(RehabTrackingContext db)
    {
        if (await db.Exercises.AnyAsync()) return;

        db.Exercises.AddRange(
            new Exercise
            {
                Title = "Gập duỗi gối chủ động (Heel Slides)",
                TargetArea = "Khớp gối",
                RecoveryPhase = "Phục hồi",
                Difficulty = 1,
                RecommendedPainMax = 4,
                DefaultDurationSeconds = 180,
                DefaultSets = 3,
                DefaultReps = 10,
                HoldSeconds = 5,
                RestSeconds = 30,
                Equipment = "Thảm tập",
                ApplicableConditions = "Phục hồi sau mổ dây chằng chéo trước (ACL), Phẫu thuật rách sụn chêm, Thoái hóa khớp gối giai đoạn 1-2",
                Contraindications = "Nhiễm trùng vết mổ khớp gối, Gãy xương quanh khớp chưa lành xương, Tràn dịch khớp gối lượng nhiều căng tức",
                Description = "Bài tập cơ bản giúp lấy lại biên độ gập duỗi gối sau phẫu thuật dây chằng hoặc thoái hóa khớp.",
                VideoUrl = "https://www.youtube.com/embed/kYJv8ZpM24Y",
                ThumbnailUrl = "/images/exercises/knee_heel_slide.jpg",
                StepInstructionsJson = "[\"Nằm ngửa trên thảm phẳng, hai chân duỗi thẳng thoải mái.\",\"Từ từ trượt gót chân đau về phía mông, gập gối đến góc tối đa không gây đau.\",\"Giữ yên ở tư thế gập tối đa trong 5 giây.\",\"Từ từ trượt gót chân trở lại vị trí ban đầu duỗi thẳng chân.\"]",
                CommonMistakesJson = "[\"Nâng gót chân hổng khỏi mặt sàn thay vì trượt nhẹ nhàng.\",\"Cố ép gối quá mức khiến điểm đau vượt ngưỡng 4/10.\",\"Xoay bàn chân ra ngoài hoặc vào trong không kiểm soát.\"]",
                RedFlagWarnings = "Nếu có tiếng rách kèm đau nhói dữ dội, khớp gối sưng to căng bóng nhanh chóng, hãy dừng tập ngay!",
                TherapeuticBenefits = "Tăng biên độ gập duỗi khớp gối (ROM), bôi trơn dịch khớp và ngăn ngừa dính bao khớp."
            },
            new Exercise
            {
                Title = "Siết cơ tứ đầu đùi (Isometric Quad Sets)",
                TargetArea = "Khớp gối",
                RecoveryPhase = "Cấp tính",
                Difficulty = 1,
                RecommendedPainMax = 3,
                DefaultDurationSeconds = 150,
                DefaultSets = 3,
                DefaultReps = 12,
                HoldSeconds = 6,
                RestSeconds = 25,
                Equipment = "Khăn cuộn",
                ApplicableConditions = "Giai đoạn sớm sau mổ ACL/PCL (tuần 1-2), Thoái hóa gối đau cấp, Bại liệt cơ tứ đầu đùi",
                Contraindications = "Đau dữ dội tại lồi củ trước xương chày, viêm gân bánh chè cấp tính",
                Description = "Bài tập tĩnh kích hoạt nhóm cơ tứ đầu đùi mà không làm chuyển động ổ khớp gối, rất an toàn giai đoạn sớm.",
                VideoUrl = "https://www.youtube.com/embed/F_fK2Q16B3M",
                ThumbnailUrl = "/images/exercises/quad_set.jpg",
                StepInstructionsJson = "[\"Ngồi hoặc nằm ngửa, chân duỗi thẳng, đặt một khăn cuộn nhỏ dưới khoeo gối.\",\"Dùng lực siết chặt mặt trước đùi, ấn mạnh khoeo chân xuống khăn cuộn.\",\"Khóa chặt xương bánh chè và giữ căng trong 6 giây.\",\"Thả lỏng 3 giây và lặp lại nhịp tiếp theo.\"]",
                CommonMistakesJson = "[\"Nín thở khi đang gồng cơ tứ đầu.\",\"Dùng lực gót chân thay vì dùng cơ mặt trước đùi.\"]",
                RedFlagWarnings = "Đau nhói dưới gân bánh chè hoặc vết mổ rỉ dịch.",
                TherapeuticBenefits = "Chống teo cơ tứ đầu đùi, hỗ trợ giữ vững khớp gối khi đứng thẳng."
            },
            new Exercise
            {
                Title = "Nâng chân thẳng có kiểm soát (Straight Leg Raise)",
                TargetArea = "Khớp gối",
                RecoveryPhase = "Tăng cường",
                Difficulty = 2,
                RecommendedPainMax = 4,
                DefaultDurationSeconds = 200,
                DefaultSets = 3,
                DefaultReps = 10,
                HoldSeconds = 5,
                RestSeconds = 30,
                Equipment = "Thảm tập",
                ApplicableConditions = "Phục hồi dây chằng sau tuần 4, Tăng sức mạnh cơ gấp hông và duỗi gối",
                Contraindications = "Rách gân bánh chè chưa phẫu thuật, Đau thắt lưng cấp tính kèm đau lan chân",
                Description = "Tăng cường sức mạnh toàn diện chuỗi cơ duỗi gối và cơ thắt lưng chậu mà không tạo áp lực nén lên sụn chêm.",
                VideoUrl = "https://www.youtube.com/embed/xL_K7U5zZ6I",
                ThumbnailUrl = "/images/exercises/slr.jpg",
                StepInstructionsJson = "[\"Nằm ngửa, chân lành gập gối 90 độ đặt bàn chân trên sàn để đỡ lưng.\",\"Chân tập duỗi thẳng hoàn toàn, gồng cơ tứ đầu khóa gối.\",\"Từ từ nâng chân thẳng lên độ cao ngang bằng đùi chân bên kia (khoảng 30-45 độ).\",\"Giữ trên cao 5 giây rồi hạ từ từ chạm sàn.\"]",
                CommonMistakesJson = "[\"Gập cong đầu gối trong lúc nâng lên.\",\"Võng lưng dưới khi nâng chân.\"]",
                RedFlagWarnings = "Đau buốt xương bánh chè hoặc đau tăng vùng thắt lưng.",
                TherapeuticBenefits = "Phục hồi cơ lực nhóm duỗi gối bậc 4-5, cải thiện dáng đi bình thường."
            },
            new Exercise
            {
                Title = "Tư thế Cây cầu (Glute Bridge)",
                TargetArea = "Cột sống thắt lưng",
                RecoveryPhase = "Phục hồi",
                Difficulty = 2,
                RecommendedPainMax = 4,
                DefaultDurationSeconds = 240,
                DefaultSets = 3,
                DefaultReps = 12,
                HoldSeconds = 4,
                RestSeconds = 30,
                Equipment = "Thảm tập",
                ApplicableConditions = "Thoát vị đĩa đệm cột sống thắt lưng L4-L5, Đau thắt lưng cơ năng, Thoái hóa cột sống lưng",
                Contraindications = "Hẹp ống sống thắt lưng nặng gây triệu chứng đi khập khiễng cách hồi, Trượt đốt sống độ 3-4",
                Description = "Tăng cường sức mạnh nhóm cơ mông và chuỗi cơ lưng dưới, giảm áp lực lên đĩa đệm thắt lưng.",
                VideoUrl = "https://www.youtube.com/embed/wPM8icPu6H8",
                ThumbnailUrl = "/images/exercises/glute_bridge.jpg",
                StepInstructionsJson = "[\"Nằm ngửa, hai gối gập 90 độ, hai bàn chân đặt phẳng trên sàn rộng bằng vai.\",\"Siết cơ bụng và cơ mông, từ từ nâng hông lên khỏi sàn sao cho đùi và thân tạo thành đường thẳng.\",\"Giữ nguyên vị trí đỉnh trong 4 giây kết hợp thở đều.\",\"Hạ hông xuống từ từ từng đốt sống một chạm sàn.\"]",
                CommonMistakesJson = "[\"Uốn cong lưng quá mức đẩy bụng lên quá cao.\",\"Không siết cơ mông mà dùng lực cơ gân khoeo chân.\"]",
                RedFlagWarnings = "Cơn đau lưng lan buốt dọc theo mông xuống mặt sau đùi và bàn chân (nghi ngờ chèn ép dây thần kinh tọa).",
                TherapeuticBenefits = "Ổn định khớp cùng chậu, củng cố cơ lưng dưới và cơ sàn chậu."
            },
            new Exercise
            {
                Title = "Tư thế Con mèo - Con bò (Cat-Cow Stretch)",
                TargetArea = "Cột sống thắt lưng",
                RecoveryPhase = "Phục hồi",
                Difficulty = 1,
                RecommendedPainMax = 3,
                DefaultDurationSeconds = 180,
                DefaultSets = 2,
                DefaultReps = 10,
                HoldSeconds = 4,
                RestSeconds = 20,
                Equipment = "Thảm tập",
                ApplicableConditions = "Đau cứng cơ thắt lưng do ngồi văn phòng lâu, Thoái hóa cột sống lưng, Cứng cột sống buổi sáng",
                Contraindications = "Gãy lún đốt sống thắt lưng cấp tính, Lao cột sống",
                Description = "Kéo giãn nhịp nhàng các đốt sống thắt lưng và ngực, giải tỏa sự co cứng cơ do ngồi lâu.",
                VideoUrl = "https://www.youtube.com/embed/ESJq520H9Fk",
                ThumbnailUrl = "/images/exercises/cat_cow.jpg",
                StepInstructionsJson = "[\"Chống hai tay và hai đầu gối trên thảm (tư thế bò bốn điểm).\",\"Hít vào thật sâu: Võng lưng xuống, ngẩng đầu mắt nhìn lên trần (tư thế Con Bò).\",\"Thở ra từ từ: Cuộn tròn lưng lên phía trên, hóp bụng, cằm thu về sát ngực (tư thế Con Mèo).\",\"Lặp lại động tác uyển chuyển theo từng nhịp thở.\"]",
                CommonMistakesJson = "[\"Di chuyển giật cục nhanh quá mức thay vì chuyển động mềm mại.\",\"Trùng khuỷu tay khi thực hiện.\"]",
                RedFlagWarnings = "Chóng mặt hoặc đau buốt đột ngột vùng thắt lưng.",
                TherapeuticBenefits = "Tăng cường tuần hoàn máu đĩa đệm, giải phóng căng thẳng cột sống."
            },
            new Exercise
            {
                Title = "Rút cằm chỉnh tư thế (Chin Tucks)",
                TargetArea = "Cột sống cổ",
                RecoveryPhase = "Cấp tính",
                Difficulty = 1,
                RecommendedPainMax = 3,
                DefaultDurationSeconds = 120,
                DefaultSets = 3,
                DefaultReps = 10,
                HoldSeconds = 5,
                RestSeconds = 20,
                Equipment = "Không cần dụng cụ",
                ApplicableConditions = "Hội chứng cổ vai gáy, Thoát vị đĩa đệm cổ C5-C6, Tư thế đầu nhô ra trước (Text Neck)",
                Contraindications = "Chấn thương cột sống cổ gãy trật chưa phẫu thuật, Chóng mặt kịch phát lành tính đang đợt cấp",
                Description = "Kích hoạt nhóm cơ gấp cổ sâu, khắc phục triệt để tư thế đầu nhô về trước (Forward Head Posture).",
                VideoUrl = "https://www.youtube.com/embed/wqq_p0p9B48",
                ThumbnailUrl = "/images/exercises/chin_tuck.jpg",
                StepInstructionsJson = "[\"Ngồi thẳng lưng trên ghế có tựa, hai vai thả lỏng tự nhiên, mắt nhìn thẳng phía trước.\",\"Đặt ngón tay nhẹ lên cằm để làm điểm mốc chuẩn.\",\"Kéo nhẹ đầu lùi thẳng về phía sau như tạo ngấn cằm (không cúi gập đầu xuống).\",\"Giữ yên 5 giây cảm nhận vùng cơ sau gáy được kéo căng nhẹ, sau đó thả lỏng.\"]",
                CommonMistakesJson = "[\"Cúi gập cổ gập cằm xuống ngực thay vì kéo tịnh tiến đầu về sau.\",\"Nâng vai lên cao khi thực hiện.\"]",
                RedFlagWarnings = "Chóng mặt quay cuồng, buồn nôn hoặc tê buốt lan xuống hai cánh tay.",
                TherapeuticBenefits = "Khắc phục thoái hóa đốt sống cổ, giảm áp lực cơ vùng chẩm gáy."
            },
            new Exercise
            {
                Title = "Bài tập con lắc Codman (Pendulum Shoulder)",
                TargetArea = "Khớp vai",
                RecoveryPhase = "Cấp tính",
                Difficulty = 1,
                RecommendedPainMax = 3,
                DefaultDurationSeconds = 180,
                DefaultSets = 3,
                DefaultReps = 10,
                HoldSeconds = 0,
                RestSeconds = 30,
                Equipment = "Bàn hoặc ghế tựa",
                ApplicableConditions = "Đông cứng khớp vai (Frozen Shoulder), Viêm gân cơ chóp xoay giai đoạn đau cấp, Sau phẫu thuật chóp xoay",
                Contraindications = "Trật khớp vai tái hồi chưa nắn chỉnh, Gãy đầu trên xương cánh tay chưa lành",
                Description = "Bài tập vận động thụ động khớp vai sử dụng trọng lực, rất hiệu quả cho viêm quanh khớp vai đông cứng (Frozen Shoulder).",
                VideoUrl = "https://www.youtube.com/embed/v8N9uRzN9sA",
                ThumbnailUrl = "/images/exercises/shoulder_pendulum.jpg",
                StepInstructionsJson = "[\"Đứng cạnh mép bàn, tay lành tì vững chắc lên bàn, hơi gập người về phía trước.\",\"Tay đau buông thõng tự nhiên vuông góc với mặt sàn, thả lỏng toàn bộ cơ vai.\",\"Sử dụng cử động nhịp nhàng của thân người để đung đưa cánh tay theo vòng tròn nhỏ.\",\"Đung đưa theo chiều kim đồng hồ 10 vòng rồi đổi ngược lại.\"]",
                CommonMistakesJson = "[\"Dùng cơ vai để chủ động lắc tay thay vì thả lỏng và dùng lực toàn thân.\",\"Lắc biên độ quá lớn gây đau nhói.\"]",
                RedFlagWarnings = "Đau buốt dữ dội hoặc cảm giác khớp vai bị trật ra khỏi ổ chảo.",
                TherapeuticBenefits = "Tách nhẹ bao khớp vai, giảm kết dính và tạo điều kiện hồi phục tuần hoàn."
            },
            new Exercise
            {
                Title = "Kéo giãn gân gót Achilles với khăn (Towel Calf Stretch)",
                TargetArea = "Cổ chân",
                RecoveryPhase = "Phục hồi",
                Difficulty = 1,
                RecommendedPainMax = 3,
                DefaultDurationSeconds = 150,
                DefaultSets = 3,
                DefaultReps = 8,
                HoldSeconds = 15,
                RestSeconds = 25,
                Equipment = "Khăn dài hoặc dây tập",
                ApplicableConditions = "Viêm cân gan chân (Plantar Fasciitis), Sau bong gân cổ chân, Cứng gân gót Achilles",
                Contraindications = "Đứt gân gót Achilles cấp tính chưa mổ, Gãy xương mắt cá chân đang bó bột",
                Description = "Kéo giãn cơ bắp chân và mạc gan chân, giúp giảm đau thốn gót chân khi bước xuống giường vào buổi sáng.",
                VideoUrl = "https://www.youtube.com/embed/sIqGZqV4JbQ",
                ThumbnailUrl = "/images/exercises/calf_stretch.jpg",
                StepInstructionsJson = "[\"Ngồi trên sàn với hai chân duỗi thẳng phía trước.\",\"Quàng một chiếc khăn dài vòng qua ức bàn chân bên đau.\",\"Hai tay nắm hai đầu khăn, từ từ kéo nhẹ nhàng về phía thân mình đến khi cảm thấy căng bắp chân.\",\"Giữ tư thế kéo căng trong 15-20 giây, hít thở đều, sau đó thả lỏng.\"]",
                CommonMistakesJson = "[\"Kéo giật mạnh đột ngột thay vì kéo từ từ tăng dần.\",\"Gập cong đầu gối làm giảm hiệu quả kéo giãn bắp chân.\"]",
                RedFlagWarnings = "Đau nhói bỏng rát gân gót hoặc cảm giác sưng phù cổ chân tăng nhanh.",
                TherapeuticBenefits = "Tăng độ dẻo dai gân gót, cải thiện tầm vận động gập mu bàn chân (dorsiflexion)."
            }
        );
        await db.SaveChangesAsync();
    }

    // ----------------------------------------------------------------
    // 10. Seed Nutrition Articles (Cẩm nang Dinh dưỡng & Chăm sóc)
    // ----------------------------------------------------------------
    private static async Task SeedNutritionArticlesAsync(RehabTrackingContext db)
    {
        if (await db.NutritionArticles.AnyAsync()) return;

        db.NutritionArticles.AddRange(
            new NutritionArticle
            {
                Title = "Chườm Nóng hay Chườm Lạnh: Khi nào nên áp dụng để giảm đau chuẩn y khoa?",
                Category = "Chăm sóc sau tập",
                ComorbidityTags = "Thoái hóa khớp, Chấn thương thể thao, Đau cơ",
                Summary = "Phân biệt rõ ràng thời điểm vàng chườm lạnh (sau chấn thương, viêm cấp) và chườm nóng (căng cứng cơ mãn tính).",
                ContentHtml = "<p>Trong vật lý trị liệu, việc lựa chọn nhiệt trị liệu (nóng hay lạnh) đóng vai trò quyết định trong việc giảm đau và thúc đẩy tái tạo mô liên kết:</p><h4>1. Khi nào chườm LẠNH (Cold Therapy)?</h4><p>Áp dụng ngay trong vòng 24 - 48 giờ sau khi chấn thương hoặc khi khớp có biểu hiện sưng, nóng, đỏ, đau. Nhiệt độ lạnh làm co mạch máu, giảm phù nề mô và gây tê tạm thời các đầu dây thần kinh. <em>Cách thực hiện:</em> Bọc đá trong khăn vải mềm, chườm 15 - 20 phút mỗi lần, cách nhau 2 tiếng.</p><h4>2. Khi nào chườm NÓNG (Heat Therapy)?</h4><p>Áp dụng cho các cơn đau âm ỉ kéo dài quá 48 giờ, co cứng cơ lưng hoặc cổ vai gáy vào buổi sáng. Nhiệt nóng giúp giãn mạch máu, tăng lưu thông máu đưa dưỡng chất nuôi dưỡng khớp.</p>",
                CoverImageUrl = "/images/knowledge/hot_cold_pack.jpg",
                AuthorDoctor = "BS. CKII Trần Minh Khoa",
                ReadTimeMinutes = 4,
                IsFeatured = true
            },
            new NutritionArticle
            {
                Title = "Chế độ dinh dưỡng Kháng viêm tự nhiên và Tái tạo sụn khớp",
                Category = "Kháng viêm",
                ComorbidityTags = "Thoái hóa khớp, Tim mạch, Gout",
                Summary = "Danh mục siêu thực phẩm giàu Omega-3, Polyphenol và Collagen Type 2 giúp phục hồi sụn khớp từ bên trong.",
                ContentHtml = "<p>Dinh dưỡng đúng cách có thể giảm tới 40% phản ứng viêm tại ổ khớp và đẩy nhanh tốc độ phục hồi chức năng dây chằng:</p><ul><li><strong>Acid béo Omega-3:</strong> Có nhiều trong cá hồi, cá trích, hạt óc chó và hạt lanh. Omega-3 ức chế sản xuất cytokine gây viêm.</li><li><strong>Củ nghệ & Curcumin:</strong> Hoạt chất Curcumin là chất chống viêm cực mạnh tương đương một số thuốc NSAIDs nhưng không gây đau dạ dày.</li><li><strong>Trái cây mọng (Quả dâu, việt quất):</strong> Chứa hàm lượng anthocyanin cao giúp chống oxy hóa và bảo vệ tế bào sụn.</li></ul>",
                CoverImageUrl = "/images/knowledge/anti_inflammatory_diet.jpg",
                AuthorDoctor = "ThS. Dinh dưỡng Lê Thị Hồng Nhung",
                ReadTimeMinutes = 5,
                IsFeatured = true
            },
            new NutritionArticle
            {
                Title = "Quy tắc Ergonomics: Tư thế làm việc và sinh hoạt chuẩn bảo vệ cột sống",
                Category = "Tư thế sinh hoạt",
                ComorbidityTags = "Thoái hóa cột sống, Đau vai gáy, Tiểu đường",
                Summary = "Hướng dẫn chi tiết góc nhìn màn hình, tư thế ngồi ghế, cách bê vật nặng đúng quy chuẩn chống tái phát thoát vị đĩa đệm.",
                ContentHtml = "<p>Hơn 80% trường hợp đau lưng tái phát bắt nguồn từ sai lệch tư thế sinh hoạt thường nhật:</p><ol><li><strong>Quy tắc bê vác vật nặng:</strong> Luôn gập đầu gối và hạ thấp hông (Squat), ôm sát vật thể vào ngực rồi dùng lực đẩy của hai chân đứng lên. Tuyệt đối không cúi cong lưng để bê vật nặng.</li><li><strong>Tư thế ngồi bàn làm việc:</strong> Màn hình ngang tầm mắt (cách 50-70cm), khuỷu tay gập 90 độ đặt trên tay vịn, hai bàn chân chạm phẳng trên sàn nhà.</li><li><strong>Quy tắc 30 phút:</strong> Cứ sau mỗi 30-45 phút ngồi làm việc, hãy đứng dậy đi lại và thực hiện 3 nhịp rút cằm (Chin Tucks) và vươn vai nhẹ.</li></ol>",
                CoverImageUrl = "/images/knowledge/ergonomics_posture.jpg",
                AuthorDoctor = "BS. CKII Trần Minh Khoa",
                ReadTimeMinutes = 6,
                IsFeatured = false
            }
        );
        await db.SaveChangesAsync();
    }

    // ----------------------------------------------------------------
    // 11. Seed Gamification Profiles
    // ----------------------------------------------------------------
    private static async Task SeedGamificationProfilesAsync(RehabTrackingContext db)
    {
        if (await db.GamificationProfiles.AnyAsync()) return;

        var patientUsers = await db.Users.Where(u => u.RoleId == 3).ToListAsync();
        int idx = 1;

        foreach (var user in patientUsers)
        {
            var profile = new GamificationProfile
            {
                UserId = user.UserId,
                TotalXP = 250 * idx,
                CurrentTier = idx >= 3 ? "Warrior" : (idx >= 2 ? "Resilient" : "Rookie"),
                CurrentStreak = idx * 2 + 1,
                LongestStreak = idx * 3 + 2,
                StreakShieldCount = 1,
                IsAnonymousLeaderboard = idx % 2 == 0,
                AnonymousDisplayName = $"Chiến binh #{user.UserId * 43 % 9000 + 1000}",
                LastActivityDate = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            };
            db.GamificationProfiles.Add(profile);
            idx++;
        }
        await db.SaveChangesAsync();
    }

    // ----------------------------------------------------------------
    // 12. Seed Lịch Hẹn, Nhắc Nhở & Thông Báo Mẫu (Mục 6)
    // ----------------------------------------------------------------
    private static async Task SeedAppointmentsAndRemindersAsync(RehabTrackingContext db)
    {
        var patient = await db.Users.FirstOrDefaultAsync(u => u.RoleId == 3);
        var doctor = await db.Users.FirstOrDefaultAsync(u => u.RoleId == 2);

        if (patient == null || doctor == null) return;

        // Seed Lịch Nhắc Nhở
        if (!await db.ReminderSchedules.AnyAsync(r => r.PatientId == patient.UserId))
        {
            db.ReminderSchedules.AddRange(
                new ReminderSchedule
                {
                    PatientId = patient.UserId,
                    Title = "Tập vật lý trị liệu buổi sáng",
                    ReminderType = "Exercise",
                    TimeOfDay = new TimeSpan(8, 30, 0),
                    DaysOfWeek = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new ReminderSchedule
                {
                    PatientId = patient.UserId,
                    Title = "Đo góc vận động (ROM) & Ghi nhật ký",
                    ReminderType = "Measurement",
                    TimeOfDay = new TimeSpan(19, 0, 0),
                    DaysOfWeek = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }

        // Seed Lịch Hẹn Khám
        if (!await db.DoctorAppointments.AnyAsync(a => a.PatientId == patient.UserId))
        {
            db.DoctorAppointments.AddRange(
                new DoctorAppointment
                {
                    PatientId = patient.UserId,
                    DoctorId = doctor.UserId,
                    AppointmentDate = DateTime.UtcNow.AddDays(2).Date.AddHours(9),
                    DurationMinutes = 30,
                    AppointmentType = "PeriodicReview",
                    Status = "Confirmed",
                    PatientReason = "Tái khám sau 2 tuần tập bài gập duỗi gối, muốn bác sĩ kiểm tra biên độ ROM",
                    DoctorNotes = "Bệnh nhân tiến triển tốt, cần chuẩn bị đo góc duỗi chủ động.",
                    MeetingLink = "https://meet.google.com/step-telehealth-demo",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow
                },
                new DoctorAppointment
                {
                    PatientId = patient.UserId,
                    DoctorId = doctor.UserId,
                    AppointmentDate = DateTime.UtcNow.AddDays(5).Date.AddHours(14),
                    DurationMinutes = 45,
                    AppointmentType = "OnlineROMCheck",
                    Status = "Pending",
                    PatientReason = "Muốn bác sĩ hướng dẫn bài tập nâng cao khớp vai",
                    CreatedAt = DateTime.UtcNow
                }
            );
        }

        // Seed Thông Báo Mẫu
        if (!await db.InAppNotifications.AnyAsync(n => n.UserId == patient.UserId))
        {
            db.InAppNotifications.AddRange(
                new InAppNotification
                {
                    UserId = patient.UserId,
                    Title = "Lịch hẹn khám đã được xác nhận",
                    Message = $"BS. {doctor.FullName} đã xác nhận lịch hẹn vào lúc {DateTime.UtcNow.AddDays(2):dd/MM/yyyy 09:00}. Nhấn để xem phòng khám.",
                    Type = "Appointment",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    ActionUrl = "/patient/appointments"
                },
                new InAppNotification
                {
                    UserId = patient.UserId,
                    Title = "Nhiệm vụ mới đang chờ bạn!",
                    Message = "Hoàn thành bài tập hôm nay để nhận thêm +50 XP và duy trì chuỗi ngày kiên trì nhé!",
                    Type = "Gamification",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-5),
                    ActionUrl = "/patient/ranking"
                }
            );
        }

        await db.SaveChangesAsync();
    }

    // ----------------------------------------------------------------
    // 13. Seed Thực Đơn Mẫu Y Khoa Phục Hồi (Mục 7)
    // ----------------------------------------------------------------
    private static async Task SeedDietaryMealPlansAsync(RehabTrackingContext db)
    {
        if (await db.DietaryMealPlans.AnyAsync()) return;

        db.DietaryMealPlans.AddRange(
            new DietaryMealPlan
            {
                Title = "Thực đơn giàu Canxi & Protein thúc đẩy liền xương gãy",
                TargetCondition = "Gãy xương / Liền xương",
                Phase = "Giai đoạn phục hồi tích cực (Tuần 3-8)",
                CaloriesTarget = 2000,
                ProteinGrams = 95,
                CalciumMg = 1200,
                BreakfastMenu = "1 tô phở bò tái nạm ít béo, 1 ly sữa tươi tiệt trùng bổ sung Canxi & Vitamin D3, 1 quả chuối sứ.",
                LunchMenu = "2 chén cơm gạo lứt, 150g cá hồi nướng bơ tỏi, 1 đĩa rau cải thìa xào nấm hương, 1 bát canh cua mồng tơi.",
                DinnerMenu = "1 chén cơm trắng, 120g ức gà rim hạt sen, đậu phụ sốt cà chua, canh sườn bí đỏ hầm nhừ.",
                SnacksMenu = "1 hũ sữa chua Hy Lạp trộn hạt chia và 30g hạnh nhân sấy mộc.",
                ClinicalNotes = "Uống đủ 2 - 2.5 lít nước/ngày. Tránh tuyệt đối cà phê đặc, nước ngọt có ga và rượu bia vì làm tăng đào thải canxi qua đường niệu.",
                AuthorDoctor = "BS. CKII Trần Minh Khoa",
                CreatedAt = DateTime.UtcNow
            },
            new DietaryMealPlan
            {
                Title = "Thực đơn kháng viêm sụn khớp & kiểm soát cân nặng",
                TargetCondition = "Thoái hóa khớp / Viêm khớp",
                Phase = "Giai đoạn củng cố & duy trì",
                CaloriesTarget = 1650,
                ProteinGrams = 80,
                CalciumMg = 1000,
                BreakfastMenu = "Cháo yến mạch nấu tôm nõn và rau chân vịt (spinach), 1 ly nước ép cần tây táo xanh.",
                LunchMenu = "1 chén cơm gạo huyết rồng, 150g cá trích hoặc cá thu kho dứa, đĩa súp lơ xanh hấp chấm kho quẹt nhẹ, canh rong biển đậu hũ.",
                DinnerMenu = "Salad cá ngừ ngâm dầu ô liu nguyên chất, trứng gà luộc lòng đào, khoai lang hấp cỡ vừa.",
                SnacksMenu = "1 ly sinh tố bơ ít đường hoặc nắm hạt óc chó giàu Omega-3 tự nhiên.",
                ClinicalNotes = "Ưu tiên gia vị có tính kháng viêm tự nhiên như nghệ vàng (Curcumin), gừng tươi và tỏi. Giảm lượng muối dưới 5g/ngày.",
                AuthorDoctor = "BS. Lê Thị Hồng Nhung",
                CreatedAt = DateTime.UtcNow
            },
            new DietaryMealPlan
            {
                Title = "Phác đồ dinh dưỡng phục hồi mô mềm & dây chằng sau mổ ACL",
                TargetCondition = "Hồi phục sau mổ ACL",
                Phase = "Giai đoạn cấp (Tuần 1-2)",
                CaloriesTarget = 1850,
                ProteinGrams = 90,
                CalciumMg = 900,
                BreakfastMenu = "Bánh mì đen kẹp 2 quả trứng ốp la, cà chua bi, 1 ly sữa chua men sống.",
                LunchMenu = "Cơm trắng vừa đủ, 150g thăn bò xào ớt chuông giàu Vitamin C, canh súp rau củ củ dền xương hầm.",
                DinnerMenu = "Cá chẽm hấp hành gừng, măng tây xào tỏi, 1 củ khoai tây nghiền với sữa tươi.",
                SnacksMenu = "Nước cam vắt tươi không đường (Vitamin C giúp tổng hợp Collagen cho dây chằng) và quả việt quất.",
                ClinicalNotes = "Vitamin C đóng vai trò enzyme đồng yếu tố không thể thiếu để liên kết các sợi Collagen tái tạo dây chằng chéo.",
                AuthorDoctor = "BS. CKII Trần Minh Khoa",
                CreatedAt = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();
    }

    // ----------------------------------------------------------------
    // Helper: Cho phép insert ID cố định vào cột IDENTITY
    // ----------------------------------------------------------------
    private static async Task SaveChangesWithIdentityInsertAsync(RehabTrackingContext db, string tableName)
    {
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [" + tableName + "] ON");
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [" + tableName + "] OFF");
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}