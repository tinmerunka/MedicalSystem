using MedicalSystem.Entities;

namespace MedicalSystem.Menus
{
    public static class PrescribedMedicationMenu
    {
        public static void Show(MedicalDbContext context)
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== PRESCRIBED MEDICATIONS ===\n");
                Console.WriteLine("1. List patient's prescriptions");
                Console.WriteLine("2. Add prescription");
                Console.WriteLine("3. Update prescription");
                Console.WriteLine("4. Delete prescription");
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
            Console.WriteLine("=== PATIENT'S PRESCRIPTIONS ===\n");

            Console.Write("Enter patient ID: ");
            if (!int.TryParse(Console.ReadLine(), out var patientId)) return;

            var patient = context.Patients.Find(patientId);
            if (patient == null)
            {
                Console.WriteLine("Patient not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nPrescriptions for {patient.FirstName} {patient.LastName}:\n");

            var prescriptions = context.PrescribedMedications.Where(
                "\"PatientId\" = @p0",
                new Dictionary<string, object?> { ["@p0"] = patientId },
                "StartDate",
                false
            );

            if (!prescriptions.Any())
            {
                Console.WriteLine("No prescriptions found.");
            }
            else
            {
                foreach (var pm in prescriptions)
                {
                    var medication = context.Medications.Find(pm.MedicationId);
                    var medName = medication?.Name ?? "Unknown";
                    var endDate = pm.EndDate?.ToString("dd.MM.yyyy") ?? "Ongoing";
                    Console.WriteLine($"ID: {pm.Id} | {medName} | {pm.Dosage} | {pm.Frequency} | {pm.StartDate:dd.MM.yyyy} - {endDate}");
                }
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private static void Add(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== ADD PRESCRIPTION ===\n");

            var prescription = new PrescribedMedication();

            Console.Write("Patient ID: ");
            if (!int.TryParse(Console.ReadLine(), out var patientId)) return;
            if (context.Patients.Find(patientId) == null)
            {
                Console.WriteLine("Patient not found.");
                Console.ReadKey();
                return;
            }
            prescription.PatientId = patientId;

            Console.WriteLine("\nAvailable medications:");
            foreach (var m in context.Medications.ToList())
            {
                Console.WriteLine($"  {m.Id} - {m.Name} ({m.DosageUnit})");
            }

            Console.Write("\nMedication ID: ");
            if (!int.TryParse(Console.ReadLine(), out var medId)) return;
            if (context.Medications.Find(medId) == null)
            {
                Console.WriteLine("Medication not found.");
                Console.ReadKey();
                return;
            }
            prescription.MedicationId = medId;

            Console.Write("Dosage (e.g., '500mg', '2 tablets'): ");
            prescription.Dosage = Console.ReadLine() ?? "";

            Console.Write("Frequency (e.g., '3 times daily', 'every 8 hours'): ");
            prescription.Frequency = Console.ReadLine() ?? "";

            Console.Write("Start date (dd.MM.yyyy): ");
            if (DateTime.TryParse(Console.ReadLine(), out var startDate))
                prescription.StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);

            Console.Write("End date (dd.MM.yyyy, leave empty if ongoing): ");
            var endInput = Console.ReadLine();
            if (!string.IsNullOrEmpty(endInput) && DateTime.TryParse(endInput, out var endDate))
                prescription.EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            context.PrescribedMedications.Add(prescription);
            context.SaveChanges();

            Console.WriteLine("\nPrescription added successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Update(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== UPDATE PRESCRIPTION ===\n");

            Console.Write("Enter prescription ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id)) return;

            var prescription = context.PrescribedMedications.Find(id);
            if (prescription == null)
            {
                Console.WriteLine("Prescription not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("(Press Enter to keep current value)\n");

            Console.Write($"Dosage [{prescription.Dosage}]: ");
            var input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input)) prescription.Dosage = input;

            Console.Write($"Frequency [{prescription.Frequency}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input)) prescription.Frequency = input;

            Console.Write($"End date [{prescription.EndDate?.ToString("dd.MM.yyyy") ?? "Ongoing"}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input) && DateTime.TryParse(input, out var endDate))
                prescription.EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            context.PrescribedMedications.Update(prescription);
            context.SaveChanges();

            Console.WriteLine("\nPrescription updated successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Delete(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== DELETE PRESCRIPTION ===\n");

            Console.Write("Enter prescription ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id)) return;

            var prescription = context.PrescribedMedications.Find(id);
            if (prescription == null)
            {
                Console.WriteLine("Prescription not found.");
                Console.ReadKey();
                return;
            }

            Console.Write("Are you sure you want to delete this prescription? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                context.PrescribedMedications.Remove(prescription);
                context.SaveChanges();
                Console.WriteLine("Prescription deleted successfully!");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}