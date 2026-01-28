using System.Reflection;
using Npgsql;

namespace CustomORM.Core
{
    /// Automatski kreira tablice u bazi na temelju entitetskih klasa
    /// Ovo je pojednostavljena verzija EF migracija
    public class MigrationEngine
    {
        private readonly string _connectionString;

        public MigrationEngine(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// Kreira sve tablice za dani DbContext ako ne postoje
        public void MigrateAll<TContext>() where TContext : CustomDbContext
        {
            var contextType = typeof(TContext);
            var entityTypes = GetEntityTypes(contextType);

            Console.WriteLine("=== MIGRATION ENGINE ===\n");

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            foreach (var entityType in entityTypes)
            {
                var tableName = EntityMapper.GetTableName(entityType);

                if (TableExists(tableName, connection))
                {
                    Console.WriteLine($"[SKIP] Table \"{tableName}\" already exists.");
                }
                else
                {
                    Console.WriteLine($"[CREATE] Creating table \"{tableName}\"...");
                    CreateTable(entityType, connection);
                    Console.WriteLine($"[OK] Table \"{tableName}\" created successfully.");
                }
            }

            Console.WriteLine("\n=== MIGRATION COMPLETE ===\n");
        }

        /// Dohvaća sve tipove entiteta iz DbContext-a
        /// Gleda DbSet<T> propertyje i izvlači T
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

        /// Provjerava postoji li tablica u bazi
        private bool TableExists(string tableName, NpgsqlConnection connection)
        {
            var sql = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = @tableName
                );";

            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@tableName", tableName);

            return (bool)command.ExecuteScalar()!;
        }

        /// Kreira tablicu za dani tip entiteta
        private void CreateTable(Type entityType, NpgsqlConnection connection)
        {
            var sql = QueryBuilder.BuildCreateTable(entityType);

            Console.WriteLine($"    SQL: {sql.Replace("\n", " ").Substring(0, Math.Min(80, sql.Length))}...");

            using var command = new NpgsqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        /// Briše sve tablice za dani DbContext
        public void DropAll<TContext>() where TContext : CustomDbContext
        {
            var contextType = typeof(TContext);
            var entityTypes = GetEntityTypes(contextType);

            Console.WriteLine("=== DROPPING ALL TABLES ===\n");

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            // Obrnuti redoslijed zbog foreign key-eva
            entityTypes.Reverse();

            foreach (var entityType in entityTypes)
            {
                var tableName = EntityMapper.GetTableName(entityType);

                if (TableExists(tableName, connection))
                {
                    Console.WriteLine($"[DROP] Dropping table \"{tableName}\"...");
                    var sql = QueryBuilder.BuildDropTable(entityType);

                    using var command = new NpgsqlCommand(sql, connection);
                    command.ExecuteNonQuery();

                    Console.WriteLine($"[OK] Table \"{tableName}\" dropped.");
                }
            }

            Console.WriteLine("\n=== DROP COMPLETE ===\n");
        }

        /// Prikazuje SQL koji bi se izvršio za kreiranje tablica (bez izvršavanja).
        /// Korisno za debug i pregled
        public void ShowMigrationPlan<TContext>() where TContext : CustomDbContext
        {
            var contextType = typeof(TContext);
            var entityTypes = GetEntityTypes(contextType);

            Console.WriteLine("=== MIGRATION PLAN ===\n");

            foreach (var entityType in entityTypes)
            {
                var tableName = EntityMapper.GetTableName(entityType);
                var sql = QueryBuilder.BuildCreateTable(entityType);

                Console.WriteLine($"-- Table: {tableName}");
                Console.WriteLine(sql);
                Console.WriteLine();
            }
        }

        /// Resetira bazu - briše sve tablice i kreira ih ponovo
        public void Reset<TContext>() where TContext : CustomDbContext
        {
            DropAll<TContext>();
            MigrateAll<TContext>();
        }
    }
}