using MedicalSystem.Entities;

namespace MedicalSystem.Menus
{
    public static class MedicalHistoryMenu
    {
        public static void Show(MedicalDbContext context)
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== MEDICAL HISTORY ===\n");
                Console.WriteLine("1. List patient's medical history");
                Console.WriteLine("2. Add medical history");
                Console.WriteLine("3. Update medical history");
                Console.WriteLine("4. Delete medical history");
                Console.WriteLine("0. Back");
                Console.Write("\nSelect option: ");

                switch (Console.ReadLine())
                {
                    case "1": ListByPatient(context); break;
                    case "2": Add(context); break;
                    case "3": Update(context); break;
                    case "4": Delete(context); break;
                    case "0": back = true; break;
                }
            }
        }

        private static void ListByPatient(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== PATIENT'S MEDICAL HISTORY ===\n");

            Console.Write("Enter patient ID: ");
            if (!int.TryParse(Console.ReadLine(), out var patientId)) return;

            var patient = context.Patients.Find(patientId);
            if (patient == null)
            {
                Console.WriteLine("Patient not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nMedical history for {patient.FirstName} {patient.LastName}:\n");

            var histories = context.MedicalHistories.Where(
                "\"PatientId\" = @p0",
                new Dictionary<string, object?> { ["@p0"] = patientId },
                "StartDate",
                false
            );

            if (!histories.Any())
            {
                Console.WriteLine("No medical history found.");
            }
            else
            {
                foreach (var h in histories)
                {
                    var endDate = h.EndDate?.ToString("dd.MM.yyyy") ?? "Ongoing";
                    Console.WriteLine($"ID: {h.Id} | {h.DiseaseName} | {h.StartDate:dd.MM.yyyy} - {endDate}");
                }
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private static void Add(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== ADD MEDICAL HISTORY ===\n");

            var history = new MedicalHistory();

            Console.Write("Patient ID: ");
            if (!int.TryParse(Console.ReadLine(), out var patientId)) return;

            if (context.Patients.Find(patientId) == null)
            {
                Console.WriteLine("Patient not found.");
                Console.ReadKey();
                return;
            }
            history.PatientId = patientId;

            Console.Write("Disease name: ");
            history.DiseaseName = Console.ReadLine() ?? "";

            Console.Write("Start date (dd.MM.yyyy): ");
            if (DateTime.TryParse(Console.ReadLine(), out var startDate))
                history.StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);

            Console.Write("End date (dd.MM.yyyy, leave empty if ongoing): ");
            var endInput = Console.ReadLine();
            if (!string.IsNullOrEmpty(endInput) && DateTime.TryParse(endInput, out var endDate))
                history.EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            context.MedicalHistories.Add(history);
            context.SaveChanges();

            Console.WriteLine("\nMedical history added successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Update(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== UPDATE MEDICAL HISTORY ===\n");

            Console.Write("Enter medical history ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id)) return;

            var history = context.MedicalHistories.Find(id);
            if (history == null)
            {
                Console.WriteLine("Record not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nCurrent: {history.DiseaseName}");
            Console.WriteLine("(Press Enter to keep current value)\n");

            Console.Write($"Disease name [{history.DiseaseName}]: ");
            var input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input)) history.DiseaseName = input;

            Console.Write($"End date [{history.EndDate?.ToString("dd.MM.yyyy") ?? "Ongoing"}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input) && DateTime.TryParse(input, out var endDate))
                history.EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            context.MedicalHistories.Update(history);
            context.SaveChanges();

            Console.WriteLine("\nMedical history updated successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Delete(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== DELETE MEDICAL HISTORY ===\n");

            Console.Write("Enter medical history ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id)) return;

            var history = context.MedicalHistories.Find(id);
            if (history == null)
            {
                Console.WriteLine("Record not found.");
                Console.ReadKey();
                return;
            }

            Console.Write("Are you sure you want to delete this record? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                context.MedicalHistories.Remove(history);
                context.SaveChanges();
                Console.WriteLine("Record deleted successfully!");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}