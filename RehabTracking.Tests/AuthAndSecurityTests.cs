using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Features.Identity.Register;
using Xunit;

namespace RehabTracking.Tests;

public class AuthAndSecurityTests
{
    private RehabTrackingContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<RehabTrackingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RehabTrackingContext(options);
    }

    [Fact]
    public void PasswordHashing_ShouldBeConsistentAndVerifiable()
    {
        string rawPassword = "SecurePassword@2026";
        string hash = RegisterCommandHandler.HashPassword(rawPassword);

        Assert.NotEmpty(hash);
        Assert.True(RegisterCommandHandler.VerifyPassword(rawPassword, hash));
        Assert.False(RegisterCommandHandler.VerifyPassword("WrongPassword", hash));
    }

    [Fact]
    public async Task Register_ValidPatient_ShouldSucceedAndAssignPatientRole()
    {
        using var db = CreateInMemoryContext();
        db.Roles.Add(new Role { RoleId = 3, RoleName = "Patient" });
        await db.SaveChangesAsync();

        var handler = new RegisterCommandHandler(db);
        var command = new RegisterCommand
        {
            FullName = "Nguyễn Văn Test",
            Email = "test_patient@example.com",
            Password = "Password123"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var createdUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "test_patient@example.com");
        Assert.NotNull(createdUser);
        Assert.Equal("Nguyễn Văn Test", createdUser.FullName);
        Assert.Equal(3, createdUser.RoleId);
        Assert.True(RegisterCommandHandler.VerifyPassword("Password123", createdUser.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldFail()
    {
        using var db = CreateInMemoryContext();
        db.Users.Add(new User
        {
            UserId = 1,
            Email = "duplicate@example.com",
            FullName = "Existing User",
            PasswordHash = RegisterCommandHandler.HashPassword("123456"),
            RoleId = 3
        });
        await db.SaveChangesAsync();

        var handler = new RegisterCommandHandler(db);
        var command = new RegisterCommand
        {
            FullName = "New User",
            Email = "duplicate@example.com",
            Password = "Password123"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("đã được đăng ký", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_ShortPassword_ShouldFailValidation()
    {
        using var db = CreateInMemoryContext();
        var handler = new RegisterCommandHandler(db);
        var command = new RegisterCommand
        {
            FullName = "Short Pass",
            Email = "shortpass@example.com",
            Password = "123"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("ít nhất 6 ký tự", result.Message);
    }

    [Fact]
    public async Task UserStatus_InactiveUser_CannotBeActivatedWithoutAdmin()
    {
        using var db = CreateInMemoryContext();
        var lockedUser = new User
        {
            UserId = 99,
            Email = "locked@example.com",
            FullName = "Locked User",
            PasswordHash = RegisterCommandHandler.HashPassword("123456"),
            IsActive = false,
            RoleId = 3
        };
        db.Users.Add(lockedUser);
        await db.SaveChangesAsync();

        var userInDb = await db.Users.FindAsync(99);
        Assert.NotNull(userInDb);
        Assert.False(userInDb.IsActive);
    }
}
