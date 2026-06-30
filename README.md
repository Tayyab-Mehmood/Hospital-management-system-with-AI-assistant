# LifePulse — Hospital Management System

LifePulse is a multi-role hospital management platform built with **Blazor (Interactive Server)** and **ASP.NET Core**. It provides dedicated, role-based portals for **Admins**, **Doctors**, and **Patients**, backed by a REST API, a SQL Server database, and an AI-powered health assistant chatbot.

The system was built as a university project, with a focus on real-world architecture: a shared DTO layer, a clean separation between the API and the frontend, and secrets management via .NET User Secrets instead of hardcoded credentials.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Screenshots](#screenshots)
- [Getting Started](#getting-started)
- [Security Note](#security-note)
- [Authors](#authors)
- [License](#license)

---

## Features

### 🧑‍⚕️ Patient Portal
- Self-registration and secure login
- Book, view, and cancel appointments with available doctors
- View prescriptions issued by doctors
- View billing history and invoices
- Manage personal profile, including a profile photo
- Chat with an AI-powered assistant for general health-related queries

### 👨‍⚕️ Doctor Portal
- View appointments assigned by the admin
- Update appointment status (e.g. pending → completed)
- Add and manage prescriptions for patients

### 🛠️ Admin Portal
- Manage hospital departments
- Register new doctors, and activate/deactivate or edit existing doctor profiles
- Manage registered patients
- Process and track patient checkouts and billing
- View all appointments across the entire system

---

## Tech Stack

| Layer | Technology |
|---|---|
| UI | Blazor (Interactive Server), Bootstrap |
| Backend API | ASP.NET Core Web API (.NET 10) |
| ORM | Entity Framework Core 10 |
| Database | SQL Server (SQL Server Express supported) |
| API Documentation | Swagger / Swashbuckle |
| AI Chatbot | Groq API (`llama-3.3-70b-versatile`) |
| Shared Contracts | Class library (DTOs) shared between API and Blazor |

---

## Project Structure

```
LifePulse/
├── LifePulse.API/          # ASP.NET Core Web API
│   ├── Controllers/        # Auth, Admin, Patient, Chatbot controllers
│   └── Data/                # LifePulseDbContext (EF Core)
├── LifePulse.Blazor/       # Blazor frontend (Admin / Doctor / Patient portals)
│   └── Components/
│       ├── Pages/           # Razor pages per portal (Login, Dashboard, Appointments, etc.)
│       └── Layout/          # Shared layout components
├── LifePulse.Shared/       # Shared DTOs used by both API and Blazor
└── LifePulse.slnx          # Solution file
```

---

## Screenshots

> 📸 Screenshots coming soon — drop your images here (e.g. into a `/screenshots` folder) and they'll render below.

### Landing Page
<!-- ![Landing Page](./screenshots/landing-page.png) -->

### Patient Portal
| Login | Dashboard | Appointments |
|---|---|---|
| <!-- ![Patient Login](./screenshots/patient-login.png) --> | <!-- ![Patient Dashboard](./screenshots/patient-dashboard.png) --> | <!-- ![Patient Appointments](./screenshots/patient-appointments.png) --> |

| Prescriptions | Billing | AI Chatbot |
|---|---|---|
| <!-- ![Prescriptions](./screenshots/patient-prescriptions.png) --> | <!-- ![Billing](./screenshots/patient-billing.png) --> | <!-- ![Chatbot](./screenshots/chatbot.png) --> |

### Doctor Portal
<!-- ![Doctor Portal](./screenshots/doctor-portal.png) -->

### Admin Portal
| Departments | Doctors List | Patients List |
|---|---|---|
| <!-- ![Departments](./screenshots/admin-departments.png) --> | <!-- ![Doctors List](./screenshots/admin-doctors.png) --> | <!-- ![Patients List](./screenshots/admin-patients.png) --> |

| Checkouts / Billing |
|---|
| <!-- ![Checkouts](./screenshots/admin-checkouts.png) --> |

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server or SQL Server Express
- A free [Groq API key](https://console.groq.com/keys) (only needed for the chatbot feature)

### 1. Clone the repository

```bash
git clone https://github.com/Tayyab-Mehmood/Hospital-Management-System.git
cd Hospital-Management-System/LifePulse
```

### 2. Configure secrets

This project does **not** commit real connection strings or API keys. Configure them locally using .NET User Secrets:

```bash
cd LifePulse.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER\SQLEXPRESS;Database=LifePulseDb;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "GroqApiKey" "your-groq-api-key"
```

Alternatively, copy `appsettings.example.json` to `appsettings.json` inside `LifePulse.API/` and fill in your own values — just don't commit it.

### 3. Apply database migrations

```bash
cd LifePulse.API
dotnet ef database update
```

### 4. Run the application

```bash
dotnet run --project LifePulse.API
```

The API will start with Swagger UI available at `/swagger`. Run the Blazor project the same way (or set both `LifePulse.API` and `LifePulse.Blazor` as startup projects in Visual Studio) to use the full app.

---

## Security Note

`appsettings.json` in this repo intentionally excludes real credentials. **Never commit real connection strings or API keys** — use User Secrets locally, and environment variables or a proper secrets manager in production.

---

## Authors

**Tayyab Mehmood**
BS Computer Science, Air University, Islamabad

**Zaib Saadat**
BS Computer Science, Air University, Islamabad

---

## License

This project was built for academic purposes as part of a university course.
