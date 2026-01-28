using System.Reflection;
using Npgsql;

namespace CustomORM.Core
{
    /// Odgovornosti:
    /// 1. Drži connection string
    /// 2. Kreira DbSet<T> za svaki entitet
    /// 3. Upravlja ChangeTracker-om
    /// 4. Izvršava SaveChanges() - INSERT, UPDATE, DELETE
    public abstract class CustomDbContext : IDisposable
    {
        /// Connection string za spajanje na PostgreSQL bazu
        protected string ConnectionString { get; private set; }

        /// Prati sve promjene na entitetima
        public ChangeTracker ChangeTracker { get; }

        /// Konstruktor - prima connection string
        protected CustomDbContext()
        {
            ChangeTracker = new ChangeTracker();

            // Pozovi OnConfiguring da korisnik postavi connection string
            var builder = new DbContextOptionsBuilder();
            OnConfiguring(builder);
            ConnectionString = builder.ConnectionString;

            // Automatski inicijaliziraj sve DbSet<T> propertyje
            InitializeDbSets();
        }


        /// Override ovu metodu za konfiguraciju konekcije
        protected virtual void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Korisnik override-a ovu metodu
        }

        /// Pronalazi sve DbSet<T> propertyje i inicijalizira ih
        /// Koristi refleksiju da automatski postavi Patients, Doctors, itd
        private void InitializeDbSets()
        {
            // Dohvati sve propertyje ovog tipa (npr. MedicalDbContext)
            var properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;

                // Provjeri je li DbSet<T>
                if (propertyType.IsGenericType &&
                    propertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                {
                    // Dohvati T iz DbSet<T>
                    var entityType = propertyType.GetGenericArguments()[0];

                    // Kreiraj DbSet<T> instancu
                    var dbSetInstance = Activator.CreateInstance(
                        propertyType,
                        ConnectionString,
                        ChangeTracker
                    );

                    // Postavi property vrijednost
                    property.SetValue(this, dbSetInstance);
                }
            }
        }

        /// Sprema sve promjene u bazu podataka
        /// Izvršava INSERT za Added, UPDATE za Modified, DELETE za Deleted entitete
        /// <returns>Broj promijenjenih redova</returns>
        public int SaveChanges()
        {
            int affectedRows = 0;

            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            // Pokreni transakciju - sve ili ništa!
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. INSERT - novi entiteti
                foreach (var entry in ChangeTracker.GetEntries(EntityState.Added))
                {
                    affectedRows += ExecuteInsert(entry.Entity, connection, transaction);
                }

                // 2. UPDATE - modificirani entiteti
                foreach (var entry in ChangeTracker.GetEntries(EntityState.Modified))
                {
                    affectedRows += ExecuteUpdate(entry.Entity, connection, transaction);
                }

                // 3. DELETE - obrisani entiteti
                foreach (var entry in ChangeTracker.GetEntries(EntityState.Deleted))
                {
                    affectedRows += ExecuteDelete(entry.Entity, connection, transaction);
                }

                // Sve OK - potvrdi transakciju
                transaction.Commit();

                // Resetiraj stanja na Unchanged
                ChangeTracker.AcceptAllChanges();
            }
            catch
            {
                // Greška - poništi sve promjene
                transaction.Rollback();
                throw;
            }

            return affectedRows;
        }

        /// Izvršava INSERT upit za jedan entitet
        private int ExecuteInsert(object entity, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            var entityType = entity.GetType();

            // Koristi refleksiju za poziv generičke metode BuildInsert<T>
            var method = typeof(QueryBuilder)
                .GetMethod(nameof(QueryBuilder.BuildInsert))!
                .MakeGenericMethod(entityType);

            var result = method.Invoke(null, new[] { entity });
            var (sql, parameters) = ((string, Dictionary<string, object?>))result!;

            using var command = new NpgsqlCommand(sql, connection, transaction);

            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            // RETURNING vraća generirani ID
            var primaryKey = EntityMapper.GetPrimaryKey(entityType);
            if (primaryKey != null && EntityMapper.IsAutoIncrement(primaryKey))
            {
                var generatedId = command.ExecuteScalar();
                if (generatedId != null)
                {
                    // Postavi generirani ID na entitet
                    var convertedId = TypeMapper.ConvertFromDb(generatedId, primaryKey.PropertyType);
                    primaryKey.SetValue(entity, convertedId);
                }
                return 1;
            }
            else
            {
                return command.ExecuteNonQuery();
            }
        }

        /// Izvršava UPDATE upit za jedan entitet
        private int ExecuteUpdate(object entity, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            var entityType = entity.GetType();

            var method = typeof(QueryBuilder)
                .GetMethod(nameof(QueryBuilder.BuildUpdate))!
                .MakeGenericMethod(entityType);

            var result = method.Invoke(null, new[] { entity });
            var (sql, parameters) = ((string, Dictionary<string, object?>))result!;

            using var command = new NpgsqlCommand(sql, connection, transaction);

            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            return command.ExecuteNonQuery();
        }

        /// Izvršava DELETE upit za jedan entitet
        private int ExecuteDelete(object entity, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            var entityType = entity.GetType();

            var method = typeof(QueryBuilder)
                .GetMethods()
                .First(m => m.Name == nameof(QueryBuilder.BuildDelete) && m.IsGenericMethod)
                .MakeGenericMethod(entityType);

            var result = method.Invoke(null, new[] { entity });
            var (sql, parameters) = ((string, Dictionary<string, object?>))result!;

            using var command = new NpgsqlCommand(sql, connection, transaction);

            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            return command.ExecuteNonQuery();
        }

        /// Izvršava proizvoljan SQL upit (za napredne slučajeve)
        public int ExecuteSql(string sql, Dictionary<string, object?>? parameters = null)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            using var command = new NpgsqlCommand(sql, connection);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            return command.ExecuteNonQuery();
        }

        /// Provjerava postoji li tablica u bazi
        public bool TableExists(string tableName)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

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


        /// Builder za konfiguraciju DbContext-a
        /// Omogućuje fluent API sličan Entity Frameworku
        public class DbContextOptionsBuilder
        {
            public string ConnectionString { get; private set; } = string.Empty;

            /// Konfigurira PostgreSQL konekciju.
            /// Slično kao EF-ov UseNpgsql()
            public DbContextOptionsBuilder UseNpgsql(string connectionString)
            {
                ConnectionString = connectionString;
                return this;
            }
        }

        /// Oslobađa resurse
        public virtual void Dispose()
        {
            ChangeTracker.Clear();
        }
    }
}