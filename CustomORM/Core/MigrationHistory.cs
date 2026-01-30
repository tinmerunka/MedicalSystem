using Npgsql;
using System.Text.Json;

namespace CustomORM.Core
{
    /// Predstavlja jedan zapis migracije u bazi
    public class MigrationRecord
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public string SnapshotJson { get; set; } = string.Empty;
        public string SqlUp { get; set; } = string.Empty;
        public string SqlDown { get; set; } = string.Empty;
    }

    /// Upravlja tablicom __MigrationHistory u bazi
    /// Sprema sve migracije i omogućuje rollback
    public class MigrationHistory
    {
        private readonly string _connectionString;
        private const string TableName = "__MigrationHistory";

        public MigrationHistory(string connectionString)
        {
            _connectionString = connectionString;
            EnsureTableExists();
        }

        /// Kreira tablicu __MigrationHistory ako ne postoji
        private void EnsureTableExists()
        {
            var sql = $@"
                CREATE TABLE IF NOT EXISTS ""{TableName}"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Version"" INTEGER NOT NULL,
                    ""Name"" VARCHAR(255) NOT NULL,
                    ""AppliedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""SnapshotJson"" TEXT NOT NULL,
                    ""SqlUp"" TEXT NOT NULL,
                    ""SqlDown"" TEXT NOT NULL
                );";

            ExecuteNonQuery(sql);
        }

        /// Dodaje novu migraciju u history
        public void AddMigration(int version, string name, SchemaSnapshot snapshot, string sqlUp, string sqlDown)
        {
            var snapshotJson = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false });

            var sql = $@"
                INSERT INTO ""{TableName}"" (""Version"", ""Name"", ""AppliedAt"", ""SnapshotJson"", ""SqlUp"", ""SqlDown"")
                VALUES (@version, @name, @appliedAt, @snapshotJson, @sqlUp, @sqlDown);";

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@version", version);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@appliedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@snapshotJson", snapshotJson);
            command.Parameters.AddWithValue("@sqlUp", sqlUp);
            command.Parameters.AddWithValue("@sqlDown", sqlDown);

            command.ExecuteNonQuery();
        }

        /// Dohvaća sve migracije sortirane po verziji
        public List<MigrationRecord> GetAllMigrations()
        {
            var migrations = new List<MigrationRecord>();

            var sql = $@"SELECT ""Id"", ""Version"", ""Name"", ""AppliedAt"", ""SnapshotJson"", ""SqlUp"", ""SqlDown"" 
                         FROM ""{TableName}"" 
                         ORDER BY ""Version"" ASC;";

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                migrations.Add(new MigrationRecord
                {
                    Id = reader.GetInt32(0),
                    Version = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    AppliedAt = reader.GetDateTime(3),
                    SnapshotJson = reader.GetString(4),
                    SqlUp = reader.GetString(5),
                    SqlDown = reader.GetString(6)
                });
            }

            return migrations;
        }

        /// Dohvaća trenutnu (zadnju) verziju
        public int GetCurrentVersion()
        {
            var sql = $@"SELECT COALESCE(MAX(""Version""), 0) FROM ""{TableName}"";";

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(sql, connection);
            var result = command.ExecuteScalar();

            return Convert.ToInt32(result);
        }

        /// Dohvaća migraciju po verziji
        public MigrationRecord? GetMigration(int version)
        {
            var sql = $@"SELECT ""Id"", ""Version"", ""Name"", ""AppliedAt"", ""SnapshotJson"", ""SqlUp"", ""SqlDown"" 
                         FROM ""{TableName}"" 
                         WHERE ""Version"" = @version;";

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@version", version);

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return new MigrationRecord
                {
                    Id = reader.GetInt32(0),
                    Version = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    AppliedAt = reader.GetDateTime(3),
                    SnapshotJson = reader.GetString(4),
                    SqlUp = reader.GetString(5),
                    SqlDown = reader.GetString(6)
                };
            }

            return null;
        }

        /// Dohvaća snapshot iz zadnje migracije
        public SchemaSnapshot? GetLatestSnapshot()
        {
            var currentVersion = GetCurrentVersion();
            if (currentVersion == 0)
                return null;

            var migration = GetMigration(currentVersion);
            if (migration == null)
                return null;

            return JsonSerializer.Deserialize<SchemaSnapshot>(migration.SnapshotJson);
        }

        /// Briše migraciju (kod rollbacka)
        public void RemoveMigration(int version)
        {
            var sql = $@"DELETE FROM ""{TableName}"" WHERE ""Version"" = @version;";

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@version", version);

            command.ExecuteNonQuery();
        }

        /// Briše sve migracije (reset)
        public void ClearAll()
        {
            var sql = $@"DELETE FROM ""{TableName}"";";
            ExecuteNonQuery(sql);
        }

        private void ExecuteNonQuery(string sql)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}