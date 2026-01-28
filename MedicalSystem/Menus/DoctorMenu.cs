using MedicalSystem.Entities;

namespace MedicalSystem.Menus
{
    public static class DoctorMenu
    {
        public static void Show(MedicalDbContext context)
        {
            Console.Clear();
            Console.WriteLine("=== DOCTORS ===\n");

            var doctors = context.Doctors.ToList();

            if (!doctors.Any())
            {
                Console.WriteLine("No doctors found.");
            }
            else
            {
                foreach (var doctor in doctors)
                {
                    Console.WriteLine($"ID: {doctor.Id} | Dr. {doctor.FirstName} {doctor.LastName} | {doctor.Specialization}");
                }
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        public static void Initialize(MedicalDbContext context)
        {
            if (!context.Doctors.Any())
            {
                Console.WriteLine("Initializing doctors (first run)...\n");

                var doctors = new List<Doctor>
                {
                    new Doctor { FirstName = "Ivan", LastName = "Horvat", Specialization = "Cardiology" },
                    new Doctor { FirstName = "Ana", LastName = "Kovač", Specialization = "Neurology" },
                    new Doctor { FirstName = "Marko", LastName = "Babić", Specialization = "Radiology" },
                    new Doctor { FirstName = "Petra", LastName = "Novak", Specialization = "Dermatology" },
                    new Doctor { FirstName = "Luka", LastName = "Jurić", Specialization = "Ophthalmology" }
                };

                context.Doctors.AddRange(doctors);
                context.SaveChanges();

                Console.WriteLine("Doctors initialized successfully!");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }
}