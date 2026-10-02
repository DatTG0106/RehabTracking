using RehabTracking.Web.Components;
using Microsoft.EntityFrameworkCore;       // Khai báo thư viện EF Core
using RehabTracking.Web.Entities;          // Khai báo thư mục chứa Models và DbContext sinh ra từ DB
using RehabTracking.Web.Infrastructure;    // DbInitializer
using Radzen;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ================================================================
// 1. CẤU HÌNH CƠ SỞ DỮ LIỆU (ENTITY FRAMEWORK CORE)
// ================================================================
builder.Services.AddDbContext<RehabTrackingContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ================================================================
// 2. CẤU HÌNH CQRS (MEDIATR)
// ================================================================
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Hệ thống Phục hồi Chức năng Y tế STEP API",
        Version = "v1",
        Description = "Nền tảng Y tế Số Phục hồi Chức năng & Theo dõi Tập luyện Thông minh (STEP). Hỗ trợ bác sĩ và bệnh nhân."
    });
});

builder.Services.AddScoped<RehabTracking.Web.Features.ECommerce.Cart.CartState>(); // Scoped: mỗi Blazor circuit (tab/user) có giỏ hàng riêng
builder.Services.AddScoped<RehabTracking.Web.Services.GamificationService>();
builder.Services.AddScoped<RehabTracking.Web.Services.RecoveryTrackingService>();
builder.Services.AddScoped<RehabTracking.Web.Services.AiHealthAssistantService>();
builder.Services.AddScoped<RehabTracking.Web.Services.AuditService>();
builder.Services.AddScoped<RehabTracking.Web.Services.AppNotificationService>();
builder.Services.AddScoped<RehabTracking.Web.Services.AppointmentService>();
builder.Services.AddScoped<RehabTracking.Web.Services.ReminderService>();
builder.Services.AddScoped<RehabTracking.Web.Services.KnowledgeService>();
builder.Services.AddScoped<RehabTracking.Web.Services.EhrService>();
builder.Services.AddHostedService<RehabTracking.Web.Services.ReminderBackgroundService>();
builder.Services.AddRadzenComponents();
builder.Services.AddSignalR();

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

if (builder.Environment.IsDevelopment())
{
    // Đảm bảo mỗi lần chạy lại ở mode dev thì session/cookie cũ sẽ bị vô hiệu hóa
    builder.Services.AddDataProtection()
        .UseEphemeralDataProtectionProvider();
}

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ================================================================
// 3. TỰ ĐỘNG MIGRATE VÀ SEED DỮ LIỆU MẪU
//    An toàn: Tự động tạo bảng & seed nếu chưa có dữ liệu
// ================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<RehabTrackingContext>();
        if (db.Database.IsSqlServer())
        {
            await db.Database.MigrateAsync();
        }
        await DbInitializer.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi tự động cập nhật CSDL trên Server: {Message}", ex.Message);
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/404");

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "STEP Rehab Tracking API v1");
    c.RoutePrefix = "swagger";
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();
app.MapHub<RehabTracking.Web.Infrastructure.SignalRHubs.DashboardHub>("/dashboardHub");

app.Run();