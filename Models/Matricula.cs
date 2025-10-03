using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortalAcademico.Models
{
    public enum EstadoMatricula { Pendiente, Confirmada, Cancelada }

    public class Matricula
    {
        public int Id { get; set; }

        [Required]
        public int CursoId { get; set; }

        public Curso Curso { get; set; } = null!;

        [Required]
        public string UsuarioId { get; set; } = null!;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public EstadoMatricula Estado { get; set; } = EstadoMatricula.Pendiente;
    }
}
