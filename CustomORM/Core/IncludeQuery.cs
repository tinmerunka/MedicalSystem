using System.Linq.Expressions;
using System.Reflection;

namespace CustomORM.Core
{
    /// Omogućuje Eager Loading - dohvat povezanih entiteta.
    /// Koristi se s Include() metodom za specificiranje koje relacije dohvatiti.
    /// Primjer:
    /// context.Patients
    ///     .Include(p => p.MedicalHistories)
    ///     .Include(p => p.PrescribedMedications)
    public class IncludeQuery<T> where T : class, new()
    {
        private readonly DbSet<T> _dbSet;
        private readonly string _connectionString;
        private readonly ChangeTracker _changeTracker;

        // Lista propertyja koje treba uključiti (eager load)
        private readonly List<PropertyInfo> _includes = new();

        public IncludeQuery(DbSet<T> dbSet, string connectionString, ChangeTracker changeTracker)
        {
            _dbSet = dbSet;
            _connectionString = connectionString;
            _changeTracker = changeTracker;
        }

        /// Dodaje navigacijsko svojstvo za eager loading
        /// Primjer:
        /// .Include(p => p.MedicalHistories)
        public IncludeQuery<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationProperty)
        {
            // Izvuci ime propertyja iz lambda expression-a
            if (navigationProperty.Body is MemberExpression memberExpr)
            {
                var property = typeof(T).GetProperty(memberExpr.Member.Name);
                if (property != null)
                {
                    _includes.Add(property);
                }
            }
            return this;  // Omogućuje chain-anje: .Include().Include()
        }

        /// Dohvaća entitet po ID-u s uključenim povezanim entitetima
        public T? Find(object id)
        {
            // 1. Dohvati glavni entitet
            var entity = _dbSet.Find(id);

            if (entity == null)
                return null;

            // 2. Za svaki Include, dohvati povezane podatke
            LoadRelatedEntities(entity);

            return entity;
        }

        /// Dohvaća sve entitete s uključenim povezanim entitetima
        public List<T> ToList()
        {
            // 1. Dohvati sve glavne entitete
            var entities = _dbSet.ToList();

            // 2. Za svaki entitet, dohvati povezane podatke
            foreach (var entity in entities)
            {
                LoadRelatedEntities(entity);
            }

            return entities;
        }

        /// Dohvaća entitete s WHERE uvjetom i uključenim povezanim entitetima
        public List<T> Where(string? whereClause = null, Dictionary<string, object?>? parameters = null)
        {
            var entities = _dbSet.Where(whereClause, parameters);

            foreach (var entity in entities)
            {
                LoadRelatedEntities(entity);
            }

            return entities;
        }

        /// Dohvaća prvi entitet koji zadovoljava uvjet
        public T? FirstOrDefault(string? whereClause = null, Dictionary<string, object?>? parameters = null)
        {
            return Where(whereClause, parameters).FirstOrDefault();
        }

        /// Učitava povezane entitete za dani entitet
        private void LoadRelatedEntities(T entity)
        {
            foreach (var includeProperty in _includes)
            {
                LoadRelatedProperty(entity, includeProperty);
            }
        }

        /// Učitava jedan povezani property (kolekciju ili pojedinačni entitet)
        private void LoadRelatedProperty(T entity, PropertyInfo property)
        {
            var propertyType = property.PropertyType;

            // Provjeri je li kolekcija (ICollection<TRelated>)
            if (propertyType.IsGenericType)
            {
                var genericTypeDef = propertyType.GetGenericTypeDefinition();

                if (genericTypeDef == typeof(ICollection<>) ||
                    genericTypeDef == typeof(List<>) ||
                    genericTypeDef == typeof(IEnumerable<>) ||
                    genericTypeDef == typeof(IList<>))
                {
                    // Dohvati tip povezanog entiteta (npr. MedicalHistory iz ICollection<MedicalHistory>)
                    var relatedEntityType = propertyType.GetGenericArguments()[0];
                    LoadCollectionProperty(entity, property, relatedEntityType);
                    return;
                }
            }

            // Pojedinačni entitet (npr. Patient, Doctor)
            if (propertyType.IsClass && propertyType != typeof(string))
            {
                LoadSingleProperty(entity, property, propertyType);
            }
        }

