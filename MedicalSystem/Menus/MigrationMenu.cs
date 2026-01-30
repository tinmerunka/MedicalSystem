using CustomORM.Core;
using MedicalSystem;

namespace MedicalSystem.Menus
{
    public static class MigrationMenu
    {
        public static void Show()
        {
            var migrator = new MigrationEngine(MedicalDbContext.GetConnectionString());

            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== DATABASE MIGRATIONS ===\n");

                // Prikaži trenutnu verziju
                var history = new MigrationHistory(MedicalDbContext.GetConnectionString());
                var currentVersion = history.GetCurrentVersion();
                Console.WriteLine($"Current database version: {currentVersion}\n");

                Console.WriteLine("1. Show migration history");
                Console.WriteLine("2. Show pending changes");
                Console.WriteLine("3. Rollback last migration");
                Console.WriteLine("4. Rollback to specific version");
                Console.WriteLine("5. Reset database (DANGER!)");
                Console.WriteLine("0. Back");
                Console.Write("\nSelect option: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        ShowHistory(migrator);
                        break;
                    case "2":
                        ShowPlan(migrator);
                        break;
                    case "3":
                        RollbackLast(migrator);
                        break;
                    case "4":
                        RollbackToVersion(migrator);
                        break;
                    case "5":
                        ResetDatabase(migrator);
                        break;
                    case "0":
                        back = true;
                        break;
                }
            }
        }

        private static void ShowHistory(MigrationEngine migrator)
        {
            Console.Clear();
            migrator.ShowHistory();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void ShowPlan(MigrationEngine migrator)
        {
            Console.Clear();
            migrator.ShowMigrationPlan<MedicalDbContext>();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void RollbackLast(MigrationEngine migrator)
        {
            Console.Clear();
            Console.Write("Are you sure you want to rollback the last migration? (y/n): ");

            if (Console.ReadLine()?.ToLower() == "y")
            {
                migrator.Rollback<MedicalDbContext>();
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void RollbackToVersion(MigrationEngine migrator)
        {
            Console.Clear();

            // Prikaži povijest
            migrator.ShowHistory();

            Console.Write("Enter target version number: ");
            if (int.TryParse(Console.ReadLine(), out int targetVersion))
            {
                Console.Write($"Are you sure you want to rollback to version {targetVersion}? (y/n): ");

                if (Console.ReadLine()?.ToLower() == "y")
                {
                    migrator.RollbackTo<MedicalDbContext>(targetVersion);
                }
            }
            else
            {
                Console.WriteLine("Invalid version number.");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void ResetDatabase(MigrationEngine migrator)
        {
            Console.Clear();
            Console.WriteLine("!!! WARNING !!!");
            Console.WriteLine("This will DELETE ALL TABLES and DATA!\n");
            Console.Write("Type 'RESET' to confirm: ");

            if (Console.ReadLine() == "RESET")
            {
                migrator.Reset<MedicalDbContext>();
            }
            else
            {
                Console.WriteLine("Reset cancelled.");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}