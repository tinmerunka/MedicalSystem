namespace CustomORM.Core
{
    /// Mapira C# tipove podataka na PostgreSQL tipove
    public static class TypeMapper
    {
        // Rječnik koji povezuje C# tipove s PostgreSQL tipovima
        private static readonly Dictionary<Type, string> CSharpToPostgres = new()
        {
            // Brojevi
            { typeof(int), "INTEGER" },
            { typeof(long), "BIGINT" },
            { typeof(short), "SMALLINT" },
            { typeof(decimal), "DECIMAL" },
            { typeof(float), "REAL" },
            { typeof(double), "DOUBLE PRECISION" },
            
            // Tekst
            { typeof(string), "VARCHAR" },
            { typeof(char), "CHAR(1)" },
            
            // Ostalo
            { typeof(bool), "BOOLEAN" },
            { typeof(DateTime), "TIMESTAMP" },
            { typeof(DateTimeOffset), "TIMESTAMPTZ" },  // TIMESTAMP WITH TIMEZONE
            { typeof(Guid), "UUID" },
            { typeof(byte[]), "BYTEA" }  // Binary data
        };

        /// Vraća PostgreSQL tip za dani C# tip.
        public static string GetPostgresType(Type clrType, int length = -1)
        {
            // Nullable tipovi (npr. int?) - izvuci osnovni tip
            // Nullable.GetUnderlyingType vraća int iz int?, ili null ako nije nullable
            var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

            // Enum tipovi spremamo kao INTEGER (0, 1, 2...)
            // Npr. Gender.Male = 0, Gender.Female = 1
            if (underlyingType.IsEnum)
                return "INTEGER";

            // Pronađi tip u rječniku
            if (CSharpToPostgres.TryGetValue(underlyingType, out var pgType))
            {
                // Za VARCHAR, dodaj duljinu ako je specificirana
                if (pgType == "VARCHAR" && length > 0)
                    return $"VARCHAR({length})";

                // Ako duljina nije specificirana, koristi TEXT (neograničeno)
                if (pgType == "VARCHAR" && length == -1)
                    return "TEXT";

                return pgType;
            }

            // Ako tip nije pronađen, koristi TEXT kao fallback
            return "TEXT";
        }


        /// Pretvara vrijednost iz baze podataka u C# tip.
        /// Baza vraća object, a mi trebamo konkretni tip.
        public static object? ConvertFromDb(object? dbValue, Type targetType)
        {
            // NULL vrijednost
            if (dbValue == null || dbValue == DBNull.Value)
                return null;

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Enum - baza vraća int, mi trebamo enum vrijednost
            if (underlyingType.IsEnum)
                return Enum.ToObject(underlyingType, dbValue);

            // Standardna konverzija
            return Convert.ChangeType(dbValue, underlyingType);
        }

        /// Pretvara C# vrijednost za slanje u bazu podataka.
        public static object? ConvertToDb(object? value)
        {
            // NULL vrijednost - baza koristi DBNull.Value
            if (value == null)
                return DBNull.Value;

            // Enum - spremi kao int
            if (value.GetType().IsEnum)
                return (int)value;

            return value;
        }
    }
}