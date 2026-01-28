using MedicalSystem.Entities;

namespace MedicalSystem.Menus
{
    public static class PatientMenu
    {
        public static void Show(MedicalDbContext context)
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== PATIENTS ===\n");
                Console.WriteLine("1. List all patients");
                Console.WriteLine("2. Add patient");
                Console.WriteLine("3. Update patient");
                Console.WriteLine("4. Delete patient");
                Console.WriteLine("5. Search patient");
                Console.WriteLine("0. Back");
                Console.Write("\nSelect option: ");

                switch (Console.ReadLine())
                {
                    case "1": ListAll(context); break;
                    case "2": Add(context); break;
                    case "3": Update(context); break;
                    case "4": Delete(context); break;
                    case "5": Search(context); break;
                    case "0": back = true; break;
                }
            }
        }

        private static void ListAll(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== ALL PATIENTS ===\n");

            var patients = context.Patients.Where(null, null, "LastName", true);

            if (!patients.Any())
            {
                Console.WriteLine("No patients found.");
            }
            else
            {
                foreach (var p in patients)
                {
                    Console.WriteLine($"ID: {p.Id} | {p.FirstName} {p.LastName} | OIB: {p.OIB} | Born: {p.DateOfBirth:dd.MM.yyyy}");
                }
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private static void Add(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== ADD PATIENT ===\n");

            var patient = new Patient();

            Console.Write("First name: ");
            patient.FirstName = Console.ReadLine() ?? "";

            Console.Write("Last name: ");
            patient.LastName = Console.ReadLine() ?? "";

            Console.Write("OIB (11 digits): ");
            patient.OIB = Console.ReadLine() ?? "";

            Console.Write("Date of birth (dd.MM.yyyy): ");
            if (DateTime.TryParse(Console.ReadLine(), out var dob))
                patient.DateOfBirth = DateTime.SpecifyKind(dob, DateTimeKind.Utc);

            Console.Write("Gender (0 = Male, 1 = Female): ");
            if (int.TryParse(Console.ReadLine(), out var gender))
                patient.Gender = (Gender)gender;

            Console.Write("Residence address: ");
            patient.ResidenceAddress = Console.ReadLine() ?? "";

            Console.Write("Permanent address: ");
            patient.PermanentAddress = Console.ReadLine() ?? "";

            try
            {
                context.Patients.Add(patient);
                context.SaveChanges();
                Console.WriteLine("\nPatient added successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Update(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== UPDATE PATIENT ===\n");

            Console.Write("Enter patient ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadKey();
                return;
            }

            var patient = context.Patients.Find(id);
            if (patient == null)
            {
                Console.WriteLine("Patient not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nCurrent: {patient.FirstName} {patient.LastName}");
            Console.WriteLine("(Press Enter to keep current value)\n");

            Console.Write($"First name [{patient.FirstName}]: ");
            var input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input)) patient.FirstName = input;

            Console.Write($"Last name [{patient.LastName}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input)) patient.LastName = input;

            Console.Write($"Residence address [{patient.ResidenceAddress}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input)) patient.ResidenceAddress = input;

            Console.Write($"Permanent address [{patient.PermanentAddress}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input)) patient.PermanentAddress = input;

            try
            {
                context.Patients.Update(patient);
                context.SaveChanges();
                Console.WriteLine("\nPatient updated successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Delete(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== DELETE PATIENT ===\n");

            Console.Write("Enter patient ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadKey();
                return;
            }

            var patient = context.Patients.Find(id);
            if (patient == null)
            {
                Console.WriteLine("Patient not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nAre you sure you want to delete {patient.FirstName} {patient.LastName}? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                context.Patients.Remove(patient);
                context.SaveChanges();
                Console.WriteLine("Patient deleted successfully!");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Search(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== SEARCH PATIENT ===\n");

            Console.Write("Search by (1 = Name, 2 = OIB): ");
            var choice = Console.ReadLine();

            List<Patient> results;

            if (choice == "2")
            {
                Console.Write("Enter OIB: ");
                var oib = Console.ReadLine() ?? "";
                results = context.Patients.Where(
                    "\"OIB\" = @p0",
                    new Dictionary<string, object?> { ["@p0"] = oib }
                );
            }
            else
            {
                Console.Write("Enter name: ");
                var name = Console.ReadLine()?.ToLower() ?? "";
                results = context.Patients.Where(
                    "LOWER(\"FirstName\") LIKE @p0 OR LOWER(\"LastName\") LIKE @p0",
                    new Dictionary<string, object?> { ["@p0"] = $"%{name}%" }
                );
            }

            Console.WriteLine($"\nFound {results.Count} patient(s):\n");
            foreach (var p in results)
            {
                Console.WriteLine($"ID: {p.Id} | {p.FirstName} {p.LastName} | OIB: {p.OIB}");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}