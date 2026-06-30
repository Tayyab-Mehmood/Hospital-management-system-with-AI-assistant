namespace LifePulse.Shared;

public class PasswordResetDto
{
    public int UserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}