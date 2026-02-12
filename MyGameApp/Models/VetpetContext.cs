using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace MyGameApp.Models;

public partial class VetpetContext : DbContext
{
    public VetpetContext()
    {
    }

    public VetpetContext(DbContextOptions<VetpetContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Bill> Bills { get; set; }

    public virtual DbSet<BillItem> BillItems { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Medicine> Medicines { get; set; }

    public virtual DbSet<Pet> Pets { get; set; }

    public virtual DbSet<PetType> PetTypes { get; set; }

    public virtual DbSet<Provider> Providers { get; set; }

    public virtual DbSet<ProviderOrder> ProviderOrders { get; set; }

    public virtual DbSet<ProviderOrderItem> ProviderOrderItems { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<StaffPosition> StaffPositions { get; set; }

    public virtual DbSet<Stock> Stocks { get; set; }

    public virtual DbSet<Treatment> Treatments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql("server=localhost;database=vetpet;user=root", Microsoft.EntityFrameworkCore.ServerVersion.Parse("9.6.0-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("appointments");

            entity.HasIndex(e => e.ClientId, "client_id");

            entity.HasIndex(e => e.PetId, "pet_id");

            entity.HasIndex(e => e.ServiceId, "service_id");

            entity.HasIndex(e => e.StaffId, "staff_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.Date)
                .HasColumnType("datetime")
                .HasColumnName("date");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.PetId).HasColumnName("pet_id");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'заплановано'")
                .HasColumnType("enum('заплановано','виконано','скасовано')")
                .HasColumnName("status");

            entity.HasOne(d => d.Client).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appointments_ibfk_1");

            entity.HasOne(d => d.Pet).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appointments_ibfk_2");

            entity.HasOne(d => d.Service).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("appointments_ibfk_4");

            entity.HasOne(d => d.Staff).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appointments_ibfk_3");
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("bills");

            entity.HasIndex(e => e.AppointmentId, "appointment_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("date");
            entity.Property(e => e.Paid)
                .HasDefaultValueSql("'без оплати'")
                .HasColumnType("enum('оплачено','без оплати')")
                .HasColumnName("paid");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(10, 2)
                .HasColumnName("total_amount");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Bills)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("bills_ibfk_1");
        });

        modelBuilder.Entity<BillItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("bill_items");

            entity.HasIndex(e => e.BillId, "bill_id");

            entity.HasIndex(e => e.MedicineId, "medicine_id");

            entity.HasIndex(e => e.ServiceId, "service_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BillId).HasColumnName("bill_id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.MedicineId).HasColumnName("medicine_id");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");

            entity.HasOne(d => d.Bill).WithMany(p => p.BillItems)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("bill_items_ibfk_1");

            entity.HasOne(d => d.Medicine).WithMany(p => p.BillItems)
                .HasForeignKey(d => d.MedicineId)
                .HasConstraintName("bill_items_ibfk_3");

            entity.HasOne(d => d.Service).WithMany(p => p.BillItems)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("bill_items_ibfk_2");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("clients");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasColumnName("last_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("medicines");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
        });

        modelBuilder.Entity<Pet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("pets");

            entity.HasIndex(e => e.ClientId, "client_id");

            entity.HasIndex(e => e.PetTypeId, "pet_type_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.Gender)
                .HasColumnType("enum('male','female')")
                .HasColumnName("gender");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.PetTypeId).HasColumnName("pet_type_id");

            entity.HasOne(d => d.Client).WithMany(p => p.Pets)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pets_ibfk_1");

            entity.HasOne(d => d.PetType).WithMany(p => p.Pets)
                .HasForeignKey(d => d.PetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pets_ibfk_2");
        });

        modelBuilder.Entity<PetType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("pet_type");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Breed)
                .HasMaxLength(50)
                .HasColumnName("breed");
            entity.Property(e => e.Species)
                .HasMaxLength(50)
                .HasColumnName("species");
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("providers");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .HasColumnName("address");
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(100)
                .HasColumnName("contact_person");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<ProviderOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("provider_orders");

            entity.HasIndex(e => e.ProviderId, "provider_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Date)
                .HasColumnType("datetime")
                .HasColumnName("date");
            entity.Property(e => e.ProviderId).HasColumnName("provider_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'в дорозі'")
                .HasColumnType("enum('доставлено','в дорозі','скасовано')")
                .HasColumnName("status");
            entity.Property(e => e.TotalCost)
                .HasPrecision(10, 2)
                .HasColumnName("total_cost");

            entity.HasOne(d => d.Provider).WithMany(p => p.ProviderOrders)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("provider_orders_ibfk_1");
        });

        modelBuilder.Entity<ProviderOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("provider_order_items");

            entity.HasIndex(e => e.MedicineId, "medicine_id");

            entity.HasIndex(e => e.OrderId, "order_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MedicineId).HasColumnName("medicine_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Medicine).WithMany(p => p.ProviderOrderItems)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("provider_order_items_ibfk_2");

            entity.HasOne(d => d.Order).WithMany(p => p.ProviderOrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("provider_order_items_ibfk_1");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("services");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("staff");

            entity.HasIndex(e => e.StaffPositionId, "staff_position_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EndTime)
                .HasDefaultValueSql("'17:00:00'")
                .HasColumnType("time")
                .HasColumnName("end_time");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasColumnName("last_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.StaffPositionId).HasColumnName("staff_position_id");
            entity.Property(e => e.StartTime)
                .HasDefaultValueSql("'09:00:00'")
                .HasColumnType("time")
                .HasColumnName("start_time");
            entity.Property(e => e.WorkDays)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Пн-Пт'")
                .HasColumnName("work_days");

            entity.HasOne(d => d.StaffPosition).WithMany(p => p.Staff)
                .HasForeignKey(d => d.StaffPositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("staff_ibfk_1");
        });

        modelBuilder.Entity<StaffPosition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("staff_position");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Position)
                .HasMaxLength(50)
                .HasColumnName("position");
            entity.Property(e => e.Salary)
                .HasPrecision(10, 2)
                .HasColumnName("salary");
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("stock");

            entity.HasIndex(e => e.MedicineId, "medicine_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MedicineId).HasColumnName("medicine_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Medicine).WithMany(p => p.Stocks)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_ibfk_1");
        });

        modelBuilder.Entity<Treatment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("treatments");

            entity.HasIndex(e => e.AppointmentId, "appointment_id");

            entity.HasIndex(e => e.MedicineId, "medicine_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.MedicineId).HasColumnName("medicine_id");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Treatments)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("treatments_ibfk_1");

            entity.HasOne(d => d.Medicine).WithMany(p => p.Treatments)
                .HasForeignKey(d => d.MedicineId)
                .HasConstraintName("treatments_ibfk_2");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Phone, "phone").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasColumnType("enum('admin','vet')")
                .HasColumnName("role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
