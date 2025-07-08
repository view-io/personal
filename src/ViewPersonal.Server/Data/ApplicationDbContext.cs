using Microsoft.EntityFrameworkCore;
using ViewPersonal.Server.Models;

namespace ViewPersonal.Server.Data
{
    /// <summary>
    /// Database context for the application
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class
        /// </summary>
        /// <param name="options">The options to be used by the context</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the versions in the database
        /// </summary>
        public DbSet<AppVersion> Versions { get; set; }

        /// <summary>
        /// Gets or sets the OS-specific version details in the database
        /// </summary>
        public DbSet<VersionOsDetails> VersionOsDetails { get; set; }

        /// <summary>
        /// Configures the model that was discovered by convention from the entity types
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the AppVersion entity
            modelBuilder.Entity<AppVersion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.VersionNumber).IsRequired().HasMaxLength(20);
            });

            // Configure the VersionOsDetails entity
            modelBuilder.Entity<VersionOsDetails>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OperatingSystem).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DownloadUrl).HasMaxLength(1000);
                
                // Configure the relationship with AppVersion
                entity.HasOne(d => d.AppVersion)
                      .WithMany(v => v.OsDetails)
                      .HasForeignKey(d => d.AppVersionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}