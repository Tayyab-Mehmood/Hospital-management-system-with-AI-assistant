namespace LifePulse.Shared;

public class CheckoutDto
{
    public int CheckoutId { get; set; }
    public int PatientId { get; set; }
    public string PatientFullName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public DateTime CheckoutDate { get; set; }
    public string? Notes { get; set; }
}