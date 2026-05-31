using Microsoft.EntityFrameworkCore;
using ProdeMundial.Domain;
using ProdeMundial.Domain.Entities;
using ProdeMundial.Infrastructure;
using ProdeMundial.Infrastructure.Migrations;

namespace ProdeMundial.Web.Services
{
    public class MatchService(IDbContextFactory<ApplicationDbContext> factory) : IMatchService
    {
       
        public event Action? OnResultUpdated;
        public event Action? OnPredictionsCleared; // 1. Declarar el evento

        public void NotifyResultUpdated() => OnResultUpdated?.Invoke();

        public async Task<List<Match>> GetUpcomingMatchesAsync()
        {
          using var context = factory.CreateDbContext();

            return await context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .OrderBy(m => m.Date)
                .ToListAsync();
        }
       
        public async Task SavePredictionAsync(Prediction prediction)
        {
            using var context = factory.CreateDbContext();

            // 1. Validar que el partido exista y NO haya comenzado todavía
            var match = await context.Matches.FindAsync(prediction.MatchId);
            if (match == null) throw new Exception("El partido no existe.");

            if (match.IsFinished || match.Date <= DateTime.Now)
            {
                throw new Exception("El partido ya comenzó.");
            }

            // 2. Buscar si ya existe una predicción previa
            var existing = await context.Predictions
                .FirstOrDefaultAsync(p => p.MatchId == prediction.MatchId && p.UserId == prediction.UserId);

            if (existing != null)
            {
                existing.PredictedHomeScore = prediction.PredictedHomeScore;
                existing.PredictedAwayScore = prediction.PredictedAwayScore;
                existing.CompanyId = prediction.CompanyId;

                context.Predictions.Update(existing);
            }
            else
            {
                // TRUCO DE SEGURIDAD: Nos aseguramos de que EF no intente re-insertar 
                // o validar relaciones complejas que vengan en nulo desde la UI.
                var nuevaPrediccion = new Prediction
                {
                    MatchId = prediction.MatchId,
                    UserId = prediction.UserId,
                    CompanyId = prediction.CompanyId,
                    PredictedHomeScore = prediction.PredictedHomeScore,
                    PredictedAwayScore = prediction.PredictedAwayScore,
                    PointsEarned = 0
                };

                context.Predictions.Add(nuevaPrediccion);
            }

            await context.SaveChangesAsync();
        }
       
        public async Task<List<UserRanking>> GetRankingAsync()
        {            
            return new List<UserRanking>(); 
        }

        public async Task<List<UserRanking>> GetRankingAsync(int companyId)
        {
            using var context = factory.CreateDbContext();

            // La base de datos calcula los puntos y los plenos, y nos devuelve solo el DTO final
            return await context.AppUsers
                .Where(u => u.CompanyId == companyId && u.IsActive)
                .Select(u => new UserRanking
                {
                    UserId = u.Id,
                    UserName = u.Name,
                    TotalPoints = u.Predictions.Sum(p => p.PointsEarned),
                    ExactResults = u.Predictions.Count(p => p.PointsEarned == 3)
                })
                .OrderByDescending(r => r.TotalPoints)
                .ThenByDescending(r => r.ExactResults)
                .ToListAsync(); // El ToListAsync se ejecuta al final, viajando por la red solo el ranking procesado
        }
        
        private int CalculatePoints(Prediction p)
        {
            // 1. SEGURIDAD: Si el partido no tiene resultado o la predicción está incompleta, 0 puntos.
            if (p.Match == null ||
                !p.Match.HomeScore.HasValue || !p.Match.AwayScore.HasValue ||
                !p.PredictedHomeScore.HasValue || !p.PredictedAwayScore.HasValue)
            {
                return 0;
            }

            // Extraemos los valores reales para trabajar cómodos
            int pHome = p.PredictedHomeScore.Value;
            int pAway = p.PredictedAwayScore.Value;
            int mHome = p.Match.HomeScore.Value;
            int mAway = p.Match.AwayScore.Value;

            // 2. Resultado Exacto -> 3 puntos
            if (pHome == mHome && pAway == mAway)
                return 3;

            // 3. Acertar el signo (Ganador o Empate) -> 1 punto
            var actualResult = Math.Sign(mHome - mAway);
            var predictedResult = Math.Sign(pHome - pAway);

            if (actualResult == predictedResult)
                return 1;

            return 0;
        }
       

