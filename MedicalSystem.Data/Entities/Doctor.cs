using CustomORM.Attributes;

namespace MedicalSystem.Data.Entities
{
    [Table("Doctors")]
    public class Doctor
    {
        [PrimaryKey(AutoIncrement = true)]
        [Column("Id")]
        public int Id { get; set; }

        [Column("FirstName", IsNullable = false)]
        public string FirstName { get; set; } = string.Empty;

        [Column("LastName", IsNullable = false)]
        public string LastName { get; set; } = string.Empty;

        [Column("Specialization", IsNullable = false)]
        public string Specialization { get; set; } = string.Empty;

        // Navigation property 
        public virtual ICollection<SpecialistExamination> Examinations { get; set; } = new List<SpecialistExamination>();
    }
}