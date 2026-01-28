using System.Reflection;
using CustomORM.Attributes;

namespace CustomORM.Core
{
    /// Koristi REFLEKSIJU za čitanje informacija o entitetskim klasama.
    /// Refleksija = sposobnost programa da "pogleda" vlastiti kod tijekom izvršavanja.
    public static class EntityMapper
    {
        /// Vraća ime tablice za danu klasu.
        /// Ako klasa ima [Table("ime")] atribut, koristi to ime.
        /// Inače koristi konvenciju: ime klase + "s" (Patient → Patients)
        public static string GetTableName(Type entityType)
        {
            // Pokušaj pronaći [Table] atribut na klasi
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>();

            // Ako postoji atribut, koristi ime iz atributa
            // Inače dodaj "s" na ime klase (konvencija)
            return tableAttr?.Name ?? entityType.Name + "s";
        }

        /// Vraća ime stupca za dani property.
        /// Ako property ima [Column("ime")] atribut, koristi to ime.
        /// Inače koristi ime propertyja.
        public static string GetColumnName(PropertyInfo property)
        {
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            return columnAttr?.Name ?? property.Name;
        }

        /// Pronalazi primarni ključ klase.
        /// 1. Prvo traži property s [PrimaryKey] atributom
        /// 2. Ako nema, traži property koji se zove "Id" (konvencija)
        public static PropertyInfo? GetPrimaryKey(Type entityType)
        {
            var properties = entityType.GetProperties();

            // 1. Traži [PrimaryKey] atribut
            var pkProperty = properties
                .FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null);

            // 2. Konvencija - property s imenom "Id"
            if (pkProperty == null)
            {
                pkProperty = properties
                    .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
            }

            return pkProperty;
        }

        /// Provjerava je li primarni ključ auto-increment (SERIAL u PostgreSQL).
        public static bool IsAutoIncrement(PropertyInfo property)
        {
            var pkAttr = property.GetCustomAttribute<PrimaryKeyAttribute>();
            // Ako nema atribut, pretpostavi da je auto-increment (konvencija)
            return pkAttr?.AutoIncrement ?? true;
        }

        /// Provjerava je li property UNIQUE.
        public static bool IsUnique(PropertyInfo property)
        {
            return property.GetCustomAttribute<UniqueAttribute>() != null;
        }

        /// Provjerava je li property NULLABLE.
        /// - Reference tipovi (string, klase) su nullable po defaultu
        /// - Value tipovi (int, DateTime) NISU nullable osim ako su int?, DateTime?
        public static bool IsNullable(PropertyInfo property)
        {
            // Provjeri [Column(IsNullable = false)]
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr != null)
                return columnAttr.IsNullable;

            var propertyType = property.PropertyType;

            // Nullable<T> tipovi (int?, DateTime?) su nullable
            if (Nullable.GetUnderlyingType(propertyType) != null)
                return true;

            // Reference tipovi (string, klase) - provjeri nullable context
            if (!propertyType.IsValueType)
                return true;

            // Value tipovi (int, DateTime) nisu nullable po defaultu
            return false;
        }

        /// Vraća default vrijednost za property ako postoji [DefaultValue] atribut.
        public static object? GetDefaultValue(PropertyInfo property)
        {
            var defaultAttr = property.GetCustomAttribute<DefaultValueAttribute>();
            return defaultAttr?.Value;
        }

        /// Vraća duljinu za VARCHAR stupac ako je specificirana.
        public static int GetColumnLength(PropertyInfo property)
        {
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            return columnAttr?.Length ?? -1;
        }

        /// Provjerava je li property FOREIGN KEY.
        public static bool IsForeignKey(PropertyInfo property)
        {
            return property.GetCustomAttribute<ForeignKeyAttribute>() != null;
        }

        /// Vraća informacije o foreign key-u.
        public static ForeignKeyAttribute? GetForeignKey(PropertyInfo property)
        {
            return property.GetCustomAttribute<ForeignKeyAttribute>();
        }

        /// Vraća sve "obične" propertyje koji se mapiraju na stupce.
        /// Isključuje navigacijska svojstva (ICollection, druge klase).
        public static IEnumerable<PropertyInfo> GetMappedProperties(Type entityType)
        {
            return entityType.GetProperties()
                .Where(p => IsMappableProperty(p));
        }

        /// Provjerava je li property "obični" stupac (ne navigacijsko svojstvo).
        private static bool IsMappableProperty(PropertyInfo property)
        {
            var propertyType = property.PropertyType;

            // Isključi kolekcije (ICollection<T>) - to su navigacijska svojstva
            if (propertyType.IsGenericType)
            {
                var genericDef = propertyType.GetGenericTypeDefinition();
                if (genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(List<>) ||
                    genericDef == typeof(IEnumerable<>) ||
                    genericDef == typeof(IList<>))
                {
                    return false;
                }
            }

            // Isključi kompleksne tipove (druge entitete) - navigacijska svojstva
            // Ali dozvoli ugrađene tipove i enume
            if (propertyType.IsClass &&
                propertyType != typeof(string) &&
                propertyType != typeof(byte[]))
            {
                return false;
            }

            return true;
        }

        /// Vraća potpunu definiciju stupca za kreiranje tablice.
        /// Npr: "FirstName TEXT NOT NULL"
        public static string GetColumnDefinition(PropertyInfo property)
        {
            var columnName = GetColumnName(property);
            var length = GetColumnLength(property);
            var pgType = TypeMapper.GetPostgresType(property.PropertyType, length);

            var parts = new List<string> { $"\"{columnName}\"", pgType };

            // PRIMARY KEY
            if (GetPrimaryKey(property.DeclaringType!) == property)
            {
                if (IsAutoIncrement(property))
                {
                    // SERIAL automatski uključuje NOT NULL
                    parts[1] = "SERIAL PRIMARY KEY";
                    return string.Join(" ", parts);
                }
                else
                {
                    parts.Add("PRIMARY KEY");
                }
            }

            // NOT NULL
            if (!IsNullable(property))
            {
                parts.Add("NOT NULL");
            }

            // UNIQUE
            if (IsUnique(property))
            {
                parts.Add("UNIQUE");
            }

            // DEFAULT
            var defaultValue = GetDefaultValue(property);
            if (defaultValue != null)
            {
                var formattedDefault = FormatDefaultValue(defaultValue);
                parts.Add($"DEFAULT {formattedDefault}");
            }

            return string.Join(" ", parts);
        }

        /// Formatira default vrijednost za SQL.
        private static string FormatDefaultValue(object value)
        {
            return value switch
            {
                string s => $"'{s}'",
                bool b => b ? "TRUE" : "FALSE",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                _ => value.ToString() ?? "NULL"
            };
        }
    }
}