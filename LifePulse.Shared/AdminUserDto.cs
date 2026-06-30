using System;

namespace LifePulse.Shared;

public class AdminUserDto
{
    public int AdminId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;  // NEW
}

// Used for the admin profile update API call
public class AdminProfileUpdateDto
{
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? NewPassword { get; set; }
}
