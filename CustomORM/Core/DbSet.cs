using System.Collections;
using System.Linq.Expressions;
using Npgsql;

namespace CustomORM.Core
{
    /// Predstavlja kolekciju entiteta jednog tipa - mapira se na tablicu u bazi.
    /// Ovo je generička klasa - DbSet<Patient> za Patients tablicu, DbSet<Doctor> za Doctors tablicu, itd.
    public class DbSet<T> : IEnumerable<T> where T : class, new()
    {
        // Connection string za spajanje na bazu
        private readonly string _connectionString;

        // Referenca na ChangeTracker koji prati promjene
        private readonly ChangeTracker _changeTracker;

        //za pristup iz IncludeQuery
        internal string ConnectionString => _connectionString;
        internal ChangeTracker ChangeTrackerInstance => _changeTracker;

        /// Konstruktor - prima connection string i change tracker od DbContext-a
        public DbSet(string connectionString, ChangeTracker changeTracker)
        {
            _connectionString = connectionString;
            _changeTracker = changeTracker;
        }

        /// Označava entitet za dodavanje u bazu
        /// Entitet se NE dodaje odmah - dodaje se kad pozoveš SaveChanges()
        public void Add(T entity)
        {
            _changeTracker.TrackAdd(entity);
        }

        /// Označava više entiteta za dodavanje
        public void AddRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                Add(entity);
            }
        }

        /// Dohvaća SVE redove iz tablice
        /// Izvršava: SELECT * FROM "TableName"
        public List<T> ToList()
        {
            var sql = QueryBuilder.BuildSelectAll(typeof(T));
            return ExecuteQuery(sql, new Dictionary<string, object?>());
        }

        /// Dohvaća entitet po primarnom ključu (ID-u)
        public T? Find(object id)
        {
            var (sql, parameters) = QueryBuilder.BuildSelectById(typeof(T), id);
            var results = ExecuteQuery(sql, parameters);
            return results.FirstOrDefault();
        }

        /// Dohvaća entitete koji zadovoljavaju WHERE uvjet
        public List<T> Where(string whereClause, Dictionary<string, object?>? parameters = null)
        {
            var (sql, allParams) = QueryBuilder.BuildSelectWhere(
                typeof(T),
                whereClause,
                parameters
            );
            return ExecuteQuery(sql, allParams);
        }

        /// Dohvaća entitete s WHERE uvjetom i sortiranjem
        public List<T> Where(
            string? whereClause = null,
            Dictionary<string, object?>? parameters = null,
            string? orderBy = null,
            bool ascending = true)
        {
            var (sql, allParams) = QueryBuilder.BuildSelectWhere(
                typeof(T),
                whereClause,
                parameters,
                orderBy,
                ascending
            );
            return ExecuteQuery(sql, allParams);
        }

        /// Započinje eager loading upit
        /// Omogućuje dohvat povezanih entiteta
        public IncludeQuery<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationProperty)
        {
            var includeQuery = new IncludeQuery<T>(this, _connectionString, _changeTracker);
            return includeQuery.Include(navigationProperty);
        }

        /// Dohvaća prvi entitet koji zadovoljava uvjet, ili null
        public T? FirstOrDefault(string? whereClause = null, Dictionary<string, object?>? parameters = null)
        {
            return Where(whereClause, parameters).FirstOrDefault();
        }

        /// Vraća broj redova u tablici (ili broj koji zadovoljava uvjet)
        /// Izvršava: SELECT COUNT(*) FROM "TableName" [WHERE ...];
        public int Count(string? whereClause = null, Dictionary<string, object?>? parameters = null)
        {
            var tableName = EntityMapper.GetTableName(typeof(T));
            var sql = $"SELECT COUNT(*) FROM \"{tableName}\"";

            if (!string.IsNullOrEmpty(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }
            sql += ";";

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(sql, connection);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        /// Provjerava postoji li barem jedan red (opcionalno s uvjetom)
        public bool Any(string? whereClause = null, Dictionary<string, object?>? parameters = null)
        {
            return Count(whereClause, parameters) > 0;
        }


        /// Označava entitet kao modificiran
        /// Promjene se spremaju kad pozoveš SaveChanges()
        public void Update(T entity)
        {
            _changeTracker.TrackModify(entity);
        }

        /// Označava entitet za brisanje
        /// Briše se kad pozoveš SaveChanges()
        public void Remove(T entity)
        {
            _changeTracker.TrackDelete(entity);
        }

        /// Označava više entiteta za brisanje
        public void RemoveRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                Remove(entity);
            }
        }


        /// Izvršava SQL upit i vraća listu entiteta
        /// pretvara retke iz baze u C# objekte
        private List<T> ExecuteQuery(string sql, Dictionary<string, object?> parameters)
        {
            var results = new List<T>();

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(sql, connection);

            // Dodaj parametre u command
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            using var reader = command.ExecuteReader();

            // Dohvati propertyje koji se mapiraju na stupce
            var properties = EntityMapper.GetMappedProperties(typeof(T)).ToList();

            while (reader.Read())
            {
                // Kreiraj novu instancu entiteta
                var entity = new T();

                // Popuni svaki property vrijednošću iz baze
                foreach (var property in properties)
                {
                    var columnName = EntityMapper.GetColumnName(property);

                    try
                    {
                        var ordinal = reader.GetOrdinal(columnName);
                        var dbValue = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
                        var convertedValue = TypeMapper.ConvertFromDb(dbValue, property.PropertyType);
                        property.SetValue(entity, convertedValue);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        // Stupac ne postoji u rezultatu - preskoči
                    }
                }

                results.Add(entity);
            }

            return results;
        }


        /// Omogućuje foreach petlju preko DbSet-a
        /// foreach (var patient in context.Patients) { ... }
        public IEnumerator<T> GetEnumerator()
        {
            return ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}