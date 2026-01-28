using CustomORM.Core;
using MedicalSystem.Menus;

namespace MedicalSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting application...\n");

            // 1. Provjeri konekciju i pokreni migracije
            try
            {
                var migrator = new MigrationEngine(MedicalDbContext.GetConnectionString());
                migrator.MigrateAll<MedicalDbContext>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== DATABASE CONNECTION ERROR ===\n");
                Console.WriteLine("Could not connect to the database!");
                Console.WriteLine("Please make sure:");
                Console.WriteLine("  1. Docker is running");
                Console.WriteLine("  2. PostgreSQL container is started:");
                Console.WriteLine("     docker start postgres-orm");
                Console.WriteLine($"\nError details: {ex.Message}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            // 2. Koristi MedicalDbContext
            using var context = new MedicalDbContext();

            // 3. Inicijaliziraj doktore pri prvom pokretanju
            DoctorMenu.Initialize(context);

            // 4. Glavni meni
            bool running = true;
            while (running)
            {
                Console.Clear();
                Console.WriteLine("=== MEDICAL SYSTEM (Custom ORM) ===\n");
                Console.WriteLine("1. Patients");
                Console.WriteLine("2. Medications");
                Console.WriteLine("3. Medical History");
                Console.WriteLine("4. Prescribed Medications");
                Console.WriteLine("5. Specialist Examinations");
                Console.WriteLine("6. View Doctors");
                Console.WriteLine("0. Exit");
                Console.Write("\nSelect option: ");

                switch (Console.ReadLine())
                {
                    case "1": PatientMenu.Show(context); break;
                    case "2": MedicationMenu.Show(context); break;
                    case "3": MedicalHistoryMenu.Show(context); break;
                    case "4": PrescribedMedicationMenu.Show(context); break;
                    case "5": SpecialistExaminationMenu.Show(context); break;
                    case "6": DoctorMenu.Show(context); break;
                    case "0": running = false; break;
                }
            }
        }
    }
}