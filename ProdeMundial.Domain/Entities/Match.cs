using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProdeMundial.Domain.Entities;

public class Match
{
    [Key]
    public int Id { get; set; }

    // Quitamos [Required] y cambiamos a int? para soportar cruces vacíos en Octavos/Cuartos
    public int? HomeTeamId { get; set; }

    [ForeignKey("HomeTeamId")]
    public Team? HomeTeam { get; set; } // Quitamos el = null!; ya que ahora SÍ puede ser nulo

    // Lo mismo para el equipo visitante
    public int? AwayTeamId { get; set; }

    [ForeignKey("AwayTeamId")]
    public Team? AwayTeam { get; set; }

    // Nuevos campos opcionales para mostrar textos provisionales como "1º Grupo A" o "Ganador Octavos 1"
    [MaxLength(100)]
    public string? HomeTeamPlaceholder { get; set; }

    [MaxLength(100)]
    public string? AwayTeamPlaceholder { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Range(0, 20, ErrorMessage = "Goles fuera de rango")]
    public int? HomeScore { get; set; }

    [Range(0, 20, ErrorMessage = "Goles fuera de rango")]
    public int? AwayScore { get; set; }

    // Nuevos campos para los penaltis en caso de empate en fases eliminatorias
    [Range(0, 20, ErrorMessage = "Penaltis fuera de rango")]
    public int? HomePenaltiesScore { get; set; }

    [Range(0, 20, ErrorMessage = "Penaltis fuera de rango")]
    public int? AwayPenaltiesScore { get; set; }

    [Required]
    public bool IsFinished { get; set; } = false;

    [Required]
    [MaxLength(50)]
    public string Phase { get; set; } = "Grupos"; // Grupos, Dieciseisavos, Octavos, etc.
}