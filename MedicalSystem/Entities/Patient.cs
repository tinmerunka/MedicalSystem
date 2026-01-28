using CustomORM.Attributes;

namespace MedicalSystem.Entities
{
    [Table("Patients")]
    public class Patient
    {
        [PrimaryKey(AutoIncrement = true)]
        [Column("Id")]
        public int Id { get; set; }

        [Column("FirstName", IsNullable = false)]
        public string FirstName { get; set; } = string.Empty;

        [Column("LastName", IsNullable = false)]
        public string LastName { get; set; } = string.Empty;

        [Column("OIB", IsNullable = false)]
        [Unique]
        public string OIB { get; set; } = string.Empty;

        [Column("DateOfBirth", IsNullable = false)]
        public DateTime DateOfBirth { get; set; }

        [Column("Gender", IsNullable = false)]
        public Gender Gender { get; set; }

        [Column("ResidenceAddress", IsNullable = false)]
        public string ResidenceAddress { get; set; } = string.Empty;

        [Column("PermanentAddress", IsNullable = false)]
        public string PermanentAddress { get; set; } = string.Empty;

        //[Column("Age", IsNullable = true)]
        //public int? Age { get; set; }

        // Navigation properties
        public virtual ICollection<MedicalHistory> MedicalHistories { get; set; } = new List<MedicalHistory>();
        public virtual ICollection<PrescribedMedication> PrescribedMedications { get; set; } = new List<PrescribedMedication>();
        public virtual ICollection<SpecialistExamination> SpecialistExaminations { get; set; } = new List<SpecialistExamination>();
    }
}