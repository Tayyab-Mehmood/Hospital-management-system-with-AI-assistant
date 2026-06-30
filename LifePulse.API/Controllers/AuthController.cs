using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifePulse.API.Data;
using LifePulse.Shared;

namespace LifePulse.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LifePulseDbContext _context;

    public AuthController(LifePulseDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // UNIFIED LOGIN (Admin / Doctor / Patient)
    // ==========================================
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request)
    {
        // 1. Check System Admins
        var admin = await _context.SystemAdmins
            .FirstOrDefaultAsync(a => a.Username.ToLower() == request.Username.ToLower());

        if (admin != null)
        {
            if (admin.PasswordHash != request.Password)
                return Ok(new AuthResponseDto { IsSuccess = false, ErrorMessage = "Invalid administrator security credentials." });

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Role = "Admin",
                UserId = admin.AdminId,
                FullName = admin.FullName,
                IsFirstLogin = false
            });
        }

        // 2. Check Doctors
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Username.ToLower() == request.Username.ToLower());

        if (doctor != null)
        {
            if (doctor.PasswordHash != request.Password)
                return Ok(new AuthResponseDto { IsSuccess = false, ErrorMessage = "Invalid physician security credentials." });

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Role = "Doctor",
                UserId = doctor.DoctorId,
                FullName = $"Dr. {doctor.FirstName} {doctor.LastName}",
                IsFirstLogin = doctor.IsFirstLogin
            });
        }

        // 3. Check Patients
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Username.ToLower() == request.Username.ToLower());

        if (patient != null)
        {
            if (patient.PasswordHash != request.Password)
                return Ok(new AuthResponseDto { IsSuccess = false, ErrorMessage = "Invalid patient credentials." });

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Role = "Patient",
                UserId = patient.PatientId,
                FullName = $"{patient.FirstName} {patient.LastName}",
                IsFirstLogin = false
            });
        }

        return Ok(new AuthResponseDto { IsSuccess = false, ErrorMessage = "The username entered does not exist." });
    }

    // ==========================================
    // PATIENT SELF-REGISTRATION
    // ==========================================
    [HttpPost("patient/register")]
    public async Task<ActionResult<AuthResponseDto>> RegisterPatient(PatientDto dto)
    {
        // Validate username uniqueness
        var usernameExists = await _context.Patients
            .AnyAsync(p => p.Username.ToLower() == dto.Username.ToLower());
        if (usernameExists)
            return Ok(new AuthResponseDto { IsSuccess = false, ErrorMessage = "This username is already taken. Please choose another." });

        // Validate email uniqueness
        var emailExists = await _context.Patients
            .AnyAsync(p => p.Email.ToLower() == dto.Email.ToLower());
        if (emailExists)
            return Ok(new AuthResponseDto { IsSuccess = false, ErrorMessage = "An account with this email already exists." });

        dto.IsActive = true;
        _context.Patients.Add(dto);
        await _context.SaveChangesAsync();

        return Ok(new AuthResponseDto
        {
            IsSuccess = true,
            Role = "Patient",
            UserId = dto.PatientId,
            FullName = $"{dto.FirstName} {dto.LastName}",
            IsFirstLogin = false
        });
    }

    // ==========================================
    // DOCTOR FIRST-LOGIN PASSWORD RESET
    // ==========================================
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(PasswordResetDto request)
    {
        var doctor = await _context.Doctors.FindAsync(request.UserId);
        if (doctor == null) return NotFound("Profile not found.");

        doctor.PasswordHash = request.NewPassword;
        doctor.IsFirstLogin = false;
        await _context.SaveChangesAsync();
        return Ok();
    }
}