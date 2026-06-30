using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifePulse.API.Data;
using LifePulse.Shared;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LifePulse.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly LifePulseDbContext _context;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;

    public ChatbotController(
        LifePulseDbContext context,
        IConfiguration config,
        IHttpClientFactory httpFactory)
    {
        _context = context;
        _config = config;
        _httpFactory = httpFactory;
    }

    // DTO defined right here — no external dependency needed
    public class HistoryMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class AskRequest
    {
        public string Question { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public bool IsPatientLoggedIn { get; set; }
        public List<HistoryMessage> History { get; set; } = new();
    }

    [HttpPost("ask")]
    public async Task<ActionResult<ChatbotResponseDto>> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question cannot be empty.");

        // ── 1. Load DB snapshot ───────────────────────────────────────────
        var activeDoctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
        var allDoctors = await _context.Doctors.ToListAsync();
        var departments = await _context.Departments.ToListAsync();
        var deptMap = departments.ToDictionary(d => d.DepartmentId, d => d.Name);

        foreach (var doc in activeDoctors)
            if (deptMap.TryGetValue(doc.DepartmentId, out var dn)) doc.DepartmentName = dn;
        foreach (var doc in allDoctors)
            if (deptMap.TryGetValue(doc.DepartmentId, out var dn)) doc.DepartmentName = dn;

        var totalPatients = await _context.Patients.CountAsync(p => p.IsActive);
        var scheduledAppts = await _context.Appointments.CountAsync(a => a.Status == "Scheduled");
        var totalAppts = await _context.Appointments.CountAsync();

        // ── 2. Build DB text ──────────────────────────────────────────────
        var sb = new StringBuilder();
        sb.AppendLine("=== LifePulse HMS Database ===");
        sb.AppendLine("Patients (active): " + totalPatients);
        sb.AppendLine("Appointments total: " + totalAppts + " | Scheduled: " + scheduledAppts);
        sb.AppendLine();
        sb.AppendLine("DEPARTMENTS (" + departments.Count + "):");
        foreach (var dept in departments.OrderBy(d => d.Name))
        {
            var cnt = allDoctors.Count(d => d.DepartmentId == dept.DepartmentId && d.IsActive);
            sb.AppendLine("  " + dept.Name + " (DeptID:" + dept.DepartmentId + ") - " + cnt + " doctor(s)");
        }
        sb.AppendLine();
        sb.AppendLine("ACTIVE DOCTORS (" + activeDoctors.Count + "):");
        foreach (var doc in activeDoctors.OrderBy(d => d.LastName))
        {
            sb.AppendLine("  DoctorID=" + doc.DoctorId
                + " | Dr. " + doc.FirstName + " " + doc.LastName
                + " | " + doc.Specialization
                + " | Dept: " + doc.DepartmentName);
            sb.AppendLine("    Days: " + doc.ScheduleDays);
            sb.AppendLine("    Hours: " + doc.WorkStartTime + " to " + doc.WorkEndTime);
            sb.AppendLine("    Fee: $" + doc.ConsultationFee.ToString("0.00"));
        }
        sb.AppendLine("=== End ===");
        var dbSnapshot = sb.ToString();

        // ── 3. Patient context ────────────────────────────────────────────
        var patientLine = request.IsPatientLoggedIn
            ? "PATIENT STATUS: LOGGED IN | PatientID=" + request.PatientId + " | Name=" + request.PatientName
            : "PATIENT STATUS: NOT LOGGED IN";

        var today = DateTime.Now.ToString("dddd dd MMMM yyyy HH:mm");

        // ── 4. System prompt ──────────────────────────────────────────────
        var systemPrompt =
            "You are MediBot, the AI booking assistant for LifePulse Hospital.\n" +
            "Current date/time: " + today + "\n" +
            patientLine + "\n\n" +

            "YOUR TWO JOBS:\n" +
            "1. Answer questions about doctors, departments, schedules, fees.\n" +
            "2. Book appointments for logged-in patients.\n\n" +

            "BOOKING FLOW:\n" +
            "Step 1 - Collect: doctor name, date, time.\n" +
            "Step 2 - Validate against the doctor schedule in the database.\n" +
            "Step 3 - Show summary and ask: Reply YES to confirm.\n" +
            "Step 4 - When user says YES (or yes, confirm, ok, sure, go ahead, book it, proceed): " +
            "output the BOOK_APPOINTMENT token IMMEDIATELY on the first line. No more questions.\n\n" +

            "BOOKING TOKEN - OUTPUT THIS EXACTLY WHEN USER CONFIRMS:\n" +
            "BOOK_APPOINTMENT:{\"doctorId\":NUMBER,\"date\":\"YYYY-MM-DDTHH:MM\",\"notes\":\"reason\"}\n" +
            "Rules: NUMBER = integer DoctorID from database. Date = 2026-06-10T11:00 format.\n" +
            "Put token on the FIRST line. Then write confirmation message after it.\n\n" +

            "EXAMPLE of correct output when user says YES:\n" +
            "BOOK_APPOINTMENT:{\"doctorId\":2,\"date\":\"2026-06-10T11:00\",\"notes\":\"checkup\"}\n" +
            "Your appointment is confirmed! Check it in your Appointments page.\n\n" +

            "SCHEDULE RULES:\n" +
            "- Only book on the doctor available days.\n" +
            "- Only book within doctor working hours.\n" +
            "- If slot is invalid, explain and suggest a valid time.\n\n" +

            "NOT LOGGED IN: Say: Please sign in first using the Sign In button at the top of the page.\n\n" +

            "OFF-TOPIC: Say: I am MediBot, LifePulse hospital assistant. I only answer hospital questions.\n\n" +

            "MEMORY: You have the full conversation history in this request. " +
            "When the user says YES, use the doctor/date/time from earlier messages to build the token.\n\n" +

            dbSnapshot;

        // ── 5. Build messages array with full history ─────────────────────
        var msgList = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var h in request.History)
            msgList.Add(new { role = h.Role, content = h.Content });

        msgList.Add(new { role = "user", content = request.Question.Trim() });

        // ── 6. Call Groq ──────────────────────────────────────────────────
        var apiKey = _config["GroqApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return StatusCode(500, "GroqApiKey not configured.");

        var groqPayload = new
        {
            model = "llama-3.3-70b-versatile",
            messages = msgList,
            temperature = 0.1,
            max_tokens = 700
        };

        var http = _httpFactory.CreateClient("GroqClient");
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        HttpResponseMessage groqResp;
        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(groqPayload),
                Encoding.UTF8,
                "application/json");
            groqResp = await http.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions", body);
        }
        catch (Exception ex)
        {
            return StatusCode(503, "Cannot reach AI: " + ex.Message);
        }

        if (!groqResp.IsSuccessStatusCode)
        {
            var err = await groqResp.Content.ReadAsStringAsync();
            return StatusCode((int)groqResp.StatusCode, "Groq error: " + err);
        }

        string rawAnswer;
        try
        {
            using var jdoc = JsonDocument.Parse(
                await groqResp.Content.ReadAsStringAsync());
            rawAnswer = jdoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
        catch
        {
            return StatusCode(500, "Failed to parse AI response.");
        }

        rawAnswer = rawAnswer
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        // ── 7. Check for booking token ────────────────────────────────────
        var bookMatch = Regex.Match(
            rawAnswer,
            @"BOOK_APPOINTMENT:\{(.+?)\}",
            RegexOptions.Singleline);

        if (!bookMatch.Success)
            return Ok(new ChatbotResponseDto { Answer = rawAnswer });

        // Token found but patient not logged in
        if (!request.IsPatientLoggedIn || request.PatientId <= 0)
        {
            var txt = rawAnswer.Replace(bookMatch.Value, "").Trim();
            return Ok(new ChatbotResponseDto
            {
                Answer = txt + "\n\nPlease sign in to complete your booking.",
                BookingResult = new AppointmentBookingResult
                {
                    Success = false,
                    Message = "You must be signed in to book an appointment."
                }
            });
        }

        // ── 8. Parse token and save appointment ───────────────────────────
        var visibleText = rawAnswer.Replace(bookMatch.Value, "").Trim();
        AppointmentBookingResult result;

        try
        {
            var json = "{" + bookMatch.Groups[1].Value + "}";
            using var jd = JsonDocument.Parse(json);
            var root = jd.RootElement;
            int doctorId = root.GetProperty("doctorId").GetInt32();
            var dateStr = root.GetProperty("date").GetString() ?? "";
            var notes = root.TryGetProperty("notes", out var np)
                            ? np.GetString() ?? "" : "";
            var apptDate = DateTime.Parse(dateStr);

            var doctor = await _context.Doctors.FindAsync(doctorId);
            var patient = await _context.Patients.FindAsync(request.PatientId);

            if (doctor == null || patient == null)
            {
                result = new AppointmentBookingResult
                {
                    Success = false,
                    Message = "Doctor or patient not found."
                };
            }
            else if (apptDate <= DateTime.Now)
            {
                result = new AppointmentBookingResult
                {
                    Success = false,
                    Message = "Appointment must be in the future."
                };
            }
            else
            {
                var days = (doctor.ScheduleDays ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries
                              | StringSplitOptions.TrimEntries)
                    .Select(d => d.ToLowerInvariant())
                    .ToHashSet();
                var reqDay = apptDate.DayOfWeek.ToString().ToLowerInvariant();

                bool hoursOk = true;
                if (DateTime.TryParse(doctor.WorkStartTime, out var st) &&
                    DateTime.TryParse(doctor.WorkEndTime, out var et))
                {
                    var t = apptDate.TimeOfDay;
                    hoursOk = t >= st.TimeOfDay && t <= et.TimeOfDay;
                }

                var conflict = await _context.Appointments.AnyAsync(a =>
                    a.DoctorId == doctorId &&
                    a.Status != "Cancelled" &&
                    a.AppointmentDate == apptDate);

                if (days.Any() && !days.Contains(reqDay))
                {
                    result = new AppointmentBookingResult
                    {
                        Success = false,
                        Message = "Dr. " + doctor.FirstName + " " + doctor.LastName
                            + " is not available on " + apptDate.DayOfWeek
                            + ". Available days: " + doctor.ScheduleDays + "."
                    };
                }
                else if (!hoursOk)
                {
                    result = new AppointmentBookingResult
                    {
                        Success = false,
                        Message = "Dr. " + doctor.FirstName + " " + doctor.LastName
                            + " works " + doctor.WorkStartTime
                            + " to " + doctor.WorkEndTime + " only."
                    };
                }
                else if (conflict)
                {
                    result = new AppointmentBookingResult
                    {
                        Success = false,
                        Message = "That time slot is already taken. Please choose another time."
                    };
                }
                else
                {
                    var appt = new AppointmentDto
                    {
                        PatientId = request.PatientId,
                        PatientName = patient.FirstName + " " + patient.LastName,
                        DoctorId = doctorId,
                        AppointmentDate = apptDate,
                        Notes = string.IsNullOrWhiteSpace(notes)
                                          ? "Booked via MediBot" : notes,
                        Status = "Scheduled",
                        CreatedAt = DateTime.Now
                    };
                    _context.Appointments.Add(appt);
                    await _context.SaveChangesAsync();

                    result = new AppointmentBookingResult
                    {
                        Success = true,
                        Message = "Appointment confirmed with Dr. "
                            + doctor.FirstName + " " + doctor.LastName
                            + " on " + apptDate.ToString("dddd, dd MMM yyyy")
                            + " at " + apptDate.ToString("HH:mm") + ".",
                        AppointmentId = appt.AppointmentId,
                        DoctorName = "Dr. " + doctor.FirstName + " " + doctor.LastName,
                        AppointmentDate = apptDate
                    };
                }
            }
        }
        catch (Exception ex)
        {
            result = new AppointmentBookingResult
            {
                Success = false,
                Message = "Booking error: " + ex.Message
            };
        }

        return Ok(new ChatbotResponseDto
        {
            Answer = string.IsNullOrWhiteSpace(visibleText)
                     ? "Processing your booking..." : visibleText,
            BookingResult = result
        });
    }
}
