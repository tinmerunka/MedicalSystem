using CustomORM.Core;
using MedicalSystem.Data.Entities;

namespace MedicalSystem.Data
{
    /// DbContext koji koristi naš CustomORM (ne Entity Framework)
    /// Nasljeđuje CustomORM.Core.DbContext
    public class MedicalDbContext : CustomDbContext
    {
        // Connection string za PostgreSQL
        private static readonly string _connectionString =
            "Host=localhost;Port=5432;Database=medical_db2;Username=admin;Password=admin123";

        // DbSet za svaki entitet - predstavlja tablicu u bazi
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Medication> Medications { get; set; } = null!;
        public DbSet<MedicalHistory> MedicalHistories { get; set; } = null!;
        public DbSet<PrescribedMedication> PrescribedMedications { get; set; } = null!;
        public DbSet<SpecialistExamination> SpecialistExaminations { get; set; } = null!;

        /// Konstruktor - poziva bazni konstruktor s connection stringom.
        public MedicalDbContext() : base(_connectionString)
        {
        }

        /// Vraća connection string (korisno za MigrationEngine)
        public static string GetConnectionString() => _connectionString;
    }
}