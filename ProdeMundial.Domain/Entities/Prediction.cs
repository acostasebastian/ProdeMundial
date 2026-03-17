using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProdeMundial.Domain.Entities;

public class Prediction
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)] // Longitud estándar para IDs de ASP.NET Identity
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int MatchId { get; set; }

    [ForeignKey("MatchId")]
    public Match Match { get; set; } = null!;

    
    [Range(0, 20, ErrorMessage = "El resultado no es realista")]
    public int? PredictedHomeScore { get; set; }

    
    [Range(0, 20, ErrorMessage = "El resultado no es realista")]
    public int? PredictedAwayScore { get; set; }

    public int PointsEarned { get; set; } = 0;
}
