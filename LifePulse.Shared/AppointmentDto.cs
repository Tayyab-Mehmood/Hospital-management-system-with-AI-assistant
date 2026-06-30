using System;

namespace LifePulse.Shared;

public class AppointmentDto
{
    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    // DoctorId may be null for legacy or unassigned appointments
    public int? DoctorId { get; set; }

    // Computed values
    public string DoctorName { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public DateTime AppointmentDate { get; set; }

    public string Status { get; set; } = "Scheduled";

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}