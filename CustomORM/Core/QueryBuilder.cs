using System.Reflection;
using System.Text;

namespace CustomORM.Core
{
    /// Generira SQL upite za CRUD operacije
    /// Uzima informacije iz EntityMapper-a i gradi odgovarajući SQL string
    public static class QueryBuilder
    {

        /// Generira CREATE TABLE upit za danu klasu
        /// Koristi se kod migracija za kreiranje tablica
        public static string BuildCreateTable(Type entityType)
        {
            var tableName = EntityMapper.GetTableName(entityType);
            var properties = EntityMapper.GetMappedProperties(entityType);

            var columns = new List<string>();

            foreach (var property in properties)
            {
                var columnDef = EntityMapper.GetColumnDefinition(property);
                columns.Add($"    {columnDef}");
            }

            var sql = new StringBuilder();
            sql.AppendLine($"CREATE TABLE IF NOT EXISTS \"{tableName}\" (");
            sql.AppendLine(string.Join(",\n", columns));
            sql.Append(");");

            return sql.ToString();
        }

        /// Generira INSERT upit za dani entitet
         
        /// Koristimo @p0, @p1... kao PARAMETRE - to sprječava SQL injection
        /// RETURNING vraća generirani Id (za auto-increment)
        public static (string Sql, Dictionary<string, object?> Parameters) BuildInsert<T>(T entity) where T : class
        {
            var entityType = typeof(T);
            var tableName = EntityMapper.GetTableName(entityType);
            var properties = EntityMapper.GetMappedProperties(entityType).ToList();
            var primaryKey = EntityMapper.GetPrimaryKey(entityType);

            var columns = new List<string>();
            var paramNames = new List<string>();
            var parameters = new Dictionary<string, object?>();

            int paramIndex = 0;

            foreach (var property in properties)
            {
                // Preskoči auto-increment primarni ključ - baza ga sama generira
                if (property == primaryKey && EntityMapper.IsAutoIncrement(property))
                    continue;

                var columnName = EntityMapper.GetColumnName(property);
                var paramName = $"@p{paramIndex}";
                var value = property.GetValue(entity);

                columns.Add($"\"{columnName}\"");
                paramNames.Add(paramName);
                parameters[paramName] = TypeMapper.ConvertToDb(value);

                paramIndex++;
            }

            var sql = new StringBuilder();
            sql.Append($"INSERT INTO \"{tableName}\" (");
            sql.Append(string.Join(", ", columns));
            sql.Append(") VALUES (");
            sql.Append(string.Join(", ", paramNames));
            sql.Append(")");

            // RETURNING - vrati generirani primarni ključ
            if (primaryKey != null && EntityMapper.IsAutoIncrement(primaryKey))
            {
                var pkColumn = EntityMapper.GetColumnName(primaryKey);
                sql.Append($" RETURNING \"{pkColumn}\"");
            }

            sql.Append(";");

            return (sql.ToString(), parameters);
        }


        /// Generira SELECT upit za dohvat svih redova
         
        public static string BuildSelectAll(Type entityType)
        {
            var tableName = EntityMapper.GetTableName(entityType);
            var properties = EntityMapper.GetMappedProperties(entityType);

            var columns = properties
                .Select(p => $"\"{EntityMapper.GetColumnName(p)}\"");

            return $"SELECT {string.Join(", ", columns)} FROM \"{tableName}\";";
        }

        /// Generira SELECT upit s WHERE uvjetom po primarnom ključu
        public static (string Sql, Dictionary<string, object?> Parameters) BuildSelectById(Type entityType, object id)
        {
            var tableName = EntityMapper.GetTableName(entityType);
            var properties = EntityMapper.GetMappedProperties(entityType);
            var primaryKey = EntityMapper.GetPrimaryKey(entityType);

            if (primaryKey == null)
                throw new InvalidOperationException($"Entity {entityType.Name} has no primary key!");

            var columns = properties
                .Select(p => $"\"{EntityMapper.GetColumnName(p)}\"");

            var pkColumn = EntityMapper.GetColumnName(primaryKey);
            var parameters = new Dictionary<string, object?> { ["@p0"] = TypeMapper.ConvertToDb(id) };

            var sql = $"SELECT {string.Join(", ", columns)} FROM \"{tableName}\" WHERE \"{pkColumn}\" = @p0;";

            return (sql, parameters);
        }

