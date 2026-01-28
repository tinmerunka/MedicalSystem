using System.Reflection;
using Npgsql;

namespace CustomORM.Core
{
    /// Upravlja migracijama baze podataka koristeći snapshot pristup
    /// Sprema stanje sheme u JSON datoteku i uspoređuje promjene
    public class MigrationEngine
    {
        private readonly string _connectionString;
        private readonly string _snapshotPath;

        public MigrationEngine(string connectionString, string? snapshotPath = null)
        {
            _connectionString = connectionString;
            _snapshotPath = snapshotPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations", "snapshot.json");
        }

        /// Izvršava migracije - uspoređuje snapshot s trenutnim entitetima
        public void MigrateAll<TContext>() where TContext : CustomDbContext
        {
            var contextType = typeof(TContext);
            var entityTypes = GetEntityTypes(contextType);

            Console.WriteLine("=== MIGRATION ENGINE ===\n");

            // 1. Učitaj stari snapshot (ako postoji)
            var oldSnapshot = SchemaSnapshot.LoadFromFile(_snapshotPath);
            if (oldSnapshot != null)
            {
                Console.WriteLine($"[INFO] Loaded existing snapshot (version {oldSnapshot.Version}, created {oldSnapshot.CreatedAt:dd.MM.yyyy HH:mm})");
            }
            else
            {
                Console.WriteLine("[INFO] No existing snapshot found - creating new schema");
            }

            // 2. Kreiraj novi snapshot iz trenutnih entiteta
            var newSnapshot = SchemaDiffer.CreateSnapshotFromEntities(entityTypes);
            newSnapshot.Version = (oldSnapshot?.Version ?? 0) + 1;

            // 3. Usporedi i dohvati promjene
            var changes = SchemaDiffer.Compare(oldSnapshot, newSnapshot);

            if (!changes.Any())
            {
                Console.WriteLine("[INFO] No changes detected - database is up to date");
                Console.WriteLine("\n=== MIGRATION COMPLETE ===\n");
                return;
            }

            // 4. Prikaži i izvrši promjene
            Console.WriteLine($"\n[INFO] Found {changes.Count} change(s):\n");

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            foreach (var change in changes)
            {
                Console.WriteLine($"  [{change.Type}] {GetChangeDescription(change)}");

                try
                {
                    ExecuteSql(change.Sql, connection);
                    Console.WriteLine($"    ✓ Success");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ✗ Error: {ex.Message}");
                    throw;
                }
            }

            // 5. Spremi novi snapshot
            newSnapshot.SaveToFile(_snapshotPath);
            Console.WriteLine($"\n[INFO] Snapshot saved to: {_snapshotPath}");

            Console.WriteLine("\n=== MIGRATION COMPLETE ===\n");
        }

        /// Prikazuje plan migracije bez izvršavanja
        public void ShowMigrationPlan<TContext>() where TContext : CustomDbContext
        {
            var contextType = typeof(TContext);
            var entityTypes = GetEntityTypes(contextType);

            Console.WriteLine("=== MIGRATION PLAN ===\n");

            var oldSnapshot = SchemaSnapshot.LoadFromFile(_snapshotPath);
            var newSnapshot = SchemaDiffer.CreateSnapshotFromEntities(entityTypes);

            var changes = SchemaDiffer.Compare(oldSnapshot, newSnapshot);

            if (!changes.Any())
            {
                Console.WriteLine("No changes detected.\n");
                return;
            }

            Console.WriteLine($"Found {changes.Count} change(s):\n");

            foreach (var change in changes)
            {
                Console.WriteLine($"[{change.Type}] {GetChangeDescription(change)}");
                Console.WriteLine($"SQL: {change.Sql}");
                Console.WriteLine();
            }
        }

        /// Resetira bazu i snapshot
        public void Reset<TContext>() where TContext : CustomDbContext
        {
            // Obriši snapshot
            if (File.Exists(_snapshotPath))
            {
                File.Delete(_snapshotPath);
                Console.WriteLine("[INFO] Deleted existing snapshot");
            }

            // Obriši sve tablice
            var contextType = typeof(TContext);
            var entityTypes = GetEntityTypes(contextType);

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            foreach (var entityType in entityTypes.AsEnumerable().Reverse())
            {
                var tableName = EntityMapper.GetTableName(entityType);
                var sql = $"DROP TABLE IF EXISTS \"{tableName}\" CASCADE;";

                try
                {
                    ExecuteSql(sql, connection);
                    Console.WriteLine($"[DROP] Dropped table \"{tableName}\"");
                }
                catch { }
            }

            // Pokreni migracije iznova
            MigrateAll<TContext>();
        }

        /// Vraća opis promjene za prikaz
        private string GetChangeDescription(SchemaChange change)
        {
            return change.Type switch
            {
                ChangeType.CreateTable => $"Create table \"{change.TableName}\"",
                ChangeType.DropTable => $"Drop table \"{change.TableName}\"",
                ChangeType.AddColumn => $"Add column \"{change.ColumnName}\" to \"{change.TableName}\"",
                ChangeType.DropColumn => $"Drop column \"{change.ColumnName}\" from \"{change.TableName}\"",
                ChangeType.AlterColumn => $"Alter column \"{change.ColumnName}\" in \"{change.TableName}\"",
                _ => change.ToString() ?? ""
            };
        }

        /// Dohvaća sve tipove entiteta iz DbContext-a
        private List<Type> GetEntityTypes(Type contextType)
        {
            var entityTypes = new List<Type>();

            var properties = contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;

                if (propertyType.IsGenericType &&
                    propertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                {
                    var entityType = propertyType.GetGenericArguments()[0];
                    entityTypes.Add(entityType);
                }
            }

            return entityTypes;
        }

        /// Izvršava SQL naredbu
        private void ExecuteSql(string sql, NpgsqlConnection connection)
        {
            using var command = new NpgsqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}