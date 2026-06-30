# LifePulse — Hospital Management System

LifePulse is a multi-role hospital management platform built with Blazor and ASP.NET Core. It provides dedicated portals for **Admins**, **Doctors**, and **Patients**, backed by a REST API, SQL Server database, and an AI-powered health assistant chatbot.

## Features

**Patient Portal**
- Registration and login
- Book, view, and cancel appointments
- View prescriptions and billing/invoices
- Manage profile (incl. profile photo)
- AI chatbot for health-related queries

**Doctor Portal**
- View assigned appointments
- Update appointment status
- Add and manage prescriptions

**Admin Portal**
- Manage departments
- Register and manage doctors (activate/deactivate, edit profile)
- Manage patients
- Process and track checkouts/billing
- View all appointments across the system

## Tech Stack

| Layer | Technology |
|---|---|
| UI | Blazor (Interactive Server), Bootstrap |
| Backend API | ASP.NET Core Web API (.NET 10) |
| ORM | Entity Framework Core 10 |
| Database | SQL Server (SQL Server Express supported) |
| API Docs | Swagger / Swashbuckle |
| AI Chatbot | Groq API (`llama-3.3-70b-versatile`) |
| Shared Contracts | Class library (DTOs) shared between API and Blazor |

## Project Structure

```
LifePulse/
├── LifePulse.API/         # ASP.NET Core Web API (controllers, EF Core DbContext)
│   ├── Controllers/        # Auth, Admin, Patient, Chatbot
│   └── Data/                # LifePulseDbContext
├── LifePulse.Blazor/      # Blazor frontend (Admin / Doctor / Patient portals)
│   └── Components/
│       ├── Pages/           # Razor pages per portal
│       └── Layout/          # Shared layout components
├── LifePulse.Shared/      # Shared DTOs used by both API and Blazor
└── LifePulse.slnx         # Solution file
```

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
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER\\SQLEXPRESS;Database=LifePulseDb;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "GroqApiKey" "your-groq-api-key"
```

Alternatively, copy `appsettings.example.json` to `appsettings.json` inside `LifePulse.API/` and fill in your own values (just don't commit it).

### 3. Apply database migrations
```bash
cd LifePulse.API
dotnet ef database update
```

### 4. Run the application
```bash
dotnet run --project LifePulse.API
```
The API will start with Swagger UI available at `/swagger`. Run the Blazor project the same way (or set both as startup projects in Visual Studio) to use the full app.

## Security Note

`appsettings.json` in this repo intentionally excludes real credentials. Never commit real connection strings or API keys — use User Secrets locally and environment variables / a secrets manager in production.

## License

This project was built for academic purposes as part of a university course.

## Author

**Tayyab Mehmood**
BS Computer Science, Air University, Islamabad