        public async Task UpdateMatchResultAsync(int matchId, int homeScore, int awayScore)
        {
            using var context = factory.CreateDbContext();

            // 1. Buscar el partido y actualizar su resultado oficial
            var match = await context.Matches.FindAsync(matchId);
            if (match == null) throw new Exception("Partido no encontrado");

            match.HomeScore = homeScore; // Asegúrate de que coincida con tus columnas (HomeScore o HomeTeamScore)
            match.AwayScore = awayScore;
            match.IsFinished = true;

            // 2. Buscar todas las predicciones de los usuarios para ESTE partido
            var predictions = await context.Predictions
                .Where(p => p.MatchId == matchId)
                .ToListAsync();

            // 3. Calcular los puntos para cada uno según las reglas de la porra
            foreach (var pred in predictions)
            {
                if (pred.PredictedHomeScore == homeScore && pred.PredictedAwayScore == awayScore)
                {
                    // Caso 1: Resultado Exacto (Pleno) -> 3 puntos
                    pred.PointsEarned = 3;
                }
                else if ((pred.PredictedHomeScore > pred.PredictedAwayScore && homeScore > awayScore) || // Acertó gana Local
                         (pred.PredictedHomeScore < pred.PredictedAwayScore && homeScore < awayScore) || // Acertó gana Visitante
                         (pred.PredictedHomeScore == pred.PredictedAwayScore && homeScore == awayScore))  // Acertó Empate
                {
                    // Caso 2: Acertó el ganador o el empate pero no el resultado exacto -> 1 punto
                    pred.PointsEarned = 1;
                }
                else
                {
                    // Caso 3: No pegó ni el resultado ni el ganador -> 0 puntos
                    pred.PointsEarned = 0;
                }
            }

            // 4. Guardar todo en un solo bloque transaccional
            await context.SaveChangesAsync();

            // 5. ¡Avisar a todos los componentes que los datos cambiaron! (Para SignalR y Blazor)
            OnResultUpdated?.Invoke();
        }


        public async Task<List<Prediction>> GetUserPredictionsAsync(int userId, int companyId)
        {
            using var context = factory.CreateDbContext();

            return await context.Predictions
                .Where(p => p.UserId == userId && p.CompanyId == companyId)
                .Include(p => p.Match)
                    .ThenInclude(m => m.HomeTeam)
                .Include(p => p.Match)
                    .ThenInclude(m => m.AwayTeam) // Corregido el encadenamiento limpio de EF Core
                .ToListAsync();
        }

        public async Task SimulateAllMatchesAsync()
        {
            using var context = factory.CreateDbContext();
            var matches = await context.Matches.ToListAsync();
            var random = new Random();

            foreach (var m in matches)
            {
                m.HomeScore = random.Next(0, 5); // Goles entre 0 y 4
                m.AwayScore = random.Next(0, 5);
                m.IsFinished = true;
            }

            await context.SaveChangesAsync();
            OnResultUpdated?.Invoke(); // ¡Esto refresca el Ranking de todos!
        }

        public async Task ResetTournamentAsync(bool removePredictions)
        {
            using var context = factory.CreateDbContext();

            // 1. Limpiar resultados de partidos
            var matches = await context.Matches.ToListAsync();
            foreach (var m in matches)
            {
                m.HomeScore = null;
                m.AwayScore = null;
                m.IsFinished = false;
            }

            // 2. Limpiar predicciones solo si el usuario lo pidió
            if (removePredictions)
            {
                var predictions = await context.Predictions.ToListAsync();
                context.Predictions.RemoveRange(predictions);
            }

            await context.SaveChangesAsync();


            if (removePredictions)
            {
                var predictions = await context.Predictions.ToListAsync();
                context.Predictions.RemoveRange(predictions);
                await context.SaveChangesAsync();

                // ¡IMPORTANTE! Avisamos a las pantallas de predicciones
                OnPredictionsCleared?.Invoke();
            }

            OnResultUpdated?.Invoke(); // Esto refresca el ranking
        }

        // --- USUARIOS ---
        public async Task<List<AppUser>> GetUsersAsync()
        {
            using var context = factory.CreateDbContext();
            return await context.AppUsers.OrderBy(u => u.Name).ToListAsync();
        }

        public async Task AddUserAsync(string name)
        {
            using var context = factory.CreateDbContext();
            context.AppUsers.Add(new AppUser { Name = name });
            await context.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int id)
        {
            using var context = factory.CreateDbContext();
            var user = await context.AppUsers.FindAsync(id);
            if (user != null)
            {
                context.AppUsers.Remove(user);
                await context.SaveChangesAsync();
            }
        }

        // Busca la empresa por el código que puso el usuario
        public async Task<Company?> GetCompanyByCodeAsync(string code)
        {
            using var context = factory.CreateDbContext();
            return await context.Companies.FirstOrDefaultAsync(c => c.InvitationCode == code);
        }

