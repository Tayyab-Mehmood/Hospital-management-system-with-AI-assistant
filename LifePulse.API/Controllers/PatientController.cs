using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifePulse.API.Data;
using LifePulse.Shared;

namespace LifePulse.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientController : ControllerBase
{
    private readonly LifePulseDbContext _context;

    public PatientController(LifePulseDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // AVAILABLE DOCTORS
    // ==========================================
    [HttpGet("available-doctors")]
    public async Task<ActionResult<IEnumerable<DoctorDto>>> GetAvailableDoctors()
    {
        var doctors = await _context.Doctors
            .Where(d => d.IsActive)
            .ToListAsync();

        var departments = await _context.Departments.ToDictionaryAsync(d => d.DepartmentId);
        foreach (var doc in doctors)
        {
            if (departments.TryGetValue(doc.DepartmentId, out var dept))
                doc.DepartmentName = dept.Name;
        }
        return Ok(doctors);
    }

    // ==========================================
    // PATIENT PROFILE
    // ==========================================
    [HttpGet("{patientId:int}")]
    public async Task<ActionResult<PatientDto>> GetPatient(int patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null) return NotFound();
        return Ok(patient);
    }

    [HttpPut("{patientId:int}")]
    public async Task<IActionResult> UpdateProfile(int patientId, PatientDto dto)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null) return NotFound();

        patient.FirstName = dto.FirstName;
        patient.LastName = dto.LastName;
        patient.Email = dto.Email;
        patient.Phone = dto.Phone;
        patient.Gender = dto.Gender;
        patient.DateOfBirth = dto.DateOfBirth;
        patient.BloodGroup = dto.BloodGroup;
        patient.EmergencyContact = dto.EmergencyContact;

        if (!string.IsNullOrWhiteSpace(dto.PasswordHash))
            patient.PasswordHash = dto.PasswordHash;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ==========================================
    // APPOINTMENTS
    // ==========================================
    [HttpGet("{patientId:int}/appointments")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetMyAppointments(int patientId)
    {
        // ✅ FIX: only return rows where DoctorId is not null
        var appointments = await _context.Appointments
            .Where(a => a.PatientId == patientId && a.DoctorId != null)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

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
        return Ok(appointments);
    }

    [HttpPost("{patientId:int}/appointments")]
    public async Task<ActionResult<AppointmentDto>> BookAppointment(int patientId, AppointmentDto dto)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null)
            return NotFound("Patient not found.");

        var doctor = await _context.Doctors.FindAsync(dto.DoctorId);
        if (doctor == null)
            return NotFound("Doctor not found.");

        if (dto.AppointmentDate <= DateTime.Now)
        {
            return BadRequest("Appointment date and time must be in the future.");
        }

        // ==========================================
        // WORKING HOURS VALIDATION
        // ==========================================
        if (DateTime.TryParse(doctor.WorkStartTime, out var startTime) &&
            DateTime.TryParse(doctor.WorkEndTime, out var endTime))
        {
            var appointmentTime = dto.AppointmentDate.TimeOfDay;
            var doctorStart = startTime.TimeOfDay;
            var doctorEnd = endTime.TimeOfDay;

            if (appointmentTime < doctorStart || appointmentTime > doctorEnd)
            {
                return BadRequest(
                    $"Dr. {doctor.FirstName} {doctor.LastName} is available only between " +
                    $"{doctor.WorkStartTime} and {doctor.WorkEndTime}.");
            }
        }

        // ==========================================
        // SCHEDULE DAY VALIDATION
        // ==========================================
        var scheduledDays = (doctor.ScheduleDays ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => d.ToLowerInvariant())
            .ToHashSet();

        var requestedDay = dto.AppointmentDate.DayOfWeek.ToString().ToLowerInvariant();

        if (scheduledDays.Any() && !scheduledDays.Contains(requestedDay))
        {
            var readable = string.Join(", ",
                (doctor.ScheduleDays ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

            return BadRequest(
                $"Dr. {doctor.FirstName} {doctor.LastName} is not available on " +
                $"{dto.AppointmentDate.DayOfWeek}. Available days: {readable}.");
        }

        // ==========================================
        // DUPLICATE / CONFLICT CHECK
        // ==========================================
        var conflict = await _context.Appointments
            .AnyAsync(a =>
                a.DoctorId == dto.DoctorId &&
                a.Status != "Cancelled" &&
                a.AppointmentDate == dto.AppointmentDate);

        if (conflict)
        {
            return Conflict(
                $"Dr. {doctor.FirstName} {doctor.LastName} already has an appointment at that time. " +
                "Please choose a different time slot.");
        }

        // ==========================================
        // SAVE APPOINTMENT
        // ==========================================
        dto.PatientId = patientId;
        dto.DoctorId = doctor.DoctorId;
        dto.PatientName = $"{patient.FirstName} {patient.LastName}";
        dto.Status = "Scheduled";
        dto.CreatedAt = DateTime.Now;

        _context.Appointments.Add(dto);
        await _context.SaveChangesAsync();

        return Ok(dto);
    }

    [HttpPut("{patientId:int}/appointments/{appointmentId:int}/cancel")]
    public async Task<IActionResult> CancelAppointment(int patientId, int appointmentId)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.PatientId == patientId);

        if (appointment == null) return NotFound();
        appointment.Status = "Cancelled";
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ==========================================
    // PRESCRIPTIONS
    // ==========================================
    [HttpGet("{patientId:int}/prescriptions")]
    public async Task<ActionResult<IEnumerable<PrescriptionDto>>> GetMyPrescriptions(int patientId)
    {
        var prescriptions = await _context.Prescriptions
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var doctors = await _context.Doctors.ToDictionaryAsync(d => d.DoctorId);
        foreach (var rx in prescriptions)
        {
            if (doctors.TryGetValue(rx.DoctorId, out var d))
                rx.DoctorName = $"Dr. {d.FirstName} {d.LastName}";
        }
        return Ok(prescriptions);
    }

    // ==========================================
    // BILLING / INVOICES (read-only for patient)
    // ==========================================
    [HttpGet("{patientId:int}/invoices")]
    public async Task<ActionResult<IEnumerable<CheckoutDto>>> GetMyInvoices(int patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null) return NotFound();

        var checkouts = await _context.Checkouts
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.CheckoutDate)
            .ToListAsync();

        var fullName = $"{patient.FirstName} {patient.LastName}";
        foreach (var c in checkouts) c.PatientFullName = fullName;

        return Ok(checkouts);
    }
}
