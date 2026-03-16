using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProdeMundial.Domain.Entities
{
    public class Team
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public string FlagUrl { get; set; } = string.Empty;
        [Required]
        [MaxLength(1)] // Grupos A, B, C... L
        public string Group { get; set; } = string.Empty;
    }
}
