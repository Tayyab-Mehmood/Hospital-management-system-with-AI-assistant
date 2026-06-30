namespace LifePulse.Shared;

public class PatientDto
{
    public int PatientId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-20);
    public string? BloodGroup { get; set; }
    public DateTime CreatedAt { get; set; }

    // Portal login fields (added to DB via your SQL scripts)
    public string Username { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? EmergencyContact { get; set; }
    public bool IsActive { get; set; } = true;

    // Profile image (base64-encoded; optional)
    public string? ProfileImageBase64 { get; set; }
    public string? ProfileImageMimeType { get; set; }
}