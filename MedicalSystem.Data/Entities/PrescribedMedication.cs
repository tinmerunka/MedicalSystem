using CustomORM.Attributes;

namespace MedicalSystem.Data.Entities
{
    [Table("PrescribedMedications")]
    public class PrescribedMedication
    {
        [PrimaryKey(AutoIncrement = true)]
        [Column("Id")]
        public int Id { get; set; }

        [Column("PatientId", IsNullable = false)]
        [ForeignKey(typeof(Patient))]
        public int PatientId { get; set; }

        [Column("MedicationId", IsNullable = false)]
        [ForeignKey(typeof(Medication))]
        public int MedicationId { get; set; }

        [Column("Dosage", IsNullable = false)]
        public string Dosage { get; set; } = string.Empty;

        [Column("Frequency", IsNullable = false)]
        public string Frequency { get; set; } = string.Empty;

        [Column("StartDate", IsNullable = false)]
        public DateTime StartDate { get; set; }

        [Column("EndDate")]
        public DateTime? EndDate { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Medication Medication { get; set; } = null!;
    }
}