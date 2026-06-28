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

        public async Task<List<Team>> GetTeamsAsync()
        {
            using var context = factory.CreateDbContext();

            return await context.Teams               
                .ToListAsync();
        }

        

        public async Task SavePredictionAsync(Prediction prediction)
        {
            using var context = factory.CreateDbContext();

            // 1. Validar que el partido exista y NO haya comenzado todavía
            var match = await context.Matches.FindAsync(prediction.MatchId);
            if (match == null) throw new Exception("El partido no existe.");

            if (match.IsFinished || match.Date <= DateTime.UtcNow)
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
                existing.WinnerTeamId = prediction.WinnerTeamId;
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
                    WinnerTeamId = prediction.WinnerTeamId,
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

            // Modificado para que sume como "Resultado Exacto" tanto si ganó 3 puntos en grupos como 5 en eliminatorias
            return await context.AppUsers
                .Where(u => u.CompanyId == companyId && u.IsActive)
                .Select(u => new UserRanking
                {
                    UserId = u.Id,
                    UserName = u.Name,
                    TotalPoints = u.Predictions.Sum(p => p.PointsEarned),
                    ExactResults = u.Predictions.Count(p => p.PointsEarned == 3 || p.PointsEarned == 5)
                })
                .OrderByDescending(r => r.TotalPoints)
                .ThenByDescending(r => r.ExactResults)
                .ToListAsync();
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
       

        public async Task UpdateMatchResultAsync(int matchId, int homeScore, int awayScore, int? winnerTeamId)
        {
            using var context = factory.CreateDbContext();

            // 1. Buscar el partido y actualizar su resultado oficial
            var match = await context.Matches.FindAsync(matchId);
            if (match == null) throw new Exception("Partido no encontrado");

            match.HomeScore = homeScore;
            match.AwayScore = awayScore;
            match.IsFinished = true;

            // ⚽ NUEVO: Guardamos el ID del equipo que clasificó en la realidad
            match.WinnerTeamId = winnerTeamId;

            // --- DETECCIÓN DE FASE ---            
            // Unificamos el criterio: si no contiene "Jornada", es eliminatoria (Octavos, Cuartos, etc.)
            bool esFaseEliminatoria = !match.Phase.Contains("Jornada", StringComparison.OrdinalIgnoreCase);

            // 2. Buscar todas las predicciones de los usuarios para ESTE partido
            var predictions = await context.Predictions
                .Where(p => p.MatchId == matchId)
                .ToListAsync();

            // 3. Calcular los puntos para cada uno según la fase actual del torneo
            foreach (var pred in predictions)
            {
                // Si por alguna razón la predicción no tiene goles (no se completó), le ponemos 0 pts
                if (!pred.PredictedHomeScore.HasValue || !pred.PredictedAwayScore.HasValue)
                {
                    pred.PointsEarned = 0;
                    continue;
                }

                int pHome = pred.PredictedHomeScore.Value;
                int pAway = pred.PredictedAwayScore.Value;

                // --- LÓGICA DE PUNTOS PARA FASE DE GRUPOS ---
                if (!esFaseEliminatoria)
                {
                    if (pHome == homeScore && pAway == awayScore)
                    {
                        pred.PointsEarned = 3; // Resultado exacto en grupos
                    }
                    else if (Math.Sign(homeScore - awayScore) == Math.Sign(pHome - pAway))
                    {
                        pred.PointsEarned = 1; // Acertó ganador/empate en grupos
                    }
                    else
                    {
                        pred.PointsEarned = 0;
                    }
                    continue; // Pasa a la siguiente predicción
                }

                // --- LÓGICA DE PUNTOS PARA ELIMINATORIAS (Tu papel borrador) ---
                // 1. Escenario Básico: Uno gana en los 90 min (Sin empate real)
                if (homeScore != awayScore)
                {
                    if (pHome == homeScore && pAway == awayScore)
                    {
                        pred.PointsEarned = 5; // Resultado exacto
                    }
                    else if (Math.Sign(homeScore - awayScore) == Math.Sign(pHome - pAway))
                    {
                        pred.PointsEarned = 2; // Acertó ganador pero no goles
                    }               

                    else
                    {
                        // ⚽ NUEVO: Erró el partido de los 90 min, pero ¿le pegó a quién clasificaba?
                        bool acertoClasificadoExtra = (pred.WinnerTeamId == winnerTeamId && winnerTeamId.HasValue);
                        pred.PointsEarned = acertoClasificadoExtra ? 1 : 0;
                    }
                }
                // 2. Escenarios de Empate Real en los 90 mins (Definición por penales)
                else
                {
                    bool acertoQueEmpataban = (pHome == pAway);
                    bool acertoClasificado = (pred.WinnerTeamId == winnerTeamId && winnerTeamId.HasValue);

                    if (acertoQueEmpataban)
                    {
                        bool mismosGoles = (pHome == homeScore);

                        if (mismosGoles && acertoClasificado)
                        {
                            pred.PointsEarned = 5; // Escenario Izquierda: Goles exactos + Clasificado (Ej: 1-1 vs 1-1, pasa A)
                        }
                        else if (!mismosGoles && acertoClasificado)
                        {
                            pred.PointsEarned = 3; // Caso A: Empate y Clasificado, con otros goles (Ej: 1-1 vs 2-2, pasa A)
                        }
                        else if (mismosGoles && !acertoClasificado)
                        {
                            pred.PointsEarned = 1; // Caso B: Goles exactos pero erró Clasificado (Ej: 1-1 vs 1-1, pasa B)
                        }
                        else if (!mismosGoles && !acertoClasificado)
                        {
                            pred.PointsEarned = 1; // Caso C: Empate pero erró goles y erró Clasificado (Ej: 1-1 vs 2-2, pasa B)
                        }
                    }
                    else
                    {
                        // 🔥 CORREGIDO: El partido real fue empate, el usuario puso que ganaba uno en los 90 min...
                        // Pero si le pegó al que avanzó en la tanda de penales, ¡le damos 1 punto!
                        pred.PointsEarned = acertoClasificado ? 1 : 0;
                    }
                }
            }

            // 4. Guardar todo en un solo bloque transaccional (actualiza el Match y todas las Predictions)
            await context.SaveChangesAsync();

            // 5. ¡Avisar a todos los componentes que los datos cambiaron!
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
                // 1. Inventamos el resultado del partido              

                if (!m.IsFinished)
                {
                    m.HomeScore = random.Next(0, 5);
                    m.AwayScore = random.Next(0, 5);
                    m.IsFinished = true;
                }

                // 2. REGLA NUEVA: Detectamos si este partido es de Octavos o superior
                // Si NO contiene la palabra "Grupos", es una fase eliminatoria (Octavos, Cuartos...)
                bool esFaseEliminatoria = !m.Phase.Contains("Jornada");
                int puntosExacto = esFaseEliminatoria ? 5 : 3;    // 5 en octavos, 3 en grupos
                int puntosResultado = esFaseEliminatoria ? 2 : 1; // 2 en octavos, 1 en grupos

                // 3. Buscamos las predicciones que hicieron los usuarios para ESTE partido concreto
                var predictions = await context.Predictions
                    .Where(p => p.MatchId == m.Id)
                    .ToListAsync();

                // 4. Les asignamos sus puntos en tiempo real antes de guardar
                foreach (var pred in predictions)
                {
                    if (pred.PredictedHomeScore == m.HomeScore && pred.PredictedAwayScore == m.AwayScore)
                    {
                        // Marcador exacto (Pleno)
                        pred.PointsEarned = puntosExacto;
                    }
                    else if ((pred.PredictedHomeScore > pred.PredictedAwayScore && m.HomeScore > m.AwayScore) ||
                             (pred.PredictedHomeScore < pred.PredictedAwayScore && m.HomeScore < m.AwayScore) ||
                             (pred.PredictedHomeScore == pred.PredictedAwayScore && m.HomeScore == m.AwayScore))
                    {
                        // Solo acertó quién ganaba o si empataban
                        pred.PointsEarned = puntosResultado;
                    }
                    else
                    {
                        // No acertó nada
                        pred.PointsEarned = 0;
                    }
                }
            }

            // 5. Guardamos en la base de datos tanto los partidos como las predicciones puntuadas
            await context.SaveChangesAsync();
            OnResultUpdated?.Invoke();
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

        public async Task<int> ActivateAllPendingUsersAsync(int companyId)
        {
            // Buscamos solo los usuarios de ese Tenant (Bar) que estén inactivos y no sean administradores
            using var context = factory.CreateDbContext();
            var pendingUsers = await context.AppUsers
                .Where(u => u.CompanyId == companyId && !u.IsActive && !u.IsAdmin)
                .ToListAsync();

            if (!pendingUsers.Any()) return 0;

            // Los activamos a todos en memoria
            foreach (var user in pendingUsers)
            {
                user.IsActive = true;
            }

            // Guardamos los cambios masivos en SQL Server
            return await context.SaveChangesAsync();
        }

        public async Task<AppUser?> GetUserByIdAsync(int userId)
        {
            using var context = factory.CreateDbContext();
            // Buscamos el usuario en la base de datos por su ID único
            return await context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId);
        }


    }
}
