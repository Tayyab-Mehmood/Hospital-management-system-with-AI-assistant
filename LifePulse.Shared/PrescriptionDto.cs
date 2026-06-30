using System;

namespace LifePulse.Shared;

public class PrescriptionDto
{
    public int PrescriptionId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;  // Computed, not stored in DB
    public string MedicineName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
}