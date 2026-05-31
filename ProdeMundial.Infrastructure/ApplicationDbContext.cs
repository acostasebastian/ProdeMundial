using Microsoft.EntityFrameworkCore;
using ProdeMundial.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ProdeMundial.Infrastructure
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    //base.OnModelCreating(modelBuilder);

        //    //// Configuración para el Equipo Local
        //    //modelBuilder.Entity<Match>()
        //    //    .HasOne(m => m.HomeTeam)
        //    //    .WithMany()
        //    //    .HasForeignKey(m => m.HomeTeamId)
        //    //    .OnDelete(DeleteBehavior.Restrict);

        //    //// Configuración para el Equipo Visitante
        //    //modelBuilder.Entity<Match>()
        //    //    .HasOne(m => m.AwayTeam)
        //    //    .WithMany()
        //    //    .HasForeignKey(m => m.AwayTeamId)
        //    //    .OnDelete(DeleteBehavior.Restrict);

        //    //// Configuramos la relación AppUser -> Company
        //    //modelBuilder.Entity<AppUser>()
        //    //    .HasOne<Company>() // Un usuario tiene una empresa
        //    //    .WithMany()        // Una empresa tiene muchos usuarios
        //    //    .HasForeignKey(u => u.CompanyId)
        //    //    .OnDelete(DeleteBehavior.Restrict); // <--- ESTO SOLUCIONA EL ERROR

        //    base.OnModelCreating(modelBuilder);

        //    // 1. CONFIGURACIÓN DE PARTIDOS (MATCH) - El origen probable del conflicto
        //    modelBuilder.Entity<Match>()
        //        .HasOne(m => m.HomeTeam)
        //        .WithMany()
        //        .HasForeignKey(m => m.HomeTeamId)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    modelBuilder.Entity<Match>()
        //        .HasOne(m => m.AwayTeam)
        //        .WithMany()
        //        .HasForeignKey(m => m.AwayTeamId)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    // 2. CONFIGURACIÓN DE USUARIO -> COMPANY
        //    modelBuilder.Entity<AppUser>()
        //        .HasOne<Company>()
        //        .WithMany()
        //        .HasForeignKey(u => u.CompanyId)
        //        .OnDelete(DeleteBehavior.Restrict); // Cambié NoAction por Restrict, es más sólido

        //    // 3. PREDICCIONES (Si tienes esta entidad, es VITAL configurarla)
        //    // Si tu clase se llama 'Prediction' o 'Apuesta', agrégalo:
        //    modelBuilder.Entity<Prediction>()
        //        .HasOne<AppUser>()
        //        .WithMany()
        //        .HasForeignKey(p => p.UserId)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    // Si la predicción también tiene CompanyId, desactiva su cascada aquí:
        //    modelBuilder.Entity<Prediction>()
        //        .HasOne<Company>()
        //        .WithMany()
        //        .HasForeignKey(p => p.CompanyId)
        //        .OnDelete(DeleteBehavior.NoAction);


        //}

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);

        //    // 1. CONFIGURACIÓN DE PARTIDOS (MATCH) - Impecable
        //    modelBuilder.Entity<Match>()
        //        .HasOne(m => m.HomeTeam)
        //        .WithMany()
        //        .HasForeignKey(m => m.HomeTeamId)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    modelBuilder.Entity<Match>()
        //        .HasOne(m => m.AwayTeam)
        //        .WithMany()
        //        .HasForeignKey(m => m.AwayTeamId)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    // 2. CONFIGURACIÓN DE USUARIO -> COMPANY
        //    // Corregido: Usamos la propiedad CompanyId mapeada explícitamente si existe, o mantenemos el tipo.
        //    modelBuilder.Entity<AppUser>()
        //        .HasOne<Company>()
        //        .WithMany()
        //        .HasForeignKey(u => u.CompanyId)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    // 3. PREDICCIONES (PREDICTION) - ¡La clave del éxito!
        //    modelBuilder.Entity<Prediction>(entity =>
        //    {
        //        // Vinculamos la propiedad física 'User' con la colección en AppUser usando 'UserId'
        //        entity.HasOne(p => p.User)
        //              .WithMany(u => u.Predictions) // Asegúrate de que en AppUser esté: public ICollection<Prediction> Predictions { get; set; }
        //              .HasForeignKey(p => p.UserId)
        //              .OnDelete(DeleteBehavior.Cascade); // Si se borra un usuario, se borran sus predicciones

        //        // Vinculamos la propiedad física 'Company' usando 'CompanyId'
        //        entity.HasOne(p => p.Company)
        //              .WithMany()
        //              .HasForeignKey(p => p.CompanyId)
        //              .OnDelete(DeleteBehavior.NoAction); // Evitamos rutas de cascada múltiples

        //        // Agregamos por seguridad la relación con el Partido (Match) para que tampoco invente nada raro
        //        entity.HasOne(p => p.Match)
        //              .WithMany()
        //              .HasForeignKey(p => p.MatchId)
        //              .OnDelete(DeleteBehavior.Restrict);
        //    });
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. CONFIGURACIÓN DE PARTIDOS (MATCH) - Ya estaba perfecta
            modelBuilder.Entity<Match>()
                .HasOne(m => m.HomeTeam)
                .WithMany()
                .HasForeignKey(m => m.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.AwayTeam)
                .WithMany()
                .HasForeignKey(m => m.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. CONFIGURACIÓN DE USUARIO -> COMPANY (CORREGIDO)
            // Ahora apuntamos a las propiedades reales en vez de usar tipos anónimos <Company>
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.HasOne(u => u.Company)
                      .WithMany() // O .WithMany(c => c.Users) si tienes la lista en Company
                      .HasForeignKey(u => u.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 3. PREDICCIONES (PREDICTION) - (CORREGIDO)
            modelBuilder.Entity<Prediction>(entity =>
            {
                entity.HasOne(p => p.User)
                      .WithMany(u => u.Predictions)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Forzamos a que no haya relación inversa mapeada al azar
                entity.HasOne(p => p.Company)
                      .WithMany() // <--- Dejar vacío Obligatoriamente si borraste la lista en Company
                      .HasForeignKey(p => p.CompanyId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(p => p.Match)
                      .WithMany()
                      .HasForeignKey(p => p.MatchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public DbSet<Team> Teams { get; set; }

        public DbSet<Match> Matches { get; set; }

        public DbSet<Prediction> Predictions { get; set; }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<TournamentConfig> TournamentConfigs { get; set; }

        public DbSet<Company> Companies { get; set; }

        public DbSet<UserCompany> UserCompanies { get; set; }       


    }
}
