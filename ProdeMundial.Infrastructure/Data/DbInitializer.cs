using ProdeMundial.Domain.Entities;
using System.Text.Json;

namespace ProdeMundial.Infrastructure.Data;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
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
            context.SaveChanges(); // ID = 1

            // 2. CREAR PRIMER BAR CLIENTE (Tu piloto real)
            var defaultCompany = new Company
            {
                Name = "Carpe Diem",
                InvitationCode = "Carpe",
                PrizeDescription = "2 cañas gratis",
                MaxUsers = 50,
                LogoUrl = "/img/default-logo.png"
            };
            context.Companies.Add(defaultCompany);
            context.SaveChanges(); // ID = 2

            // 🔥 3 y 4. LECTURA AUTOMÁTICA DEL FIXTURE DESDE EL ARCHIVO JSON
            try
            {
                //string rutaActual = Directory.GetCurrentDirectory();

                //string rutaJson = rutaActual.EndsWith("ProdeMundial")
                //    ? Path.Combine(rutaActual, "..", "ProdeMundial.Infrastructure", "Data", "fixture-mundial.json")
                //    : Path.Combine(rutaActual, "ProdeMundial.Infrastructure", "Data", "fixture-mundial.json");

                //rutaJson = Path.GetFullPath(rutaJson);

                string rutaActual = Directory.GetCurrentDirectory();
                string rutaJson = "";

                // 1. INTENTO EN PRODUCCIÓN (Como lo tienes subido en Somee: carpeta Data en la raíz del sitio)
                string rutaProduccion = Path.Combine(rutaActual, "Data", "fixture-mundial.json");

                // 2. INTENTO EN DESARROLLO LOCAL (Estructura de proyectos de Visual Studio)
                string rutaDesarrollo = rutaActual.EndsWith("ProdeMundial")
                    ? Path.Combine(rutaActual, "..", "ProdeMundial.Infrastructure", "Data", "fixture-mundial.json")
                    : Path.Combine(rutaActual, "ProdeMundial.Infrastructure", "Data", "fixture-mundial.json");

                // Decidimos cuál usar según cuál exista físicamente
                if (File.Exists(rutaProduccion))
                {
                    rutaJson = rutaProduccion;
                }
                else
                {
                    rutaJson = Path.GetFullPath(rutaDesarrollo);
                }

                Console.WriteLine($"🔍 Ruta final seleccionada para el JSON: {rutaJson}");

                if (File.Exists(rutaJson))
                {
                    string jsonTexto = File.ReadAllText(rutaJson);
                    var partidosJson = JsonSerializer.Deserialize<List<MatchJsonDto>>(jsonTexto);

                    if (partidosJson != null)
                    {
                        Console.WriteLine($"📊 Partidos detectados en el JSON: {partidosJson.Count}");

                        // 1. Saneamos datos rápidos del JSON directamente en memoria antes de procesar
                        foreach (var p in partidosJson)
                        {
                            if (p.HomeTeam == "Mxico") p.HomeTeam = "México";
                            if (p.AwayTeam == "Mxico") p.AwayTeam = "México";

                            // Forzamos que el grupo sea siempre de 1 solo carácter para cumplir con [MaxLength(1)]
                            if (!string.IsNullOrEmpty(p.Group) && p.Group.Length > 1)
                            {
                                p.Group = p.Group.Substring(0, 1);
                            }
                        }

                        // 2. Cargamos los equipos actuales de la BD una sola vez para no duplicar
                        var equiposEnBd = context.Teams.Select(t => t.Name).ToHashSet();

                        // 3. Fase de creación de equipos únicos
                        foreach (var p in partidosJson)
                        {
                            // Solo agregamos equipos reales que tengan un nombre definido
                            if (!string.IsNullOrEmpty(p.HomeTeam) && !equiposEnBd.Contains(p.HomeTeam))
                            {
                                context.Teams.Add(new Team
                                {
                                    Name = p.HomeTeam,
                                    Group = string.IsNullOrEmpty(p.Group) ? "A" : p.Group,
                                    //FlagUrl = $"https://flagcdn.com/{p.HomeTeamCode.ToLower().Replace("-", "")}.svg"
                                    FlagUrl = $"https://flagcdn.com/{p.HomeTeamCode.ToLower()}.svg"
                                });
                                equiposEnBd.Add(p.HomeTeam);
                            }

                            if (!string.IsNullOrEmpty(p.AwayTeam) && !equiposEnBd.Contains(p.AwayTeam))
                            {
                                context.Teams.Add(new Team
                                {
                                    Name = p.AwayTeam,
                                    Group = string.IsNullOrEmpty(p.Group) ? "A" : p.Group,
                                    // FlagUrl = $"https://flagcdn.com/{p.AwayTeamCode.ToLower().Replace("-", "")}.svg"
                                    FlagUrl = $"https://flagcdn.com/{p.AwayTeamCode.ToLower()}.svg"

                                });
                                equiposEnBd.Add(p.AwayTeam);
                            }
                        }

                        Console.WriteLine("Guardando nuevos equipos en la base de datos...");
                        context.SaveChanges();

                        // 4. Creamos el diccionario de IDs rápido
                        var teamsDict = context.Teams.ToDictionary(t => t.Name, t => t.Id);
                        int partidosInsertados = 0;

                        // 5. Fase de inserción de los partidos
                        foreach (var p in partidosJson)
                        {
                            // Control de seguridad por si las fases superan los 50 caracteres del MaxLength de Match
                            string faseSaneada = p.Phase.Length > 50 ? p.Phase.Substring(0, 50) : p.Phase;

                            // Buscamos los IDs de forma segura por si vienen nulos o vacíos en fases eliminatorias
                            int? homeId = null;
                            int? awayId = null;

                            if (!string.IsNullOrEmpty(p.HomeTeam) && teamsDict.TryGetValue(p.HomeTeam, out int hId))
                                homeId = hId;

                            if (!string.IsNullOrEmpty(p.AwayTeam) && teamsDict.TryGetValue(p.AwayTeam, out int aId))
                                awayId = aId;

                            context.Matches.Add(new Match
                            {
                                HomeTeamId = homeId,
                                AwayTeamId = awayId,
                                // Si no hay equipo asignado, guardamos un texto provisional genérico en el Placeholder
                                HomeTeamPlaceholder = homeId == null ? "Por clasificar" : null,
                                AwayTeamPlaceholder = awayId == null ? "Por clasificar" : null,
                                Phase = faseSaneada,
                                Date = DateTime.SpecifyKind(p.DateUtc, DateTimeKind.Utc),
                                IsFinished = false
                            });
                            partidosInsertados++;
                        }

                        context.SaveChanges();
                        Console.WriteLine($"✅ ¡ÉXITO GLOBAL! Se guardaron {partidosInsertados} partidos reales en la BD.");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Archivo no encontrado en la ruta de infraestructura: {rutaJson}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error grave al cargar la semilla del fixture: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Detalle interno: {ex.InnerException.Message}");
                }
            }

            // 5. Usuario Único SuperAdmin (Tú) atado a la Empresa Maestra (Id = 1)
            var adminGlobal = new AppUser
            {
                Name = "Sebastian",
                PhoneNumber = "677058946",
                Email = "sebastian_acosta85@hotmail.com",
                AccessPin = "1985",
                IsAdmin = true,
                IsActive = true,
                CompanyId = globalCompany.Id
            };

            context.AppUsers.Add(adminGlobal);
            context.SaveChanges();

            context.UserCompanies.Add(new UserCompany
            {
                UserId = adminGlobal.Id,
                CompanyId = globalCompany.Id,
                IsGroupAdmin = true
            });

            // 6. CREAR EL ADMINISTRADOR DEL BAR CARPE DIEM (Atado a la CompanyId = 2)
            var adminBar = new AppUser
            {
                Name = "Borja",
                PhoneNumber = "627665029",
                Email = "carpediem@prode.com",
                AccessPin = "4321",
                IsAdmin = true,
                IsActive = true,
                CompanyId = defaultCompany.Id
            };
            context.AppUsers.Add(adminBar);
            context.SaveChanges();

            context.UserCompanies.Add(new UserCompany
            {
                UserId = adminBar.Id,
                CompanyId = defaultCompany.Id,
                IsGroupAdmin = true
            });

            // 🔥 7. NUEVO: TU USUARIO DE PRUEBA COMO CLIENTE COMÚN (Atado a Carpe Diem)
            var clienteTest = new AppUser
            {
                Name = "Sebastian Test",
                PhoneNumber = "654654654",
                Email = "test_cliente@prode.com",
                AccessPin = "1234",
                IsAdmin = false,
                IsActive = true,
                CompanyId = defaultCompany.Id
            };
            context.AppUsers.Add(clienteTest);
            context.SaveChanges();

            context.UserCompanies.Add(new UserCompany
            {
                UserId = clienteTest.Id,
                CompanyId = defaultCompany.Id,
                IsGroupAdmin = false
            });

            context.SaveChanges();
        }
    }

    public class MatchJsonDto
    {
        public string HomeTeam { get; set; } = string.Empty;
        public string HomeTeamCode { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;
        public string AwayTeamCode { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public DateTime DateUtc { get; set; }
    }
}