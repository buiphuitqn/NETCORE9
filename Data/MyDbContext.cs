using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using CORE_BE.Data;
using CORE_BE.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CORE_BE.Data
{
    public class MyDbContext
        : IdentityDbContext<
            ApplicationUser,
            ApplicationRole,
            Guid,
            IdentityUserClaim<Guid>,
            ApplicationUserRole,
            IdentityUserLogin<Guid>,
            IdentityRoleClaim<Guid>,
            IdentityUserToken<Guid>
        >
    {
        public MyDbContext(DbContextOptions<MyDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<IdentityUserLogin<Guid>>()
                .HasKey(x => new { x.LoginProvider, x.ProviderKey });

            modelBuilder
                .Entity<IdentityUserToken<Guid>>()
                .HasKey(x => new
                {
                    x.UserId,
                    x.LoginProvider,
                    x.Name,
                });

            modelBuilder.Entity<ApplicationUserRole>().HasKey(x => new { x.User_Id, x.Role_Id });
            modelBuilder.Entity<Menu_Role>().HasKey(x => new { x.Menu_Id, x.Role_Id });

            modelBuilder
                .Entity<Menu_Role>()
                .HasOne(x => x.Menu)
                .WithMany(x => x.Menu_Roles)
                .HasForeignKey(x => x.Menu_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<Menu_Role>()
                .HasOne(x => x.Role)
                .WithMany(x => x.Menu_Roles)
                .HasForeignKey(x => x.Role_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhanQuyen_DonVi>().HasKey(x => new { x.User_Id, x.DonVi_Id });

            modelBuilder
                .Entity<PhanQuyen_DonVi>()
                .HasOne(x => x.User)
                .WithMany(x => x.PhanQuyen_DonVis)
                .HasForeignKey(x => x.User_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<PhanQuyen_DonVi>()
                .HasOne(x => x.DonVi)
                .WithMany(x => x.PhanQuyen_DonVis)
                .HasForeignKey(x => x.DonVi_Id)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder
                .Entity<IdracLog>()
                .HasOne(x => x.Server)
                .WithMany()
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder
                .Entity<InfoServer>()
                .HasOne(x => x.Server)
                .WithMany()
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<StatusModuleHistory>()
                .HasOne(x => x.Server)
                .WithMany()
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<StatusModule>()
                .HasOne(x => x.Server)
                .WithMany()
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // Define DbSets for your entities here
        // public DbSet<YourEntity> YourEntities { get; set; }
        public DbSet<PhanQuyen_DonVi> PhanQuyen_DonVis { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Menu_Role> Menu_Roles { get; set; }
        public DbSet<Server> Server { get; set; }
        public DbSet<IdracLog> IdracLog { get; set; }
        public DbSet<InfoServer> InfoServer { get; set; }
        public DbSet<StatusModule> StatusModule { get; set; }
        public DbSet<StatusModuleHistory> statusModuleHistory { get; set; }
    }
}
