using MedicalSystem.Entities;

namespace MedicalSystem.Menus
{
    public static class MedicationMenu
    {
        public static void Show(MedicalDbContext context)
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== MEDICATIONS ===\n");
                Console.WriteLine("1. List all medications");
                Console.WriteLine("2. Add medication");
                Console.WriteLine("3. Update medication");
                Console.WriteLine("4. Delete medication");
                Console.WriteLine("0. Back");
                Console.Write("\nSelect option: ");

                switch (Console.ReadLine())
                {
                    case "1": ListAll(context); break;
                    case "2": Add(context); break;
                    case "3": Update(context); break;
                    case "4": Delete(context); break;
                    case "0": back = true; break;
                }
            }
        }

        private static void ListAll(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== ALL MEDICATIONS ===\n");

            var medications = context.Medications.Where(null, null, "Name", true);

            if (!medications.Any())
            {
                Console.WriteLine("No medications found.");
            }
            else
            {
                foreach (var m in medications)
                {
                    Console.WriteLine($"ID: {m.Id} | {m.Name} | Unit: {m.DosageUnit} | {m.Description ?? "No description"}");
                }
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private static void Add(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== ADD MEDICATION ===\n");

            var medication = new Medication();

            Console.Write("Name: ");
            medication.Name = Console.ReadLine() ?? "";

            Console.Write("Dosage unit (e.g., mg, tablets, ml): ");
            medication.DosageUnit = Console.ReadLine() ?? "";

            Console.Write("Description (optional): ");
            medication.Description = Console.ReadLine();
            if (string.IsNullOrEmpty(medication.Description))
                medication.Description = null;

            context.Medications.Add(medication);
            context.SaveChanges();

            Console.WriteLine("\nMedication added successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Update(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== UPDATE MEDICATION ===\n");

            Console.Write("Enter medication ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id)) return;

            var medication = context.Medications.Find(id);
            if (medication == null)
            {
                Console.WriteLine("Medication not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nCurrent: {medication.Name}");
            Console.WriteLine("(Press Enter to keep current value)\n");

            Console.Write($"Name [{medication.Name}]: ");
            var input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input)) medication.Name = input;

            Console.Write($"Dosage unit [{medication.DosageUnit}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input)) medication.DosageUnit = input;

            Console.Write($"Description [{medication.Description ?? "None"}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input)) medication.Description = input;

            context.Medications.Update(medication);
            context.SaveChanges();

            Console.WriteLine("\nMedication updated successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Delete(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== DELETE MEDICATION ===\n");

            Console.Write("Enter medication ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id)) return;

            var medication = context.Medications.Find(id);
            if (medication == null)
            {
                Console.WriteLine("Medication not found.");
                Console.ReadKey();
                return;
            }

            Console.Write($"Are you sure you want to delete {medication.Name}? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                context.Medications.Remove(medication);
                context.SaveChanges();
                Console.WriteLine("Medication deleted successfully!");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}