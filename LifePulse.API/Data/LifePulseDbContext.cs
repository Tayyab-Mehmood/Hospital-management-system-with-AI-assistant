using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using LifePulse.Shared;

namespace LifePulse.API.Data;

public class LifePulseDbContext : DbContext
{
    public LifePulseDbContext(DbContextOptions<LifePulseDbContext> options) : base(options)
    {
    }

    public DbSet<DepartmentDto> Departments { get; set; }
    public DbSet<DoctorDto> Doctors { get; set; }
    public DbSet<PatientDto> Patients { get; set; }
    public DbSet<CheckoutDto> Checkouts { get; set; }
    public DbSet<AppointmentDto> Appointments { get; set; }
    public DbSet<PrescriptionDto> Prescriptions { get; set; }
    public DbSet<AdminUserDto> SystemAdmins { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Department Mapping
        modelBuilder.Entity<DepartmentDto>().ToTable("Departments").HasKey(d => d.DepartmentId);
        modelBuilder.Entity<DepartmentDto>().Property(d => d.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // Doctors Mapping
        modelBuilder.Entity<DoctorDto>().ToTable("Doctors").HasKey(d => d.DoctorId);
        modelBuilder.Entity<DoctorDto>().Ignore(d => d.DepartmentName);
        modelBuilder.Entity<DoctorDto>().Property(d => d.IsFirstLogin).HasDefaultValue(true);
        modelBuilder.Entity<DoctorDto>().Property(d => d.IsActive).HasDefaultValue(true);
        modelBuilder.Entity<DoctorDto>().Property(d => d.ConsultationFee).HasDefaultValue(150.00m);
        modelBuilder.Entity<DoctorDto>().Property(d => d.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // Patient Mapping
        modelBuilder.Entity<PatientDto>().ToTable("Patients").HasKey(p => p.PatientId);
        modelBuilder.Entity<PatientDto>().Property(p => p.IsActive).HasDefaultValue(true);
        modelBuilder.Entity<PatientDto>().Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // Checkout Mapping
        modelBuilder.Entity<CheckoutDto>().ToTable("Checkouts").HasKey(c => c.CheckoutId);
        modelBuilder.Entity<CheckoutDto>().Ignore(c => c.PatientFullName);
        modelBuilder.Entity<CheckoutDto>().Property(c => c.CheckoutDate)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // Appointment Mapping
        modelBuilder.Entity<AppointmentDto>().ToTable("Appointments").HasKey(a => a.AppointmentId);
        modelBuilder.Entity<AppointmentDto>().Ignore(a => a.DoctorName);
        modelBuilder.Entity<AppointmentDto>().Ignore(a => a.DepartmentName);
        // NOTE: PatientName is NOT ignored — it is a real NOT NULL column in the DB
        modelBuilder.Entity<AppointmentDto>().Property(a => a.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // Prescription Mapping
        modelBuilder.Entity<PrescriptionDto>().ToTable("Prescriptions").HasKey(p => p.PrescriptionId);
        modelBuilder.Entity<PrescriptionDto>().Ignore(p => p.DoctorName);
        modelBuilder.Entity<PrescriptionDto>().Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        // System Admin Mapping
        modelBuilder.Entity<AdminUserDto>().ToTable("SystemAdmins").HasKey(a => a.AdminId);
        modelBuilder.Entity<AdminUserDto>().Property(a => a.Email).HasMaxLength(255).IsRequired(false).HasDefaultValue("");
    }
}