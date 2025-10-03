using System.ComponentModel.DataAnnotations;

namespace PortalAcademico.Models
{
    public class Curso : IValidatableObject
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Codigo { get; set; } = null!; // único

        [Required, StringLength(200)]
        public string Nombre { get; set; } = null!;

        [Range(1, int.MaxValue, ErrorMessage = "Los créditos deben ser mayores que 0")]
        public int Creditos { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El cupo máximo debe ser mayor que 0")]
        public int CupoMaximo { get; set; }

        [Required]
        public TimeSpan HorarioInicio { get; set; }

        [Required]
        public TimeSpan HorarioFin { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<Matricula> Matriculas { get; set; } = new List<Matricula>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (HorarioInicio >= HorarioFin)
                yield return new ValidationResult("HorarioInicio debe ser anterior a HorarioFin",
                    new[] { nameof(HorarioInicio), nameof(HorarioFin) });

            if (Creditos <= 0)
                yield return new ValidationResult("Créditos debe ser mayor que 0", new[] { nameof(Creditos) });
        }
    }
}