        /// Učitava kolekciju povezanih entiteta
        /// Npr. Patient.MedicalHistories
        private void LoadCollectionProperty(T entity, PropertyInfo property, Type relatedEntityType)
        {
            // Pronađi foreign key u povezanom entitetu
            // Npr. MedicalHistory ima PatientId koji pokazuje na Patient
            var foreignKeyProperty = FindForeignKeyProperty(relatedEntityType, typeof(T));

            if (foreignKeyProperty == null)
                return;

            // Dohvati ID glavnog entiteta
            var primaryKey = EntityMapper.GetPrimaryKey(typeof(T));
            if (primaryKey == null)
                return;

            var primaryKeyValue = primaryKey.GetValue(entity);

            // Kreiraj upit: SELECT * FROM MedicalHistories WHERE PatientId = @p0
            var tableName = EntityMapper.GetTableName(relatedEntityType);
            var fkColumnName = EntityMapper.GetColumnName(foreignKeyProperty);

            var sql = $"SELECT * FROM \"{tableName}\" WHERE \"{fkColumnName}\" = @p0;";
            var parameters = new Dictionary<string, object?> { ["@p0"] = primaryKeyValue };

            // Izvrši upit
            var relatedEntities = ExecuteQueryForType(relatedEntityType, sql, parameters);

            // Kreiraj listu odgovarajućeg tipa i popuni je
            var listType = typeof(List<>).MakeGenericType(relatedEntityType);
            var list = Activator.CreateInstance(listType);

            var addMethod = listType.GetMethod("Add");
            foreach (var relatedEntity in relatedEntities)
            {
                addMethod?.Invoke(list, new[] { relatedEntity });
            }

            // Postavi vrijednost na property
            property.SetValue(entity, list);
        }

        /// Učitava pojedinačni povezani entitet
        /// Npr. MedicalHistory.Patient
        private void LoadSingleProperty(T entity, PropertyInfo property, Type relatedEntityType)
        {
            // Pronađi foreign key u GLAVNOM entitetu
            // Npr. MedicalHistory ima PatientId
            var foreignKeyPropertyName = property.Name + "Id";  // Konvencija: Patient -> PatientId
            var foreignKeyProperty = typeof(T).GetProperty(foreignKeyPropertyName);

            if (foreignKeyProperty == null)
                return;

            var foreignKeyValue = foreignKeyProperty.GetValue(entity);

            if (foreignKeyValue == null)
                return;

            // Dohvati povezani entitet po ID-u
            var relatedPrimaryKey = EntityMapper.GetPrimaryKey(relatedEntityType);
            if (relatedPrimaryKey == null)
                return;

            var tableName = EntityMapper.GetTableName(relatedEntityType);
            var pkColumnName = EntityMapper.GetColumnName(relatedPrimaryKey);

            var sql = $"SELECT * FROM \"{tableName}\" WHERE \"{pkColumnName}\" = @p0;";
            var parameters = new Dictionary<string, object?> { ["@p0"] = foreignKeyValue };

            var relatedEntities = ExecuteQueryForType(relatedEntityType, sql, parameters);
            var relatedEntity = relatedEntities.FirstOrDefault();

            property.SetValue(entity, relatedEntity);
        }

        /// Pronalazi foreign key property u povezanom entitetu
        /// Npr. u MedicalHistory traži PatientId za vezu s Patient
        private PropertyInfo? FindForeignKeyProperty(Type relatedEntityType, Type parentEntityType)
        {
            var expectedName = parentEntityType.Name + "Id";  // Konvencija: Patient -> PatientId
            return relatedEntityType.GetProperty(expectedName);
        }

        /// Izvršava SQL upit i vraća listu objekata danog tipa
        private List<object> ExecuteQueryForType(Type entityType, string sql, Dictionary<string, object?> parameters)
        {
            var results = new List<object>();

            using var connection = new Npgsql.NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new Npgsql.NpgsqlCommand(sql, connection);

            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }

            using var reader = command.ExecuteReader();

            var properties = EntityMapper.GetMappedProperties(entityType).ToList();

            while (reader.Read())
            {
                var entity = Activator.CreateInstance(entityType)!;

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
                        // Stupac ne postoji - preskoči
                    }
                }

                results.Add(entity);
            }

            return results;
        }
    }
}