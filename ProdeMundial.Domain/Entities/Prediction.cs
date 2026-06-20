using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProdeMundial.Domain.Entities;

public class Prediction
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El usuario es obligatorio")]
    public int UserId { get; set; } // Cambiado a int para coincidir con AppUser

    //[ForeignKey("UserId")]
    // Movimos el ForeignKey aquí arriba, apuntando a la propiedad de objeto
    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; } = null!;

    [Required(ErrorMessage = "La empresa/bar es obligatoria")]
    public int CompanyId { get; set; } // <--- Nuevo: Relación con el grupo

    [ForeignKey(nameof(CompanyId))]
    public Company Company { get; set; } = null!;

    [Required]
    public int MatchId { get; set; }
 
    [ForeignKey(nameof(MatchId))]
    public Match Match { get; set; } = null!;

    [Required(ErrorMessage = "Debes indicar los goles del equipo local")]
    [Range(0, 20, ErrorMessage = "El resultado no es realista")]
    public int? PredictedHomeScore { get; set; }

    [Required(ErrorMessage = "Debes indicar los goles del equipo visitante")]
    [Range(0, 20, ErrorMessage = "El resultado no es realista")]
    public int? PredictedAwayScore { get; set; }

    public int PointsEarned { get; set; } = 0;

    public int? WinnerTeamId { get; set; }
}
