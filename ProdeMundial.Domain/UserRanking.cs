using System;
using System.Collections.Generic;
using System.Text;

namespace ProdeMundial.Domain
{
    public class UserRanking
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public int TotalPoints { get; set; }
        public int ExactResults { get; set; } // Opcional: para desempatar
    }
}
