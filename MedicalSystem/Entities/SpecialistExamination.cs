using CustomORM.Attributes;

namespace MedicalSystem.Entities
{
    [Table("SpecialistExaminations")]
    public class SpecialistExamination
    {
        [PrimaryKey(AutoIncrement = true)]
        [Column("Id")]
        public int Id { get; set; }

        [Column("PatientId", IsNullable = false)]
        [ForeignKey(typeof(Patient))]
        public int PatientId { get; set; }

        [Column("DoctorId", IsNullable = false)]
        [ForeignKey(typeof(Doctor))]
        public int DoctorId { get; set; }

        [Column("ExaminationType", IsNullable = false)]
        public ExaminationType ExaminationType { get; set; }

        [Column("AppointmentDate", IsNullable = false)]
        public DateTime AppointmentDate { get; set; }

        [Column("Notes")]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Doctor Doctor { get; set; } = null!;
    }
}