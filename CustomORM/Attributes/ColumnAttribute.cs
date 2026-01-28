namespace CustomORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; }
        public string TypeName { get; set; }
        public bool IsNullable { get; set; } = true;
        public int Length { get; set; } = -1;

        public ColumnAttribute(string name)
        {
            Name = name;
        }
    }
}