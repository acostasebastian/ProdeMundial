using Microsoft.EntityFrameworkCore;
using ProdeMundial.Domain;
using ProdeMundial.Domain.Entities;
using ProdeMundial.Infrastructure;

namespace ProdeMundial.Web.Services
{
    public class MatchService(IDbContextFactory<ApplicationDbContext> factory) : IMatchService
    {
       
        public event Action? OnResultUpdated;
        public event Action? OnPredictionsCleared; // 1. Declarar el evento

        public void NotifyResultUpdated() => OnResultUpdated?.Invoke();

        public async Task<List<Match>> GetUpcomingMatchesAsync()
        {
            // IMPORTANTE: Ahora cada método debe crear su propio context así:
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

            var existing = await context.Predictions
                .FirstOrDefaultAsync(p => p.MatchId == prediction.MatchId && p.UserId == prediction.UserId);

            if (existing != null)
            {
                existing.PredictedHomeScore = prediction.PredictedHomeScore;
                existing.PredictedAwayScore = prediction.PredictedAwayScore;
            }
            else
            {
                context.Predictions.Add(prediction);
            }
            await context.SaveChangesAsync();
        }

        //public async Task<List<UserRanking>> GetRankingAsync()
        //{
        //    using var context = factory.CreateDbContext();

        //    var predictions = await context.Predictions
        //        .Include(p => p.Match)
        //        .Where(p => p.Match.IsFinished) // Solo contamos partidos terminados
        //        .ToListAsync();

        //    return predictions
        //        .GroupBy(p => p.UserId)
        //        .Select(group => new UserRanking
        //        {
        //            UserId = group.Key,
        //            TotalPoints = group.Sum(p => CalculatePoints(p))
        //        })
        //        .OrderByDescending(r => r.TotalPoints)
        //        .ToList();
        //}

        public async Task<List<UserRanking>> GetRankingAsync()
        {
            using var context = factory.CreateDbContext();

            // Traemos todos los usuarios y todos los partidos finalizados con sus predicciones
            var users = await context.AppUsers.ToListAsync();
            var matches = await context.Matches.Where(m => m.IsFinished).ToListAsync();
            var predictions = await context.Predictions.ToListAsync();

            var ranking = users.Select(user => {
                int points = 0;
                int exacts = 0;
                var userPredictions = predictions.Where(p => p.UserId == user.Name);
               
                foreach (var pred in userPredictions)
                {
                    var match = matches.FirstOrDefault(m => m.Id == pred.MatchId);

                    // 1. SEGURIDAD: Solo calculamos si el partido terminó Y si el usuario puso AMBOS goles
                    if (match != null &&
                        match.HomeScore.HasValue && match.AwayScore.HasValue &&
                        pred.PredictedHomeScore.HasValue && pred.PredictedAwayScore.HasValue)
                    {
                        // 2. Usamos .Value para extraer el número real del int?
                        int pHome = pred.PredictedHomeScore.Value;
                        int pAway = pred.PredictedAwayScore.Value;
                        int mHome = match.HomeScore.Value;
                        int mAway = match.AwayScore.Value;

                        // Puntos por resultado exacto (3 pts)
                        if (pHome == mHome && pAway == mAway)
                        {
                            points += 3;
                            exacts += 1;
                        }
                        // Puntos por acertar ganador o empate (1 pt)
                        // Math.Sign funciona perfecto con los .Value
                        else if (Math.Sign(pHome - pAway) == Math.Sign(mHome - mAway))
                        {
                            points += 1;
                        }
                    }
                }

                return new UserRanking { UserId = user.Name, TotalPoints = points, ExactResults = exacts };
            })
            .OrderByDescending(r => r.TotalPoints)
            .ThenByDescending(r => r.ExactResults) // <-- DESEMPATE POR EXACTOS
            .ThenBy(r => r.UserId) // Alfabético si empatan en puntos
            .ToList();

            return ranking;
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
            var match = await context.Matches.FindAsync(matchId);
            if (match != null)
            {
                match.HomeScore = homeScore;
                match.AwayScore = awayScore;
                match.IsFinished = true; // Al cargar resultado, lo damos por terminado
                await context.SaveChangesAsync();
                // Avisamos a quien esté escuchando que los datos cambiaron
                OnResultUpdated?.Invoke();
            }
        }

        public async Task<List<Prediction>> GetUserPredictionsAsync(string userId)
        {
            using var context = factory.CreateDbContext();
            return await context.Predictions
                .Where(p => p.UserId == userId)
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

        // --- CONFIGURACIÓN DE MARCA ---
        public async Task<TournamentConfig> GetConfigAsync()
        {
            using var context = factory.CreateDbContext();
            var config = await context.TournamentConfigs.FirstOrDefaultAsync();

            // Si la tabla está vacía (primera vez), devolvemos uno con valores por defecto
            if (config == null)
            {
                return new TournamentConfig
                {
                    CompanyName = "Mi Prode",
                    PrizeDescription = "¡Premio por definir!",
                    LogoUrl = "/img/default-logo.png"
                };
            }
            return config;
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



    }
}
