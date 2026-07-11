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
builder.Services.AddControllers();
builder.Services.AddScoped<RehabTracking.Web.Features.ECommerce.Cart.CartState>(); // Scoped: mỗi Blazor circuit (tab/user) có giỏ hàng riêng
builder.Services.AddRadzenComponents();
builder.Services.AddSignalR();

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login"; // Or /access-denied
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
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
// 3. SEED DỮ LIỆU MẪU (chỉ chạy trong môi trường Development)
//    An toàn: mỗi phương thức kiểm tra AnyAsync() trước khi insert
// ================================================================
if (app.Environment.IsDevelopment())
{
    await DbInitializer.SeedAsync(app.Services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();
app.MapHub<RehabTracking.Web.Infrastructure.SignalRHubs.DashboardHub>("/dashboardHub");

app.Run();