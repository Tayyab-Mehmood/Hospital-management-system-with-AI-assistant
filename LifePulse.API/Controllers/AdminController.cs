using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifePulse.API.Data;
using LifePulse.Shared;

namespace LifePulse.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly LifePulseDbContext _context;

    public AdminController(LifePulseDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // ADMIN PROFILE
    // ==========================================
    [HttpGet("profile/{adminId}")]
    public async Task<ActionResult<AdminUserDto>> GetAdminProfile(int adminId)
    {
        var admin = await _context.SystemAdmins.FindAsync(adminId);
        if (admin == null) return NotFound();
        // Never return the password hash to the client
        admin.PasswordHash = string.Empty;
        return Ok(admin);
    }

    [HttpPut("profile/{adminId}")]
    public async Task<IActionResult> UpdateAdminProfile(int adminId, [FromBody] AdminProfileUpdateDto dto)
    {
        var admin = await _context.SystemAdmins.FindAsync(adminId);
        if (admin == null) return NotFound();

        // Username uniqueness check (exclude self)
        if (!string.IsNullOrWhiteSpace(dto.Username))
        {
            var taken = await _context.SystemAdmins
                .AnyAsync(a => a.Username.ToLower() == dto.Username.ToLower() && a.AdminId != adminId);
            if (taken)
                return BadRequest("That username is already taken by another admin account.");
            admin.Username = dto.Username;
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            admin.FullName = dto.FullName;

        if (!string.IsNullOrWhiteSpace(dto.Email))
            admin.Email = dto.Email;

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            admin.PasswordHash = dto.NewPassword;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ==========================================
    // CLINICAL DEPARTMENT MANAGEMENT
    // ==========================================
    [HttpGet("departments")]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments() =>
        await _context.Departments.ToListAsync();

    [HttpPost("departments")]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(DepartmentDto departmentDto)
    {
        if (string.IsNullOrWhiteSpace(departmentDto.Name))
            return BadRequest("Department name is required.");

        // Prevent duplicate names
        var exists = await _context.Departments
            .AnyAsync(d => d.Name.ToLower() == departmentDto.Name.ToLower());
        if (exists)
            return BadRequest("A department with this name already exists.");

        _context.Departments.Add(departmentDto);
        await _context.SaveChangesAsync();
        return Ok(departmentDto);
    }

    [HttpPut("departments/{id}")]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] DepartmentDto dto)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept == null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Department name cannot be empty.");

        // Prevent duplicate names (excluding self)
        var taken = await _context.Departments
            .AnyAsync(d => d.Name.ToLower() == dto.Name.ToLower() && d.DepartmentId != id);
        if (taken)
            return BadRequest("Another department with this name already exists.");

        dept.Name = dto.Name;
        dept.Description = dto.Description;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("departments/{id}")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept == null) return NotFound();

        // Check if any doctors are assigned to this department
        var hasDoctors = await _context.Doctors.AnyAsync(d => d.DepartmentId == id);
        if (hasDoctors)
            return BadRequest("Cannot delete this department — it still has doctors assigned to it. Reassign or remove them first.");

        _context.Departments.Remove(dept);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ==========================================
    // DOCTOR ADVANCED MANAGEMENT
    // ==========================================
    [HttpGet("doctors")]
    public async Task<ActionResult<IEnumerable<DoctorDto>>> GetDoctors()
    {
        var doctors = await _context.Doctors.ToListAsync();
        var departments = await _context.Departments.ToDictionaryAsync(d => d.DepartmentId);
        foreach (var doc in doctors)
            if (departments.TryGetValue(doc.DepartmentId, out var dept))
                doc.DepartmentName = dept.Name;
        return doctors;
    }

    [HttpPost("doctors")]
    public async Task<ActionResult<DoctorDto>> RegisterDoctor(DoctorDto doctorDto)
    {
        doctorDto.PasswordHash = "DefaultPassword123!";
        doctorDto.IsFirstLogin = true;
        doctorDto.IsActive = true;

        var checkExisting = await _context.Doctors.AnyAsync(d =>
            d.Username.ToLower() == doctorDto.Username.ToLower());
        if (checkExisting)
            return BadRequest("The selected username has already been allocated to another profile.");

        _context.Doctors.Add(doctorDto);
        await _context.SaveChangesAsync();
        return Ok(doctorDto);
    }

    [HttpPut("doctors/{id}/toggle-status")]
    public async Task<IActionResult> ToggleDoctorStatus(int id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor == null) return NotFound();
        doctor.IsActive = !doctor.IsActive;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("doctors/{id}")]
    public async Task<IActionResult> DeleteDoctor(int id)
    {
        var exists = await _context.Doctors.AnyAsync(d => d.DoctorId == id);
        if (!exists) return NotFound();
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Doctors WHERE DoctorId = {0}", id);
        return NoContent();
    }

    [HttpPut("doctors/{id}/profile")]
    public async Task<IActionResult> UpdateDoctorProfile(int id, DoctorDto dto)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor == null) return NotFound();

        doctor.Email = dto.Email;
        doctor.Phone = dto.Phone;
        doctor.Gender = dto.Gender;
        doctor.DateOfBirth = dto.DateOfBirth;
        doctor.PhysicalAddress = dto.PhysicalAddress;
        doctor.AboutMe = dto.AboutMe;
        doctor.ProfilePictureUrl = dto.ProfilePictureUrl;

        if (!string.IsNullOrWhiteSpace(dto.PasswordHash))
            doctor.PasswordHash = dto.PasswordHash;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ==========================================
    // PATIENT HUB OPERATIONS
    // ==========================================
    [HttpGet("patients")]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetPatients() =>
        await _context.Patients.ToListAsync();

    [HttpDelete("patients/{id}")]
    public async Task<IActionResult> DeletePatient(int id)
    {
        var exists = await _context.Patients.AnyAsync(p => p.PatientId == id);
        if (!exists) return NotFound();
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Prescriptions WHERE PatientId = {0}", id);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Checkouts WHERE PatientId = {0}", id);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Appointments WHERE PatientId = {0}", id);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Patients WHERE PatientId = {0}", id);
        return NoContent();
    }

    // ==========================================
    // CASHIER CHECKOUT AUDITING
    // ==========================================
    [HttpGet("checkouts")]
    public async Task<ActionResult<IEnumerable<CheckoutDto>>> GetCheckouts()
    {
        var checkouts = await _context.Checkouts.ToListAsync();
        var patients = await _context.Patients.ToDictionaryAsync(p => p.PatientId);
        foreach (var c in checkouts)
            if (patients.TryGetValue(c.PatientId, out var pat))
                c.PatientFullName = $"{pat.FirstName} {pat.LastName}";
        return checkouts;
    }

    [HttpPost("checkouts")]
    public async Task<ActionResult<CheckoutDto>> ProcessCheckout(CheckoutDto checkoutDto)
    {
        // Only allow invoicing patients who have at least one appointment
        var hasAppointment = await _context.Appointments
            .AnyAsync(a => a.PatientId == checkoutDto.PatientId);
        if (!hasAppointment)
            return BadRequest("Cannot generate an invoice for a patient with no appointment records.");

        _context.Checkouts.Add(checkoutDto);
        await _context.SaveChangesAsync();
        return Ok(checkoutDto);
    }

    [HttpPut("checkouts/{id}")]
    public async Task<IActionResult> UpdateCheckout(int id, CheckoutDto checkoutDto)
    {
        if (id != checkoutDto.CheckoutId) return BadRequest();
        var exist = await _context.Checkouts.FindAsync(id);
        if (exist == null) return NotFound();
        _context.Entry(exist).CurrentValues.SetValues(checkoutDto);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("checkouts/{id}")]
    public async Task<IActionResult> DeleteCheckout(int id)
    {
        var exists = await _context.Checkouts.AnyAsync(c => c.CheckoutId == id);
        if (!exists) return NotFound();
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Checkouts WHERE CheckoutId = {0}", id);
        return NoContent();
    }

    // ==========================================
    // APPOINTMENTS
    // ==========================================
    [HttpGet("appointments")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointments()
    {
        var appointments = await _context.Appointments.ToListAsync();
        var doctors = await _context.Doctors.ToDictionaryAsync(d => d.DoctorId);
        var departments = await _context.Departments.ToDictionaryAsync(d => d.DepartmentId);

        foreach (var app in appointments)
        {
            if (app.DoctorId.HasValue && doctors.TryGetValue(app.DoctorId.Value, out var d))
            {
                app.DoctorName = $"Dr. {d.FirstName} {d.LastName}";
                if (departments.TryGetValue(d.DepartmentId, out var dept))
                    app.DepartmentName = dept.Name;
            }
        }
        return appointments;
    }

    [HttpGet("doctors/{doctorId}/appointments")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetDoctorAppointments(int doctorId)
    {
        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId)
            .ToListAsync();

        var patients = await _context.Patients.ToDictionaryAsync(p => p.PatientId);
        var doctors = await _context.Doctors.ToDictionaryAsync(d => d.DoctorId);
        var departments = await _context.Departments.ToDictionaryAsync(d => d.DepartmentId);

        foreach (var app in appointments)
        {
            if (patients.TryGetValue(app.PatientId, out var pat))
                app.PatientName = $"{pat.FirstName} {pat.LastName}";

            if (app.DoctorId.HasValue && doctors.TryGetValue(app.DoctorId.Value, out var d))
            {
                app.DoctorName = $"Dr. {d.FirstName} {d.LastName}";
                if (departments.TryGetValue(d.DepartmentId, out var dept))
                    app.DepartmentName = dept.Name;
            }
        }
        return appointments;
    }

    [HttpPut("appointments/{id}/status")]
    public async Task<IActionResult> UpdateAppointmentStatus(int id, [FromBody] string status)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound();
        appointment.Status = status;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ==========================================
    // PRESCRIPTIONS
    // ==========================================
    [HttpGet("patients/{patientId}/prescriptions")]
    public async Task<ActionResult<IEnumerable<PrescriptionDto>>> GetPatientPrescriptions(int patientId) =>
        await _context.Prescriptions
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    [HttpPost("prescriptions")]
    public async Task<ActionResult<PrescriptionDto>> AddPrescription(PrescriptionDto prescriptionDto)
    {
        _context.Prescriptions.Add(prescriptionDto);
        await _context.SaveChangesAsync();
        return Ok(prescriptionDto);
    }

    [HttpPut("prescriptions/{id}/status")]
    public async Task<IActionResult> UpdatePrescriptionStatus(int id, [FromBody] string status)
    {
        var rx = await _context.Prescriptions.FindAsync(id);
        if (rx == null) return NotFound();
        var allowed = new[] { "Active", "Completed", "Discontinued" };
        if (!allowed.Contains(status))
            return BadRequest("Invalid status. Must be Active, Completed, or Discontinued.");
        rx.Status = status;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}