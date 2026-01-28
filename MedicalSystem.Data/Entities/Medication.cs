using CustomORM.Attributes;

namespace MedicalSystem.Data.Entities
{
    [Table("Medications")]
    public class Medication
    {
        [PrimaryKey(AutoIncrement = true)]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name", IsNullable = false)]
        public string Name { get; set; } = string.Empty;

        [Column("DosageUnit", IsNullable = false)]
        public string DosageUnit { get; set; } = string.Empty;

        [Column("Description")]
        public string? Description { get; set; }

        // Navigation property
        public virtual ICollection<PrescribedMedication> PrescribedMedications { get; set; } = new List<PrescribedMedication>();
    }
}