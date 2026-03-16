using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProdeMundial.Domain.Entities
{
    // Para personalizar la "Marca" del cliente
    public class TournamentConfig
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la Marca es obligatorio.")]
        [MaxLength(100)]
        public string CompanyName { get; set; } = "Mi Prode";
        public string? LogoUrl { get; set; } = "/img/default-logo.png";

        [Required(ErrorMessage = "La descripción del premio es obligatoria")]
        public string PrizeDescription { get; set; } = "¡Premio a definir!";
    }
}
