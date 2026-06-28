using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RehabTracking.Web.Entities;

public partial class RehabTrackingContext : DbContext
{
    public RehabTrackingContext()
    {
    }

    public RehabTrackingContext(DbContextOptions<RehabTrackingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DailyHealthMetric> DailyHealthMetrics { get; set; }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<ExerciseSession> ExerciseSessions { get; set; }

    public virtual DbSet<HotelReservation> HotelReservations { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<PatientProfile> PatientProfiles { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TreatmentPlan> TreatmentPlans { get; set; }

    public virtual DbSet<User> Users { get; set; }
    
    public virtual DbSet<ProductVariant> ProductVariants { get; set; }
    public virtual DbSet<Review> Reviews { get; set; }
    public virtual DbSet<Voucher> Vouchers { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyHealthMetric>(entity =>
        {
            entity.HasKey(e => e.MetricId).HasName("PK__DailyHea__56105645820B80B0");

            entity.Property(e => e.MetricId).HasColumnName("MetricID");
            entity.Property(e => e.CaloriesBurned).HasDefaultValue(0.0);
            entity.Property(e => e.DistanceKm).HasDefaultValue(0.0);
            entity.Property(e => e.HealthAlertStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Normal");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.StepsCount).HasDefaultValue(0);

            entity.HasOne(d => d.Patient).WithMany(p => p.DailyHealthMetrics)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DailyHealthMetrics_PatientProfiles");
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.DeviceId).HasName("PK__Devices__49E123316BE83E05");

            entity.HasIndex(e => e.DeviceSerialNumber, "UQ__Devices__D027D14B0150D48E").IsUnique();

            entity.Property(e => e.DeviceId).HasColumnName("DeviceID");
            entity.Property(e => e.DeviceSerialNumber).HasMaxLength(100);
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Inactive");

            entity.HasOne(d => d.Patient).WithMany(p => p.Devices)
                .HasForeignKey(d => d.PatientId)
                .HasConstraintName("FK_Devices_PatientProfiles");
        });

        modelBuilder.Entity<ExerciseSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__Exercise__C9F492708A02E4CE");

            entity.Property(e => e.SessionId).HasColumnName("SessionID");
            entity.Property(e => e.AvgEmg).HasColumnName("AvgEMG");
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.MaxRom).HasColumnName("MaxROM");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");

            entity.HasOne(d => d.Patient).WithMany(p => p.ExerciseSessions)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExerciseSessions_PatientProfiles");
        });

        modelBuilder.Entity<HotelReservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__HotelRes__3214EC2730249E15");

            entity.ToTable("HotelReservation");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GuestName).HasMaxLength(255);
            entity.Property(e => e.RoomNumber).HasMaxLength(50);
            entity.Property(e => e.RoomType).HasMaxLength(50);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAF5903466A");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ShippingAddress).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D30C741DBBDA");

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Products");
        });

        modelBuilder.Entity<PatientProfile>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK__PatientP__970EC3465E93CAF5");

            entity.HasIndex(e => e.UserId, "UQ__PatientP__1788CCAD2087F25E").IsUnique();

            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Doctor).WithMany(p => p.PatientProfileDoctors)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK_PatientProfiles_Doctors");

            entity.HasOne(d => d.User).WithOne(p => p.PatientProfileUser)
                .HasForeignKey<PatientProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatientProfiles_Users");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6EDA802B95D");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("ImageURL");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(150);
            entity.Property(e => e.ProductType).HasMaxLength(50);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3AFB9E19AF");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160E5F2E535").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<TreatmentPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__Treatmen__755C22D7AA36A793");

            entity.Property(e => e.PlanId).HasColumnName("PlanID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.Doctor).WithMany(p => p.TreatmentPlans)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TreatmentPlans_Doctors");

            entity.HasOne(d => d.Patient).WithMany(p => p.TreatmentPlans)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TreatmentPlans_PatientProfiles");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACDE9ED35C");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534B51C1CB2").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
