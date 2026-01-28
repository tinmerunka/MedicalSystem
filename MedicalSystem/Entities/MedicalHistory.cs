using CustomORM.Attributes;

namespace MedicalSystem.Entities
{
    [Table("MedicalHistories")]
    public class MedicalHistory
    {
        [PrimaryKey(AutoIncrement = true)]
        [Column("Id")]
        public int Id { get; set; }

        [Column("PatientId", IsNullable = false)]
        [ForeignKey(typeof(Patient))]
        public int PatientId { get; set; }

        [Column("DiseaseName", IsNullable = false)]
        public string DiseaseName { get; set; } = string.Empty;

        [Column("StartDate", IsNullable = false)]
        public DateTime StartDate { get; set; }

        [Column("EndDate")]
        public DateTime? EndDate { get; set; }

        // Navigation property
        public virtual Patient Patient { get; set; } = null!;
    }
}