using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ProdeMundial.Domain.Entities
{
    // Para gestionar quiénes participan
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del usuario es obligatorio.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Campos opcionales para soportar ambos mundos
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }

        [Required(ErrorMessage = "El Pin de Acceso es obligatorio.")]
        public string AccessPin { get; set; } = string.Empty; // El PIN sirve para ambos

        // Relación con la empresa
        public int CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public Company Company { get; set; } = null!; // La propiedad de navegación

        public bool IsAdmin { get; set; } = false;
        public bool IsActive { get; set; } = false; // Por defecto desactivado

        public ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();

    }
}
