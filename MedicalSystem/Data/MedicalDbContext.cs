using CustomORM.Core;
using MedicalSystem.Entities;

namespace MedicalSystem
{
    /// DbContext koji koristi naš CustomORM (ne Entity Framework)
    /// Nasljeđuje CustomORM.Core.DbContext
    public class MedicalDbContext : CustomDbContext
    {

        // DbSet za svaki entitet - predstavlja tablicu u bazi
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Medication> Medications { get; set; } = null!;
        public DbSet<MedicalHistory> MedicalHistories { get; set; } = null!;
        public DbSet<PrescribedMedication> PrescribedMedications { get; set; } = null!;
        public DbSet<SpecialistExamination> SpecialistExaminations { get; set; } = null!;

        /// Konfiguracija konekcije
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=medical_db2;Username=admin;Password=admin123");
        }

        /// Vraća connection string (korisno za MigrationEngine)
        public static string GetConnectionString() =>
            "Host=localhost;Port=5432;Database=medical_db2;Username=admin;Password=admin123";
    }
}