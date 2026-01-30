using System.Reflection;
using Npgsql;

namespace CustomORM.Core
{
    /// Upravlja migracijama baze podataka
    /// Podržava migrate up i rollback
    public class MigrationEngine
    {
        private readonly string _connectionString;
        private readonly MigrationHistory _history;

        public MigrationEngine(string connectionString)
        {
            _connectionString = connectionString;
            _history = new MigrationHistory(connectionString);
        }

        /// Izvršava migracije - uspoređuje snapshot s trenutnim entitetima
        public void MigrateAll<TContext>() where TContext : CustomDbContext
        {
            var contextType = typeof(TContext);
            var entityTypes = GetEntityTypes(contextType);

            Console.WriteLine("=== MIGRATION ENGINE ===\n");

            // 1. Dohvati trenutnu verziju i snapshot iz baze
            var currentVersion = _history.GetCurrentVersion();
            var oldSnapshot = _history.GetLatestSnapshot();

            Console.WriteLine($"[INFO] Current database version: {currentVersion}");

            // 2. Kreiraj novi snapshot iz trenutnih entiteta
            var newSnapshot = SchemaDiffer.CreateSnapshotFromEntities(entityTypes);

            // 3. Usporedi i dohvati promjene
            var changes = SchemaDiffer.Compare(oldSnapshot, newSnapshot);

            if (!changes.Any())
            {
                Console.WriteLine("[INFO] No changes detected - database is up to date\n");
                return;
            }

            // 4. Generiraj SQL UP i DOWN
            var sqlUp = SchemaDiffer.GenerateAllSqlUp(changes);
            var sqlDown = SchemaDiffer.GenerateAllSqlDown(changes);

            // 5. Prikaži promjene
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

            // 6. Spremi migraciju u history
            var newVersion = currentVersion + 1;
            var migrationName = GenerateMigrationName(changes);

            _history.AddMigration(newVersion, migrationName, newSnapshot, sqlUp, sqlDown);

            Console.WriteLine($"\n[INFO] Migration v{newVersion} '{migrationName}' applied successfully");
            Console.WriteLine($"[INFO] To rollback, use: Rollback<{contextType.Name}>()\n");
        }

        /// Rollback zadnje migracije
        public void Rollback<TContext>() where TContext : CustomDbContext
        {
            var currentVersion = _history.GetCurrentVersion();

            if (currentVersion == 0)
            {
                Console.WriteLine("[INFO] No migrations to rollback.\n");
                return;
            }

            RollbackTo<TContext>(currentVersion - 1);
        }

        /// Rollback do određene verzije
        public void RollbackTo<TContext>(int targetVersion) where TContext : CustomDbContext
        {
            var currentVersion = _history.GetCurrentVersion();

            Console.WriteLine("=== MIGRATION ROLLBACK ===\n");
            Console.WriteLine($"[INFO] Current version: {currentVersion}");
            Console.WriteLine($"[INFO] Target version: {targetVersion}\n");

            if (targetVersion >= currentVersion)
            {
                Console.WriteLine("[INFO] Target version must be less than current version.\n");
                return;
            }

            if (targetVersion < 0)
            {
                Console.WriteLine("[INFO] Target version cannot be negative.\n");
                return;
            }

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            // Rollback od trenutne do target verzije (unatrag)
            for (int version = currentVersion; version > targetVersion; version--)
            {
                var migration = _history.GetMigration(version);

                if (migration == null)
                {
                    Console.WriteLine($"[ERROR] Migration v{version} not found!\n");
                    return;
                }

                Console.WriteLine($"[ROLLBACK] v{version} '{migration.Name}'");

                try
                {
                    // Izvrši SQL DOWN
                    var downStatements = migration.SqlDown.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var sql in downStatements)
                    {
                        if (!string.IsNullOrWhiteSpace(sql))
                        {
                            Console.WriteLine($"  Executing: {sql.Substring(0, Math.Min(60, sql.Length))}...");
                            ExecuteSql(sql, connection);
                        }
                    }

                    // Obriši migraciju iz historya
                    _history.RemoveMigration(version);

                    Console.WriteLine($"  ✓ Rolled back successfully\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Error: {ex.Message}\n");
                    throw;
                }
            }

            Console.WriteLine($"[INFO] Rollback complete. Current version: {targetVersion}\n");
        }

        /// Prikazuje povijest migracija
        public void ShowHistory()
        {
            var migrations = _history.GetAllMigrations();
            var currentVersion = _history.GetCurrentVersion();

            Console.WriteLine("=== MIGRATION HISTORY ===\n");

            if (!migrations.Any())
            {
                Console.WriteLine("No migrations applied yet.\n");
                return;
            }

            Console.WriteLine($"Current version: {currentVersion}\n");

            foreach (var m in migrations.OrderByDescending(x => x.Version))
            {
                var marker = m.Version == currentVersion ? " (current)" : "";
                Console.WriteLine($"  [v{m.Version}] {m.Name}{marker}");
                Console.WriteLine($"        Applied: {m.AppliedAt:dd.MM.yyyy HH:mm:ss}");
            }

            Console.WriteLine();
        }

        /// Prikazuje plan migracije bez izvršavanja
        public void ShowMigrationPlan<TContext>() where TContext : CustomDbContext
        {
            var contextType = typeof(TContext);
            var entityTypes = GetEntityTypes(contextType);

            Console.WriteLine("=== MIGRATION PLAN ===\n");

            var oldSnapshot = _history.GetLatestSnapshot();
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
                Console.WriteLine($"  UP:   {change.Sql}");
                Console.WriteLine($"  DOWN: {SchemaDiffer.GenerateSqlDown(change)}");
                Console.WriteLine();
            }
        }

        /// Resetira sve - briše tablice i history
        public void Reset<TContext>() where TContext : CustomDbContext
        {
            Console.WriteLine("=== RESET DATABASE ===\n");

            var contextType = typeof(TContext);
            var entityTypes = GetEntityTypes(contextType);

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            // Obriši sve tablice (obrnutim redoslijedom)
            var reversedTypes = entityTypes.ToList();
            reversedTypes.Reverse();

            foreach (var entityType in reversedTypes)
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

            // Očisti history
            _history.ClearAll();
            Console.WriteLine("[INFO] Cleared migration history");

            Console.WriteLine("\n[INFO] Reset complete. Run MigrateAll() to recreate.\n");
        }

        /// Generira ime migracije na temelju promjena
        private string GenerateMigrationName(List<SchemaChange> changes)
        {
            if (changes.Count == 0)
                return "EmptyMigration";

            var firstChange = changes.First();

            return firstChange.Type switch
            {
                ChangeType.CreateTable when changes.All(c => c.Type == ChangeType.CreateTable)
                    => "InitialCreate",
                ChangeType.CreateTable
                    => $"Create{firstChange.TableName}",
                ChangeType.AddColumn
                    => $"Add{firstChange.ColumnName}To{firstChange.TableName}",
                ChangeType.DropColumn
                    => $"Remove{firstChange.ColumnName}From{firstChange.TableName}",
                ChangeType.AlterColumn
                    => $"Alter{firstChange.ColumnName}In{firstChange.TableName}",
                ChangeType.DropTable
                    => $"Drop{firstChange.TableName}",
                _ => $"Migration_{DateTime.UtcNow:yyyyMMddHHmmss}"
            };
        }

        /// Vraća opis promjene
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

        /// Dohvaća tipove entiteta iz DbContext-a
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

        /// Izvršava SQL
        private void ExecuteSql(string sql, NpgsqlConnection connection)
        {
            using var command = new NpgsqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}