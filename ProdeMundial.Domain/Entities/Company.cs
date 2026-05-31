using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProdeMundial.Domain.Entities
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del negocio es obligatorio")]
        [MaxLength(100)]
        public string Name { get; set; } = null!; 

        [Required(ErrorMessage = "El código de invitación es necesario")]
        [MaxLength(20)]
        public string InvitationCode { get; set; } = null!;


        // Control de tu negocio: Cuántos espacios le vendiste al bar
         public int MaxUsers { get; set; } = 50;

        public string? LogoUrl { get; set; } = "/img/default-logo.png";

        // Traemos PrizeDescription aquí para que cada bar defina el suyo
        [Required(ErrorMessage = "La descripción del premio es obligatoria")]
        public string PrizeDescription { get; set; } = "¡Premio a definir!";

        public bool IsActive { get; set; } = true;

        // Relaciones       
        public ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
    }
}
