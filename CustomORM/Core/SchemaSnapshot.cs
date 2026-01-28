using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustomORM.Core
{
    /// Predstavlja snapshot jednog stupca
    public class ColumnSnapshot
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("nullable")]
        public bool Nullable { get; set; } = true;

        [JsonPropertyName("primaryKey")]
        public bool PrimaryKey { get; set; } = false;

        [JsonPropertyName("autoIncrement")]
        public bool AutoIncrement { get; set; } = false;

        [JsonPropertyName("unique")]
        public bool Unique { get; set; } = false;

        [JsonPropertyName("defaultValue")]
        public string? DefaultValue { get; set; }
    }


    /// Predstavlja snapshot jedne tablice
    public class TableSnapshot
    {
        [JsonPropertyName("tableName")]
        public string TableName { get; set; } = string.Empty;

        [JsonPropertyName("columns")]
        public List<ColumnSnapshot> Columns { get; set; } = new();
    }

    /// Predstavlja snapshot cijele baze (sve tablice)
    public class SchemaSnapshot
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("tables")]
        public List<TableSnapshot> Tables { get; set; } = new();

        /// Sprema snapshot u JSON datoteku
        public void SaveToFile(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(this, options);

            // Kreiraj direktorij ako ne postoji
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, json);
        }

        
        /// Učitava snapshot iz JSON datoteke
        public static SchemaSnapshot? LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<SchemaSnapshot>(json);
        }

        
        /// Pronalazi tablicu po imenu
        public TableSnapshot? GetTable(string tableName)
        {
            return Tables.FirstOrDefault(t =>
                t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        }
    }
}