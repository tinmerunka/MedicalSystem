using MedicalSystem.Entities;

namespace MedicalSystem.Menus
{
    public static class SpecialistExaminationMenu
    {
        public static void Show(MedicalDbContext context)
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== SPECIALIST EXAMINATIONS ===\n");
                Console.WriteLine("1. List patient's examinations");
                Console.WriteLine("2. Schedule examination");
                Console.WriteLine("3. Update examination");
                Console.WriteLine("4. Cancel examination");
                Console.WriteLine("0. Back");
                Console.Write("\nSelect option: ");

                switch (Console.ReadLine())
                {
                    case "1": ListByPatient(context); break;
                    case "2": Schedule(context); break;
                    case "3": Update(context); break;
                    case "4": Cancel(context); break;
                    case "0": back = true; break;
                }
            }
        }

        private static void ListByPatient(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== PATIENT'S EXAMINATIONS ===\n");

            Console.Write("Enter patient ID: ");
            if (!int.TryParse(Console.ReadLine(), out var patientId)) return;

            var patient = context.Patients.Find(patientId);
            if (patient == null)
            {
                Console.WriteLine("Patient not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nExaminations for {patient.FirstName} {patient.LastName}:\n");

            var examinations = context.SpecialistExaminations.Where(
                "\"PatientId\" = @p0",
                new Dictionary<string, object?> { ["@p0"] = patientId },
                "AppointmentDate",
                false
            );

            if (!examinations.Any())
            {
                Console.WriteLine("No examinations found.");
            }
            else
            {
                foreach (var ex in examinations)
                {
                    var doctor = context.Doctors.Find(ex.DoctorId);
                    var doctorName = doctor != null ? $"Dr. {doctor.LastName}" : "Unknown";
                    Console.WriteLine($"ID: {ex.Id} | {ex.ExaminationType} | {ex.AppointmentDate:dd.MM.yyyy HH:mm} | {doctorName} | {ex.Notes ?? "No notes"}");
                }
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        private static void Schedule(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== SCHEDULE EXAMINATION ===\n");

            var examination = new SpecialistExamination();

            Console.Write("Patient ID: ");
            if (!int.TryParse(Console.ReadLine(), out var patientId)) return;
            if (context.Patients.Find(patientId) == null)
            {
                Console.WriteLine("Patient not found.");
                Console.ReadKey();
                return;
            }
            examination.PatientId = patientId;

            Console.WriteLine("\nExamination types:");
            var types = Enum.GetValues<ExaminationType>();
            for (int i = 0; i < types.Length; i++)
            {
                Console.WriteLine($"  {i} - {types[i]}");
            }

            Console.Write("\nSelect type (number): ");
            if (int.TryParse(Console.ReadLine(), out var typeIndex) && typeIndex >= 0 && typeIndex < types.Length)
                examination.ExaminationType = types[typeIndex];

            Console.WriteLine("\nAvailable doctors:");
            foreach (var d in context.Doctors.ToList())
            {
                Console.WriteLine($"  {d.Id} - Dr. {d.FirstName} {d.LastName} ({d.Specialization})");
            }

            Console.Write("\nDoctor ID: ");
            if (!int.TryParse(Console.ReadLine(), out var doctorId)) return;
            if (context.Doctors.Find(doctorId) == null)
            {
                Console.WriteLine("Doctor not found.");
                Console.ReadKey();
                return;
            }
            examination.DoctorId = doctorId;

            Console.Write("Appointment date and time (dd.MM.yyyy HH:mm): ");
            if (DateTime.TryParse(Console.ReadLine(), out var appointmentDate))
                examination.AppointmentDate = DateTime.SpecifyKind(appointmentDate, DateTimeKind.Utc);

            Console.Write("Notes (optional): ");
            examination.Notes = Console.ReadLine();
            if (string.IsNullOrEmpty(examination.Notes))
                examination.Notes = null;

            context.SpecialistExaminations.Add(examination);
            context.SaveChanges();

            Console.WriteLine("\nExamination scheduled successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Update(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== UPDATE EXAMINATION ===\n");

            Console.Write("Enter examination ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id)) return;

            var examination = context.SpecialistExaminations.Find(id);
            if (examination == null)
            {
                Console.WriteLine("Examination not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("(Press Enter to keep current value)\n");

            Console.Write($"Appointment date [{examination.AppointmentDate:dd.MM.yyyy HH:mm}]: ");
            var input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input) && DateTime.TryParse(input, out var newDate))
                examination.AppointmentDate = DateTime.SpecifyKind(newDate, DateTimeKind.Utc);

            Console.Write($"Notes [{examination.Notes ?? "None"}]: ");
            input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input)) examination.Notes = input;

            context.SpecialistExaminations.Update(examination);
            context.SaveChanges();

            Console.WriteLine("\nExamination updated successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Cancel(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== CANCEL EXAMINATION ===\n");

            Console.Write("Enter examination ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id)) return;

            var examination = context.SpecialistExaminations.Find(id);
            if (examination == null)
            {
                Console.WriteLine("Examination not found.");
                Console.ReadKey();
                return;
            }

            var doctor = context.Doctors.Find(examination.DoctorId);
            var doctorName = doctor != null ? $"Dr. {doctor.LastName}" : "Unknown";

            Console.Write($"Are you sure you want to cancel {examination.ExaminationType} with {doctorName}? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                context.SpecialistExaminations.Remove(examination);
                context.SaveChanges();
                Console.WriteLine("Examination cancelled successfully!");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}