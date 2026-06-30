namespace LifePulse.Shared;

public class AuthResponseDto
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Admin" or "Doctor"
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsFirstLogin { get; set; }
}