using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProdeMundial.Domain.Entities;

public class Match
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int HomeTeamId { get; set; }

    [ForeignKey("HomeTeamId")]
    public Team HomeTeam { get; set; } = null!;

    [Required]
    public int AwayTeamId { get; set; }

    [ForeignKey("AwayTeamId")]
    public Team AwayTeam { get; set; } = null!;

    [Required]
    public DateTime Date { get; set; }

    [Range(0, 50, ErrorMessage = "Goles fuera de rango")] // Nadie mete 50 goles
    public int? HomeScore { get; set; }

    [Range(0, 50, ErrorMessage = "Goles fuera de rango")]
    public int? AwayScore { get; set; }

    [Required]
    public bool IsFinished { get; set; } = false;

    [MaxLength(50)]
    public string Phase { get; set; } = "Grupos"; // Grupos, Octavos, etc.
}