        // Busca al usuario por teléfono/email y valida el PIN
        public async Task<AppUser?> AuthenticateUserAsync(string identity, string pin)
        {
            using var context = factory.CreateDbContext();
            return await context.AppUsers.FirstOrDefaultAsync(u =>
                (u.PhoneNumber == identity || u.Email == identity) && u.AccessPin == pin);
        }

        public async Task<AppUser?> AuthenticateUserWithoutCodeAsync(string identity, string pin)
        {
            using var context = factory.CreateDbContext();

                
            
            var usu = await context.AppUsers
                .FirstOrDefaultAsync(u => (u.Email == identity || u.PhoneNumber == identity) && u.AccessPin == pin);

            // Buscamos al usuario solo por su mail/tel y pin
            return usu;
        }
           

        public async Task<bool> RegisterUserAsync(AppUser user, string invitationCode, string identityInput)
        {
            using var context = factory.CreateDbContext();

            // 1. Validar el código de invitación
            var company = await context.Companies
                .FirstOrDefaultAsync(c => c.InvitationCode == invitationCode);

            if (company == null) return false;

            // 2. DETECCIÓN INTELIGENTE
            // Si contiene un '@' asumimos que es Email, si no, es Teléfono
            if (identityInput.Contains("@"))
            {
                user.Email = identityInput.Trim().ToLower();
                user.PhoneNumber = null;
            }
            else
            {
                // Limpiamos espacios o guiones del teléfono
                user.PhoneNumber = new string(identityInput.Where(char.IsDigit).ToArray());
                user.Email = null;
            }

            // 2. Asignar el ID de la empresa al nuevo usuario
            user.CompanyId = company.Id;
            user.IsActive = false; // Por defecto desactivado hasta que el dueño vea el pago

            context.AppUsers.Add(user);
            await context.SaveChangesAsync();
            return true;
        }

        // --- CONFIGURACIÓN DE MARCA ---
     
        public async Task<Company> GetConfigAsync(int companyId)
        {
            using var context = factory.CreateDbContext();

            // Buscamos la empresa específica
            var company = await context.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId);

            // Si por algún motivo no existe o el ID es 0, devolvemos el objeto por defecto
            if (company == null)
            {
                return new Company
                {
                    Name = "Mi Prode",
                    PrizeDescription = "¡Premio por definir!",
                    LogoUrl = "/img/default-logo.png"
                };
            }

            return company;
        }

        public async Task UpdateConfigAsync(TournamentConfig config)
        {
            using var context = factory.CreateDbContext();
            if (config.Id == 0)
            {
                context.TournamentConfigs.Add(config);
            }
            else
            {
                // El "Update" de EF Core se encarga de todo si el ID ya existe
                context.TournamentConfigs.Update(config);
            }
            await context.SaveChangesAsync();
        }

        
        /// /////////////
        
        public async Task<List<AppUser>> GetUsersByCompanyAsync(int companyId)
        {
            using var context = factory.CreateDbContext();
            return await context.AppUsers
                .Where(u => u.CompanyId == companyId)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task UpdateUserStatusAsync(AppUser user)
        {
            using var context = factory.CreateDbContext();

            // Solo actualizamos el estado de activación y quizás el nombre 
            // para no sobreescribir relaciones por error
            var existing = await context.AppUsers.FindAsync(user.Id);
            if (existing != null)
            {
                existing.IsActive = user.IsActive;
                await context.SaveChangesAsync();
            }
        }

        public async Task UpdateCompanyAsync(Company company)
        {
            using var context = factory.CreateDbContext();

            // Usamos el ID de la empresa para buscar la entidad original
            var existing = await context.Companies.FindAsync(company.Id);
            if (existing != null)
            {
                existing.Name = company.Name;
                existing.LogoUrl = company.LogoUrl;
                existing.PrizeDescription = company.PrizeDescription;
                existing.InvitationCode = company.InvitationCode;
                existing.MaxUsers = company.MaxUsers;

                await context.SaveChangesAsync();
            }
        }

        ////////////     

        public async Task ToggleUserActiveAsync(int userId, bool isActive)
        {
            using var context = factory.CreateDbContext();
            var user = await context.AppUsers.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = isActive;
                await context.SaveChangesAsync();
            }
        }

        public async Task RegistrarBarYAdminAsync(Company nuevaEmpresa, AppUser adminUser)
        {
            using var context = factory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 1. Guardar la Empresa
                context.Companies.Add(nuevaEmpresa);
                await context.SaveChangesAsync();

                // 2. Asignar el ID de la empresa al nuevo Admin
                adminUser.CompanyId = nuevaEmpresa.Id;
                adminUser.IsAdmin = true;
                adminUser.IsActive = true; // El admin nace activo

                context.AppUsers.Add(adminUser);
                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }




    }
}
