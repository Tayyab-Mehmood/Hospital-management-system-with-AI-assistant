using System;

namespace LifePulse.Shared;

public class DoctorDto
{
    public int DoctorId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Security Properties
    public string Username { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public bool IsFirstLogin { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Advanced HR & Profile Settings
    public string ProfilePictureUrl { get; set; } = "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=256&h=256&q=80";
    public string Gender { get; set; } = "Male";
    public DateTime? DateOfBirth { get; set; } = DateTime.Today.AddYears(-35);
    public string AboutMe { get; set; } = string.Empty;
    public string PhysicalAddress { get; set; } = string.Empty;
    public string WorkType { get; set; } = "Full-Time";
    public DateTime? DateOfEmployment { get; set; } = DateTime.Today;
    public decimal Salary { get; set; } = 5000.00m;
    public string ScheduleDays { get; set; } = "Monday, Tuesday, Wednesday, Thursday, Friday";
    public string WorkStartTime { get; set; } = "09:00 AM";
    public string WorkEndTime { get; set; } = "05:00 PM";

    // Patient-facing consultation fee
    public decimal ConsultationFee { get; set; } = 150.00m;
}