        /// Generira SELECT upit s proizvoljnim WHERE uvjetom
        public static (string Sql, Dictionary<string, object?> Parameters) BuildSelectWhere(
            Type entityType,
            string? whereClause = null,
            Dictionary<string, object?>? whereParams = null,
            string? orderBy = null,
            bool ascending = true)
        {
            var tableName = EntityMapper.GetTableName(entityType);
            var properties = EntityMapper.GetMappedProperties(entityType);

            var columns = properties
                .Select(p => $"\"{EntityMapper.GetColumnName(p)}\"");

            var sql = new StringBuilder();
            sql.Append($"SELECT {string.Join(", ", columns)} FROM \"{tableName}\"");

            var parameters = new Dictionary<string, object?>();

            // WHERE
            if (!string.IsNullOrEmpty(whereClause))
            {
                sql.Append($" WHERE {whereClause}");
                if (whereParams != null)
                {
                    foreach (var param in whereParams)
                        parameters[param.Key] = param.Value;
                }
            }

            // ORDER BY
            if (!string.IsNullOrEmpty(orderBy))
            {
                var direction = ascending ? "ASC" : "DESC";
                sql.Append($" ORDER BY \"{orderBy}\" {direction}");
            }

            sql.Append(";");

            return (sql.ToString(), parameters);
        }


        /// Generira UPDATE upit za dani entitet
        public static (string Sql, Dictionary<string, object?> Parameters) BuildUpdate<T>(T entity) where T : class
        {
            var entityType = typeof(T);
            var tableName = EntityMapper.GetTableName(entityType);
            var properties = EntityMapper.GetMappedProperties(entityType).ToList();
            var primaryKey = EntityMapper.GetPrimaryKey(entityType);

            if (primaryKey == null)
                throw new InvalidOperationException($"Entity {entityType.Name} has no primary key!");

            var setClauses = new List<string>();
            var parameters = new Dictionary<string, object?>();

            int paramIndex = 0;

            foreach (var property in properties)
            {
                // Preskoči primarni ključ u SET dijelu
                if (property == primaryKey)
                    continue;

                var columnName = EntityMapper.GetColumnName(property);
                var paramName = $"@p{paramIndex}";
                var value = property.GetValue(entity);

                setClauses.Add($"\"{columnName}\" = {paramName}");
                parameters[paramName] = TypeMapper.ConvertToDb(value);

                paramIndex++;
            }

            // WHERE po primarnom ključu
            var pkColumn = EntityMapper.GetColumnName(primaryKey);
            var pkValue = primaryKey.GetValue(entity);
            parameters["@pId"] = TypeMapper.ConvertToDb(pkValue);

            var sql = $"UPDATE \"{tableName}\" SET {string.Join(", ", setClauses)} WHERE \"{pkColumn}\" = @pId;";

            return (sql, parameters);
        }


        /// Generira DELETE upit po primarnom ključu
        public static (string Sql, Dictionary<string, object?> Parameters) BuildDelete(Type entityType, object id)
        {
            var tableName = EntityMapper.GetTableName(entityType);
            var primaryKey = EntityMapper.GetPrimaryKey(entityType);

            if (primaryKey == null)
                throw new InvalidOperationException($"Entity {entityType.Name} has no primary key!");

            var pkColumn = EntityMapper.GetColumnName(primaryKey);
            var parameters = new Dictionary<string, object?> { ["@p0"] = TypeMapper.ConvertToDb(id) };

            var sql = $"DELETE FROM \"{tableName}\" WHERE \"{pkColumn}\" = @p0;";

            return (sql, parameters);
        }

        /// Generira DELETE upit za dani entitet (koristi njegov primarni ključ)
        public static (string Sql, Dictionary<string, object?> Parameters) BuildDelete<T>(T entity) where T : class
        {
            var entityType = typeof(T);
            var primaryKey = EntityMapper.GetPrimaryKey(entityType);

            if (primaryKey == null)
                throw new InvalidOperationException($"Entity {entityType.Name} has no primary key!");

            var pkValue = primaryKey.GetValue(entity);
            return BuildDelete(entityType, pkValue!);
        }


        /// Generira DROP TABLE upit
        public static string BuildDropTable(Type entityType)
        {
            var tableName = EntityMapper.GetTableName(entityType);
            return $"DROP TABLE IF EXISTS \"{tableName}\" CASCADE;";
        }

    }
}