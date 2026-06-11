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

        Task<List<Prediction>> GetUserPredictionsAsync(int userId, int companyId);

        Task SimulateAllMatchesAsync();
        Task ResetTournamentAsync(bool deletePredictions);

        //Usuarios y Empresa

        Task<List<AppUser>> GetUsersAsync();
        Task AddUserAsync(string name);
        Task DeleteUserAsync(int id);
       
        Task<Company> GetConfigAsync(int companyId);
        Task UpdateConfigAsync(TournamentConfig config);

        void NotifyResultUpdated();

        Task<Company?> GetCompanyByCodeAsync(string code);
        Task<AppUser?> AuthenticateUserAsync(string identity, string pin);

        Task<List<AppUser>> GetUsersByCompanyAsync(int companyId);
        Task UpdateUserStatusAsync(AppUser user);
        Task UpdateCompanyAsync(Company company);


        Task<AppUser?> AuthenticateUserWithoutCodeAsync(string identity, string pin);
        Task<bool> RegisterUserAsync(AppUser user, string invitationCode, string identityInput);

        Task<List<UserRanking>> GetRankingAsync(int companyId);

        Task ToggleUserActiveAsync(int userId, bool isActive);

        Task RegistrarBarYAdminAsync(Company nuevaEmpresa, AppUser adminUser);

        Task<int> ActivateAllPendingUsersAsync(int companyId);

        Task<AppUser?> GetUserByIdAsync(int userId);

    }
}
