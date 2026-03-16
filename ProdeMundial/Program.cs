using Microsoft.EntityFrameworkCore; // <--- AGREGADO
using ProdeMundial.Infrastructure;     // <--- AGREGADO (Asegúrate de que este sea el namespace de tu ApplicationDbContext)
using ProdeMundial.Web;
using ProdeMundial.Web.Components;
using ProdeMundial.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Obtener la cadena de conexión del appsettings.json
var connectionString = builder.Configuration.GetConnectionString("defaultConnection");

// 2. Configurar el DbContext para que use SQL Server y guarde las migraciones en Infrastructure
// Se usa un factory para poder usar el singleton y ver los cambios en "tiempo real"
//Al usar la fábrica, cada vez que alguien guarda un resultado, se abre una conexión minúscula, se guarda y se cierra.
//Esto evita que la conexión se quede "trabada" y permite que el evento de tiempo real funcione para todos los usuarios a la vez.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("ProdeMundial.Infrastructure")));




// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

//Servicio de Matchs
builder.Services.AddSingleton<IMatchService, MatchService>();

//Servicio de Sesion de Usuario 
//builder.Services.AddSingleton<UserSession>();
builder.Services.AddScoped<UserSession>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var context = services.GetRequiredService<ApplicationDbContext>();

//    // Esto aplica las migraciones pendientes automáticamente y carga los datos
//    context.Database.Migrate();
//    ProdeMundial.Infrastructure.Data.DbInitializer.Seed(context);
//}

// BLOQUE DE INICIALIZACIÓN SEGURO
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // OPCIÓN A: Si usas Migraciones (recomendado)
        context.Database.Migrate();

        // OPCIÓN B: Si no tienes archivos de migración aún, usa esto en su lugar:
        // context.Database.EnsureCreated();

        ProdeMundial.Infrastructure.Data.DbInitializer.Seed(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al migrar o sembrar la base de datos.");
        // Opcional: podrías decidir si la app debe seguir corriendo o no
    }
}

app.Run();