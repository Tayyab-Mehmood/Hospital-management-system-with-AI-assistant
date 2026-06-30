namespace LifePulse.Shared;

// Response DTO (used by both controller and Blazor frontend)
public class ChatbotResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public AppointmentBookingResult? BookingResult { get; set; }
}

public class AppointmentBookingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime? AppointmentDate { get; set; }
}
