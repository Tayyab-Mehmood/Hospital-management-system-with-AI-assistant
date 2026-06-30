-- ============================================================
-- LifePulse HMS — Database Schema
-- Run this script against SQL Server to create the database
-- and all required tables with sample seed data.
-- ============================================================

-- 1. Create Database
CREATE DATABASE LifePulseDb;
GO

USE LifePulseDb;
GO

-- ============================================================
-- 2. Departments
-- ============================================================
CREATE TABLE Departments (
    DepartmentId INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description VARCHAR(500) NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- ============================================================
-- 3. Doctors
-- ============================================================
CREATE TABLE Doctors (
    DoctorId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    Phone VARCHAR(20) NOT NULL,
    Specialization VARCHAR(100) NOT NULL,
    DepartmentId INT NOT NULL,
    IsActive BIT DEFAULT 1,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    IsFirstLogin BIT DEFAULT 1,
    ProfilePictureUrl VARCHAR(MAX) NULL,
    Gender VARCHAR(20) NULL,
    DateOfBirth DATE NULL,
    AboutMe VARCHAR(MAX) NULL,
    PhysicalAddress VARCHAR(255) NULL,
    WorkType VARCHAR(50) NULL,            -- 'Full-Time' or 'Part-Time'
    DateOfEmployment DATE NULL,
    Salary DECIMAL(18,2) NULL,
    ScheduleDays VARCHAR(255) NULL,       -- e.g. 'Monday, Tuesday, ...'
    WorkStartTime VARCHAR(20) NULL,       -- e.g. '09:00 AM'
    WorkEndTime VARCHAR(20) NULL,         -- e.g. '05:00 PM'
    ConsultationFee DECIMAL(18,2) NOT NULL DEFAULT 150.00,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Doctors_Departments FOREIGN KEY (DepartmentId)
        REFERENCES Departments(DepartmentId) ON DELETE CASCADE
);
GO

-- ============================================================
-- 4. Patients
-- ============================================================
CREATE TABLE Patients (
    PatientId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    Phone VARCHAR(20) NOT NULL,
    Gender VARCHAR(10) NOT NULL,
    DateOfBirth DATE NOT NULL,
    BloodGroup VARCHAR(5) NULL,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(MAX) NULL,
    EmergencyContact VARCHAR(50) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    ProfileImageBase64 NVARCHAR(MAX) NULL,
    ProfileImageMimeType NVARCHAR(100) NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- ============================================================
-- 5. Appointments
-- ============================================================
CREATE TABLE Appointments (
    AppointmentId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NULL,
    PatientName VARCHAR(100) NOT NULL,
    DoctorId INT NOT NULL,
    AppointmentDate DATETIME NOT NULL,
    Status VARCHAR(20) DEFAULT 'Scheduled', -- Scheduled, Completed, Cancelled
    Notes VARCHAR(1000) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Appointments_Doctors FOREIGN KEY (DoctorId)
        REFERENCES Doctors(DoctorId) ON DELETE CASCADE,
    CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientId)
        REFERENCES Patients(PatientId) ON DELETE NO ACTION
);
GO

-- ============================================================
-- 6. Prescriptions
-- ============================================================
CREATE TABLE Prescriptions (
    PrescriptionId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    MedicineName VARCHAR(100) NOT NULL,
    Dosage VARCHAR(50) NOT NULL,        -- e.g. '500mg'
    Frequency VARCHAR(50) NOT NULL,     -- e.g. 'Twice a day'
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Status VARCHAR(20) DEFAULT 'Active', -- Active, Completed, Discontinued
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Prescriptions_Patients FOREIGN KEY (PatientId)
        REFERENCES Patients(PatientId) ON DELETE CASCADE,
    CONSTRAINT FK_Prescriptions_Doctors FOREIGN KEY (DoctorId)
        REFERENCES Doctors(DoctorId)
);
GO

-- ============================================================
-- 7. Checkouts (Billing)
-- ============================================================
CREATE TABLE Checkouts (
    CheckoutId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaymentStatus VARCHAR(20) DEFAULT 'Unpaid', -- Unpaid, Paid
    CheckoutDate DATETIME DEFAULT GETDATE(),
    Notes VARCHAR(500) NULL,
    CONSTRAINT FK_Checkouts_Patients FOREIGN KEY (PatientId)
        REFERENCES Patients(PatientId) ON DELETE CASCADE
);
GO

-- ============================================================
-- 8. System Admins
-- ============================================================
CREATE TABLE SystemAdmins (
    AdminId INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL DEFAULT '',
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- ============================================================
-- 9. Seed Data (sample/demo data for local testing only)
-- NOTE: PasswordHash values below are plain-text placeholders
-- for demo purposes, not real hashes. Do not use in production.
-- ============================================================

INSERT INTO Departments (Name, Description) VALUES
('Cardiology', 'Heart and blood vessel care.'),
('Neurology', 'Brain, spinal cord, and nerve disorders.'),
('Pediatrics', 'Medical care for infants, children, and adolescents.'),
('General Medicine', 'Routine health checkups and non-surgical treatments.');
GO

INSERT INTO Doctors (FirstName, LastName, Email, Phone, Specialization, DepartmentId, IsActive, Username, PasswordHash, IsFirstLogin, ConsultationFee) VALUES
('John', 'Smith', 'dr.johnsmith@lifepulse.com', '+1234567890', 'Senior Cardiologist', 1, 1, 'dr.john', 'DefaultPassword123!', 1, 180.00),
('Sarah', 'Jane', 'dr.sarahjane@lifepulse.com', '+1987654321', 'Pediatric Neurologist', 2, 1, 'dr.sarah', 'DefaultPassword123!', 1, 150.00);
GO

INSERT INTO SystemAdmins (Username, PasswordHash, FullName, Email) VALUES
('admin', 'AdminPass123!', 'Super Admin', 'admin@lifepulse.com');
GO