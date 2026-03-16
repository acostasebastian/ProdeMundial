using ProdeMundial.Domain;
using ProdeMundial.Domain.Entities;

namespace ProdeMundial.Web.Services
{
    public interface IMatchService
    {
        event Action OnResultUpdated; // <--- Agregado
        event Action OnPredictionsCleared; // Nuevo evento

        Task<List<Match>> GetUpcomingMatchesAsync();
        Task SavePredictionAsync(Prediction prediction);
        
        Task<List<UserRanking>> GetRankingAsync();

        Task UpdateMatchResultAsync(int matchId, int homeScore, int awayScore);

        Task<List<Prediction>> GetUserPredictionsAsync(string userId);

        Task SimulateAllMatchesAsync();
        Task ResetTournamentAsync(bool deletePredictions);

        //Usuarios y Empresa

        Task<List<AppUser>> GetUsersAsync();
        Task AddUserAsync(string name);
        Task DeleteUserAsync(int id);
        Task<TournamentConfig> GetConfigAsync();
        Task UpdateConfigAsync(TournamentConfig config);

        void NotifyResultUpdated();




    }
}
