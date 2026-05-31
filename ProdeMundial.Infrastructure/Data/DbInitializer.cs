using ProdeMundial.Domain.Entities;

namespace ProdeMundial.Infrastructure.Data;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        if (!context.Matches.Any())
            if (!context.Matches.Any())
            {
                // 1. CREAR EMPRESA MAESTRA (Para ti como SuperAdmin)
                var globalCompany = new Company
                {
                    Name = "Sistema Global Prode",
                    InvitationCode = "SUPERADMIN",
                    PrizeDescription = "Administración General del SaaS",
                    MaxUsers = 9999,
                    LogoUrl = "/img/default-logo.png"
                };
                context.Companies.Add(globalCompany);
                context.SaveChanges(); // Guardamos para obtener el ID = 1

                // 2. CREAR PRIMER BAR CLIENTE (Para hacer pruebas multi-tenant)
                var defaultCompany = new Company
                {
                    Name = "Bar de Prueba",
                    InvitationCode = "PRUEBA26",
                    PrizeDescription = "Una cena gratis al ganador",
                    MaxUsers = 50,
                    LogoUrl = "/img/default-logo.png"
                };
                context.Companies.Add(defaultCompany);
                context.SaveChanges(); // Guardamos para obtener el ID = 2

                // 3. Equipos
                var mexico = new Team { Name = "México", Group = "A", FlagUrl = "https://flagcdn.com/mx.svg" };
                var usa = new Team { Name = "Estados Unidos", Group = "A", FlagUrl = "https://flagcdn.com/us.svg" };
                var canada = new Team { Name = "Canadá", Group = "B", FlagUrl = "https://flagcdn.com/ca.svg" };
                var argentina = new Team { Name = "Argentina", Group = "B", FlagUrl = "https://flagcdn.com/ar.svg" };
                var espana = new Team { Name = "España", Group = "C", FlagUrl = "https://flagcdn.com/es.svg" };
                var francia = new Team { Name = "Francia", Group = "C", FlagUrl = "https://flagcdn.com/fr.svg" };
                var brasil = new Team { Name = "Brasil", Group = "D", FlagUrl = "https://flagcdn.com/br.svg" };
                var inglaterra = new Team { Name = "Inglaterra", Group = "D", FlagUrl = "https://flagcdn.com/gb.svg" };

                // 4. Partidos
                context.Matches.AddRange(
                    // JORNADA 1
                    new Match { HomeTeam = mexico, AwayTeam = usa, Date = new DateTime(2026, 06, 11, 20, 0, 0), Phase = "Jornada 1 - Estadio Azteca" },
                    new Match { HomeTeam = canada, AwayTeam = brasil, Date = new DateTime(2026, 06, 12, 17, 0, 0), Phase = "Jornada 1 - Toronto" },
                    new Match { HomeTeam = argentina, AwayTeam = francia, Date = new DateTime(2026, 06, 12, 21, 0, 0), Phase = "Jornada 1 - Los Ángeles" },

                    // JORNADA 2
                    new Match { HomeTeam = espana, AwayTeam = inglaterra, Date = new DateTime(2026, 06, 15, 15, 0, 0), Phase = "Jornada 2 - Miami" },
                    new Match { HomeTeam = mexico, AwayTeam = argentina, Date = new DateTime(2026, 06, 16, 18, 0, 0), Phase = "Jornada 2 - Guadalajara" },
                    new Match { HomeTeam = francia, AwayTeam = brasil, Date = new DateTime(2026, 06, 16, 21, 0, 0), Phase = "Jornada 2 - New York" },

                    // JORNADA 3
                    new Match { HomeTeam = usa, AwayTeam = inglaterra, Date = new DateTime(2026, 06, 20, 19, 0, 0), Phase = "Jornada 3 - Seattle" },
                    new Match { HomeTeam = espana, AwayTeam = canada, Date = new DateTime(2026, 06, 21, 14, 0, 0), Phase = "Jornada 3 - Vancouver" },
                    new Match { HomeTeam = brasil, AwayTeam = argentina, Date = new DateTime(2026, 06, 21, 20, 0, 0), Phase = "Jornada 3 - Dallas" }
                );

                // 5. Usuario Único SuperAdmin (Tú) atado a la Empresa Maestra (Id = 1)
                var admin = new AppUser
                {
                    Name = "Sebastian",
                    PhoneNumber = "677058946",
                    Email = "sebastian_acosta85@hotmail.com",
                    AccessPin = "1985",
                    IsAdmin = true,
                    IsActive = true,
                    CompanyId = globalCompany.Id // 🔥 Vinculado a la administración global
                };
                context.AppUsers.Add(admin);
                context.SaveChanges();

                // 6. Vincularte en la tabla intermedia también con la empresa global
                context.UserCompanies.Add(new UserCompany
                {
                    UserId = admin.Id,
                    CompanyId = globalCompany.Id,
                    IsGroupAdmin = true
                });

                context.SaveChanges();
            }

    }
}