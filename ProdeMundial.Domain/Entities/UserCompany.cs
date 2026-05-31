using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProdeMundial.Domain.Entities
{
    public class UserCompany
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        public int TotalPoints { get; set; } = 0;

        public bool IsGroupAdmin { get; set; } = false; // Por si el del bar quiere gestionar su propio ranking
    }
}
