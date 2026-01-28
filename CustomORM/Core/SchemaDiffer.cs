namespace CustomORM.Core
{
    /// Tipovi promjena koje mogu nastati
    public enum ChangeType
    {
        CreateTable,
        DropTable,
        AddColumn,
        DropColumn,
        AlterColumn
    }

    /// Predstavlja jednu promjenu u shemi
    public class SchemaChange
    {
        public ChangeType Type { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string? ColumnName { get; set; }
        public ColumnSnapshot? OldColumn { get; set; }
        public ColumnSnapshot? NewColumn { get; set; }
        public string Sql { get; set; } = string.Empty;
    }

    /// Uspoređuje snapshot s trenutnim entitetima i generira promjene
    public static class SchemaDiffer
    {
        /// <summary>
        /// Kreira snapshot trenutnog stanja entiteta (iz koda)
        public static SchemaSnapshot CreateSnapshotFromEntities(IEnumerable<Type> entityTypes)
        {
            var snapshot = new SchemaSnapshot
            {
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var entityType in entityTypes)
            {
                var tableSnapshot = CreateTableSnapshot(entityType);
                snapshot.Tables.Add(tableSnapshot);
            }

            return snapshot;
        }

        /// Kreira snapshot jedne tablice iz entiteta
        private static TableSnapshot CreateTableSnapshot(Type entityType)
        {
            var tableSnapshot = new TableSnapshot
            {
                TableName = EntityMapper.GetTableName(entityType)
            };

            var properties = EntityMapper.GetMappedProperties(entityType);
            var primaryKey = EntityMapper.GetPrimaryKey(entityType);

            foreach (var property in properties)
            {
                var columnSnapshot = new ColumnSnapshot
                {
                    Name = EntityMapper.GetColumnName(property),
                    Type = TypeMapper.GetPostgresType(property.PropertyType, EntityMapper.GetColumnLength(property)),
                    Nullable = EntityMapper.IsNullable(property),
                    PrimaryKey = property == primaryKey,
                    AutoIncrement = property == primaryKey && EntityMapper.IsAutoIncrement(property),
                    Unique = EntityMapper.IsUnique(property),
                    DefaultValue = EntityMapper.GetDefaultValue(property)?.ToString()
                };

                tableSnapshot.Columns.Add(columnSnapshot);
            }

            return tableSnapshot;
        }

        /// Uspoređuje stari i novi snapshot i vraća listu promjena
        public static List<SchemaChange> Compare(SchemaSnapshot? oldSnapshot, SchemaSnapshot newSnapshot)
        {
            var changes = new List<SchemaChange>();

            // Ako nema starog snapshota, sve tablice su nove
            if (oldSnapshot == null)
            {
                foreach (var table in newSnapshot.Tables)
                {
                    changes.Add(new SchemaChange
                    {
                        Type = ChangeType.CreateTable,
                        TableName = table.TableName,
                        Sql = GenerateCreateTableSql(table)
                    });
                }
                return changes;
            }

            // Provjeri svaku tablicu u novom snapshotu
            foreach (var newTable in newSnapshot.Tables)
            {
                var oldTable = oldSnapshot.GetTable(newTable.TableName);

                if (oldTable == null)
                {
                    // Nova tablica
                    changes.Add(new SchemaChange
                    {
                        Type = ChangeType.CreateTable,
                        TableName = newTable.TableName,
                        Sql = GenerateCreateTableSql(newTable)
                    });
                }
                else
                {
                    // Usporedi stupce
                    var columnChanges = CompareColumns(oldTable, newTable);
                    changes.AddRange(columnChanges);
                }
            }

            // Provjeri obrisane tablice
            foreach (var oldTable in oldSnapshot.Tables)
            {
                var newTable = newSnapshot.GetTable(oldTable.TableName);
                if (newTable == null)
                {
                    changes.Add(new SchemaChange
                    {
                        Type = ChangeType.DropTable,
                        TableName = oldTable.TableName,
                        Sql = $"DROP TABLE IF EXISTS \"{oldTable.TableName}\" CASCADE;"
                    });
                }
            }

            return changes;
        }

        /// Uspoređuje stupce između stare i nove tablice
        private static List<SchemaChange> CompareColumns(TableSnapshot oldTable, TableSnapshot newTable)
        {
            var changes = new List<SchemaChange>();

            // Provjeri nove i promijenjene stupce
            foreach (var newColumn in newTable.Columns)
            {
                var oldColumn = oldTable.Columns.FirstOrDefault(c =>
                    c.Name.Equals(newColumn.Name, StringComparison.OrdinalIgnoreCase));

                if (oldColumn == null)
                {
                    // Novi stupac
                    changes.Add(new SchemaChange
                    {
                        Type = ChangeType.AddColumn,
                        TableName = newTable.TableName,
                        ColumnName = newColumn.Name,
                        NewColumn = newColumn,
                        Sql = GenerateAddColumnSql(newTable.TableName, newColumn)
                    });
                }
                else if (!ColumnsEqual(oldColumn, newColumn))
                {
                    // Promijenjen stupac
                    changes.Add(new SchemaChange
                    {
                        Type = ChangeType.AlterColumn,
                        TableName = newTable.TableName,
                        ColumnName = newColumn.Name,
                        OldColumn = oldColumn,
                        NewColumn = newColumn,
                        Sql = GenerateAlterColumnSql(newTable.TableName, oldColumn, newColumn)
                    });
                }
            }

            // Provjeri obrisane stupce
            foreach (var oldColumn in oldTable.Columns)
            {
                var newColumn = newTable.Columns.FirstOrDefault(c =>
                    c.Name.Equals(oldColumn.Name, StringComparison.OrdinalIgnoreCase));

                if (newColumn == null)
                {
                    changes.Add(new SchemaChange
                    {
                        Type = ChangeType.DropColumn,
                        TableName = newTable.TableName,
                        ColumnName = oldColumn.Name,
                        OldColumn = oldColumn,
                        Sql = $"ALTER TABLE \"{newTable.TableName}\" DROP COLUMN \"{oldColumn.Name}\";"
                    });
                }
            }

            return changes;
        }

        /// Provjerava jesu li dva stupca jednaka
        private static bool ColumnsEqual(ColumnSnapshot a, ColumnSnapshot b)
        {
            return a.Type == b.Type &&
                   a.Nullable == b.Nullable &&
                   a.Unique == b.Unique &&
                   a.DefaultValue == b.DefaultValue;
            // Ne uspoređujemo PrimaryKey i AutoIncrement jer se ne mogu mijenjati
        }

        /// Generira CREATE TABLE SQL
        private static string GenerateCreateTableSql(TableSnapshot table)
        {
            var columns = new List<string>();

            foreach (var col in table.Columns)
            {
                var colDef = new List<string> { $"\"{col.Name}\"" };

                if (col.PrimaryKey && col.AutoIncrement)
                {
                    colDef.Add("SERIAL PRIMARY KEY");
                }
                else
                {
                    colDef.Add(col.Type);

                    if (col.PrimaryKey)
                        colDef.Add("PRIMARY KEY");

                    if (!col.Nullable)
                        colDef.Add("NOT NULL");

                    if (col.Unique)
                        colDef.Add("UNIQUE");

                    if (!string.IsNullOrEmpty(col.DefaultValue))
                        colDef.Add($"DEFAULT {col.DefaultValue}");
                }

                columns.Add("    " + string.Join(" ", colDef));
            }

            return $"CREATE TABLE IF NOT EXISTS \"{table.TableName}\" (\n{string.Join(",\n", columns)}\n);";
        }

        /// Generira ALTER TABLE ADD COLUMN SQL
        private static string GenerateAddColumnSql(string tableName, ColumnSnapshot column)
        {
            var parts = new List<string> { column.Type };

            if (!column.Nullable)
            {
                // Za NOT NULL stupac, moramo dodati DEFAULT vrijednost
                var defaultVal = GetDefaultValueForType(column.Type);
                parts.Add($"DEFAULT {defaultVal}");
            }

            if (column.Unique)
                parts.Add("UNIQUE");

            return $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{column.Name}\" {string.Join(" ", parts)};";
        }

        /// Generira ALTER TABLE ALTER COLUMN SQL
        private static string GenerateAlterColumnSql(string tableName, ColumnSnapshot oldCol, ColumnSnapshot newCol)
        {
            var sqls = new List<string>();

            // Promjena tipa
            if (oldCol.Type != newCol.Type)
            {
                sqls.Add($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{newCol.Name}\" TYPE {newCol.Type};");
            }

            // Promjena nullable
            if (oldCol.Nullable != newCol.Nullable)
            {
                if (newCol.Nullable)
                    sqls.Add($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{newCol.Name}\" DROP NOT NULL;");
                else
                    sqls.Add($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{newCol.Name}\" SET NOT NULL;");
            }

            // Promjena UNIQUE (kompleksnije - treba constraint)
            if (oldCol.Unique != newCol.Unique)
            {
                if (newCol.Unique)
                    sqls.Add($"ALTER TABLE \"{tableName}\" ADD CONSTRAINT \"{tableName}_{newCol.Name}_unique\" UNIQUE (\"{newCol.Name}\");");
                else
                    sqls.Add($"ALTER TABLE \"{tableName}\" DROP CONSTRAINT IF EXISTS \"{tableName}_{newCol.Name}_unique\";");
            }

            return string.Join("\n", sqls);
        }

        /// Vraća default vrijednost za dani tip (za NOT NULL stupce)
        private static string GetDefaultValueForType(string pgType)
        {
            return pgType.ToUpper() switch
            {
                "INTEGER" or "BIGINT" or "SMALLINT" or "SERIAL" => "0",
                "DECIMAL" or "REAL" or "DOUBLE PRECISION" => "0.0",
                "BOOLEAN" => "FALSE",
                "TIMESTAMP" or "TIMESTAMPTZ" => "NOW()",
                _ => "''"  // TEXT, VARCHAR, etc.
            };
        }
    }
}